using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Context;

/// <summary>
/// EF Core database context for the AI Document Intelligence Platform.
/// Wires all domain entities to PostgreSQL with pgvector support.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<AnalysisSession> AnalysisSessions => Set<AnalysisSession>();
    public DbSet<ComparisonSession> ComparisonSessions => Set<ComparisonSession>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiUsageMetric> AiUsageMetrics => Set<AiUsageMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable the pgvector extension.
        modelBuilder.HasPostgresExtension("vector");

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
