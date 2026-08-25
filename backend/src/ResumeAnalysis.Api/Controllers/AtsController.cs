using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Ats;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.Extensions;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/ats")]
[Authorize]
public class AtsController : ControllerBase
{
    private readonly IAtsService _service;
    public AtsController(IAtsService service) => _service = service;

    /// <summary>Create/store ATS analysis for a resume</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AtsAnalysisDto>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Create([FromBody] CreateAtsRequest request, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.CreateAsync(candidateId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<AtsAnalysisDto>.Ok(dto, "Analysis stored."));
    }

    /// <summary>Get analysis by id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AtsAnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetByIdAsync(candidateId, id, ct);
        return Ok(ApiResponse<AtsAnalysisDto>.Ok(dto));
    }

    /// <summary>Get latest analysis for a resume</summary>
    [HttpGet("resume/{resumeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AtsAnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetLatestForResume(Guid resumeId, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetLatestForResumeAsync(candidateId, resumeId, ct);
        return Ok(ApiResponse<AtsAnalysisDto>.Ok(dto));
    }

    /// <summary>Get analysis history for a resume</summary>
    [HttpGet("resume/{resumeId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AtsAnalysisDto>>), 200)]
    public async Task<IActionResult> GetHistoryForResume(Guid resumeId, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.GetHistoryForResumeAsync(candidateId, resumeId, ct);
        return Ok(ApiResponse<IReadOnlyList<AtsAnalysisDto>>.Ok(list));
    }

    /// <summary>List all analyses for current candidate</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AtsAnalysisDto>>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListForCandidateAsync(candidateId, ct);
        return Ok(ApiResponse<IReadOnlyList<AtsAnalysisDto>>.Ok(list));
    }
}
