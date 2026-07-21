using Microsoft.EntityFrameworkCore;
using VatDesk.Core.Models;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persists a built DeclarationSummary as aggregates + validation issues only — raw invoice
/// rows are never stored (see architecture.md persistence schema / README PII tradeoff note).
/// </summary>
public class DeclarationRepository(VatDeskDbContext dbContext)
{
    public async Task<DeclarationEntity> SaveAsync(
        string sourceFilename,
        SourceFormat sourceFormat,
        string countryCode,
        DeclarationSummary summary,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = new DeclarationEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceFilename = sourceFilename,
            SourceFormat = sourceFormat,
            CountryCode = countryCode,
            Status = DeclarationStatusCalculator.FromValidation(summary.Validation),
            TotalOutputVat = summary.TotalOutputVat,
            TotalInputVat = summary.TotalDeductibleInputVat,
            NetVatPayable = summary.NetVatPayable,
            ValidRows = summary.Validation.ValidRows,
            WarningRows = summary.Validation.WarningRows,
            ErrorRows = summary.Validation.ErrorRows,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        entity.CategoryTotals.AddRange(summary.PerCategory.Select(c => new DeclarationCategoryTotalEntity
        {
            DeclarationId = entity.Id,
            VatCode = c.VatCode,
            Direction = c.Direction,
            RowCount = c.RowCount,
            TotalNet = c.TotalNet,
            TotalVat = c.TotalVat,
            TotalGross = c.TotalGross,
        }));

        entity.ValidationIssues.AddRange(summary.Validation.Issues.Select(i => new ValidationIssueEntity
        {
            DeclarationId = entity.Id,
            RowNumber = i.RowNumber,
            RuleId = i.RuleId,
            Severity = i.Severity,
            Message = i.Message,
        }));

        dbContext.Declarations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public Task<DeclarationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Declarations
            .AsNoTracking()
            .Include(d => d.CategoryTotals)
            .Include(d => d.ValidationIssues)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<List<DeclarationEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        dbContext.Declarations
            .AsNoTracking()
            .Include(d => d.User)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
}
