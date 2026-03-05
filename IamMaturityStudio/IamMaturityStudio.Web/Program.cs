using IamMaturityStudio.Web.Components;
using IamMaturityStudio.Web.Services;
using IamMaturityStudio.Web.Services.Api;
using IamMaturityStudio.Web.Services.DevAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

var useMockIdentity = builder.Configuration.GetValue<bool>("Features:UseMockIdentity");

if (!useMockIdentity)
{
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = "https://localhost:7153";
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<ChartDataAdapter>();

builder.Services.AddHttpClient<AuthClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<OrgsClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<QuestionnaireClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<AssessmentsClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ResponsesClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<EvidenceClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ScoringClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<DashboardClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ReportsClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<AiGuidanceClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

if (useMockIdentity)
{
    builder.Services.AddScoped<DevIdentityState>();
    builder.Services.AddScoped<AuthenticationStateProvider, MockAuthenticationStateProvider>();
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
if (!useMockIdentity)
{
    app.UseAuthentication();
}
app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();