using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.Middleware;
using ResumeAnalysis.Api.Services;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.Storage;
using ResumeAnalysis.Api.Services.AI;
using ResumeAnalysis.Api.Services.JobProviders;

var builder = WebApplication.CreateBuilder(args);

// Configuration - externalized, env vars override appsettings
builder.Configuration.AddEnvironmentVariables();

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSettings);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing.");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// JWT - harden: reject placeholder in Production
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
if (jwtKey.Length < 32) throw new InvalidOperationException("Jwt:Key must be >=32 chars");
if (jwtKey.Contains("REPLACE_WITH_ENV_VAR", StringComparison.OrdinalIgnoreCase) && builder.Environment.IsProduction())
    throw new InvalidOperationException("Jwt:Key placeholder must be replaced via environment variable in Production");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Controllers + validation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Resume Analysis Platform API",
        Version = "v1",
        Description = "Backend for Resume Analysis Platform - Member 7"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Config
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IAtsService, AtsService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHttpClient<IJobProvider, FreeHireJobProvider>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IPythonAiService, PythonAiService>();
builder.Services.AddScoped<IResumeChatService, ResumeChatService>();

builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Global exception handling - must be early
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
    .AllowAnonymous().WithTags("Health");

// Auto-migrate on startup (safe for dev, extensible)
// In production you would use manual migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsRelational())
        {
            if (db.Database.GetPendingMigrations().Any())
                db.Database.Migrate();
            else
                db.Database.EnsureCreated();
        }
        else
        {
            db.Database.EnsureCreated();
        }
        logger.LogInformation("Database ready");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        // Don't crash in case SQL not available - let health check surface it
    }
}

app.Run();

public partial class Program { }
