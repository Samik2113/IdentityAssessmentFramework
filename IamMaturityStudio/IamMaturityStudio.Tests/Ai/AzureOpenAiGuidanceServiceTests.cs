using System.Text.Json;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IamMaturityStudio.Tests.Ai;

public class AzureOpenAiGuidanceServiceTests
{
    [Fact]
    public async Task Parses_Valid_Json_Response()
    {
        var client = new FakeClient("{\"explanation\":\"x\",\"examples\":[\"a\"],\"evidenceSuggestions\":[\"b\"],\"checklist\":[\"c\"]}");
        var service = CreateService(client);

        var result = await service.GenerateAsync(CreateRequest(), Guid.NewGuid(), CancellationToken.None);

        result.Explanation.Should().Be("x");
        result.Examples.Should().ContainSingle().Which.Should().Be("a");
    }

    [Fact]
    public async Task Extracts_First_Json_Block_When_Prose_Wrapped()
    {
        var wrapped = "Result below:\n```json\n{\"explanation\":\"wrapped\",\"examples\":[\"a\"],\"evidenceSuggestions\":[\"b\"],\"checklist\":[\"c\"]}\n```";
        var client = new FakeClient(wrapped);
        var service = CreateService(client);

        var result = await service.GenerateAsync(CreateRequest(), Guid.NewGuid(), CancellationToken.None);

        result.Explanation.Should().Be("wrapped");
    }

    [Fact]
    public async Task Falls_Back_When_Ai_Fails()
    {
        var client = new FakeClient(throwError: true);
        var service = CreateService(client);

        var result = await service.GenerateAsync(CreateRequest(), Guid.NewGuid(), CancellationToken.None);

        result.Explanation.Should().Contain("Focus remediation");
        result.Checklist.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Uses_Cache_For_Identical_Request_And_Org()
    {
        var client = new FakeClient("{\"explanation\":\"cached\",\"examples\":[\"a\"],\"evidenceSuggestions\":[\"b\"],\"checklist\":[\"c\"]}");
        var service = CreateService(client);
        var orgId = Guid.NewGuid();

        await service.GenerateAsync(CreateRequest(), orgId, CancellationToken.None);
        await service.GenerateAsync(CreateRequest(), orgId, CancellationToken.None);

        client.CallCount.Should().Be(1);
    }

    private static AzureOpenAiGuidanceService CreateService(FakeClient client)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Enabled"] = "true",
            ["AzureOpenAI:Endpoint"] = "https://dev-openai.openai.azure.com",
            ["AzureOpenAI:Deployment"] = "gpt-4o-mini",
            ["AzureOpenAI:ApiVersion"] = "2024-02-15-preview",
            ["AzureOpenAI:ApiKey"] = "dev-key",
            ["AzureOpenAI:MaxOutputTokens"] = "600",
            ["AzureOpenAI:Temperature"] = "0.2"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new AzureOpenAiGuidanceService(
            client,
            new AiPromptRedactor(),
            new MemoryCache(new MemoryCacheOptions()),
            configuration,
            NullLogger<AzureOpenAiGuidanceService>.Instance,
            new DevNoopAiGuidanceService());
    }

    private static AiGuidanceRequest CreateRequest()
    {
        var levels = JsonDocument.Parse("{\"Manual\":\"desc\"}").RootElement;
        return new AiGuidanceRequest("Access Control", "Privileged Access", "Is PAM enforced?", "High", levels, "Organization: Contoso");
    }

    private sealed class FakeClient : IAzureOpenAiClient
    {
        private readonly string _response;
        private readonly bool _throwError;

        public FakeClient(string response = "", bool throwError = false)
        {
            _response = response;
            _throwError = throwError;
        }

        public int CallCount { get; private set; }

        public Task<AzureOpenAiResult> GetGuidanceJsonAsync(string endpoint, string deployment, string apiVersion, string? apiKey, string systemPrompt, string userPrompt, int maxOutputTokens, double temperature, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_throwError)
            {
                throw new HttpRequestException("simulated failure");
            }

            return Task.FromResult(new AzureOpenAiResult(_response, 123));
        }
    }
}
