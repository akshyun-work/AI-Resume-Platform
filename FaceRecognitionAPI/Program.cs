using System.Text;
using FaceRecognitionAPI.Data;
using FaceRecognitionAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services;
using ResumeAnalysis.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Controllers / Swagger
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================
// Database
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// ============================================================
// JWT configuration
// ============================================================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSettings =
    builder.Configuration
        .GetSection("Jwt")
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Jwt configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) ||
    jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters.");
}

// ============================================================
// JWT authentication
// ============================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key))
            };
    });

// ============================================================
// JWT token service
// ============================================================

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();

// ============================================================
// Face recognition services
// ============================================================

builder.Services.AddScoped<
    FaceRecognitionService>();

builder.Services.AddHostedService<
    FastApiHostedService>();

builder.Services.AddHostedService<
    AnnIndexSyncHostedService>();

builder.Services.AddHttpClient<
    PythonFaceService>(client =>
    {
        client.BaseAddress =
            new Uri("http://localhost:8000");
    });

// ============================================================
// Build application
// ============================================================

var app = builder.Build();

// ============================================================
// Development tools
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// HTTP pipeline
// ============================================================



app.UseCors("Frontend");

// Authentication must come before Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();