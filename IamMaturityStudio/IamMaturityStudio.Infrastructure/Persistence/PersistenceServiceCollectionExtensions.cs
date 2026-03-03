using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IamMaturityStudio.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var cs = ResolveConnectionString(config, env);
        services.AddDbContext<IamDbContext>(opt =>
            opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure()));

        return services;
    }

    public static string ResolveConnectionString(IConfiguration config, IHostEnvironment env)
    {
        var fromAspNetCoreEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(fromAspNetCoreEnvVar))
        {
            return fromAspNetCoreEnvVar;
        }

        var fromConfig = config["ConnectionStrings:Default"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        if (config is IConfigurationRoot root && root.Providers.Any(p => p.GetType().Name.Contains("KeyVault", StringComparison.OrdinalIgnoreCase)))
        {
            var fromKeyVaultFallback = config["ConnectionStrings--Default"];
            if (!string.IsNullOrWhiteSpace(fromKeyVaultFallback))
            {
                return fromKeyVaultFallback;
            }
        }

        throw new InvalidOperationException(
            $"No SQL connection string configured for environment '{env.EnvironmentName}'. " +
            "Set ASPNETCORE_ConnectionStrings__Default, or configure ConnectionStrings:Default in appsettings, " +
            "or provide ConnectionStrings--Default via Azure Key Vault configuration.");
    }
}