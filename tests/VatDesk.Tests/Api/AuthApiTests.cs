using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using VatDesk.Api.Dtos;
using VatDesk.Core.Models;

namespace VatDesk.Tests.Api;

public class AuthApiTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public AuthApiTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidAdminCredentials_ReturnsTokenWithAdminRoleClaim()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(VatDeskWebApplicationFactory.AdminEmail, VatDeskWebApplicationFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Token));
        Assert.Equal("Admin", dto.Role);
        Assert.Equal("Nagy Katalin", dto.Name);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.Token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task Login_ValidViewerCredentials_ReturnsTokenWithViewerRoleClaim()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(VatDeskWebApplicationFactory.ViewerEmail, VatDeskWebApplicationFactory.ViewerPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto!.Token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Viewer");
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameGenericMessage()
    {
        using var client = _factory.CreateClient();

        var wrongPasswordResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(VatDeskWebApplicationFactory.AdminEmail, "not-the-password"));
        var unknownEmailResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("nobody@demo.hu", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);

        var wrongPasswordProblem = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        var unknownEmailProblem = await unknownEmailResponse.Content.ReadFromJsonAsync<ProblemDetailsBody>();

        // Never lets a caller distinguish "no such account" from "wrong password".
        Assert.Equal(wrongPasswordProblem!.Title, unknownEmailProblem!.Title);
    }

    [Fact]
    public async Task Login_EmptyEmailOrPassword_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("", ""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsPrincipal()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal(VatDeskWebApplicationFactory.AdminEmail, me!.Email);
        Assert.Equal("Admin", me.Role);
    }

    [Fact]
    public async Task Me_Anonymous_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record ProblemDetailsBody(string? Title, string? Detail, int? Status);
}
