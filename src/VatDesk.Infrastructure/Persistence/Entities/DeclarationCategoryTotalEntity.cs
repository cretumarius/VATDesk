using VatDesk.Core.Models;

namespace VatDesk.Infrastructure.Persistence.Entities;

public class DeclarationCategoryTotalEntity
{
    public Guid DeclarationId { get; set; }
    public string VatCode { get; set; } = null!;
    public Direction Direction { get; set; }
    public int RowCount { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public DeclarationEntity? Declaration { get; set; }
}
