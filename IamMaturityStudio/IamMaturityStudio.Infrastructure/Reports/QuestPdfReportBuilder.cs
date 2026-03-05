using IamMaturityStudio.Application.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IamMaturityStudio.Infrastructure.Reports;

public interface IReportPdfBuilder
{
    byte[] Build(ReportDocumentModel model);
}

public sealed record ReportQuestionRow(string DomainCode, string CategoryCode, string QuestionCode, string QuestionText, string Level, string? Comment, int EvidenceCount);

public sealed record ReportDocumentModel(
    string OrganizationName,
    string AssessmentName,
    int AssessmentYear,
    DateTimeOffset GeneratedAtUtc,
    DashboardResponse Dashboard,
    IReadOnlyList<ReportQuestionRow> QuestionRows,
    ReportChartArtifacts ChartArtifacts,
    IReadOnlyList<string> Warnings);

public sealed class QuestPdfReportBuilder : IReportPdfBuilder
{
    public byte[] Build(ReportDocumentModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"IAM Maturity Assessment Report ({model.AssessmentYear})").SemiBold().FontSize(16);
                        column.Item().Text($"Organization: {model.OrganizationName}");
                        column.Item().Text($"Assessment: {model.AssessmentName}");
                        column.Item().Text($"Generated (UTC): {model.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}").FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Element(x => BuildKpiBlock(x, model.Dashboard.Kpis));
                        column.Item().Element(x => BuildDomainBlock(x, model.Dashboard.Domains));

                        column.Item().Text("Radar Chart").SemiBold();
                        column.Item().Element(x => BuildChart(x, model.ChartArtifacts.RadarChartPath, "Radar chart unavailable"));

                        column.Item().Text("Heatmap Chart").SemiBold();
                        column.Item().Element(x => BuildChart(x, model.ChartArtifacts.HeatmapChartPath, "Heatmap chart unavailable"));

                        if (model.Warnings.Count > 0)
                        {
                            column.Item().Text("Generation Warnings").SemiBold();
                            foreach (var warning in model.Warnings)
                            {
                                column.Item().Text($"- {warning}").FontColor(Colors.Orange.Darken2);
                            }
                        }

                        column.Item().Text("Question Summary").SemiBold();
                        column.Item().Element(x => BuildQuestionTable(x, model.QuestionRows));
                    });

                    page.Footer().AlignRight().Text(x => x.Span("Confidential - IAM Maturity Studio"));
                });
            })
            .GeneratePdf();
    }

    private static void BuildKpiBlock(IContainer container, DashboardKpi kpi)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(170);
                columns.RelativeColumn();
            });

            AddKpiRow(table, "Overall Percent", $"{Math.Round(kpi.OverallPercent, 2)}%");
            AddKpiRow(table, "Evidence Completeness", $"{Math.Round(kpi.EvidenceCompletenessPercent, 2)}%");
            AddKpiRow(table, "Gaps", kpi.GapsCount.ToString());
            AddKpiRow(table, "Quick Wins", kpi.QuickWinsCount.ToString());
        });
    }

    private static void AddKpiRow(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(1).PaddingVertical(4).Text(label).SemiBold();
        table.Cell().BorderBottom(1).PaddingVertical(4).Text(value);
    }

    private static void BuildDomainBlock(IContainer container, IReadOnlyList<DomainScoreDto> domains)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(120);
                columns.ConstantColumn(120);
                columns.ConstantColumn(120);
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).Text("Domain").SemiBold();
                header.Cell().BorderBottom(1).Text("Percent").SemiBold();
                header.Cell().BorderBottom(1).Text("Maturity (0-5)").SemiBold();
            });

            foreach (var domain in domains)
            {
                table.Cell().BorderBottom(1).PaddingVertical(3).Text(domain.DomainCode);
                table.Cell().BorderBottom(1).PaddingVertical(3).Text($"{Math.Round(domain.Percent, 2)}%");
                table.Cell().BorderBottom(1).PaddingVertical(3).Text(Math.Round(domain.Maturity0To5, 2).ToString("0.##"));
            }
        });
    }

    private static void BuildChart(IContainer container, string? path, string fallback)
    {
        var hasImage = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        if (hasImage)
        {
            container.MaxHeight(260).Image(path!).FitArea();
            return;
        }

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Text(fallback)
            .FontColor(Colors.Grey.Darken1);
    }

    private static void BuildQuestionTable(IContainer container, IReadOnlyList<ReportQuestionRow> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(52);
                columns.ConstantColumn(62);
                columns.ConstantColumn(58);
                columns.RelativeColumn();
                columns.ConstantColumn(56);
                columns.ConstantColumn(40);
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).Text("Domain").SemiBold();
                header.Cell().BorderBottom(1).Text("Category").SemiBold();
                header.Cell().BorderBottom(1).Text("Code").SemiBold();
                header.Cell().BorderBottom(1).Text("Question").SemiBold();
                header.Cell().BorderBottom(1).Text("Level").SemiBold();
                header.Cell().BorderBottom(1).Text("Evd").SemiBold();
            });

            foreach (var row in rows)
            {
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.DomainCode);
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.CategoryCode);
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.QuestionCode);
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.QuestionText);
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.Level);
                table.Cell().BorderBottom(1).PaddingVertical(2).Text(row.EvidenceCount.ToString());
            }
        });
    }
}
