using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IamMaturityStudio.Infrastructure.Services;

public sealed class DevNoopAiGuidanceService : IAiGuidanceService
{
    public Task<AiGuidanceResponse> GenerateAsync(AiGuidanceRequest request, Guid orgId, CancellationToken cancellationToken)
    {
        return Task.FromResult(CreateFallback(request));
    }

    public static AiGuidanceResponse CreateFallback(AiGuidanceRequest request)
    {
        return new AiGuidanceResponse(
            $"Focus remediation for {request.Domain.Trim()} / {request.Category.Trim()} using measurable IAM controls.",
            new[]
            {
                "Define a single control owner and review cadence.",
                "Document current-state gap and target-state control behavior."
            },
            new[]
            {
                "Approved policy/standard document.",
                "Recent access review evidence and sign-off.",
                "System configuration screenshot with timestamp."
            },
            new[]
            {
                "Validate scope and impacted systems.",
                "Assign remediation tasks and due dates.",
                "Track completion and retain audit evidence."
            });
    }
}

public sealed class AzureOpenAiGuidanceService : IAiGuidanceService
{
    private const string CachePrefix = "ai-guidance";
    private readonly IAzureOpenAiClient _client;
    private readonly IAiPromptRedactor _redactor;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAiGuidanceService> _logger;
    private readonly DevNoopAiGuidanceService _fallback;

    public AzureOpenAiGuidanceService(
        IAzureOpenAiClient client,
        IAiPromptRedactor redactor,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AzureOpenAiGuidanceService> logger,
        DevNoopAiGuidanceService fallback)
    {
        _client = client;
        _redactor = redactor;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _fallback = fallback;
    }

    public async Task<AiGuidanceResponse> GenerateAsync(AiGuidanceRequest request, Guid orgId, CancellationToken cancellationToken)
    {
        var options = ReadOptions();
        var cacheKey = BuildCacheKey(orgId, request);
        var requestHash = cacheKey.Split(':').Last();

        if (_cache.TryGetValue(cacheKey, out AiGuidanceResponse? cached) && cached is not null)
        {
            _logger.LogInformation("AI guidance cache hit requestHash={RequestHash}", requestHash);
            return cached;
        }

        _logger.LogInformation("AI guidance cache miss requestHash={RequestHash}", requestHash);

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.Deployment))
        {
            var disabledFallback = DevNoopAiGuidanceService.CreateFallback(request);
            _cache.Set(cacheKey, disabledFallback, TimeSpan.FromHours(12));
            return disabledFallback;
        }

        var systemPrompt = "You are an IAM advisor. Return ONLY minified JSON with keys: explanation, examples, evidenceSuggestions, checklist. No markdown or prose.";
        var userPrompt = BuildRedactedPrompt(request);

        var sw = Stopwatch.StartNew();
        int? totalTokens = null;
        try
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

                    var result = await _client.GetGuidanceJsonAsync(
                        options.Endpoint!,
                        options.Deployment!,
                        options.ApiVersion,
                        options.ApiKey,
                        systemPrompt,
                        userPrompt,
                        options.MaxOutputTokens,
                        options.Temperature,
                        timeoutCts.Token);

                    totalTokens = result.TotalTokens;
                    var parsed = ParseResponseOrFallback(result.RawContent, request);
                    _cache.Set(cacheKey, parsed, TimeSpan.FromHours(12));
                    sw.Stop();
                    _logger.LogInformation("AI guidance completed requestHash={RequestHash} elapsedMs={ElapsedMs} tokenUsage={TokenUsage}", requestHash, sw.ElapsedMilliseconds, totalTokens);
                    return parsed;
                }
                catch (HttpRequestException) when (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                }
                catch (TaskCanceledException) when (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                }
            }
        }
        catch
        {
        }

        sw.Stop();
        _logger.LogWarning("AI guidance fallback due to provider failure requestHash={RequestHash} elapsedMs={ElapsedMs}", requestHash, sw.ElapsedMilliseconds);
        var fallback = await _fallback.GenerateAsync(request, orgId, cancellationToken);
        _cache.Set(cacheKey, fallback, TimeSpan.FromHours(12));
        return fallback;
    }

    private AzureOpenAiDevOptions ReadOptions()
    {
        var section = _configuration.GetSection("AzureOpenAI");
        return new AzureOpenAiDevOptions
        {
            Enabled = section.GetValue<bool>("Enabled"),
            Endpoint = section["Endpoint"],
            Deployment = section["Deployment"],
            ApiVersion = section["ApiVersion"] ?? "2024-02-15-preview",
            ApiKey = section["ApiKey"],
            MaxOutputTokens = section.GetValue<int?>("MaxOutputTokens") ?? 600,
            Temperature = section.GetValue<double?>("Temperature") ?? 0.2
        };
    }

    private string BuildRedactedPrompt(AiGuidanceRequest request)
    {
        var levelDefinitionsText = request.LevelDefinitions is JsonElement jsonElement
            ? jsonElement.GetRawText()
            : JsonSerializer.Serialize(request.LevelDefinitions);

        return $"Domain: {_redactor.Redact(request.Domain)}\n" +
               $"Category: {_redactor.Redact(request.Category)}\n" +
               $"Question: {_redactor.Redact(request.QuestionText)}\n" +
               $"BusinessRisk: {_redactor.Redact(request.BusinessRisk)}\n" +
               $"LevelDefinitions: {_redactor.Redact(levelDefinitionsText)}\n" +
               $"UserComment: {_redactor.Redact(request.UserComment ?? string.Empty)}";
    }

    private static AiGuidanceResponse ParseResponseOrFallback(string rawResponse, AiGuidanceRequest request)
    {
        if (!TryExtractFirstJsonObject(rawResponse, out var jsonBlock))
        {
            return DevNoopAiGuidanceService.CreateFallback(request);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AiGuidanceResponse>(jsonBlock, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Explanation) || parsed.Examples is null || parsed.EvidenceSuggestions is null || parsed.Checklist is null)
            {
                return DevNoopAiGuidanceService.CreateFallback(request);
            }

            return parsed;
        }
        catch
        {
            return DevNoopAiGuidanceService.CreateFallback(request);
        }
    }

    internal static bool TryExtractFirstJsonObject(string input, out string jsonBlock)
    {
        jsonBlock = string.Empty;
        var start = input.IndexOf('{');
        if (start < 0)
        {
            return false;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < input.Length; i++)
        {
            var c = input[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    jsonBlock = input[start..(i + 1)];
                    return true;
                }
            }
        }

        return false;
    }

    private static string BuildCacheKey(Guid orgId, AiGuidanceRequest request)
    {
        var levelDefinitionsText = request.LevelDefinitions is JsonElement jsonElement
            ? jsonElement.GetRawText()
            : JsonSerializer.Serialize(request.LevelDefinitions);

        var normalized = string.Join('|',
            Normalize(request.Domain),
            Normalize(request.Category),
            Normalize(request.QuestionText),
            Normalize(levelDefinitionsText));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{CachePrefix}:{orgId:N}:{hash}";
    }

    private static string Normalize(string input)
    {
        return string.Join(' ', input.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class AzureOpenAiDevOptions
{
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public string? Deployment { get; set; }
    public string ApiVersion { get; set; } = "2024-02-15-preview";
    public string? ApiKey { get; set; }
    public int MaxOutputTokens { get; set; } = 600;
    public double Temperature { get; set; } = 0.2;
}
