using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Ats;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Chat;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Jobs;
using ResumeAnalysis.Api.DTOs.Matches;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services.Storage;

public class MatchChatTests : IClassFixture<Phase4Factory>
{
    private readonly Phase4Factory _factory;
    public MatchChatTests(Phase4Factory factory) => _factory = factory;

    private async Task<(HttpClient client, Guid candidateId)> CreateAuthClientAsync(string? email = null)
    {
        var client = _factory.CreateClient();
        email ??= $"p4_{Guid.NewGuid():N}@example.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "P@ssw0rd123!", FullName = "Phase4 User" });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        var me = await client.GetAsync("/api/candidates/me");
        var meBody = await me.Content.ReadFromJsonAsync<ApiResponse<CandidateDto>>();
        return (client, meBody!.Data!.Id);
    }

    private static byte[] PdfBytes() => Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\n");
    private static MultipartFormDataContent PdfContent(byte[] bytes, string name = "resume.pdf")
    {
        var c = new MultipartFormDataContent();
        var fc = new ByteArrayContent(bytes);
        fc.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        c.Add(fc, "File", name);
        return c;
    }
    private async Task<Guid> UploadResumeAsync(HttpClient client)
    {
        var resp = await client.PostAsync("/api/resumes", PdfContent(PdfBytes()));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Resumes.ResumeDto>>();
        return body!.Data!.Id;
    }

    [Fact]
    public async Task Matching_Create_List_Ownership_Invalid_Works()
    {
        var (clientA, _) = await CreateAuthClientAsync();
        var (clientB, _) = await CreateAuthClientAsync();
        var resumeA = await UploadResumeAsync(clientA);
        var resumeB = await UploadResumeAsync(clientB);

        // Create job as A
        var jobReq = new CreateJobRequest { Title = "Match Job", Description = "Description for matching job with enough length", Company = "MatchCo", RequiredSkills = new List<string>{"C#"}, IsActive = true };
        var jobResp = await clientA.PostAsJsonAsync("/api/jobs", jobReq);
        var job = await jobResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var jobId = job!.Data!.Id;

        // Create match as A (protected internal method via POST)
        var create = new CreateMatchRequest
        {
            ResumeId = resumeA,
            JobId = jobId,
            MatchScore = 78,
            MatchingSkills = new List<string>{"C#"},
            MissingSkills = new List<string>{"Docker"},
            MatchingKeywords = new List<string>{"ASP.NET"},
            MissingKeywords = new List<string>{"Kubernetes"},
            Reasons = new List<string>{"Good fit for backend"}
        };
        var resp = await clientA.PostAsJsonAsync("/api/matches", create);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<ApiResponse<MatchResultDto>>();
        Assert.Equal(78, created!.Data!.MatchScore);
        Assert.Contains("C#", created.Data.MatchingSkills!);
        Assert.Contains("Kubernetes", created.Data.MissingKeywords!);
        var matchId = created.Data.Id;

        // Get by id
        var get = await clientA.GetAsync($"/api/matches/{matchId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadFromJsonAsync<ApiResponse<MatchResultDto>>();
        Assert.Equal(matchId, getBody!.Data!.Id);

        // List for candidate
        var list = await clientA.GetAsync("/api/matches");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<ApiResponse<List<MatchResultDto>>>();
        Assert.Single(listBody!.Data!);

        // List for job
        var listJob = await clientA.GetAsync($"/api/matches/job/{jobId}");
        Assert.Equal(HttpStatusCode.OK, listJob.StatusCode);
        var listJobBody = await listJob.Content.ReadFromJsonAsync<ApiResponse<List<MatchResultDto>>>();
        Assert.Single(listJobBody!.Data!);

        // List for resume
        var listResume = await clientA.GetAsync($"/api/matches/resume/{resumeA}");
        Assert.Equal(HttpStatusCode.OK, listResume.StatusCode);
        var listResumeBody = await listResume.Content.ReadFromJsonAsync<ApiResponse<List<MatchResultDto>>>();
        Assert.Single(listResumeBody!.Data!);

        // Create second match for same resume/job with different score
        create.MatchScore = 85;
        var resp2 = await clientA.PostAsJsonAsync("/api/matches", create);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);
        var list2 = await clientA.GetAsync("/api/matches");
        var list2Body = await list2.Content.ReadFromJsonAsync<ApiResponse<List<MatchResultDto>>>();
        Assert.Equal(2, list2Body!.Data!.Count);

        // Ownership: B cannot access A's match
        var getByB = await clientB.GetAsync($"/api/matches/{matchId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByB.StatusCode);
        var listByB = await clientB.GetAsync("/api/matches");
        var listBBody = await listByB.Content.ReadFromJsonAsync<ApiResponse<List<MatchResultDto>>>();
        Assert.Empty(listBBody!.Data!);

        // B cannot create match for A's resume -> 401
        var badCreateByB = new CreateMatchRequest { ResumeId = resumeA, JobId = jobId, MatchScore = 50 };
        var badResp = await clientB.PostAsJsonAsync("/api/matches", badCreateByB);
        Assert.Equal(HttpStatusCode.Unauthorized, badResp.StatusCode);

        // Invalid resume -> 404
        var invalidResume = new CreateMatchRequest { ResumeId = Guid.NewGuid(), JobId = jobId, MatchScore = 50 };
        var invResp = await clientA.PostAsJsonAsync("/api/matches", invalidResume);
        Assert.Equal(HttpStatusCode.NotFound, invResp.StatusCode);

        // Invalid job -> 404
        var invalidJob = new CreateMatchRequest { ResumeId = resumeA, JobId = Guid.NewGuid(), MatchScore = 50 };
        var invJobResp = await clientA.PostAsJsonAsync("/api/matches", invalidJob);
        Assert.Equal(HttpStatusCode.NotFound, invJobResp.StatusCode);

        // Invalid score -> 400
        var badScore = new CreateMatchRequest { ResumeId = resumeA, JobId = jobId, MatchScore = 150 };
        var badScoreResp = await clientA.PostAsJsonAsync("/api/matches", badScore);
        Assert.Equal(HttpStatusCode.BadRequest, badScoreResp.StatusCode);

        // Invalid IDs -> 404
        var notFound = await clientA.GetAsync($"/api/matches/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);

        // Unauthenticated -> 401
        var anon = _factory.CreateClient();
        var unauthList = await anon.GetAsync("/api/matches");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);
        var unauthCreate = await anon.PostAsJsonAsync("/api/matches", create);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthCreate.StatusCode);
    }

