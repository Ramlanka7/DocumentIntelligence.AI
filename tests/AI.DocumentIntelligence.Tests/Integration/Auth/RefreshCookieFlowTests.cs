using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Integration.Auth;

/// <summary>
/// End-to-end tests for the HttpOnly refresh-token cookie flow: login issues the cookie,
/// refresh consumes and rotates it with an empty request body (the browser scenario), and
/// the cookie is scoped so it never travels outside the /auth endpoints.
/// </summary>
[Collection("Integration")]
public sealed class RefreshCookieFlowTests
{
    private readonly ApiWebApplicationFactory _factory;

    public RefreshCookieFlowTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginThenRefreshWithEmptyBody_UsesCookieAndRotatesIt()
    {
        _factory.SeedAnalystUser("cookie-flow@test.com");

        // WebApplicationFactory's default client handles cookies like a browser. The refresh
        // cookie is marked Secure, so the client must talk over https for the cookie
        // container to send it back (exactly as a real browser would).
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        // 1. Login — establishes the refresh_token cookie.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand("cookie-flow@test.com", "Password@123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstAccessToken = loginJson.GetProperty("accessToken").GetString();
        firstAccessToken.Should().NotBeNullOrWhiteSpace();

        loginResponse.Headers.TryGetValues("Set-Cookie", out var loginCookies).Should().BeTrue();
        var loginCookie = loginCookies!.Single(c => c.StartsWith("refresh_token="));
        loginCookie.Should().ContainEquivalentOf("httponly");
        loginCookie.Should().ContainEquivalentOf("samesite=strict");
        loginCookie.Should().ContainEquivalentOf("path=/api/v1/auth");

        // 2. Refresh with an EMPTY body — the token must come from the cookie alone.
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        refreshJson.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        refreshJson.GetProperty("refreshToken").GetString().Should().BeEmpty();

        // The cookie is rotated on every refresh (new Set-Cookie issued).
        refreshResponse.Headers.TryGetValues("Set-Cookie", out var refreshCookies).Should().BeTrue();
        refreshCookies.Should().Contain(c => c.StartsWith("refresh_token="));

        // 3. The rotated cookie keeps working: a second empty-body refresh succeeds too.
        var secondRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshWithoutCookieOrBody_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });

        // No cookie, no body token → validation/auth failure, never a 200.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ExpiresTheRefreshCookie()
    {
        var user = _factory.SeedAnalystUser("cookie-logout@test.com");
        var client = _factory.CreateAuthenticatedClient(user);

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Deletion is expressed as a Set-Cookie with an expiry in the past.
        logoutResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cleared = cookies!.Single(c => c.StartsWith("refresh_token="));
        cleared.Should().ContainEquivalentOf("expires=");
    }
}
