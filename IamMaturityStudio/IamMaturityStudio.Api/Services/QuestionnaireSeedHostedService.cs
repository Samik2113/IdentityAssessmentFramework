using IamMaturityStudio.Infrastructure.Seeding;

namespace IamMaturityStudio.Api.Services;

public class QuestionnaireSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<QuestionnaireSeedHostedService> _logger;

    public QuestionnaireSeedHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<QuestionnaireSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runOnStartup = _configuration.GetValue<bool>("Seeding:RunOnStartup");
        if (!runOnStartup)
        {
            return;
        }

        var seedPath = _configuration.GetValue<string>("Seeding:SeedPath")
                       ?? "Seed/iam_maturity_questionnaire_seed.json";
        var fullPath = Path.Combine(_environment.ContentRootPath, seedPath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Questionnaire seed skipped. File not found at {SeedPath}", fullPath);
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<IQuestionnaireSeedImporter>();
            await using var stream = File.OpenRead(fullPath);
            var result = await importer.ImportAsync(stream, stoppingToken);

            _logger.LogInformation(
                "Questionnaire seed completed. Domains {DomainsInserted}/{DomainsUpdated}, Categories {CategoriesInserted}/{CategoriesUpdated}, Questions {QuestionsInserted}/{QuestionsUpdated}, ScoringModelsInserted {ScoringModelsInserted}, OrganizationsInserted {OrganizationsInserted}",
                result.DomainsInserted,
                result.DomainsUpdated,
                result.CategoriesInserted,
                result.CategoriesUpdated,
                result.QuestionsInserted,
                result.QuestionsUpdated,
                result.ScoringModelsInserted,
                result.OrganizationsInserted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Questionnaire seed failed during startup.");
        }
    }
}