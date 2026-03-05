using FluentAssertions;
using IamMaturityStudio.Infrastructure.Reports;

namespace IamMaturityStudio.Tests.Reports;

public class ReportFileNameTests
{
    [Fact]
    public void Create_Uses_Slugified_Organization_Name()
    {
        var timestamp = new DateTimeOffset(2026, 03, 05, 9, 10, 11, TimeSpan.Zero);

        var fileName = ReportFileName.Create("EY India - IAM", 2026, timestamp);

        fileName.Should().Be("iam-assessment-ey-india-iam-2026-20260305091011.pdf");
    }
}
