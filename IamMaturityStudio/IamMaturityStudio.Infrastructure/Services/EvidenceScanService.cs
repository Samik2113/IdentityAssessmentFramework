using System.Threading.Channels;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IamMaturityStudio.Infrastructure.Services;

public sealed class EvidenceScanService : IEvidenceScanService
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public Task QueueScanAsync(Guid evidenceFileId, CancellationToken cancellationToken)
    {
        return _queue.Writer.WriteAsync(evidenceFileId, cancellationToken).AsTask();
    }

    internal IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}

public sealed class EvidenceScanBackgroundService : BackgroundService
{
    private readonly EvidenceScanService _scanService;
    private readonly IServiceScopeFactory _scopeFactory;

    public EvidenceScanBackgroundService(EvidenceScanService scanService, IServiceScopeFactory scopeFactory)
    {
        _scanService = scanService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evidenceFileId in _scanService.ReadAllAsync(stoppingToken))
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IamDbContext>();
            var evidenceFile = await dbContext.EvidenceFiles.FirstOrDefaultAsync(f => f.Id == evidenceFileId, stoppingToken);
            if (evidenceFile is null)
            {
                continue;
            }

            evidenceFile.VirusScanStatus = "Clean";
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
