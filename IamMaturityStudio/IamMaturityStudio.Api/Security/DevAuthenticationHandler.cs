using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IamMaturityStudio.Api.Security;

public sealed class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Mock";

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var requestedRole = Request.Headers.TryGetValue("X-Dev-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader)
            ? roleHeader.ToString()
            : "Admin";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000123"),
            new("oid", "00000000-0000-0000-0000-000000000123"),
            new(ClaimTypes.Name, "Dev Api User"),
            new(ClaimTypes.Email, "dev.api.user@local.test"),
            new(ClaimTypes.Role, requestedRole),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "Consultant"),
            new(ClaimTypes.Role, "ClientRespondent")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
