namespace IamMaturityStudio.Web.Services.Api;

public class ReportsClient : ApiClientBase
{
    public ReportsClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<ReportResponse> GeneratePdfAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/reports/{assessmentId}/pdf", JsonBody(new GenerateReportRequest(assessmentId, "Standard", null)), cancellationToken);
        return await ReadAsync<ReportResponse>(response, cancellationToken);
    }
}
