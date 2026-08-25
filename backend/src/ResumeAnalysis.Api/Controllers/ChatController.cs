using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.DTOs.Chat;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.Extensions;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;
    public ChatController(IChatService service) => _service = service;

    /// <summary>Create a new chat session</summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDto>), 201)]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.CreateSessionAsync(candidateId, request, ct);
        return CreatedAtAction(nameof(GetSession), new { id = dto.Id }, ApiResponse<ChatSessionDto>.Ok(dto, "Session created."));
    }

    /// <summary>List chat sessions for current candidate</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChatSessionDto>>), 200)]
    public async Task<IActionResult> ListSessions(CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListSessionsAsync(candidateId, ct);
        return Ok(ApiResponse<IReadOnlyList<ChatSessionDto>>.Ok(list));
    }

    /// <summary>Get chat session with messages (conversation history)</summary>
    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.GetSessionAsync(candidateId, id, ct);
        return Ok(ApiResponse<ChatSessionDetailDto>.Ok(dto));
    }

    /// <summary>Delete a chat session</summary>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        await _service.DeleteSessionAsync(candidateId, id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Session deleted."));
    }

    /// <summary>Add a message to a session (user or assistant). Designed for future LLM integration: external LLM can POST assistant messages via same endpoint.</summary>
    [HttpPost("sessions/{id:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageDto>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] CreateChatMessageRequest request, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var dto = await _service.AddMessageAsync(candidateId, id, request, ct);
        return CreatedAtAction(nameof(GetSession), new { id }, ApiResponse<ChatMessageDto>.Ok(dto, "Message added."));
    }

    /// <summary>List messages for a session</summary>
    [HttpGet("sessions/{id:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChatMessageDto>>), 200)]
    public async Task<IActionResult> ListMessages(Guid id, CancellationToken ct)
    {
        var candidateId = User.GetCandidateId();
        var list = await _service.ListMessagesAsync(candidateId, id, ct);
        return Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Ok(list));
    }
}
