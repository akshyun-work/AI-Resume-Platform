using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp, CancellationToken ct = default)
    {
        var subject = $"Your Password Reset Code: {otp} - AI Resume Platform";
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "there" : fullName;

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
</head>
<body style='margin: 0; padding: 0; background-color: #0b0f19; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #f8fafc;'>
    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='table-layout: fixed;'>
        <tr>
            <td align='center' style='padding: 40px 15px;'>
                <table border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 520px; background-color: #111827; border: 1px solid #1f2937; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);'>
                    <tr>
                        <td style='padding: 32px 32px 20px 32px; text-align: center; border-bottom: 1px solid #1f2937; background: linear-gradient(180deg, rgba(79, 70, 229, 0.1) 0%, rgba(17, 24, 39, 0) 100%);'>
                            <div style='display: inline-block; width: 48px; height: 48px; line-height: 48px; border-radius: 12px; background: #4f46e5; color: #ffffff; font-size: 22px; font-weight: bold;'>
                                &#x1F512;
                            </div>
                            <h1 style='margin: 16px 0 0 0; font-size: 22px; font-weight: 700; color: #ffffff;'>Password Reset Request</h1>
                            <p style='margin: 8px 0 0 0; font-size: 14px; color: #9ca3af;'>AI-Powered Resume Analysis Platform</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 32px;'>
                            <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: #d1d5db;'>
                                Hello <strong style='color: #ffffff;'>{WebUtility.HtmlEncode(displayName)}</strong>,
                            </p>
                            <p style='margin: 0 0 24px 0; font-size: 14px; line-height: 22px; color: #9ca3af;'>
                                We received a request to reset the password for your account. Use the 6-digit verification code below to complete your password reset:
                            </p>
                            <div style='background-color: #1f2937; border: 1px solid #374151; border-radius: 12px; padding: 20px; text-align: center; margin-bottom: 24px;'>
                                <span style='font-family: monospace, Courier; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #818cf8; display: inline-block; padding-left: 8px;'>{otp}</span>
                            </div>
                            <p style='margin: 0 0 16px 0; font-size: 13px; line-height: 20px; color: #9ca3af;'>
                                &#x23F1; This code is valid for <strong>15 minutes</strong>. If you did not request a password reset, you can safely ignore this email; your account remains secure.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 20px 32px; text-align: center; border-top: 1px solid #1f2937; background-color: #0f172a;'>
                            <p style='margin: 0; font-size: 12px; color: #64748b;'>
                                &copy; {DateTime.UtcNow.Year} AI Resume Platform. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        // Always log prominently in development/console for immediate visibility
        _logger.LogInformation(
            "\n" +
            "======================================================================\n" +
            " [PASSWORD RESET OTP] \n" +
            " To: {Email} ({FullName})\n" +
            " OTP Code: {Otp}\n" +
            " Valid For: 15 minutes\n" +
            "======================================================================",
            toEmail, displayName, otp);

        // If SMTP configuration is provided, attempt to send real email
        if (!string.IsNullOrWhiteSpace(_settings.SmtpHost) && !string.IsNullOrWhiteSpace(_settings.Username))
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail, displayName));

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Password reset OTP email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password reset email via SMTP to {Email}. Using fallback logger.", toEmail);
                // We do not rethrow so local dev / sandbox environments remain functional
            }
        }
    }
}
