using IamMaturityStudio.Api.Endpoints;
using IamMaturityStudio.Api.Middleware;
using IamMaturityStudio.Api.Security;
using IamMaturityStudio.Api.Services;
using IamMaturityStudio.Application;
using IamMaturityStudio.Infrastructure;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Seeding;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructure(builder.Configuration);

var useMockIdentity = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Features:UseMockIdentity");

if (useMockIdentity)
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = DevAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevAuthenticationHandler.SchemeName;
            options.DefaultScheme = DevAuthenticationHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, _ => { });
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AiGuidancePerUser", httpContext =>
    {
        var userId = httpContext.User.FindFirst("oid")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "IamMaturityStudio API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

if (builder.Configuration.GetValue<bool>("Seeding:RunOnStartup"))
{
    builder.Services.AddHostedService<QuestionnaireSeedHostedService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    if (db.Database.IsSqlite())
    {
        db.Database.EnsureCreated();

        try
        {
            _ = db.Questionnaires.Any();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
    else if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestampUtc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapGet("/health/db", async (IamDbContext db, CancellationToken ct) =>
{
    var connected = await db.Database.CanConnectAsync(ct);
    if (!connected)
    {
        return Results.Problem("Database connectivity check failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "Healthy",
        provider = db.Database.ProviderName
    });
}).AllowAnonymous();

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

app.MapCoreEndpoints();

app.Run();

public partial class Program;