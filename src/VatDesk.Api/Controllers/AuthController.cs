using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VatDesk.Api.Auth;
using VatDesk.Api.Dtos;
using VatDesk.Infrastructure.Persistence.Entities;
using VatDesk.Infrastructure.Persistence.Repositories;

namespace VatDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserRepository users, IJwtTokenService tokenService) : ControllerBase
{
    private static readonly PasswordHasher<UserEntity> Hasher = new();

    // Same generic message for "no such user" and "wrong password" — never lets a caller
    // distinguish account existence from a bad password (security checklist item 9 / no
    // user-enumeration).
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.Login)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Problem(title: InvalidCredentialsMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            return Problem(title: InvalidCredentialsMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Problem(title: InvalidCredentialsMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokenService.IssueToken(user);
        return Ok(new LoginResponseDto(token, expiresAt, user.Id, user.Email, user.DisplayName, user.Role.ToString()));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeDto> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var name = User.FindFirstValue(ClaimTypes.Name)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        return Ok(new MeDto(id, email, name, role));
    }
}
