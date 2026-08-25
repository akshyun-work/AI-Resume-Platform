using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Candidates;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.Extensions;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CandidatesController> _logger;
    public CandidatesController(ApplicationDbContext db, ILogger<CandidatesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Get current authenticated candidate profile</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CandidateDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var id = User.GetCandidateId();
        var c = await _db.Candidates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null) return NotFound(ApiResponse<CandidateDto>.Fail("Candidate not found."));
        var dto = new CandidateDto { Id = c.Id, Email = c.Email, FullName = c.FullName, Phone = c.Phone, CreatedAt = c.CreatedAt };
        return Ok(ApiResponse<CandidateDto>.Ok(dto));
    }

    /// <summary>Update current authenticated candidate profile</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<CandidateDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateCandidateRequest request, CancellationToken ct)
    {
        var id = User.GetCandidateId();
        var c = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null) return NotFound(ApiResponse<CandidateDto>.Fail("Candidate not found."));

        c.FullName = request.FullName.Trim();
        c.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Candidate updated {CandidateId}", id);

        var dto = new CandidateDto { Id = c.Id, Email = c.Email, FullName = c.FullName, Phone = c.Phone, CreatedAt = c.CreatedAt };
        return Ok(ApiResponse<CandidateDto>.Ok(dto, "Profile updated."));
    }
}
