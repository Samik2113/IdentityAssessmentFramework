using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IamMaturityStudio.Web.Services.Api;

public sealed record MeResponse(Guid UserId, string Name, string Email, IReadOnlyList<string> Roles, IReadOnlyList<Guid> OrgMemberships);
public sealed record Organization(Guid Id, string Name, string? LogoUrl, string? ThemeJson, string? HeatmapBandsJson);
public sealed record CreateOrgRequest(string Name, string? ThemeJson);
public sealed record UpdateOrgBrandingRequest(string? LogoUrl, string? ThemeJson, string? HeatmapBands);
public sealed record CreateAssessmentRequest(Guid OrgId, Guid QuestionnaireId, int AssessmentYear, string Name);
public sealed record UpdateAssessmentStatusRequest(string Status);
public sealed record InviteParticipantsRequest(IReadOnlyList<string> Emails, string Role);
public sealed record AssessmentSummaryResponse(Guid Id, Guid OrgId, string Name, int AssessmentYear, string Status, Guid QuestionnaireId, Guid CreatedBy, DateTimeOffset CreatedAt);
public sealed record UpsertResponseRequest(Guid QuestionId, string Level, string? Comment, int? Confidence);
public sealed record BulkUpsertResponsesRequest(IReadOnlyList<UpsertResponseRequest> Items);
public sealed record ResponseListForConsultant(Guid QuestionId, string Level, string? Comment, int? Confidence, int EvidenceCount, decimal? Score);
public sealed record ResponseListForRespondent(Guid QuestionId, string Level, string? Comment, int? Confidence, int EvidenceCount);
public sealed record CreateEvidenceRequestRequest(Guid QuestionId, DateOnly? DueDate, string? Notes);
public sealed record UpdateEvidenceRequestStatusRequest(string Status, string? Notes);
public sealed record CreateEvidenceUploadRequest(Guid EvidenceRequestId, string FileName, string FileType, long FileSizeBytes);
public sealed record CreateEvidenceUploadResponse(string UploadUrl, string BlobName, DateTimeOffset ExpiresAt);
public sealed record CompleteEvidenceUploadRequest(Guid EvidenceRequestId, string BlobName, string FileName, string FileType);
public sealed record CompleteEvidenceUploadResponse(Guid EvidenceFileId, string BlobName, string VirusScanStatus);
public sealed record EvidenceRequestDto(Guid Id, Guid QuestionId, string Status, DateOnly? DueDate, string? Notes);
public sealed record ScoreSnapshotResponse(decimal OverallPercent, decimal OverallMaturity0To5, IReadOnlyList<DomainScoreDto> Domains);
public sealed record DomainScoreDto(Guid DomainId, string DomainCode, decimal Percent, decimal Maturity0To5);
public sealed record DashboardResponse(DashboardKpi Kpis, IReadOnlyList<DomainScoreDto> Domains, IReadOnlyList<DashboardCategoryScore> Categories, IReadOnlyList<DashboardRadarSeries> RadarSeries, IReadOnlyList<DashboardHeatmapCell> Heatmap);
public sealed record DashboardKpi(decimal OverallPercent, decimal EvidenceCompletenessPercent, int GapsCount, int QuickWinsCount);
public sealed record DashboardCategoryScore(Guid CategoryId, string CategoryCode, decimal Percent);
public sealed record DashboardRadarSeries(string Axis, decimal Value);
public sealed record DashboardHeatmapCell(string DomainCode, string CategoryCode, decimal Percent, string Band);
public sealed record GenerateReportRequest(string ReportType, string? ThemeOverride);
public sealed record ReportResponse(string ReportUrl);
public sealed record AiGuidanceRequest(string Domain, string Category, string QuestionText, string BusinessRisk, object LevelDefinitions, string? UserComment);
public sealed record AiGuidanceResponse(string Explanation, IReadOnlyList<string> Examples, IReadOnlyList<string> EvidenceSuggestions, IReadOnlyList<string> Checklist);

public sealed record QuestionnaireTreeResponse(Guid QuestionnaireId, string Name, string Version, IReadOnlyList<QuestionnaireDomainNode> Domains);
public sealed record QuestionnaireDomainNode(Guid Id, string Code, string Name, IReadOnlyList<QuestionnaireCategoryNode> Categories);
public sealed record QuestionnaireCategoryNode(Guid Id, string Code, string Name, decimal Weight, string BusinessRisk, IReadOnlyList<QuestionnaireQuestionNode> Questions);
public sealed record QuestionnaireQuestionNode(Guid Id, string Code, string Text, decimal DefaultWeight);

public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;

    protected ApiClientBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"API request failed ({(int)response.StatusCode}): {text}");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("API response body was empty.");
    }

    protected static StringContent JsonBody<T>(T value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
