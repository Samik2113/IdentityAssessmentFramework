using IamMaturityStudio.Web.Services.Api;

namespace IamMaturityStudio.Web.Services;

public class ChartDataAdapter
{
    public RadarChartData ToRadar(DashboardResponse response)
    {
        return new RadarChartData(
            response.Domains.Select(d => d.DomainCode).ToList(),
            response.Domains.Select(d => (double)d.Percent).ToList());
    }

    public HeatmapData ToHeatmap(DashboardResponse response)
    {
        var rows = response.Categories
            .Select(c => new HeatmapRow(c.CategoryCode, GetBand(c.Percent), (double)c.Percent))
            .ToList();

        return new HeatmapData(rows);
    }

    private static string GetBand(decimal percent)
    {
        if (percent < 40) return "Red";
        if (percent < 70) return "Amber";
        return "Green";
    }
}

public sealed record RadarChartData(IReadOnlyList<string> Labels, IReadOnlyList<double> Values);
public sealed record HeatmapData(IReadOnlyList<HeatmapRow> Rows);
public sealed record HeatmapRow(string Label, string Band, double Value);