    [Fact]
    public async Task Chat_Sessions_Messages_Ownership_Invalid_Works()
    {
        var (clientA, _) = await CreateAuthClientAsync();
        var (clientB, _) = await CreateAuthClientAsync();

        // Create session
        var create = await clientA.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequest { Title = "My Chat" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var sessBody = await create.Content.ReadFromJsonAsync<ApiResponse<ChatSessionDto>>();
        var sessionId = sessBody!.Data!.Id;
        Assert.Equal("My Chat", sessBody.Data.Title);

        // Create session without title -> defaults to New Chat
        var create2 = await clientA.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequest { });
        var sess2Body = await create2.Content.ReadFromJsonAsync<ApiResponse<ChatSessionDto>>();
        Assert.Equal("New Chat", sess2Body!.Data!.Title);

        // List sessions for A
        var list = await clientA.GetAsync("/api/chat/sessions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<ApiResponse<List<ChatSessionDto>>>();
        Assert.Equal(2, listBody!.Data!.Count);

        // B list should be empty
        var listB = await clientB.GetAsync("/api/chat/sessions");
        var listBBody = await listB.Content.ReadFromJsonAsync<ApiResponse<List<ChatSessionDto>>>();
        Assert.Empty(listBBody!.Data!);

        // Get session detail (empty history)
        var get = await clientA.GetAsync($"/api/chat/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadFromJsonAsync<ApiResponse<ChatSessionDetailDto>>();
        Assert.Empty(getBody!.Data!.Messages);

        // Add user message
        var msg1 = await clientA.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = "Hello, review my resume?" });
        Assert.Equal(HttpStatusCode.Created, msg1.StatusCode);
        var msg1Body = await msg1.Content.ReadFromJsonAsync<ApiResponse<ChatMessageDto>>();
        Assert.Equal("User", msg1Body!.Data!.Sender);
        Assert.Equal("Hello, review my resume?", msg1Body.Data.Content);

        // Add assistant message (simulating future LLM)
        var msg2 = await clientA.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = "Your resume looks good, add more keywords.", Sender = "Assistant" });
        Assert.Equal(HttpStatusCode.Created, msg2.StatusCode);
        var msg2Body = await msg2.Content.ReadFromJsonAsync<ApiResponse<ChatMessageDto>>();
        Assert.Equal("Assistant", msg2Body!.Data!.Sender);

        // List messages
        var msgs = await clientA.GetAsync($"/api/chat/sessions/{sessionId}/messages");
        Assert.Equal(HttpStatusCode.OK, msgs.StatusCode);
        var msgsBody = await msgs.Content.ReadFromJsonAsync<ApiResponse<List<ChatMessageDto>>>();
        Assert.Equal(2, msgsBody!.Data!.Count);
        Assert.Equal("Hello, review my resume?", msgsBody.Data[0].Content);
        Assert.Equal("Your resume looks good, add more keywords.", msgsBody.Data[1].Content);

        // Get session again should have history
        var get2 = await clientA.GetAsync($"/api/chat/sessions/{sessionId}");
        var get2Body = await get2.Content.ReadFromJsonAsync<ApiResponse<ChatSessionDetailDto>>();
        Assert.Equal(2, get2Body!.Data!.Messages.Count);

        // Ownership: B cannot access A's session
        var getByB = await clientB.GetAsync($"/api/chat/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByB.StatusCode);
        var addByB = await clientB.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = "Hi" });
        Assert.Equal(HttpStatusCode.Unauthorized, addByB.StatusCode);
        var listMsgByB = await clientB.GetAsync($"/api/chat/sessions/{sessionId}/messages");
        Assert.Equal(HttpStatusCode.Unauthorized, listMsgByB.StatusCode);

        // Invalid session id -> 404
        var fakeId = Guid.NewGuid();
        var notFound = await clientA.GetAsync($"/api/chat/sessions/{fakeId}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        var addNotFound = await clientA.PostAsJsonAsync($"/api/chat/sessions/{fakeId}/messages", new CreateChatMessageRequest { Content = "Hi" });
        Assert.Equal(HttpStatusCode.NotFound, addNotFound.StatusCode);

        // Empty/invalid message -> 400
        var empty = await clientA.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        var whitespace = await clientA.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, whitespace.StatusCode);
        var tooLong = new string('a', 4001);
        var longMsg = await clientA.PostAsJsonAsync($"/api/chat/sessions/{sessionId}/messages", new CreateChatMessageRequest { Content = tooLong });
        Assert.Equal(HttpStatusCode.BadRequest, longMsg.StatusCode);

        // Unauthenticated -> 401
        var anon = _factory.CreateClient();
        var unauthList = await anon.GetAsync("/api/chat/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);
        var unauthCreate = await anon.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequest { });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthCreate.StatusCode);

        // Delete session and verify history gone
        var del = await clientA.DeleteAsync($"/api/chat/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
        var getDeleted = await clientA.GetAsync($"/api/chat/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        var listAfterDel = await clientA.GetAsync("/api/chat/sessions");
        var listAfterBody = await listAfterDel.Content.ReadFromJsonAsync<ApiResponse<List<ChatSessionDto>>>();
        Assert.Single(listAfterBody!.Data!); // only second session left
    }
}

