namespace IamMaturityStudio.Web.Services.Api;

public class AiGuidanceClient : ApiClientBase
{
    public AiGuidanceClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<AiGuidanceResponse> GetGuidanceAsync(AiGuidanceRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/ai/guidance", JsonBody(request), cancellationToken);
        return await ReadAsync<AiGuidanceResponse>(response, cancellationToken);
    }
}
