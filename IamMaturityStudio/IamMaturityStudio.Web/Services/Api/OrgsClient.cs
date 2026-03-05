namespace IamMaturityStudio.Web.Services.Api;

public class OrgsClient : ApiClientBase
{
    public OrgsClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<Organization>> GetAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/orgs", cancellationToken);
        return await ReadAsync<IReadOnlyList<Organization>>(response, cancellationToken);
    }

    public async Task<UpdateOrgBrandingRequest> GetBrandingAsync(Guid orgId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync($"/orgs/{orgId}/branding", cancellationToken);
        return await ReadAsync<UpdateOrgBrandingRequest>(response, cancellationToken);
    }

    public async Task<Organization> SaveBrandingAsync(Guid orgId, UpdateOrgBrandingRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsync($"/orgs/{orgId}/branding", JsonBody(request), cancellationToken);
        return await ReadAsync<Organization>(response, cancellationToken);
    }
}
