namespace VatDesk.Api.Dtos;

public record VatCategoryDto(
    string Code,
    string Kind,
    decimal? Rate,
    string DisplayNameHu,
    string DisplayNameEn,
    int SortOrder);
