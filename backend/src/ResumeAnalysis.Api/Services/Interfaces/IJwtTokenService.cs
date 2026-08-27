using ResumeAnalysis.Api.Entities;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IJwtTokenService
{
    (string token, DateTime expiresAt) GenerateToken(Candidate candidate);
}
