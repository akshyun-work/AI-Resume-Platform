using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Auth;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
