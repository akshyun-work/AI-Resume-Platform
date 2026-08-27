using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Matches;
using ResumeAnalysis.Api.Extensions;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _service;
    public MatchesController(IMatchService service) => _service = service;

    /// <summary>Create a match result (protected, for internal matching service)</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MatchResultDto>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Create([FromBody] CreateMatchRequest request, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.CreateAsync(candidateId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<MatchResultDto>.Ok(dto, "Match stored."));
    }

    /// <summary>List all matches for current candidate</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MatchResultDto>>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListAsync(candidateId, ct);
        return Ok(ApiResponse<IReadOnlyList<MatchResultDto>>.Ok(list));
    }

    /// <summary>Get match by id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MatchResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetByIdAsync(candidateId, id, ct);
        return Ok(ApiResponse<MatchResultDto>.Ok(dto));
    }

    /// <summary>List matches for a specific job</summary>
    [HttpGet("job/{jobId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MatchResultDto>>), 200)]
    public async Task<IActionResult> ListForJob(Guid jobId, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListForJobAsync(candidateId, jobId, ct);
        return Ok(ApiResponse<IReadOnlyList<MatchResultDto>>.Ok(list));
    }

    /// <summary>List matches for a specific resume</summary>
    [HttpGet("resume/{resumeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MatchResultDto>>), 200)]
    public async Task<IActionResult> ListForResume(Guid resumeId, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListForResumeAsync(candidateId, resumeId, ct);
        return Ok(ApiResponse<IReadOnlyList<MatchResultDto>>.Ok(list));
    }
}
