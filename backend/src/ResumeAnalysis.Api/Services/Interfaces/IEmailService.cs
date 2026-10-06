namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp, CancellationToken ct = default);
}
