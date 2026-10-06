using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Candidate registration</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registration successful."));
    }

    /// <summary>Candidate login</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
    }

    /// <summary>Request password reset OTP via email</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse<bool>.Ok(true, "A 6-digit verification code has been sent to your email."));
    }

    /// <summary>Verify password reset OTP</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        var result = await _auth.VerifyOtpAsync(request, ct);
        return Ok(ApiResponse<bool>.Ok(result, "Verification code is valid."));
    }

    /// <summary>Reset password using verified OTP</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Password has been reset successfully. Please sign in with your new password."));
    }
}
