using System.Security.Claims;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Api.Security;

public static class UserContextFactory
{
    public static RequesterContext Create(ClaimsPrincipal principal, IApplicationDataContext dataContext)
    {
        var oid = principal.FindFirstValue("oid") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var name = principal.FindFirstValue("name") ?? principal.Identity?.Name ?? string.Empty;
        var email = principal.FindFirstValue("preferred_username") ?? principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var user = dataContext.Users.FirstOrDefault(u => u.AadObjectId == oid) ?? dataContext.Users.FirstOrDefault(u => u.Email == email);
        var userId = user?.Id ?? (Guid.TryParse(oid, out var parsedOid) ? parsedOid : Guid.NewGuid());

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Concat(principal.FindAll("roles").Select(c => c.Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roles.Count == 0)
        {
            roles.Add("ClientRespondent");
        }

        var orgMemberships = new List<Guid>();
        if (user is not null)
        {
            orgMemberships = dataContext.OrganizationMemberships
                .Where(m => m.UserId == user.Id)
                .Select(m => m.OrganizationId)
                .Distinct()
                .ToList();
        }

        if (orgMemberships.Count == 0)
        {
            var orgClaim = principal.FindFirstValue("orgs") ?? principal.FindFirstValue("orgId");
            if (!string.IsNullOrWhiteSpace(orgClaim))
            {
                foreach (var part in orgClaim.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Guid.TryParse(part, out var orgId))
                    {
                        orgMemberships.Add(orgId);
                    }
                }
            }
        }

        return new RequesterContext(userId, name, email, roles, orgMemberships.Distinct().ToList());
    }
}