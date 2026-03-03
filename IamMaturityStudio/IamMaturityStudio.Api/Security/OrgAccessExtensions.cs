using IamMaturityStudio.Api.Security;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Api.Security;

public static class OrgAccessExtensions
{
    public static RouteHandlerBuilder RequireOrgAccess(this RouteHandlerBuilder builder, string routeParameterName = "orgId")
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var dataContext = context.HttpContext.RequestServices.GetRequiredService<IApplicationDataContext>();
            var requester = UserContextFactory.Create(context.HttpContext.User, dataContext);

            if (!context.HttpContext.Request.RouteValues.TryGetValue(routeParameterName, out var rawOrgId) ||
                rawOrgId is null ||
                !Guid.TryParse(rawOrgId.ToString(), out var orgId))
            {
                return Results.BadRequest(new { message = "orgId route value is required." });
            }

            var hasAccess = requester.Roles.Contains("Admin") || requester.OrgMemberships.Contains(orgId);
            if (!hasAccess)
            {
                return Results.Forbid();
            }

            return await next(context);
        });
    }
}