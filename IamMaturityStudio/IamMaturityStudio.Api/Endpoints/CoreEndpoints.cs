using FluentValidation;
using IamMaturityStudio.Api.Security;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Features;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace IamMaturityStudio.Api.Endpoints;

public static class CoreEndpoints
{
    public static IEndpointRouteBuilder MapCoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapOrganizationEndpoints();
        app.MapQuestionnaireEndpoints();
        app.MapAssessmentEndpoints();
        app.MapResponseEndpoints();
        app.MapEvidenceEndpoints();
        app.MapScoringDashboardReportAiEndpoints();
        return app;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", [Authorize] (HttpContext httpContext, IApplicationDataContext dataContext) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(new MeResponse(requester.UserId, requester.Name, requester.Email, requester.Roles.ToList(), requester.OrgMemberships.ToList()));
        });

        return app;
    }

    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orgs", [Authorize(Roles = "Admin")] async (HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetOrganizationsQuery(requester)));
        });

        app.MapPost("/orgs", [Authorize(Roles = "Admin")] async (CreateOrgRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<CreateOrgRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new CreateOrganizationCommand(requester, request), ct));
        });

        app.MapGet("/orgs/{orgId:guid}/branding", [Authorize(Roles = "Admin,Consultant")] async (Guid orgId, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetOrgBrandingQuery(requester, orgId), ct));
        }).RequireOrgAccess("orgId");

        app.MapPut("/orgs/{orgId:guid}/branding", [Authorize(Roles = "Admin")] async (Guid orgId, UpdateOrgBrandingRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<UpdateOrgBrandingRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new UpdateOrgBrandingCommand(requester, orgId, request), ct));
        }).RequireOrgAccess("orgId");

        return app;
    }

    public static IEndpointRouteBuilder MapQuestionnaireEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/questionnaires", [Authorize] async (HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetQuestionnairesQuery(requester), ct));
        });

        app.MapGet("/questionnaires/{id:guid}/tree", [Authorize] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetQuestionnaireTreeQuery(requester, id), ct));
        });

        return app;
    }

    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/assessments", [Authorize(Roles = "Consultant,Admin")] async (HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetAssessmentsQueryV2(requester), ct));
        });

        app.MapPost("/assessments", [Authorize(Roles = "Consultant,Admin")] async (CreateAssessmentRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<CreateAssessmentRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new CreateAssessmentCommand(requester, request), ct));
        });

        app.MapGet("/assessments/{id:guid}", [Authorize] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            if (requester.Roles.Contains("Consultant") || requester.Roles.Contains("Admin"))
            {
                return Results.Ok(await mediator.Send(new GetAssessmentForConsultantQuery(requester, id), ct));
            }

            return Results.Ok(await mediator.Send(new GetAssessmentForRespondentQuery(requester, id), ct));
        });

        app.MapPatch("/assessments/{id:guid}/status", [Authorize(Roles = "Consultant,Admin")] async (Guid id, UpdateAssessmentStatusRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<UpdateAssessmentStatusRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new UpdateAssessmentStatusCommand(requester, id, request), ct));
        });

        app.MapPost("/assessments/{id:guid}/invite", [Authorize(Roles = "Consultant,Admin")] async (Guid id, InviteParticipantsRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<InviteParticipantsRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(new { invited = await mediator.Send(new InviteParticipantsCommand(requester, id, request), ct) });
        });

        return app;
    }

    public static IEndpointRouteBuilder MapResponseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/assessments/{id:guid}/responses", [Authorize] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            if (requester.Roles.Contains("Consultant") || requester.Roles.Contains("Admin"))
            {
                return Results.Ok(await mediator.Send(new GetResponsesForConsultantQuery(requester, id), ct));
            }

            return Results.Ok(await mediator.Send(new GetResponsesForRespondentQuery(requester, id), ct));
        });

        app.MapPatch("/assessments/{id:guid}/responses", [Authorize(Roles = "ClientRespondent,Admin")] async (Guid id, BulkUpsertResponsesRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<BulkUpsertResponsesRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(new { upserted = await mediator.Send(new BulkUpsertResponsesCommand(requester, id, request), ct) });
        });

        app.MapPut("/assessments/{id:guid}/responses/{questionId:guid}", [Authorize(Roles = "ClientRespondent,Admin")] async (Guid id, Guid questionId, UpsertResponseRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<UpsertResponseRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new UpsertResponseCommand(requester, id, questionId, request), ct));
        });

        return app;
    }

    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/assessments/{id:guid}/evidence-requests", [Authorize(Roles = "Consultant,Admin")] async (Guid id, CreateEvidenceRequestRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<CreateEvidenceRequestRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new CreateEvidenceRequestCommand(requester, id, request), ct));
        });

        app.MapGet("/assessments/{id:guid}/evidence-requests", [Authorize(Roles = "Consultant,Admin")] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetEvidenceRequestsQuery(requester, id), ct));
        });

        app.MapPatch("/assessments/{id:guid}/evidence-requests/{reqId:guid}", [Authorize(Roles = "Consultant,Admin")] async (Guid id, Guid reqId, UpdateEvidenceRequestStatusRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<UpdateEvidenceRequestStatusRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new UpdateEvidenceRequestStatusCommand(requester, id, reqId, request), ct));
        });

        app.MapPost("/assessments/{id:guid}/evidence", [Authorize(Roles = "ClientRespondent,Admin")] async (Guid id, CreateEvidenceUploadRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<CreateEvidenceUploadRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new CreateEvidenceUploadCommand(requester, id, request), ct));
        });

        app.MapPost("/assessments/{id:guid}/evidence/complete", [Authorize(Roles = "ClientRespondent,Admin")] async (Guid id, CompleteEvidenceUploadRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<CompleteEvidenceUploadRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new CompleteEvidenceUploadCommand(requester, id, request), ct));
        });

        return app;
    }

    public static IEndpointRouteBuilder MapScoringDashboardReportAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/assessments/{id:guid}/score", [Authorize(Roles = "Consultant,Admin")] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new ComputeScoreCommand(requester, id), ct));
        });

        app.MapGet("/assessments/{id:guid}/dashboard", [Authorize(Roles = "Consultant,Admin")] async (Guid id, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, CancellationToken ct) =>
        {
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetDashboardQuery(requester, id), ct));
        });

        app.MapPost("/reports/{assessmentId:guid}/pdf", [Authorize(Roles = "Consultant,Admin")] async (Guid assessmentId, GenerateReportRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<GenerateReportRequest> validator, CancellationToken ct) =>
        {
            var normalizedRequest = request with { AssessmentId = assessmentId };
            await validator.ValidateAndThrowAsync(normalizedRequest, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GenerateReportCommand(requester, assessmentId, normalizedRequest), ct));
        });

        app.MapPost("/ai/guidance", [Authorize(Roles = "Admin,Consultant,ClientRespondent")] async (AiGuidanceRequest request, HttpContext httpContext, IMediator mediator, IApplicationDataContext dataContext, IValidator<AiGuidanceRequest> validator, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);
            var requester = UserContextFactory.Create(httpContext.User, dataContext);
            return Results.Ok(await mediator.Send(new GetAiGuidanceCommand(requester, request), ct));
        })
        .RequireRateLimiting("AiGuidancePerUser");

        return app;
    }
}