namespace IamMaturityStudio.Infrastructure.Seeding;

public interface IQuestionnaireSeedImporter
{
    Task<SeedResult> ImportAsync(Stream jsonStream, CancellationToken ct);
}