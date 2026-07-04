using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace AI.DocumentIntelligence.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    private static readonly string[] AiUsageMetricColumns = ["user_id", "created_at_utc"];
    private static readonly string[] HnswIndexOperators = ["vector_cosine_ops"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:vector", ",,");

        migrationBuilder.CreateTable(
            name: "ai_usage_metrics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                operation_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: true),
                processing_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                token_completion_tokens = table.Column<int>(type: "integer", nullable: false),
                token_estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                token_prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ai_usage_metrics", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "analysis_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                capability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                custom_question = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                executive_summary = table.Column<string>(type: "text", nullable: true),
                processing_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                failure_reason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                action_items = table.Column<List<string>>(type: "jsonb", nullable: false),
                document_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                key_findings = table.Column<List<string>>(type: "jsonb", nullable: false),
                recommendations = table.Column<List<string>>(type: "jsonb", nullable: false),
                risks_identified = table.Column<List<string>>(type: "jsonb", nullable: false),
                token_completion_tokens = table.Column<int>(type: "integer", nullable: false),
                token_estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                token_prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                referenced_sources = table.Column<string>(type: "jsonb", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_analysis_sessions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                details = table.Column<string>(type: "text", nullable: true),
                ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_logs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "chat_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                document_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_chat_sessions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "comparison_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                comparison_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                executive_overview = table.Column<string>(type: "text", nullable: true),
                risk_analysis = table.Column<string>(type: "text", nullable: true),
                processing_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                failure_reason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                document_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                key_differences = table.Column<List<string>>(type: "jsonb", nullable: false),
                recommendations = table.Column<List<string>>(type: "jsonb", nullable: false),
                token_completion_tokens = table.Column<int>(type: "integer", nullable: false),
                token_estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                token_prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                detailed_change_log = table.Column<string>(type: "jsonb", nullable: true),
                source_citations = table.Column<string>(type: "jsonb", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_comparison_sessions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                metadata_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                metadata_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                metadata_page_count = table.Column<int>(type: "integer", nullable: false),
                metadata_content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                storage_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                extracted_text = table.Column<string>(type: "text", nullable: true),
                failure_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_documents", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                refresh_token_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                refresh_token_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "chat_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                chat_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                ordinal = table.Column<int>(type: "integer", nullable: false),
                role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                content = table.Column<string>(type: "text", nullable: false),
                token_completion_tokens = table.Column<int>(type: "integer", nullable: false),
                token_estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                token_prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                citations = table.Column<string>(type: "jsonb", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_chat_messages", x => x.id);
                table.ForeignKey(
                    name: "FK_chat_messages_chat_sessions_chat_session_id",
                    column: x => x.chat_session_id,
                    principalTable: "chat_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "document_chunks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                document_id = table.Column<Guid>(type: "uuid", nullable: false),
                index = table.Column<int>(type: "integer", nullable: false),
                content = table.Column<string>(type: "text", nullable: false),
                page_number = table.Column<int>(type: "integer", nullable: false),
                paragraph_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                token_count = table.Column<int>(type: "integer", nullable: false),
                embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_chunks", x => x.id);
                table.ForeignKey(
                    name: "FK_document_chunks_documents_document_id",
                    column: x => x.document_id,
                    principalTable: "documents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_ai_usage_metrics_user_id_created_at_utc",
            table: "ai_usage_metrics",
            columns: AiUsageMetricColumns);

        migrationBuilder.CreateIndex(
            name: "ix_analysis_sessions_owner_id",
            table: "analysis_sessions",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_created_at_utc",
            table: "audit_logs",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_user_id",
            table: "audit_logs",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_chat_messages_chat_session_id",
            table: "chat_messages",
            column: "chat_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_chat_sessions_owner_id",
            table: "chat_sessions",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "ix_comparison_sessions_owner_id",
            table: "comparison_sessions",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "ix_document_chunks_document_id",
            table: "document_chunks",
            column: "document_id");

        migrationBuilder.CreateIndex(
            name: "ix_document_chunks_embedding_hnsw",
            table: "document_chunks",
            column: "embedding")
            .Annotation("Npgsql:IndexMethod", "hnsw")
            .Annotation("Npgsql:IndexOperators", HnswIndexOperators);

        migrationBuilder.CreateIndex(
            name: "ix_documents_owner_id",
            table: "documents",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ai_usage_metrics");
        migrationBuilder.DropTable(name: "analysis_sessions");
        migrationBuilder.DropTable(name: "audit_logs");
        migrationBuilder.DropTable(name: "chat_messages");
        migrationBuilder.DropTable(name: "comparison_sessions");
        migrationBuilder.DropTable(name: "document_chunks");
        migrationBuilder.DropTable(name: "users");
        migrationBuilder.DropTable(name: "chat_sessions");
        migrationBuilder.DropTable(name: "documents");
    }
}