public class Phase4Factory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbName = $"Phase4Db_{Guid.NewGuid()}";
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"Phase4Storage_{Guid.NewGuid():N}");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var d = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (d != null) services.Remove(d);
            var dd = services.SingleOrDefault(x => x.ServiceType == typeof(ApplicationDbContext));
            if (dd != null) services.Remove(dd);
            var dbName = _dbName;
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.Configure<StorageSettings>(o => { o.Path = _storagePath; o.MaxFileSizeBytes = 5*1024*1024; o.AllowedExtensions = new[]{".pdf"}; o.AllowedContentTypes = new[]{"application/pdf"}; });
            var storageDesc = services.SingleOrDefault(x => x.ServiceType == typeof(IFileStorage));
            if (storageDesc != null) services.Remove(storageDesc);
            services.AddSingleton<IFileStorage, LocalFileStorage>();

            var pyDesc = services.SingleOrDefault(x => x.ServiceType == typeof(ResumeAnalysis.Api.Services.AI.IPythonAiService));
            if (pyDesc != null) services.Remove(pyDesc);
            services.AddSingleton<ResumeAnalysis.Api.Services.AI.IPythonAiService, Phase4MockPythonAiService>();

            Directory.CreateDirectory(_storagePath);
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (Directory.Exists(_storagePath)) Directory.Delete(_storagePath, true); } catch { }
    }
}

public class Phase4MockPythonAiService : ResumeAnalysis.Api.Services.AI.IPythonAiService
{
    public Task<string> AnalyzeResumeAsync(string pdfPath, string jobDescription, object? jobData, CancellationToken ct)
    {
        var response = """
        {
            "resume": { "skills": ["C#", "SQL"] },
            "ats_result": {
                "score": 85,
                "breakdown": { "Formatting": 90, "Content": 80 },
                "keywords_identified": ["ASP.NET", "EF Core"],
                "missing_keywords": ["Docker"],
                "missing_skills": ["Kubernetes"],
                "issues": ["Too long"],
                "recommendations": ["Shorten resume"]
            },
            "job_match": { "score": 78 },
            "match_score": 78,
            "comparison": {
                "matched_required": ["C#"],
                "missing_required": ["Kubernetes"],
                "matched_job_skills": ["ASP.NET"],
                "missing_job_skills": ["Kubernetes"]
            },
            "gemini_analysis": {
                "match_summary": "Strong candidate match.",
                "why_you_match": ["C#", "ASP.NET"],
                "what_is_missing": { "required": ["Kubernetes"], "preferred": [], "job_specific": ["Docker"] },
                "score_explanation": { "score": 78, "explanation": "Demonstrates strong foundational qualifications.", "factors": [] },
                "improvement_actions": ["Learn Kubernetes"]
            },
            "career_recommendations": []
        }
        """;
        return Task.FromResult(response);
    }

    public Task<string> ChatAsync(string pdfPath, string message, object? conversation, CancellationToken ct)
    {
        return Task.FromResult("""{"answer": "Test answer from AI assistant.", "conversation": []}""");
    }

    public Task<string> StructureJobAsync(string rawJobDescription, CancellationToken ct)
    {
        return Task.FromResult("{}");
    }
}
