using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext db, IJwtTokenService jwt, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Candidates.AnyAsync(c => c.Email == normalizedEmail, ct);
        if (exists)
        {
            _logger.LogWarning("Registration failed: email already exists {Email}", normalizedEmail);
            throw new InvalidOperationException("Email already registered.");
        }

        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Candidate registered {CandidateId} {Email}", candidate.Id, candidate.Email);

        var (token, expiresAt) = _jwt.GenerateToken(candidate);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Candidate = new CandidateDto
            {
                Id = candidate.Id,
                Email = candidate.Email,
                FullName = candidate.FullName,
                Phone = candidate.Phone,
                CreatedAt = candidate.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
        if (candidate == null || !BCrypt.Net.BCrypt.Verify(request.Password, candidate.PasswordHash))
        {
            _logger.LogWarning("Login failed for {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("Candidate logged in {CandidateId} {Email}", candidate.Id, candidate.Email);

        var (token, expiresAt) = _jwt.GenerateToken(candidate);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Candidate = new CandidateDto
            {
                Id = candidate.Id,
                Email = candidate.Email,
                FullName = candidate.FullName,
                Phone = candidate.Phone,
                CreatedAt = candidate.CreatedAt
            }
        };
    }
}
