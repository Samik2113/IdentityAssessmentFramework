using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Persistence.Repositories;
using IamMaturityStudio.Infrastructure.Seeding;
using IamMaturityStudio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IamMaturityStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IQuestionnaireSeedImporter, QuestionnaireSeedImporter>();
        services.AddScoped<IApplicationDataContext, ApplicationDataContext>();
        services.AddSingleton<IStorageSasService, StorageSasService>();

        var blobServiceUri = configuration["Azure:BlobServiceUri"];
        if (!string.IsNullOrWhiteSpace(blobServiceUri))
        {
            services.AddSingleton(new BlobServiceClient(new Uri(blobServiceUri), new DefaultAzureCredential()));
        }

        var keyVaultUri = configuration["Azure:KeyVaultUri"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            services.AddSingleton(new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential()));
        }

        var openAiEndpoint = configuration["Azure:OpenAIEndpoint"];
        if (!string.IsNullOrWhiteSpace(openAiEndpoint))
        {
            services.AddSingleton<IAzureOpenAiClient>(new AzureOpenAiClient(openAiEndpoint));
        }

        return services;
    }
}