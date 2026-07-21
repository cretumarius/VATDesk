using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Api.Dtos;

internal static class DtoMapping
{
    public static VatCategoryDto ToDto(this VatCategory category) => new(
        category.Code,
        category.Kind.ToString(),
        category.Rate,
        category.DisplayNameHu,
        category.DisplayNameEn,
        category.SortOrder);

    /// <summary>
    /// Category total order is not persisted (composite PK has no sequence column), so it's
    /// re-derived here from the registry: OUT block then IN block, by category SortOrder —
    /// the same rule HungarianVatDeclarationStrategy applies when it first builds the summary.
    /// </summary>
    public static DeclarationDto ToDto(this DeclarationEntity entity, IVatCategoryRegistry registry) => new(
        entity.Id,
        entity.SourceFilename,
        entity.SourceFormat.ToString(),
        entity.CountryCode,
        entity.Status.ToString(),
        entity.CreatedAt,
        entity.CategoryTotals
            .OrderBy(c => c.Direction)
            .ThenBy(c => registry.TryGet(c.VatCode, out var category) ? category.SortOrder : int.MaxValue)
            .Select(c => new CategoryTotalDto(c.VatCode, c.Direction.ToString(), c.RowCount, c.TotalNet, c.TotalVat, c.TotalGross))
            .ToList(),
        entity.TotalOutputVat,
        entity.TotalInputVat,
        entity.NetVatPayable,
        new ValidationSummaryDto(
            entity.ValidRows,
            entity.WarningRows,
            entity.ErrorRows,
            entity.ValidationIssues
                .OrderBy(i => i.RowNumber)
                .ThenBy(i => i.RuleId, StringComparer.Ordinal)
                .Select(i => new ValidationIssueDto(i.RowNumber, i.RuleId, i.Severity.ToString(), i.Message))
                .ToList()));

    public static DeclarationListItemDto ToListItemDto(this DeclarationEntity entity) => new(
        entity.Id,
        entity.SourceFilename,
        entity.SourceFormat.ToString(),
        entity.CountryCode,
        entity.Status.ToString(),
        entity.TotalOutputVat,
        entity.TotalInputVat,
        entity.NetVatPayable,
        entity.ValidRows,
        entity.WarningRows,
        entity.ErrorRows,
        entity.CreatedAt,
        entity.User?.DisplayName);
}
