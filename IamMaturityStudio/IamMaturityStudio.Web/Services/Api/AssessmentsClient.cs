using System.Net.Http.Json;
using System.Text.Json;

namespace IamMaturityStudio.Web.Services.Api;

public class AssessmentsClient : ApiClientBase
{
    public AssessmentsClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<AssessmentSummaryResponse>> GetAssessmentsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/assessments", cancellationToken);
        return await ReadAsync<IReadOnlyList<AssessmentSummaryResponse>>(response, cancellationToken);
    }

    public async Task<AssessmentSummaryResponse> CreateAsync(CreateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/assessments", JsonBody(request), cancellationToken);
        return await ReadAsync<AssessmentSummaryResponse>(response, cancellationToken);
    }

    public async Task<AssessmentSummaryResponse> UpdateStatusAsync(Guid assessmentId, string status, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PatchAsync($"/assessments/{assessmentId}/status", JsonBody(new UpdateAssessmentStatusRequest(status)), cancellationToken);
        return await ReadAsync<AssessmentSummaryResponse>(response, cancellationToken);
    }

    public async Task<int> InviteAsync(Guid assessmentId, IReadOnlyList<string> emails, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/assessments/{assessmentId}/invite", JsonBody(new InviteParticipantsRequest(emails, "ClientRespondent")), cancellationToken);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("invited").GetInt32();
    }
}
