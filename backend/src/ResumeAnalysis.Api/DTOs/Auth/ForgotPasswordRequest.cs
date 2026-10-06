using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
