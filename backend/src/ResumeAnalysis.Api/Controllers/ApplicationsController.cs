using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Applications;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.Extensions;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _service;
    public ApplicationsController(IApplicationService service) => _service = service;

    /// <summary>Apply to a job</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Apply([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.ApplyAsync(candidateId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ApplicationDto>.Ok(dto, "Application submitted."));
    }

    /// <summary>List candidate applications</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApplicationDto>>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListAsync(candidateId, ct);
        return Ok(ApiResponse<IReadOnlyList<ApplicationDto>>.Ok(list));
    }

    /// <summary>Get application details</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetByIdAsync(candidateId, id, ct);
        return Ok(ApiResponse<ApplicationDto>.Ok(dto));
    }
}
