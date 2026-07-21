using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Infrastructure.Pdf;

/// <summary>
/// A4 declaration summary PDF per architecture.md's PDF section: header, summary cards,
/// category table (OUT block then IN block), validation summary, disclaimer footer, page numbers.
/// </summary>
public class QuestPdfReportRenderer : IReportRenderer
{
    private static readonly NumberFormatInfo HufFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalDigits = 0,
    };

    public Task<byte[]> RenderAsync(DeclarationSummary summary, DeclarationMetadata metadata, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, metadata));
                page.Content().Element(c => ComposeContent(c, summary));
                page.Footer().Element(ComposeFooter);
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static void ComposeHeader(QuestPDF.Infrastructure.IContainer container, DeclarationMetadata metadata)
    {
        container.PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().Text("VATDesk — VAT Declaration Summary").FontSize(16).Bold();
            column.Item().Text($"Declaration ID: {metadata.DeclarationId}").FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text($"Generated: {metadata.GeneratedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}")
                .FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text($"Source file: {metadata.SourceFilename}").FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeContent(QuestPDF.Infrastructure.IContainer container, DeclarationSummary summary)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => SummaryCard(c, "Total Output VAT", summary.TotalOutputVat));
                row.RelativeItem().Element(c => SummaryCard(c, "Total Deductible Input VAT", summary.TotalDeductibleInputVat));
                row.RelativeItem().Element(c => SummaryCard(c, "Net VAT Payable", summary.NetVatPayable));
            });

            column.Item().Element(c => CategoryTable(c, summary.PerCategory));
            column.Item().Element(c => ValidationSection(c, summary.Validation));
        });
    }

    private static void SummaryCard(QuestPDF.Infrastructure.IContainer container, string label, decimal amount)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(2).Text(FormatHuf(amount)).FontSize(14).Bold();
        });
    }

    private static void CategoryTable(QuestPDF.Infrastructure.IContainer container, IReadOnlyList<CategoryTotal> categories)
    {
        container.Column(column =>
        {
            column.Item().Text("Category breakdown").FontSize(12).Bold();

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "VAT Code");
                    HeaderCell(header.Cell(), "Rows");
                    HeaderCell(header.Cell().AlignRight(), "Net");
                    HeaderCell(header.Cell().AlignRight(), "VAT");
                    HeaderCell(header.Cell().AlignRight(), "Gross");
                });

                Direction? currentDirection = null;
                foreach (var category in categories)
                {
                    if (category.Direction != currentDirection)
                    {
                        currentDirection = category.Direction;
                        var label = currentDirection == Direction.Out ? "Sales (Output VAT)" : "Purchases (Input VAT)";
                        table.Cell().ColumnSpan(5).PaddingTop(8).PaddingBottom(2)
                            .Text(label).Bold().FontColor(Colors.Blue.Darken2);
                    }

                    table.Cell().Text(category.VatCode);
                    table.Cell().Text(category.RowCount.ToString(CultureInfo.InvariantCulture));
                    table.Cell().AlignRight().Text(FormatHuf(category.TotalNet));
                    table.Cell().AlignRight().Text(FormatHuf(category.TotalVat));
                    table.Cell().AlignRight().Text(FormatHuf(category.TotalGross));
                }

                if (categories.Count == 0)
                {
                    table.Cell().ColumnSpan(5).PaddingTop(4).Text("No rows were included in totals.").Italic();
                }
            });
        });
    }

    private static void ValidationSection(QuestPDF.Infrastructure.IContainer container, ValidationSummary validation)
    {
        container.Column(column =>
        {
            column.Item().Text("Validation summary").FontSize(12).Bold();
            column.Item().PaddingTop(4).Text(
                $"Valid rows: {validation.ValidRows}    Warnings: {validation.WarningRows}    Errors: {validation.ErrorRows}");

            if (validation.Issues.Count == 0)
            {
                return;
            }

            column.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(60);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Row");
                    HeaderCell(header.Cell(), "Rule");
                    HeaderCell(header.Cell(), "Severity");
                    HeaderCell(header.Cell(), "Message");
                });

                foreach (var issue in validation.Issues)
                {
                    table.Cell().Text(issue.RowNumber == 0 ? "-" : issue.RowNumber.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Text(issue.RuleId);
                    table.Cell().Text(issue.Severity.ToString());
                    table.Cell().Text(issue.Message).FontSize(8);
                }
            });
        });
    }

    private static void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
    {
        container.PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().AlignCenter().Text(
                    "Generated by VATDesk for review purposes. This document is not an official filing with NAV.")
                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            column.Item().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void HeaderCell(QuestPDF.Infrastructure.IContainer container, string text) =>
        container.Text(text).Bold().FontSize(9);

    private static string FormatHuf(decimal amount) => $"{amount.ToString("N0", HufFormat)} Ft";
}
