using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IamMaturityStudio.Infrastructure.Persistence.Repositories;

public class AssessmentRepository : IAssessmentRepository
{
    private readonly IamDbContext _dbContext;

    public AssessmentRepository(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Assessment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Assessments
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}