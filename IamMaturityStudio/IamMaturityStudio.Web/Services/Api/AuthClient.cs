using System.Net.Http.Json;

namespace IamMaturityStudio.Web.Services.Api;

public class AuthClient : ApiClientBase
{
    public AuthClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/auth/me", cancellationToken);
        return await ReadAsync<MeResponse>(response, cancellationToken);
    }
}
