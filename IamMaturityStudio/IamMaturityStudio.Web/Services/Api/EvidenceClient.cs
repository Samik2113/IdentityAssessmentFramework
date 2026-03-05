namespace IamMaturityStudio.Web.Services.Api;

public class EvidenceClient : ApiClientBase
{
    public EvidenceClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<EvidenceRequestDto>> GetRequestsAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/assessments/{assessmentId}/evidence-requests", cancellationToken);
        return await ReadAsync<IReadOnlyList<EvidenceRequestDto>>(response, cancellationToken);
    }

    public async Task<EvidenceRequestDto> CreateRequestAsync(Guid assessmentId, CreateEvidenceRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/assessments/{assessmentId}/evidence-requests", JsonBody(request), cancellationToken);
        return await ReadAsync<EvidenceRequestDto>(response, cancellationToken);
    }

    public async Task<EvidenceRequestDto> UpdateRequestStatusAsync(Guid assessmentId, Guid requestId, string status, string? notes, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PatchAsync($"/assessments/{assessmentId}/evidence-requests/{requestId}", JsonBody(new UpdateEvidenceRequestStatusRequest(status, notes)), cancellationToken);
        return await ReadAsync<EvidenceRequestDto>(response, cancellationToken);
    }

    public async Task<CreateEvidenceUploadResponse> CreateUploadAsync(Guid assessmentId, CreateEvidenceUploadRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/assessments/{assessmentId}/evidence", JsonBody(request), cancellationToken);
        return await ReadAsync<CreateEvidenceUploadResponse>(response, cancellationToken);
    }

    public async Task<CompleteEvidenceUploadResponse?> CompleteUploadAsync(Guid assessmentId, CompleteEvidenceUploadRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/assessments/{assessmentId}/evidence/complete", JsonBody(request), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CompleteEvidenceUploadResponse>(cancellationToken: cancellationToken);
    }
}
