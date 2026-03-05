using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace IamMaturityStudio.Infrastructure.Services;

public interface IAzureOpenAiClient
{
    Task<AzureOpenAiResult> GetGuidanceJsonAsync(
        string endpoint,
        string deployment,
        string apiVersion,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        double temperature,
        CancellationToken cancellationToken);
}

public sealed record AzureOpenAiResult(string RawContent, int? TotalTokens);

public sealed class AzureOpenAiClient : IAzureOpenAiClient
{
    private static readonly Uri ScopeUri = new("https://cognitiveservices.azure.com/.default");
    private readonly HttpClient _httpClient;
    private readonly TokenCredential _tokenCredential;
    private readonly ILogger<AzureOpenAiClient> _logger;

    public AzureOpenAiClient(HttpClient httpClient, ILogger<AzureOpenAiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tokenCredential = new DefaultAzureCredential();
    }

    public async Task<AzureOpenAiResult> GetGuidanceJsonAsync(
        string endpoint,
        string deployment,
        string apiVersion,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        double temperature,
        CancellationToken cancellationToken)
    {
        var uri = new Uri($"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("api-key", apiKey);
        }
        else
        {
            var token = await _tokenCredential.GetTokenAsync(new TokenRequestContext([ScopeUri.ToString()]), cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxOutputTokens,
            temperature,
            response_format = new { type = "json_object" }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure OpenAI call failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException($"Azure OpenAI returned {(int)response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var modelContent = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        int? totalTokens = null;
        if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var total))
        {
            totalTokens = total.GetInt32();
        }

        return new AzureOpenAiResult(modelContent, totalTokens);
    }
}