using VatDesk.Core.Validation;

namespace VatDesk.Infrastructure.Persistence.Entities;

public class ValidationIssueEntity
{
    public long Id { get; set; }
    public Guid DeclarationId { get; set; }
    public int RowNumber { get; set; }
    public string RuleId { get; set; } = null!;
    public Severity Severity { get; set; }
    public string Message { get; set; } = null!;

    public DeclarationEntity? Declaration { get; set; }
}
