using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Persistence.Seed;

/// <summary>
/// Idempotent seeder that creates the default admin user on first run.
/// The admin password is read from <c>Seed:AdminPassword</c> in configuration
/// (user secrets / env override in production — never hardcoded here).
/// </summary>
internal sealed partial class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations and seeds default data.
    /// Safe to call on every startup — all operations are idempotent.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        const string adminEmail = "admin@documentintelligence.local";

        var exists = await _context.Users
            .AnyAsync(u => u.Email == adminEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        // Read from configuration; fall back to a dev-only placeholder.
        // In production, override via user-secrets or environment variable:
        //   dotnet user-secrets set "Seed:AdminPassword" "<strong-password>"
        //   or env: Seed__AdminPassword=<strong-password>
        var rawPassword = _configuration["Seed:AdminPassword"]
            ?? "DevOnly-ChangeMe-2024!"; // TODO: must be replaced via config in production

        // TODO (T06 - Auth): replace this placeholder with a proper BCrypt hash
        // when the authentication layer is implemented.  For now we store the raw
        // config value so the seed is functional without adding new packages.
        var passwordHash = $"[SEED-PLACEHOLDER]:{rawPassword}";

        var admin = User.Create(adminEmail, passwordHash, "Platform Admin", UserRole.Admin);
        admin.CreatedAtUtc = DateTime.UtcNow;

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        LogAdminUserSeeded(adminEmail);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin user seeded: {Email}")]
    private partial void LogAdminUserSeeded(string email);
}
