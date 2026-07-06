using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.DocumentIntelligence.Persistence.Migrations;

/// <summary>
/// Maps PostgreSQL's <c>xmin</c> system column on <c>users</c> as an optimistic concurrency
/// token. <c>xmin</c> exists implicitly on every PostgreSQL table, so no schema change is
/// required — this migration only brings the EF model snapshot in sync with the mapping.
/// </summary>
public partial class UserXminConcurrencyToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty: xmin is a PostgreSQL system column that already exists.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty: system columns are never dropped.
    }
}
