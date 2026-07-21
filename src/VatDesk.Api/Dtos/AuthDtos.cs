namespace VatDesk.Api.Dtos;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string Name,
    string Role);

public record MeDto(
    Guid UserId,
    string Email,
    string Name,
    string Role);
