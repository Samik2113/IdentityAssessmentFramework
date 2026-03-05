using IamMaturityStudio.Application.Contracts;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace IamMaturityStudio.Infrastructure.Reports;

public interface IChartRenderer
{
    Task<ReportChartArtifacts> RenderAsync(DashboardResponse dashboard, string outputDirectory, string filePrefix, CancellationToken cancellationToken);
}

public sealed record ReportChartArtifacts(string? RadarChartPath, string? HeatmapChartPath, IReadOnlyList<string> Warnings);

public sealed class ChartRenderer : IChartRenderer
{
    private readonly ILogger<ChartRenderer> _logger;

    public ChartRenderer(ILogger<ChartRenderer> logger)
    {
        _logger = logger;
    }

    public Task<ReportChartArtifacts> RenderAsync(DashboardResponse dashboard, string outputDirectory, string filePrefix, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var warnings = new List<string>();

        var radarPath = Path.Combine(outputDirectory, $"{filePrefix}-radar.png");
        var heatmapPath = Path.Combine(outputDirectory, $"{filePrefix}-heatmap.png");

        try
        {
            RenderRadar(dashboard, radarPath);
        }
        catch (Exception ex)
        {
            warnings.Add("Radar chart rendering failed.");
            _logger.LogWarning(ex, "Radar chart rendering failed for report prefix {Prefix}", filePrefix);
            radarPath = null;
        }

        try
        {
            RenderHeatmap(dashboard, heatmapPath);
        }
        catch (Exception ex)
        {
            warnings.Add("Heatmap chart rendering failed.");
            _logger.LogWarning(ex, "Heatmap chart rendering failed for report prefix {Prefix}", filePrefix);
            heatmapPath = null;
        }

        return Task.FromResult(new ReportChartArtifacts(radarPath, heatmapPath, warnings));
    }

    private static void RenderRadar(DashboardResponse dashboard, string outputPath)
    {
        const int size = 620;
        var info = new SKImageInfo(size, size);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var center = new SKPoint(size / 2f, size / 2f);
        var maxRadius = size * 0.38f;
        var axes = dashboard.RadarSeries.Count == 0
            ? new[] { new DashboardRadarSeries("No data", 0m) }
            : dashboard.RadarSeries;

        using var gridPaint = new SKPaint { Color = new SKColor(210, 214, 220), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var axisPaint = new SKPaint { Color = new SKColor(180, 185, 193), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var fillPaint = new SKPaint { Color = new SKColor(37, 99, 235, 80), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var linePaint = new SKPaint { Color = new SKColor(37, 99, 235), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        using var labelPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 14 };

        for (var ring = 1; ring <= 5; ring++)
        {
            canvas.DrawCircle(center, maxRadius * ring / 5f, gridPaint);
        }

        var polygon = new SKPath();
        for (var index = 0; index < axes.Count; index++)
        {
            var angle = (float)(-Math.PI / 2 + (2 * Math.PI * index / axes.Count));
            var value = Math.Clamp((float)axes[index].Value / 100f, 0f, 1f);

            var axisX = center.X + (float)Math.Cos(angle) * maxRadius;
            var axisY = center.Y + (float)Math.Sin(angle) * maxRadius;
            canvas.DrawLine(center.X, center.Y, axisX, axisY, axisPaint);

            var pointX = center.X + (float)Math.Cos(angle) * maxRadius * value;
            var pointY = center.Y + (float)Math.Sin(angle) * maxRadius * value;
            if (index == 0)
            {
                polygon.MoveTo(pointX, pointY);
            }
            else
            {
                polygon.LineTo(pointX, pointY);
            }

            var labelRadius = maxRadius + 24;
            var labelX = center.X + (float)Math.Cos(angle) * labelRadius;
            var labelY = center.Y + (float)Math.Sin(angle) * labelRadius;
            canvas.DrawText(axes[index].Axis, labelX - 20, labelY + 4, labelPaint);
        }

        polygon.Close();
        canvas.DrawPath(polygon, fillPaint);
        canvas.DrawPath(polygon, linePaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    private static void RenderHeatmap(DashboardResponse dashboard, string outputPath)
    {
        const int width = 920;
        var rows = Math.Max(1, dashboard.Heatmap.Select(h => h.DomainCode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        const int rowHeight = 58;
        var height = 120 + rows * rowHeight;

        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 14 };
        using var smallTextPaint = new SKPaint { Color = new SKColor(65, 65, 65), IsAntialias = true, TextSize = 12 };
        using var borderPaint = new SKPaint { Color = new SKColor(218, 220, 224), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

        var grouped = dashboard.Heatmap
            .GroupBy(h => h.DomainCode)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (grouped.Count == 0)
        {
            canvas.DrawText("No heatmap data available", 24, 56, textPaint);
        }

        var y = 56;
        foreach (var domain in grouped)
        {
            canvas.DrawText(domain.Key, 24, y + 24, textPaint);
            var cells = domain.OrderBy(x => x.CategoryCode, StringComparer.OrdinalIgnoreCase).ToList();
            var cellWidth = Math.Max(120, (width - 180) / Math.Max(1, cells.Count));

            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                var x = 160 + index * cellWidth;
                var rect = new SKRect(x, y, x + cellWidth - 6, y + 38);

                using var fillPaint = new SKPaint { Color = ResolveBandColor(cell.Band), IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, borderPaint);

                canvas.DrawText($"{cell.CategoryCode} {Math.Round(cell.Percent, 1)}%", rect.Left + 8, rect.MidY + 4, smallTextPaint);
            }

            y += rowHeight;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    private static SKColor ResolveBandColor(string band)
    {
        return band.ToLowerInvariant() switch
        {
            "high" => new SKColor(220, 38, 38),
            "med" or "medium" => new SKColor(245, 158, 11),
            "low" => new SKColor(22, 163, 74),
            _ => new SKColor(59, 130, 246)
        };
    }
}
