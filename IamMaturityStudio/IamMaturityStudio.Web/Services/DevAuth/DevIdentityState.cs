using System.Security.Claims;

namespace IamMaturityStudio.Web.Services.DevAuth;

public class DevIdentityState
{
    private const string DefaultRole = "ClientRespondent";

    public string CurrentRole { get; private set; } = DefaultRole;
    public event Action? Changed;

    public void SetRole(string role)
    {
        CurrentRole = role;
        Changed?.Invoke();
    }

    public ClaimsPrincipal BuildPrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000123"),
            new("oid", "00000000-0000-0000-0000-000000000123"),
            new(ClaimTypes.Name, "Dev User"),
            new(ClaimTypes.Email, "dev.user@local.test"),
            new(ClaimTypes.Role, CurrentRole)
        };

        var identity = new ClaimsIdentity(claims, "Mock");
        return new ClaimsPrincipal(identity);
    }
}
