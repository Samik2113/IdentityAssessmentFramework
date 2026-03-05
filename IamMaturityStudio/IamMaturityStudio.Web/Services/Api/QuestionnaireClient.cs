namespace IamMaturityStudio.Web.Services.Api;

public class QuestionnaireClient : ApiClientBase
{
    public QuestionnaireClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<QuestionnaireTreeResponse> GetTreeAsync(Guid questionnaireId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/questionnaires/{questionnaireId}/tree", cancellationToken);
        return await ReadAsync<QuestionnaireTreeResponse>(response, cancellationToken);
    }
}
