using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Infrastructure.Persistence.Repositories;

/// <summary>Reconstructs the Core DeclarationSummary shape from a persisted entity (e.g. for PDF rendering).</summary>
public static class DeclarationEntityExtensions
{
    public static DeclarationSummary ToSummary(this DeclarationEntity entity, IVatCategoryRegistry registry)
    {
        var perCategory = entity.CategoryTotals
            .OrderBy(c => c.Direction)
            .ThenBy(c => registry.TryGet(c.VatCode, out var category) ? category.SortOrder : int.MaxValue)
            .Select(c => new CategoryTotal(c.VatCode, c.Direction, c.RowCount, c.TotalNet, c.TotalVat, c.TotalGross))
            .ToList();

        var issues = entity.ValidationIssues
            .OrderBy(i => i.RowNumber)
            .ThenBy(i => i.RuleId, StringComparer.Ordinal)
            .Select(i => new ValidationIssue(i.RowNumber, i.RuleId, i.Severity, i.Message))
            .ToList();

        return new DeclarationSummary(
            perCategory,
            entity.TotalOutputVat,
            entity.TotalInputVat,
            entity.NetVatPayable,
            new ValidationSummary(entity.ValidRows, entity.WarningRows, entity.ErrorRows, issues));
    }
}
