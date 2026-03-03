using IamMaturityStudio.Domain.Entities;

namespace IamMaturityStudio.Application.Interfaces;

public interface IAssessmentRepository
{
    Task<IReadOnlyList<Assessment>> GetAllAsync(CancellationToken cancellationToken = default);
}