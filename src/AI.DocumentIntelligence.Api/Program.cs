using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AI.DocumentIntelligence.Api.Infrastructure;
using AI.DocumentIntelligence.Api.Middleware;
using AI.DocumentIntelligence.Application;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Infrastructure;
using AI.DocumentIntelligence.Infrastructure.Auth;
using AI.DocumentIntelligence.Persistence;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Core MVC / OpenAPI ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

// ---- Application + Infrastructure + Persistence ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence();

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

// ---- Global exception handler (must be outermost) ----
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ---- Auth middleware ordering is critical ----
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ---- Audit middleware (after auth so the principal is populated) ----
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program;
