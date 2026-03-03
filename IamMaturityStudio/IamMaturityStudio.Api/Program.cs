using IamMaturityStudio.Api.Services;
using IamMaturityStudio.Application;
using IamMaturityStudio.Infrastructure;
using IamMaturityStudio.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Configuration.GetValue<bool>("Seeding:RunOnStartup"))
{
    builder.Services.AddHostedService<QuestionnaireSeedHostedService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestampUtc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapPost("/admin/seed/questionnaire", async (
    IQuestionnaireSeedImporter importer,
    IConfiguration configuration,
    IHostEnvironment environment,
    CancellationToken ct) =>
{
    var seedPath = configuration.GetValue<string>("Seeding:SeedPath")
                   ?? "Seed/iam_maturity_questionnaire_seed.json";
    var fullPath = Path.Combine(environment.ContentRootPath, seedPath);

    if (!File.Exists(fullPath))
    {
        return Results.NotFound(new
        {
            message = "Seed file not found.",
            seedPath = fullPath
        });
    }

    await using var stream = File.OpenRead(fullPath);
    var result = await importer.ImportAsync(stream, ct);
    return Results.Ok(result);
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.Run();