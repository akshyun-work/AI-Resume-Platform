using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Chat;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.AI;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class ResumeChatService : IResumeChatService
{
	private readonly ApplicationDbContext _db;
	private readonly IPythonAiService _pythonAiService;
	private readonly ILogger<ResumeChatService> _logger;

	public ResumeChatService(
		ApplicationDbContext db,
		IPythonAiService pythonAiService,
		ILogger<ResumeChatService> logger)
	{
		_db = db;
		_pythonAiService = pythonAiService;
		_logger = logger;
	}

	public async Task<ResumeChatResponse> AskAsync(
		Guid candidateId,
		Guid resumeId,
		ResumeChatRequest request,
		CancellationToken ct)
	{
		if (resumeId == Guid.Empty)
			throw new ArgumentException("Invalid resume id.");

		if (request == null)
			throw new ArgumentException("Chat request is required.");

		if (request.ChatSessionId == Guid.Empty)
			throw new ArgumentException("Chat session id is required.");

		if (string.IsNullOrWhiteSpace(request.Message))
			throw new ArgumentException("Chat message is required.");

		var resume = await _db.Resumes
			.AsNoTracking()
			.FirstOrDefaultAsync(
				r => r.Id == resumeId,
				ct);

		if (resume == null)
			throw new KeyNotFoundException(
				"Resume not found.");

		if (resume.CandidateId != candidateId)
			throw new UnauthorizedAccessException(
				"Access denied to resume.");

		if (string.IsNullOrWhiteSpace(resume.StoragePath))
			throw new InvalidOperationException(
				"Resume storage path is missing.");

		// Load existing chat session.
		var session = await _db.ChatSessions
			.FirstOrDefaultAsync(
				s => s.Id == request.ChatSessionId,
				ct);

		if (session == null)
			throw new KeyNotFoundException(
				"Chat session not found.");

		if (session.CandidateId != candidateId)
			throw new UnauthorizedAccessException(
				"Access denied to chat session.");

		// Load existing conversation from database.
		var messages = await _db.ChatMessages
			.AsNoTracking()
			.Where(m => m.ChatSessionId == session.Id)
			.OrderBy(m => m.CreatedAt)
			.ToListAsync(ct);

		_logger.LogInformation(
			"Starting resume chatbot for resume {ResumeId}, session {SessionId}, candidate {CandidateId}",
			resumeId,
			session.Id,
			candidateId);

		// Convert database messages into the format expected by Python.
		var pythonConversation = messages
			.Select(message => new
			{
				role = message.Sender == ChatSender.Assistant
					? "assistant"
					: "user",

				message = message.Content
			})
			.ToList();

		// Send existing conversation + current question to Python.
		var pythonOutput = await _pythonAiService.ChatAsync(
			resume.StoragePath,
			request.Message,
			pythonConversation,
			ct);

		if (string.IsNullOrWhiteSpace(pythonOutput))
			throw new InvalidOperationException(
				"Python chatbot returned empty output.");

		string answer;

		try
		{
			using var document = JsonDocument.Parse(pythonOutput);

			var root = document.RootElement;

			if (root.ValueKind == JsonValueKind.Object &&
				root.TryGetProperty("answer", out var answerElement))
			{
				answer = answerElement.GetString()
					?? string.Empty;
			}
			else
			{
				answer = pythonOutput;
			}
		}
		catch (JsonException)
		{
			answer = pythonOutput;
		}

		if (string.IsNullOrWhiteSpace(answer))
			throw new InvalidOperationException(
				"Python chatbot returned an empty answer.");

		// Save the user's new message.
		var userMessage = new ChatMessage
		{
			Id = Guid.NewGuid(),
			ChatSessionId = session.Id,
			Sender = ChatSender.User,
			Content = request.Message.Trim(),
			CreatedAt = DateTime.UtcNow
		};

		// Save the assistant's response.
		var assistantMessage = new ChatMessage
		{
			Id = Guid.NewGuid(),
			ChatSessionId = session.Id,
			Sender = ChatSender.Assistant,
			Content = answer,
			CreatedAt = DateTime.UtcNow
		};

		_db.ChatMessages.Add(userMessage);
		_db.ChatMessages.Add(assistantMessage);

		session.UpdatedAt = DateTime.UtcNow;

		await _db.SaveChangesAsync(ct);

		// Return the complete updated conversation.
		var updatedConversation = messages
			.Select(ChatMessageDto.FromEntity)
			.ToList();

		updatedConversation.Add(
			ChatMessageDto.FromEntity(userMessage));

		updatedConversation.Add(
			ChatMessageDto.FromEntity(assistantMessage));

		_logger.LogInformation(
			"Resume chatbot completed for resume {ResumeId}, session {SessionId}, candidate {CandidateId}",
			resumeId,
			session.Id,
			candidateId);

		return new ResumeChatResponse
		{
			Answer = answer,
			Conversation = updatedConversation
		};
	}
}