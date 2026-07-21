using VatDesk.Core.Models;

namespace VatDesk.Core.Abstractions;

/// <summary>Renders a declaration summary to a downloadable PDF (QuestPDF in Infrastructure).</summary>
public interface IReportRenderer
{
    Task<byte[]> RenderAsync(
        DeclarationSummary summary,
        DeclarationMetadata metadata,
        CancellationToken cancellationToken = default);
}

public record DeclarationMetadata(
    Guid DeclarationId,
    string SourceFilename,
    DateTimeOffset GeneratedAt);
