using VatDesk.Core.Models;

namespace VatDesk.Infrastructure.Persistence.Entities;

public class DeclarationEntity
{
    public Guid Id { get; set; }

    // Nullable: this session ships no auth (Phase 5), so uploads are anonymous. Becomes
    // required once JWT auth lands and every request carries an authenticated user.
    public Guid? UserId { get; set; }
    public string SourceFilename { get; set; } = null!;
    public SourceFormat SourceFormat { get; set; }
    public string CountryCode { get; set; } = null!;
    public DeclarationStatus Status { get; set; }
    public decimal TotalOutputVat { get; set; }
    public decimal TotalInputVat { get; set; }
    public decimal NetVatPayable { get; set; }
    public int ValidRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity? User { get; set; }
    public List<DeclarationCategoryTotalEntity> CategoryTotals { get; set; } = [];
    public List<ValidationIssueEntity> ValidationIssues { get; set; } = [];
}
