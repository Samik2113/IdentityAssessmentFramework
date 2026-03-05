namespace IamMaturityStudio.Web.Services.Api;

public class ScoringClient : ApiClientBase
{
    public ScoringClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<ScoreSnapshotResponse> ComputeAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/assessments/{assessmentId}/score", JsonBody(new { }), cancellationToken);
        return await ReadAsync<ScoreSnapshotResponse>(response, cancellationToken);
    }
}
