using System.Text.Json;

namespace IamMaturityStudio.Web.Services.Api;

public class ResponsesClient : ApiClientBase
{
    public ResponsesClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<ResponseListForRespondent>> GetRespondentAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/assessments/{assessmentId}/responses", cancellationToken);
        return await ReadAsync<IReadOnlyList<ResponseListForRespondent>>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ResponseListForConsultant>> GetConsultantAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/assessments/{assessmentId}/responses", cancellationToken);
        return await ReadAsync<IReadOnlyList<ResponseListForConsultant>>(response, cancellationToken);
    }

    public async Task<int> SaveBulkAsync(Guid assessmentId, IReadOnlyList<UpsertResponseRequest> items, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PatchAsync($"/assessments/{assessmentId}/responses", JsonBody(new BulkUpsertResponsesRequest(items)), cancellationToken);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("upserted").GetInt32();
    }
}
