using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// The EF Core database context for the AI Document Intelligence Platform.
/// Applies all entity configurations from the Persistence assembly and enables
/// the PostgreSQL <c>vector</c> extension for pgvector semantic-search support.
/// </summary>
internal sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<User> Users => Set<User>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
