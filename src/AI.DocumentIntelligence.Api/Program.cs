using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AI.DocumentIntelligence.Api.Infrastructure;
using AI.DocumentIntelligence.Api.Middleware;
using AI.DocumentIntelligence.Api.Observability;
using AI.DocumentIntelligence.Application;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Infrastructure;
using AI.DocumentIntelligence.Infrastructure.Auth;
using AI.DocumentIntelligence.Persistence;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Structured logging (Serilog) ----
// Must be first so that startup errors are captured with the bootstrap logger.
builder.AddSerilogLogging();

// ---- Core MVC / OpenAPI ----
// Keep the "Async" suffix in action names so that cross-action link generation
// (CreatedAtAction(nameof(...Async))) resolves correctly instead of throwing at runtime.
builder.Services
    .AddControllers(options => options.SuppressAsyncSuffixInActionNames = false)
    // Serialize/deserialize enums by their string names so the Angular client can send and read
    // enum values as strings (e.g. chat AiRole, DifferenceType, statuses) instead of magic integers.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// ---- Swagger with JWT auth ----
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Document Intelligence API",
        Version = "v1",
        Description = "AI-powered document analysis, comparison, and chat platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// ---- CORS — origins loaded from config so production can restrict to the real hostname ----
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()));

// ---- Request size limits (aligned with UploadOptions) ----
builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

// ---- Application + Infrastructure + Persistence ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// ---- OpenTelemetry (traces + metrics) ----
// Exporters (OTLP, Azure Monitor) activate only when their config is present.
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

// ---- Health checks ----
// Individual checks are registered by AddInfrastructure() and AddPersistence().
// AddHealthChecks() here ensures the health check service is registered in case
// the order of DI extension calls changes in future.
builder.Services.AddHealthChecks();

// ---- HTTP context accessor (required by CurrentUserService) ----
builder.Services.AddHttpContextAccessor();

// ---- ICurrentUser resolved from HTTP context ----
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

// ---- JWT authentication ----
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
const string KeyPlaceholder = "REPLACE_WITH_256BIT_SECRET_IN_USER_SECRETS";
var secretKey = jwtSection["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) || secretKey == KeyPlaceholder)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey is not configured. Supply it via user-secrets or an environment variable.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

// ---- Authorization policies ----
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("AnalystOrAbove", p => p.RequireRole("Admin", "Analyst"));
    opts.AddPolicy("ViewerOrAbove", p => p.RequireRole("Admin", "Analyst", "Viewer"));
});

// ---- Rate limiting ----
// GlobalLimiter partitions by authenticated user ID (falling back to IP for anonymous callers)
// so that one user's traffic cannot exhaust the quota of another.
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey =
            context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });

    // Tighter named limiter applied to auth endpoints specifically.
    opts.AddFixedWindowLimiter("AuthEndpoints", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
});

// ---- API versioning ----
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

var app = builder.Build();

// ---- Database migration ----
// Only runs when a real connection string is present (skipped in tests that use in-memory fakes).
var dbConnectionString = app.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(dbConnectionString))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// ---- Global exception handler (must be outermost) ----
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Intelligence API v1");
        c.RoutePrefix = "swagger";
    });
}

// ---- Correlation ID (before Serilog request logging so the ID appears in every log event) ----
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

// ---- Serilog request logging (after CorrelationId so it enriches the completion log) ----
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdKey, out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// ---- CORS (before auth) ----
app.UseCors();

// ---- Auth middleware ordering is critical ----
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ---- Audit middleware (after auth so the principal is populated) ----
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();

// ---- Health check endpoints ----
// /health/live  — liveness: just verifies the process is up (no external checks).
// /health/ready — readiness: verifies DB, Search, and AI provider connectivity.
// /health       — full report: combines all registered checks.
var healthCheckOptions = new HealthCheckOptions
{
    ResponseWriter = ObservabilityExtensions.WriteHealthCheckResponseAsync,
};

app.MapHealthChecks("/health", healthCheckOptions).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks: just confirms the process responds.
    ResponseWriter = ObservabilityExtensions.WriteHealthCheckResponseAsync,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = ObservabilityExtensions.WriteHealthCheckResponseAsync,
}).AllowAnonymous();

app.Run();

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program;
