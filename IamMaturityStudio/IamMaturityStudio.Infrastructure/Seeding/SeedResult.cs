namespace IamMaturityStudio.Infrastructure.Seeding;

public sealed record SeedResult(
    int DomainsInserted,
    int DomainsUpdated,
    int CategoriesInserted,
    int CategoriesUpdated,
    int QuestionsInserted,
    int QuestionsUpdated,
    int ScoringModelsInserted,
    int OrganizationsInserted);