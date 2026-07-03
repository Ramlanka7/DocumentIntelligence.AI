using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AI.DocumentIntelligence.Tests.Integration;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for integration tests.
/// Replaces real infrastructure (repositories, AI, search, storage) with in-memory fakes,
/// and overrides the JWT configuration so the app starts without real secrets.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>The JWT signing key used by both the factory (to mint tokens) and the API.</summary>
    public const string TestJwtSecretKey = "TestSecretKey_At_Least_32_Characters_Long_For_HS256";

    public const string TestIssuer = "DocumentIntelligence";
    public const string TestAudience = "DocumentIntelligenceClient";

    /// <summary>In-memory user repository shared across the lifetime of the test server.</summary>
    public InMemoryUserRepository UserRepository { get; } = new();

    /// <summary>In-memory document repository shared across the lifetime of the test server.</summary>
    public InMemoryDocumentRepository DocumentRepository { get; } = new();

    public ApiWebApplicationFactory()
    {
        // Program.cs validates Jwt:SecretKey during service registration (before builder.Build()),
        // so ConfigureAppConfiguration overrides arrive too late via DeferredHostBuilder.
        // Environment variables are part of the WebApplication.CreateBuilder() default pipeline
        // and are therefore present when Program.cs reads the configuration.
        // Note: double-underscore (__) is the .NET environment variable separator for nested config.
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestAudience);
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiryDays", "7");
        Environment.SetEnvironmentVariable("AzureOpenAI__Endpoint", "https://stub.openai.azure.com/");
        Environment.SetEnvironmentVariable("AzureOpenAI__ApiKey", "stub-api-key");
        Environment.SetEnvironmentVariable("AzureSearch__Endpoint", "https://stub.search.windows.net");
        Environment.SetEnvironmentVariable("AzureSearch__ApiKey", "stub-search-key");
        Environment.SetEnvironmentVariable("ApplicationInsights__ConnectionString", "");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "");
        Environment.SetEnvironmentVariable("Anthropic__ApiKey", "stub-anthropic-key");
        Environment.SetEnvironmentVariable("OpenAI__ApiKey", "stub-openai-key");
        Environment.SetEnvironmentVariable("Ollama__BaseUrl", "http://localhost:11434");
        Environment.SetEnvironmentVariable("AzureStorage__ConnectionString", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("AzureStorage__ContainerName", "test-docs");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ---- Repositories: replace stubs with in-memory implementations ----
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IDocumentRepository>();
            services.RemoveAll<IUnitOfWork>();

            services.AddSingleton<IUserRepository>(UserRepository);
            services.AddSingleton<IDocumentRepository>(DocumentRepository);
            services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

            // ---- AI / search / storage: replace real clients with no-op stubs ----
            services.RemoveAll<IAIProvider>();
            services.AddSingleton<IAIProvider, StubAIProvider>();

            services.RemoveAll<ISearchService>();
            services.AddSingleton<ISearchService, StubSearchService>();

            services.RemoveAll<IEmbeddingService>();
            services.AddSingleton<IEmbeddingService, StubEmbeddingService>();

            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage, StubFileStorage>();

            services.RemoveAll<IAuditService>();
            services.AddSingleton<IAuditService, StubAuditService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Remove env vars set in constructor so they do not leak into other test processes.
            Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
            Environment.SetEnvironmentVariable("Jwt__Issuer", null);
            Environment.SetEnvironmentVariable("Jwt__Audience", null);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", null);
            Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiryDays", null);
            Environment.SetEnvironmentVariable("AzureOpenAI__Endpoint", null);
            Environment.SetEnvironmentVariable("AzureOpenAI__ApiKey", null);
            Environment.SetEnvironmentVariable("AzureSearch__Endpoint", null);
            Environment.SetEnvironmentVariable("AzureSearch__ApiKey", null);
            Environment.SetEnvironmentVariable("ApplicationInsights__ConnectionString", null);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
            Environment.SetEnvironmentVariable("Anthropic__ApiKey", null);
            Environment.SetEnvironmentVariable("OpenAI__ApiKey", null);
            Environment.SetEnvironmentVariable("Ollama__BaseUrl", null);
            Environment.SetEnvironmentVariable("AzureStorage__ConnectionString", null);
            Environment.SetEnvironmentVariable("AzureStorage__ContainerName", null);
        }

        base.Dispose(disposing);
    }

    // ---- JWT token minting helpers ----

    /// <summary>
    /// Mints a signed JWT that the test API will accept, with the given role.
    /// </summary>
    public string MintToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Creates an <see cref="HttpClient"/> pre-authenticated as the supplied user.</summary>
    public HttpClient CreateAuthenticatedClient(User user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", MintToken(user));
        return client;
    }

    /// <summary>Creates a test Admin user and seeds it in the in-memory user repository.</summary>
    public User SeedAdminUser(string email = "admin@example.com", string password = "Password@123!")
    {
        var user = User.Create(email, BCrypt.Net.BCrypt.HashPassword(password), "Test Admin", UserRole.Admin);
        UserRepository.Seed([user]);
        return user;
    }

    /// <summary>Creates a test Analyst user and seeds it in the in-memory user repository.</summary>
    public User SeedAnalystUser(string email = "analyst@example.com", string password = "Password@123!")
    {
        var user = User.Create(email, BCrypt.Net.BCrypt.HashPassword(password), "Test Analyst", UserRole.Analyst);
        UserRepository.Seed([user]);
        return user;
    }

    /// <summary>Creates a test Viewer user and seeds it in the in-memory user repository.</summary>
    public User SeedViewerUser(string email = "viewer@example.com", string password = "Password@123!")
    {
        var user = User.Create(email, BCrypt.Net.BCrypt.HashPassword(password), "Test Viewer", UserRole.Viewer);
        UserRepository.Seed([user]);
        return user;
    }
}
