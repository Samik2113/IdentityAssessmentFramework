using FluentAssertions;
using IamMaturityStudio.Web.Services;
using IamMaturityStudio.Web.Services.Api;

namespace IamMaturityStudio.Tests.Web;

public class ChartDataAdapterTests
{
    [Fact]
    public void Maps_Dashboard_To_Radar_And_Heatmap()
    {
        var dashboard = new DashboardResponse(
            new DashboardKpi(72, 80, 2, 3),
            new[] { new DomainScoreDto(Guid.NewGuid(), "ID", 72, 3.6m) },
            new[]
            {
                new DashboardCategoryScore(Guid.NewGuid(), "C1", 25),
                new DashboardCategoryScore(Guid.NewGuid(), "C2", 65),
                new DashboardCategoryScore(Guid.NewGuid(), "C3", 90)
            },
            Array.Empty<DashboardRadarSeries>(),
            Array.Empty<DashboardHeatmapCell>());

        var adapter = new ChartDataAdapter();

        var radar = adapter.ToRadar(dashboard);
        var heatmap = adapter.ToHeatmap(dashboard);

        radar.Labels.Should().ContainSingle().Which.Should().Be("ID");
        radar.Values.Should().ContainSingle().Which.Should().Be(72);

        heatmap.Rows.Should().HaveCount(3);
        heatmap.Rows[0].Band.Should().Be("Red");
        heatmap.Rows[1].Band.Should().Be("Amber");
        heatmap.Rows[2].Band.Should().Be("Green");
    }
}
