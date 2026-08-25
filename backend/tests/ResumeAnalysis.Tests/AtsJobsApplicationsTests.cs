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
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Jobs;
using ResumeAnalysis.Api.DTOs.Applications;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services.Storage;

public class AtsJobsApplicationsTests : IClassFixture<Phase3Factory>
{
    private readonly Phase3Factory _factory;
    public AtsJobsApplicationsTests(Phase3Factory factory) => _factory = factory;

    private async Task<(HttpClient client, Guid candidateId)> CreateAuthClientAsync(string? email = null)
    {
        var client = _factory.CreateClient();
        email ??= $"p3_{Guid.NewGuid():N}@example.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "P@ssw0rd123!",
            FullName = "Phase3 User",
            Phone = "999"
        });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        // Get candidate id via /api/candidates/me
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
    public async Task Ats_Create_Retrieve_Authorization_Works()
    {
        var (clientA, _) = await CreateAuthClientAsync();
        var (clientB, _) = await CreateAuthClientAsync();
        var resumeId = await UploadResumeAsync(clientA);

        // Create ATS
        var create = new CreateAtsRequest
        {
            ResumeId = resumeId,
            OverallScore = 85,
            CategoryScores = new Dictionary<string,int>{{"Formatting",90},{"Content",80}},
            SkillsIdentified = new List<string>{"C#","SQL"},
            KeywordsIdentified = new List<string>{"ASP.NET","EF Core"},
            MissingKeywords = new List<string>{"Docker"},
            MissingSkills = new List<string>{"Kubernetes"},
            Issues = new List<string>{"Too long"},
            Recommendations = new List<string>{"Shorten resume"}
        };
        var resp = await clientA.PostAsJsonAsync("/api/ats", create);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<ApiResponse<AtsAnalysisDto>>();
        Assert.Equal(85, created!.Data!.OverallScore);
        Assert.Equal(2, created.Data.CategoryScores!.Count);
        var analysisId = created.Data.Id;

        // Get by id
        var get = await clientA.GetAsync($"/api/ats/{analysisId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadFromJsonAsync<ApiResponse<AtsAnalysisDto>>();
        Assert.Equal(analysisId, getBody!.Data!.Id);

        // Get latest for resume
        var latest = await clientA.GetAsync($"/api/ats/resume/{resumeId}");
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
        var latestBody = await latest.Content.ReadFromJsonAsync<ApiResponse<AtsAnalysisDto>>();
        Assert.Equal(analysisId, latestBody!.Data!.Id);

        // History
        var hist = await clientA.GetAsync($"/api/ats/resume/{resumeId}/history");
        Assert.Equal(HttpStatusCode.OK, hist.StatusCode);
        var histBody = await hist.Content.ReadFromJsonAsync<ApiResponse<List<AtsAnalysisDto>>>();
        Assert.Single(histBody!.Data!);

        // List for candidate
        var list = await clientA.GetAsync("/api/ats");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<ApiResponse<List<AtsAnalysisDto>>>();
        Assert.Single(listBody!.Data!);

        // Create second analysis for same resume
        create.OverallScore = 90;
        var resp2 = await clientA.PostAsJsonAsync("/api/ats", create);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);
        var hist2 = await clientA.GetAsync($"/api/ats/resume/{resumeId}/history");
        var hist2Body = await hist2.Content.ReadFromJsonAsync<ApiResponse<List<AtsAnalysisDto>>>();
        Assert.Equal(2, hist2Body!.Data!.Count);
        // Latest should be second
        var latest2 = await clientA.GetAsync($"/api/ats/resume/{resumeId}");
        var latest2Body = await latest2.Content.ReadFromJsonAsync<ApiResponse<AtsAnalysisDto>>();
        Assert.Equal(90, latest2Body!.Data!.OverallScore);

        // Authorization: B cannot access A's analysis
        var getByB = await clientB.GetAsync($"/api/ats/{analysisId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByB.StatusCode);
        var latestByB = await clientB.GetAsync($"/api/ats/resume/{resumeId}");
        Assert.Equal(HttpStatusCode.Unauthorized, latestByB.StatusCode);

        // Invalid resume -> 404
        var badCreate = new CreateAtsRequest { ResumeId = Guid.NewGuid(), OverallScore = 50 };
        var badResp = await clientA.PostAsJsonAsync("/api/ats", badCreate);
        Assert.Equal(HttpStatusCode.NotFound, badResp.StatusCode);

        // Invalid score -> 400
        var invalidScore = new CreateAtsRequest { ResumeId = resumeId, OverallScore = 150 };
        var invalidResp = await clientA.PostAsJsonAsync("/api/ats", invalidScore);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResp.StatusCode);

        // Unauthenticated -> 401
        var anon = _factory.CreateClient();
        var unauth = await anon.GetAsync($"/api/ats/{analysisId}");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        var unauthPost = await anon.PostAsJsonAsync("/api/ats", create);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthPost.StatusCode);
    }

    [Fact]
    public async Task Jobs_Crud_Search_Pagination_Works()
    {
        var (authClient, _) = await CreateAuthClientAsync();
        var anonClient = _factory.CreateClient();

        // Create jobs
        var job1 = new CreateJobRequest { Title = "Backend Engineer", Description = "Build APIs with C# and .NET", Company = "Acme", Location = "Remote", EmploymentType = "Full-time", RequiredSkills = new List<string>{"C#","SQL"}, PreferredSkills = new List<string>{"Docker"}, IsActive = true };
        var job2 = new CreateJobRequest { Title = "Frontend Engineer", Description = "React and TypeScript frontend", Company = "BetaCorp", Location = "NYC", EmploymentType = "Contract", RequiredSkills = new List<string>{"React","TS"}, IsActive = true };
        var job3 = new CreateJobRequest { Title = "Backend Engineer", Description = "Python backend", Company = "Acme", Location = "Remote", EmploymentType = "Full-time", RequiredSkills = new List<string>{"Python"}, IsActive = false };

        var r1 = await authClient.PostAsJsonAsync("/api/jobs", job1);
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        var j1 = await r1.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var r2 = await authClient.PostAsJsonAsync("/api/jobs", job2);
        var j2 = await r2.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var r3 = await authClient.PostAsJsonAsync("/api/jobs", job3);
        var j3 = await r3.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();

        // Get by id (anon allowed)
        var get = await anonClient.GetAsync($"/api/jobs/{j1!.Data!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        // Search all (anon)
        var searchAll = await anonClient.GetAsync("/api/jobs?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchAll.StatusCode);
        var searchAllBody = await searchAll.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.True(searchAllBody!.Data!.total >= 3);

        // Search by title
        var searchBackend = await anonClient.GetAsync("/api/jobs?search=Backend");
        var backendBody = await searchBackend.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.True(backendBody!.Data!.total >= 2);

        // Filter by location Remote
        var loc = await anonClient.GetAsync("/api/jobs?location=Remote");
        var locBody = await loc.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.True(locBody!.Data!.total >= 2);

        // Filter by IsActive true
        var active = await anonClient.GetAsync("/api/jobs?isActive=true");
        var activeBody = await active.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        // Should not include inactive job3
        var hasInactive = activeBody!.Data!.items.Any(i => i.id == j3!.Data!.Id);
        Assert.False(hasInactive);

        // Skills filter
        var skills = await anonClient.GetAsync("/api/jobs?skills=C#");
        var skillsBody = await skills.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.True(skillsBody!.Data!.total >= 1);

        // Pagination
        var page1 = await anonClient.GetAsync("/api/jobs?page=1&pageSize=1");
        var p1Body = await page1.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.Single(p1Body!.Data!.items);
        var page2 = await anonClient.GetAsync("/api/jobs?page=2&pageSize=1");
        var p2Body = await page2.Content.ReadFromJsonAsync<ApiResponse<SearchWrapper>>();
        Assert.Single(p2Body!.Data!.items);
        Assert.NotEqual(p1Body.Data.items[0].id, p2Body.Data.items[0].id);

        // Update job
        var upd = new UpdateJobRequest { Title = "Backend Engineer Updated", Description = "Updated desc with more details here", Company = "Acme", Location = "Remote", EmploymentType = "Full-time", RequiredSkills = new List<string>{"C#"}, IsActive = true };
        var updResp = await authClient.PutAsJsonAsync($"/api/jobs/{j1.Data.Id}", upd);
        Assert.Equal(HttpStatusCode.OK, updResp.StatusCode);
        var updBody = await updResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        Assert.Equal("Backend Engineer Updated", updBody!.Data!.Title);

        // Delete job
        var del = await authClient.DeleteAsync($"/api/jobs/{j2!.Data!.Id}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
        var getDeleted = await anonClient.GetAsync($"/api/jobs/{j2.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);

        // Validation: missing title -> 400
        var bad = new CreateJobRequest { Title = "", Description = "short", Company = "" };
        var badResp = await authClient.PostAsJsonAsync("/api/jobs", bad);
        Assert.Equal(HttpStatusCode.BadRequest, badResp.StatusCode);

        // Unauthenticated create -> 401
        var unauthCreate = await anonClient.PostAsJsonAsync("/api/jobs", job1);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthCreate.StatusCode);

        // Invalid id -> 404
        var notFound = await anonClient.GetAsync($"/api/jobs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Applications_Create_List_Ownership_Duplicate_Works()
    {
        var (clientA, _) = await CreateAuthClientAsync();
        var (clientB, _) = await CreateAuthClientAsync();
        var resumeA = await UploadResumeAsync(clientA);
        var resumeB = await UploadResumeAsync(clientB);

        // Create jobs as A
        var jobReq = new CreateJobRequest { Title = "Test Job For Apply", Description = "Description for application testing with enough length", Company = "TestCo", Location = "Remote", RequiredSkills = new List<string>{"C#"}, IsActive = true };
        var jobResp = await clientA.PostAsJsonAsync("/api/jobs", jobReq);
        var job = await jobResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var jobId = job!.Data!.Id;

        // Inactive job for negative test
        var inactiveReq = new CreateJobRequest { Title = "Inactive Job", Description = "Inactive job description with enough length", Company = "TestCo", IsActive = false };
        var inactiveResp = await clientA.PostAsJsonAsync("/api/jobs", inactiveReq);
        var inactiveJob = await inactiveResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();

        // Apply success
        var apply = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = jobId, ResumeId = resumeA });
        Assert.Equal(HttpStatusCode.Created, apply.StatusCode);
        var appBody = await apply.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>();
        var appId = appBody!.Data!.Id;
        Assert.Equal("Applied", appBody.Data.Status);

        // Duplicate apply -> 409
        var dup = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = jobId, ResumeId = resumeA });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // B can also apply to same job (different candidate) -> should succeed
        var applyB = await clientB.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = jobId, ResumeId = resumeB });
        Assert.Equal(HttpStatusCode.Created, applyB.StatusCode);

        // List A should have 1, B has 1
        var listA = await clientA.GetAsync("/api/applications");
        var listABody = await listA.Content.ReadFromJsonAsync<ApiResponse<List<ApplicationDto>>>();
        Assert.Single(listABody!.Data!);
        var listB = await clientB.GetAsync("/api/applications");
        var listBBody = await listB.Content.ReadFromJsonAsync<ApiResponse<List<ApplicationDto>>>();
        Assert.Single(listBBody!.Data!);

        // Get details ownership
        var getA = await clientA.GetAsync($"/api/applications/{appId}");
        Assert.Equal(HttpStatusCode.OK, getA.StatusCode);
        var getByB = await clientB.GetAsync($"/api/applications/{appId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByB.StatusCode);

        // Invalid job -> 404
        var badJob = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = Guid.NewGuid(), ResumeId = resumeA });
        Assert.Equal(HttpStatusCode.NotFound, badJob.StatusCode);

        // Resume not owned by candidate -> 401
        var wrongResume = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = jobId, ResumeId = resumeB });
        // This is duplicate already, so create new job for this test
        var newJobResp = await clientA.PostAsJsonAsync("/api/jobs", new CreateJobRequest { Title = "Another Job", Description = "Another description with enough length for test", Company = "TestCo", IsActive = true });
        var newJob = await newJobResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var wrongResume2 = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = newJob!.Data!.Id, ResumeId = resumeB });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongResume2.StatusCode);

        // Inactive job -> 409
        var inactiveApply = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = inactiveJob!.Data!.Id, ResumeId = resumeA });
        Assert.Equal(HttpStatusCode.Conflict, inactiveApply.StatusCode);

        // Invalid resume -> 404 with new job
        var newJob2Resp = await clientA.PostAsJsonAsync("/api/jobs", new CreateJobRequest { Title = "Job2", Description = "Description for job2 with enough length", Company = "TestCo", IsActive = true });
        var newJob2 = await newJob2Resp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var invalidResume = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = newJob2!.Data!.Id, ResumeId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, invalidResume.StatusCode);

        // Unauthenticated -> 401
        var anon = _factory.CreateClient();
        var unauth = await anon.GetAsync("/api/applications");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        var unauthPost = await anon.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = jobId });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthPost.StatusCode);

        // Apply without resume (optional) -> should succeed for B on new job
        var noResumeJobResp = await clientA.PostAsJsonAsync("/api/jobs", new CreateJobRequest { Title = "NoResume Job", Description = "Description for noresume job with enough length", Company = "TestCo", IsActive = true });
        var noResumeJob = await noResumeJobResp.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var noResumeApply = await clientB.PostAsJsonAsync("/api/applications", new CreateApplicationRequest { JobId = noResumeJob!.Data!.Id, ResumeId = null });
        Assert.Equal(HttpStatusCode.Created, noResumeApply.StatusCode);
    }

    private class SearchWrapper
    {
        public List<Item> items { get; set; } = new();
        public int total { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
    }
    private class Item { public Guid id { get; set; } }
}

public class Phase3Factory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbName = $"Phase3Db_{Guid.NewGuid()}";
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"Phase3Storage_{Guid.NewGuid():N}");
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
            Directory.CreateDirectory(_storagePath);
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (Directory.Exists(_storagePath)) Directory.Delete(_storagePath, true); } catch { }
    }
}
