using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace IamMaturityStudio.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string SqlServerProvider = "SqlServer";
    public const string SqliteProvider = "Sqlite";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var provider = ResolveProvider(config);
        if (provider.Equals(SqliteProvider, StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = ResolveSqliteConnectionString(config, env);
            services.AddDbContext<IamDbContext>(opt => opt.UseSqlite(sqliteConnectionString));
            return services;
        }

        if (!provider.Equals(SqlServerProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported persistence provider '{provider}'. Supported values: {SqlServerProvider}, {SqliteProvider}.");
        }

        var sqlServerConnectionString = ResolveConnectionString(config, env);
        services.AddDbContext<IamDbContext>(opt =>
            opt.UseSqlServer(sqlServerConnectionString, sql => sql.EnableRetryOnFailure()));

        return services;
    }

    public static string ResolveProvider(IConfiguration config)
    {
        var fromAspNetCoreEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_Persistence__Provider");
        if (!string.IsNullOrWhiteSpace(fromAspNetCoreEnvVar))
        {
            return fromAspNetCoreEnvVar;
        }

        var fromConfig = config["Persistence:Provider"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        return SqlServerProvider;
    }

    public static string ResolveConnectionString(IConfiguration config, IHostEnvironment env)
    {
        var fromAspNetCoreEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(fromAspNetCoreEnvVar))
        {
            if (env.IsProduction() && IsManagedIdentityConnectionString(fromAspNetCoreEnvVar))
            {
                return fromAspNetCoreEnvVar;
            }

            return fromAspNetCoreEnvVar;
        }

        var fromConfig = config["ConnectionStrings:Default"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            if (env.IsProduction() && IsManagedIdentityConnectionString(fromConfig))
            {
                return fromConfig;
            }

            return fromConfig;
        }

        if (config is IConfigurationRoot root && root.Providers.Any(p => p.GetType().Name.Contains("KeyVault", StringComparison.OrdinalIgnoreCase)))
        {
            var fromKeyVaultFallback = config["ConnectionStrings--Default"];
            if (!string.IsNullOrWhiteSpace(fromKeyVaultFallback))
            {
                if (env.IsProduction() && IsManagedIdentityConnectionString(fromKeyVaultFallback))
                {
                    return fromKeyVaultFallback;
                }

                return fromKeyVaultFallback;
            }
        }

        throw new InvalidOperationException(
            $"No SQL connection string configured for environment '{env.EnvironmentName}'. " +
            "Set ASPNETCORE_ConnectionStrings__Default, or configure ConnectionStrings:Default in appsettings, " +
            "or provide ConnectionStrings--Default via Azure Key Vault configuration.");
    }

    private static bool IsManagedIdentityConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (!TryGetValue(builder, "Authentication", out var authenticationValue))
        {
            return false;
        }

        var isActiveDirectoryDefault = string.Equals(authenticationValue?.Trim(), "Active Directory Default", StringComparison.OrdinalIgnoreCase);
        if (!isActiveDirectoryDefault)
        {
            return false;
        }

        return !ContainsAnyKey(builder, "User ID", "UID", "Password", "PWD");
    }

    private static bool TryGetValue(DbConnectionStringBuilder builder, string key, out string? value)
    {
        if (!builder.TryGetValue(key, out var rawValue) || rawValue is null)
        {
            value = null;
            return false;
        }

        value = rawValue.ToString();
        return true;
    }

    private static bool ContainsAnyKey(DbConnectionStringBuilder builder, params string[] keys)
        => keys.Any(builder.ContainsKey);

    public static string ResolveSqliteConnectionString(IConfiguration config, IHostEnvironment env)
    {
        var fromAspNetCoreEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_ConnectionStrings__Sqlite");
        if (!string.IsNullOrWhiteSpace(fromAspNetCoreEnvVar))
        {
            return NormalizeSqliteConnectionString(fromAspNetCoreEnvVar, env);
        }

        var fromConfig = config["ConnectionStrings:Sqlite"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return NormalizeSqliteConnectionString(fromConfig, env);
        }

        var dataFolder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataFolder);
        return $"Data Source={Path.Combine(dataFolder, "iam-maturity-studio.dev.db")}";
    }

    private static string NormalizeSqliteConnectionString(string connectionString, IHostEnvironment env)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (!builder.TryGetValue("Data Source", out var dataSourceValue) || dataSourceValue is null)
        {
            return connectionString;
        }

        var sourcePath = dataSourceValue.ToString();
        if (string.IsNullOrWhiteSpace(sourcePath) || sourcePath == ":memory:")
        {
            return connectionString;
        }

        var fullPath = Path.IsPathRooted(sourcePath)
            ? sourcePath
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, sourcePath));

        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        builder["Data Source"] = fullPath;
        return builder.ConnectionString;
    }
}