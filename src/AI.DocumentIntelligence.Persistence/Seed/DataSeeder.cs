using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Persistence.Seed;

/// <summary>
/// Idempotent seeder that creates the initial admin user so a fresh deployment can be
/// logged into (registration itself requires an existing admin).
///
/// The admin password comes from <c>Seed:AdminPassword</c> configuration and is stored
/// only as a BCrypt hash via <see cref="IPasswordHasher"/> — never in plaintext.
/// In Production a missing password is a hard startup error when seeding is enabled;
/// in other environments seeding is skipped with a warning.
/// Requires the database schema to already be migrated.
/// </summary>
internal sealed partial class DataSeeder
{
    private const string DefaultAdminEmail = "admin@documentintelligence.local";

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        AppDbContext context,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        IHostEnvironment environment,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Seeds default data. Safe to call on every startup — all operations are idempotent.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminEmail = _configuration["Seed:AdminEmail"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            adminEmail = DefaultAdminEmail;
        }

        adminEmail = adminEmail.Trim().ToLowerInvariant();

        var exists = await _context.Users
            .AnyAsync(u => u.Email == adminEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        var rawPassword = _configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(rawPassword))
        {
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Seed:AdminPassword is not configured. Database seeding is enabled "
                    + "(Database:SeedOnStartup) but no admin password was supplied. "
                    + "Set Seed__AdminPassword via your secret store — the platform has no "
                    + "other way to create the first user.");
            }

            LogAdminSeedSkipped(adminEmail);
            return;
        }

        var passwordHash = _passwordHasher.Hash(rawPassword);

        var admin = User.Create(adminEmail, passwordHash, "Platform Admin", UserRole.Admin);

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        LogAdminUserSeeded(adminEmail);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin user seeded: {Email}")]
    private partial void LogAdminUserSeeded(string email);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Admin user '{Email}' not seeded: Seed:AdminPassword is not configured. "
            + "Set it and restart, or create the user manually.")]
    private partial void LogAdminSeedSkipped(string email);
}
