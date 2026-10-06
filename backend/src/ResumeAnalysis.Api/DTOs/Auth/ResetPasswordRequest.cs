using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters."), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
