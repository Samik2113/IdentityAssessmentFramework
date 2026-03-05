using FluentValidation;
using IamMaturityStudio.Application.Contracts;

namespace IamMaturityStudio.Application.Validation;

public sealed class CreateOrgRequestValidator : AbstractValidator<CreateOrgRequest>
{
    public CreateOrgRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateOrgBrandingRequestValidator : AbstractValidator<UpdateOrgBrandingRequest>
{
    public UpdateOrgBrandingRequestValidator()
    {
        RuleFor(x => x.LogoUrl).MaximumLength(2048).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
    }
}

public sealed class CreateAssessmentRequestValidator : AbstractValidator<CreateAssessmentRequest>
{
    public CreateAssessmentRequestValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.QuestionnaireId).NotEmpty();
        RuleFor(x => x.AssessmentYear).InclusiveBetween(2000, 3000);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateAssessmentStatusRequestValidator : AbstractValidator<UpdateAssessmentStatusRequest>
{
    public UpdateAssessmentStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("Invalid status transition target.");
    }

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "InCollection", "UnderReview", "EvidenceRequested", "EvidenceReceived", "Finalized"
    };
}

public sealed class InviteParticipantsRequestValidator : AbstractValidator<InviteParticipantsRequest>
{
    public InviteParticipantsRequestValidator()
    {
        RuleFor(x => x.Role).Equal("ClientRespondent");
        RuleFor(x => x.Emails).NotEmpty();
        RuleForEach(x => x.Emails).EmailAddress();
    }
}

public sealed class UpsertResponseRequestValidator : AbstractValidator<UpsertResponseRequest>
{
    public UpsertResponseRequestValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Level)
            .NotEmpty()
            .Must(level => AllowedLevels.Contains(level));
        RuleFor(x => x.Confidence).InclusiveBetween(1, 5).When(x => x.Confidence.HasValue);
    }

    private static readonly HashSet<string> AllowedLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Manual", "Partial", "Fully", "NA"
    };
}

public sealed class BulkUpsertResponsesRequestValidator : AbstractValidator<BulkUpsertResponsesRequest>
{
    public BulkUpsertResponsesRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new UpsertResponseRequestValidator());
    }
}

public sealed class CreateEvidenceRequestRequestValidator : AbstractValidator<CreateEvidenceRequestRequest>
{
    public CreateEvidenceRequestRequestValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}

public sealed class UpdateEvidenceRequestStatusRequestValidator : AbstractValidator<UpdateEvidenceRequestStatusRequest>
{
    public UpdateEvidenceRequestStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("Invalid evidence request status.");
    }

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Open", "Submitted", "Approved", "Rejected"
    };
}

public sealed class CreateEvidenceUploadRequestValidator : AbstractValidator<CreateEvidenceUploadRequest>
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public CreateEvidenceUploadRequestValidator()
    {
        RuleFor(x => x.EvidenceRequestId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxFileSizeBytes);
    }
}

public sealed class CompleteEvidenceUploadRequestValidator : AbstractValidator<CompleteEvidenceUploadRequest>
{
    public CompleteEvidenceUploadRequestValidator()
    {
        RuleFor(x => x.EvidenceRequestId).NotEmpty();
        RuleFor(x => x.BlobName).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileType).NotEmpty().MaximumLength(100);
    }
}

public sealed class GenerateReportRequestValidator : AbstractValidator<GenerateReportRequest>
{
    public GenerateReportRequestValidator()
    {
        RuleFor(x => x.ReportType).Equal("Standard");
    }
}

public sealed class AiGuidanceRequestValidator : AbstractValidator<AiGuidanceRequest>
{
    public AiGuidanceRequestValidator()
    {
        RuleFor(x => x.Domain).NotEmpty();
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.BusinessRisk).NotEmpty().MaximumLength(4000);
    }
}