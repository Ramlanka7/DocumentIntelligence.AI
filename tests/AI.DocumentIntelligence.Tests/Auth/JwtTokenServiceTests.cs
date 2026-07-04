using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AI.DocumentIntelligence.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SecretKey = "super-secret-key-that-is-at-least-32-bytes-long!!",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7,
    };

    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(Options));

    private static User CreateUser(UserRole role = UserRole.Analyst) =>
        User.Create("test@example.com", "hashedpw", "Test User", role);

    [Fact]
    public void GenerateAccessToken_ProducesValidJwt()
    {
        var svc = CreateService();
        var user = CreateUser();

        var token = svc.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.SecretKey));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Options.Issuer,
            ValidateAudience = true,
            ValidAudience = Options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
        };

        var principal = handler.ValidateToken(token, validationParams, out _);
        principal.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_ContainsUserIdAndEmailAndRoleClaims()
    {
        var svc = CreateService();
        var user = CreateUser(UserRole.Admin);

        var token = svc.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateRefreshToken_ProducesNonEmptyBase64String()
    {
        var svc = CreateService();

        var token = svc.GenerateRefreshToken();

        token.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var svc = CreateService();

        var t1 = svc.GenerateRefreshToken();
        var t2 = svc.GenerateRefreshToken();

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void HashToken_IsDeterministic()
    {
        var svc = CreateService();
        const string plain = "some-refresh-token";

        var h1 = svc.HashToken(plain);
        var h2 = svc.HashToken(plain);

        h1.Should().Be(h2);
    }

    [Fact]
    public void HashToken_DifferentInputProducesDifferentHash()
    {
        var svc = CreateService();

        var h1 = svc.HashToken("token-a");
        var h2 = svc.HashToken("token-b");

        h1.Should().NotBe(h2);
    }

    [Fact]
    public void AccessTokenExpiry_MatchesOptions()
    {
        var svc = CreateService();

        svc.AccessTokenExpiry.Should().Be(TimeSpan.FromMinutes(Options.AccessTokenExpiryMinutes));
    }

    [Fact]
    public void RefreshTokenExpiry_MatchesOptions()
    {
        var svc = CreateService();

        svc.RefreshTokenExpiry.Should().Be(TimeSpan.FromDays(Options.RefreshTokenExpiryDays));
    }
}
