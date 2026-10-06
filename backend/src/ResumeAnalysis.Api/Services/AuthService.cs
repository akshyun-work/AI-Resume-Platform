using System.Security.Cryptography;
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
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IJwtTokenService jwt,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _emailService = emailService;
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
                CreatedAt = candidate.CreatedAt,
                HasFaceRegistered = false
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
        var hasFace = await _db.FaceEmbeddings.AnyAsync(f => f.CandidateId == candidate.Id, ct);

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
                CreatedAt = candidate.CreatedAt,
                HasFaceRegistered = hasFace
            }
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
        if (candidate == null)
        {
            _logger.LogWarning("Forgot password requested for non-existent email {Email}", normalizedEmail);
            // Throw friendly message or silent return
            throw new InvalidOperationException("No account found with this email address.");
        }

        // Invalidate previous unused OTPs for this email
        var existingOtps = await _db.PasswordResetOtps
            .Where(o => o.Email == normalizedEmail && !o.IsUsed)
            .ToListAsync(ct);

        foreach (var oldOtp in existingOtps)
        {
            oldOtp.IsUsed = true;
        }

        // Generate 6-digit numeric OTP
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

        var resetRecord = new PasswordResetOtp
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.PasswordResetOtps.Add(resetRecord);
        await _db.SaveChangesAsync(ct);

        // Send email
        await _emailService.SendPasswordResetOtpAsync(candidate.Email, candidate.FullName, otp, ct);

        _logger.LogInformation("Password reset OTP generated and sent for candidate {CandidateId} {Email}", candidate.Id, candidate.Email);
    }

    public async Task<bool> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedOtp = request.Otp.Trim();

        var validOtp = await _db.PasswordResetOtps
            .Where(o => o.Email == normalizedEmail && o.Otp == normalizedOtp && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (validOtp == null)
        {
            _logger.LogWarning("Invalid or expired OTP attempt for {Email}", normalizedEmail);
            throw new InvalidOperationException("The verification code is invalid or has expired. Please request a new one.");
        }

        return true;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedOtp = request.Otp.Trim();

        var validOtp = await _db.PasswordResetOtps
            .Where(o => o.Email == normalizedEmail && o.Otp == normalizedOtp && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (validOtp == null)
        {
            _logger.LogWarning("Reset password attempt with invalid OTP for {Email}", normalizedEmail);
            throw new InvalidOperationException("The verification code is invalid or has expired. Please request a new one.");
        }

        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
        if (candidate == null)
        {
            throw new InvalidOperationException("Candidate account not found.");
        }

        // Update password
        candidate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        candidate.UpdatedAt = DateTime.UtcNow;

        // Mark OTP as used
        validOtp.IsUsed = true;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset successfully completed for candidate {CandidateId} {Email}", candidate.Id, candidate.Email);
    }
}
