namespace VatDesk.Core.Models;

/// <summary>Derives the persisted declaration status from a built DeclarationSummary's validation counts.</summary>
public static class DeclarationStatusCalculator
{
    public static DeclarationStatus FromValidation(ValidationSummary validation)
    {
        if (validation.ValidRows == 0 && validation.WarningRows == 0 && validation.ErrorRows > 0)
        {
            return DeclarationStatus.Failed;
        }

        return validation.WarningRows > 0 || validation.ErrorRows > 0
            ? DeclarationStatus.CompletedWithWarnings
            : DeclarationStatus.Completed;
    }
}
