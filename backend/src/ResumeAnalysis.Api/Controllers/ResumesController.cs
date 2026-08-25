using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Resumes;
using ResumeAnalysis.Api.Extensions;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly IResumeService _service;
    public ResumesController(IResumeService service) => _service = service;

    public class ResumeUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    /// <summary>Upload a PDF resume (creates new version)</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ResumeDto>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] ResumeUploadRequest request, CancellationToken ct)
    {
        var file = request?.File;
        if (file == null) return BadRequest(ApiResponse<ResumeDto>.Fail("File is required."));
        var candidateId = User.GetCandidateId();
        var dto = await _service.UploadAsync(candidateId, file, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ResumeDto>.Ok(dto, "Resume uploaded."));
    }

    /// <summary>List all resumes for current candidate (ordered by version desc, latest first)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ResumeDto>>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListAsync(candidateId, ct);
        return Ok(ApiResponse<IReadOnlyList<ResumeDto>>.Ok(list));
    }

    /// <summary>Get latest resume for current candidate</summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(ApiResponse<ResumeDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetLatestAsync(candidateId, ct);
        if (dto == null) return NotFound(ApiResponse<ResumeDto>.Fail("No resumes found."));
        return Ok(ApiResponse<ResumeDto>.Ok(dto));
    }

    /// <summary>Get resume metadata by id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ResumeDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetAsync(candidateId, id, ct);
        return Ok(ApiResponse<ResumeDto>.Ok(dto));
    }

    /// <summary>Download resume file</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var (stream, contentType, fileName) = await _service.DownloadAsync(candidateId, id, ct);
        return File(stream, contentType, fileName);
    }

    /// <summary>Delete resume</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        await _service.DeleteAsync(candidateId, id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Resume deleted."));
    }
}
