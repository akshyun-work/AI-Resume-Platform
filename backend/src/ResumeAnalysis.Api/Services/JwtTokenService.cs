using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
    }

    public (string token, DateTime expiresAt) GenerateToken(Candidate candidate)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, candidate.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, candidate.Email),
            new Claim(ClaimTypes.NameIdentifier, candidate.Id.ToString()),
            new Claim(ClaimTypes.Email, candidate.Email),
            new Claim(ClaimTypes.Name, candidate.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
