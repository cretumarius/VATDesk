namespace VatDesk.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; set; }
    public string Issuer { get; set; } = "VatDesk";
    public string Audience { get; set; } = "VatDesk";
    public int ExpiryMinutes { get; set; } = 30;
}
