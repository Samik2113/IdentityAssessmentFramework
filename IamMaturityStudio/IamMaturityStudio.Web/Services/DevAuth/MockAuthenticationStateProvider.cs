using Microsoft.AspNetCore.Components.Authorization;

namespace IamMaturityStudio.Web.Services.DevAuth;

public class MockAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly DevIdentityState _identityState;

    public MockAuthenticationStateProvider(DevIdentityState identityState)
    {
        _identityState = identityState;
        _identityState.Changed += OnChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_identityState.BuildPrincipal()));
    }

    private void OnChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _identityState.Changed -= OnChanged;
    }
}
