namespace IamMaturityStudio.Web.Services.Api;

public class DashboardClient : ApiClientBase
{
    public DashboardClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<DashboardResponse> GetAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/assessments/{assessmentId}/dashboard", cancellationToken);
        return await ReadAsync<DashboardResponse>(response, cancellationToken);
    }
}
