using IamMaturityStudio.Application.Contracts;

namespace IamMaturityStudio.Application.Security;

public static class AccessGuards
{
    public static void EnsureOrgMember(RequesterContext requester, Guid orgId)
    {
        if (!requester.OrgMemberships.Contains(orgId) && !requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("User does not have access to this organization.");
        }
    }

    public static void EnsureConsultant(RequesterContext requester)
    {
        if (!requester.Roles.Contains("Consultant") && !requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("Consultant role is required.");
        }
    }

    public static void EnsureRespondent(RequesterContext requester)
    {
        if (!requester.Roles.Contains("ClientRespondent") && !requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("ClientRespondent role is required.");
        }
    }
}

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}