using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Chat;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ApplicationDbContext db, ILogger<ChatService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ChatSessionDto> CreateSessionAsync(Guid candidateId, CreateChatSessionRequest request, CancellationToken ct)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? "New Chat" : request.Title.Trim();
        if (title.Length > 200) throw new ArgumentException("Title must be ≤200 characters.");

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Chat session created {SessionId} candidate {CandidateId}", session.Id, candidateId);
        return new ChatSessionDto { Id = session.Id, CandidateId = session.CandidateId, Title = session.Title, CreatedAt = session.CreatedAt, UpdatedAt = session.UpdatedAt, MessageCount = 0 };
    }

    public async Task<IReadOnlyList<ChatSessionDto>> ListSessionsAsync(Guid candidateId, CancellationToken ct)
    {
        var sessions = await _db.ChatSessions.AsNoTracking()
            .Where(s => s.CandidateId == candidateId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);

        var ids = sessions.Select(s => s.Id).ToList();
        var counts = await _db.ChatMessages.AsNoTracking()
            .Where(m => ids.Contains(m.ChatSessionId))
            .GroupBy(m => m.ChatSessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionId, x => x.Count, ct);

        return sessions.Select(s => new ChatSessionDto
        {
            Id = s.Id,
            CandidateId = s.CandidateId,
            Title = s.Title,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            MessageCount = counts.TryGetValue(s.Id, out var c) ? c : 0
        }).ToList();
    }

    public async Task<ChatSessionDetailDto> GetSessionAsync(Guid candidateId, Guid sessionId, CancellationToken ct)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var session = await _db.ChatSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) throw new KeyNotFoundException("Chat session not found.");
        if (session.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");

        var messages = await _db.ChatMessages.AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return new ChatSessionDetailDto
        {
            Id = session.Id,
            CandidateId = session.CandidateId,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = messages.Select(ChatMessageDto.FromEntity).ToList()
        };
    }

    public async Task DeleteSessionAsync(Guid candidateId, Guid sessionId, CancellationToken ct)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) throw new KeyNotFoundException("Chat session not found.");
        if (session.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        _db.ChatSessions.Remove(session);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Chat session deleted {SessionId} candidate {CandidateId}", sessionId, candidateId);
    }

    public async Task<ChatMessageDto> AddMessageAsync(Guid candidateId, Guid sessionId, CreateChatMessageRequest request, CancellationToken ct)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Invalid id.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Message content is required.");
        var content = request.Content.Trim();
        if (content.Length > 4000) throw new ArgumentException("Message must be ≤4000 characters.");

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) throw new KeyNotFoundException("Chat session not found.");
        if (session.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");

        ChatSender sender = ChatSender.User;
        if (!string.IsNullOrWhiteSpace(request.Sender))
        {
            if (!Enum.TryParse<ChatSender>(request.Sender, true, out var parsed))
                throw new ArgumentException("Sender must be 'User' or 'Assistant'.");
            sender = parsed;
        }

        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Sender = sender,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(msg);
        session.UpdatedAt = DateTime.UtcNow;
        // Auto-update title from first user message if still default
        if (session.Title == "New Chat" && sender == ChatSender.User && _db.ChatMessages.Count(m => m.ChatSessionId == sessionId) == 0)
        {
            session.Title = content.Length > 50 ? content.Substring(0, 50) + "…" : content;
        }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Chat message added {MessageId} session {SessionId} sender {Sender}", msg.Id, sessionId, sender);
        return ChatMessageDto.FromEntity(msg);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid candidateId, Guid sessionId, CancellationToken ct)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var session = await _db.ChatSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) throw new KeyNotFoundException("Chat session not found.");
        if (session.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        var msgs = await _db.ChatMessages.AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return msgs.Select(ChatMessageDto.FromEntity).ToList();
    }
}
