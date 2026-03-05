using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDataContext _dataContext;

    public ReportService(IApplicationDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public ReportResponse Generate(Guid assessmentId, Guid orgId, GenerateReportRequest request)
    {
        var reportUrl = $"https://reports.local/{orgId}/{assessmentId}/{Guid.NewGuid():N}.pdf";
        _dataContext.Add(new Domain.Entities.Report
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            ReportType = request.ReportType,
            ReportUrl = reportUrl
        });
        _dataContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return new ReportResponse(reportUrl);
    }
}
