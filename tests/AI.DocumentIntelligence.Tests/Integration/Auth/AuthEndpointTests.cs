using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Application.Features.Auth.Refresh;
using AI.DocumentIntelligence.Domain.Enums;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Integration.Auth;

/// <summary>
/// Integration tests for auth endpoints using <see cref="ApiWebApplicationFactory"/>.
/// Repositories are in-memory; no real network or database calls are made.
/// </summary>
[Collection("Integration")]
public sealed class AuthEndpointTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- POST /api/v1/auth/login ----

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var user = _factory.SeedAnalystUser("analyst-login@test.com");
        var client = _factory.CreateClient();

        var body = new LoginCommand("analyst-login@test.com", "Password@123!");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var body = new LoginCommand("nobody@example.com", "Password@123!");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        _factory.SeedAnalystUser("analyst-wrongpw@test.com");
        var client = _factory.CreateClient();

        var body = new LoginCommand("analyst-wrongpw@test.com", "WrongPassword@1");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400()
    {
        var client = _factory.CreateClient();

        var body = new LoginCommand("not-an-email", "Password@123!");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var body = new LoginCommand("user@example.com", "");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns403()
    {
        // Arrange: create an inactive user
        var user = _factory.SeedAnalystUser("inactive-user@test.com");
        user.Deactivate();
        var client = _factory.CreateClient();

        // Act
        var body = new LoginCommand("inactive-user@test.com", "Password@123!");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- POST /api/v1/auth/refresh ----

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();

        var body = new RefreshTokenCommand("invalid-refresh-token-that-does-not-exist");
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_Returns400()
    {
        var client = _factory.CreateClient();

        var body = new RefreshTokenCommand("");
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- POST /api/v1/auth/logout ----

    [Fact]
    public async Task Logout_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidJwt_Returns204()
    {
        var user = _factory.SeedAnalystUser("analyst-logout@test.com");
        var client = _factory.CreateAuthenticatedClient(user);

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---- POST /api/v1/auth/register ----

    [Fact]
    public async Task Register_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var body = new
        {
            email = "new-user@test.com",
            password = "Password@123!",
            fullName = "New User",
            role = (int)UserRole.Analyst,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithAnalystRole_Returns403()
    {
        var analyst = _factory.SeedAnalystUser("analyst-reg@test.com");
        var client = _factory.CreateAuthenticatedClient(analyst);

        var body = new
        {
            email = "new-user@test.com",
            password = "Password@123!",
            fullName = "New User",
            role = (int)UserRole.Analyst,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithAdminRole_CreatesUser_Returns201()
    {
        var admin = _factory.SeedAdminUser("admin-reg@test.com");
        var client = _factory.CreateAuthenticatedClient(admin);

        var body = new
        {
            email = $"newuser-{Guid.NewGuid():N}@test.com",
            password = "Password@123!",
            fullName = "New Integration User",
            role = (int)UserRole.Analyst,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var admin = _factory.SeedAdminUser("admin-dup-reg@test.com");
        // Seed an existing user with the same email that will be registered.
        _factory.SeedAnalystUser("duplicate@test.com");
        var client = _factory.CreateAuthenticatedClient(admin);

        var body = new
        {
            email = "duplicate@test.com",
            password = "Password@123!",
            fullName = "Duplicate User",
            role = (int)UserRole.Analyst,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
