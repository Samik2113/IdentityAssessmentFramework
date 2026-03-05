namespace IamMaturityStudio.Application.Reports;

public interface IReportService
{
    Task<ReportResponse> GenerateAsync(Guid assessmentId, Guid orgId, GenerateReportRequest request, CancellationToken cancellationToken);
}
