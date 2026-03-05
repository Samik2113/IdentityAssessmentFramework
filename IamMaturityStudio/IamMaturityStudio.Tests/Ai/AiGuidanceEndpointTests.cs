using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using FluentAssertions;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IamMaturityStudio.Tests.Ai;

public class AiGuidanceEndpointTests
{
    [Fact]
    public async Task Returns_200_For_Valid_Request()
    {
        await using var factory = new AiApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/ai/guidance", JsonContent(new
        {
            domain = "Access Control",
            category = "Privileged Access",
            questionText = "Is PAM enforced?",
            businessRisk = "High",
            levelDefinitions = new { Manual = "manual" },
            userComment = "none"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("explanation");
        body.Should().Contain("examples");
        body.Should().Contain("evidenceSuggestions");
        body.Should().Contain("checklist");
    }

    [Fact]
    public async Task Returns_400_When_Required_Fields_Missing()
    {
        await using var factory = new AiApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/ai/guidance", JsonContent(new
        {
            domain = "",
            category = "Privileged Access",
            questionText = "",
            businessRisk = "High",
            levelDefinitions = new { Manual = "manual" }
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_429_When_Rate_Limit_Exceeded()
    {
        await using var factory = new AiApiFactory();
        using var client = factory.CreateClient();

        HttpStatusCode lastStatus = HttpStatusCode.OK;
        for (var i = 0; i < 11; i++)
        {
            var response = await client.PostAsync("/ai/guidance", JsonContent(new
            {
                domain = "Access Control",
                category = "Privileged Access",
                questionText = "Is PAM enforced?",
                businessRisk = "High",
                levelDefinitions = new { Manual = "manual" }
            }));
            lastStatus = response.StatusCode;
        }

        lastStatus.Should().Be((HttpStatusCode)429);
    }

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }

    private sealed class AiApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<IamDbContext>>();
                services.RemoveAll<IamDbContext>();
                services.AddDbContext<IamDbContext>(options =>
                    options.UseInMemoryDatabase($"ai-endpoint-{Guid.NewGuid():N}"));

                services.RemoveAll<IAiGuidanceService>();
                services.AddScoped<IAiGuidanceService, TestAiGuidanceService>();

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }

        protected override void ConfigureClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            base.ConfigureClient(client);
        }
    }

    private sealed class TestAiGuidanceService : IAiGuidanceService
    {
        public Task<AiGuidanceResponse> GenerateAsync(AiGuidanceRequest request, Guid orgId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiGuidanceResponse(
                "test",
                new[] { "example" },
                new[] { "evidence" },
                new[] { "check" }));
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim("oid", "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Role, "ClientRespondent")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
