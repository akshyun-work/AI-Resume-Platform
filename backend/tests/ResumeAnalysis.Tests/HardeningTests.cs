using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Jobs;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services.Storage;

public class HardeningTests : IClassFixture<HardeningFactory>
{
    private readonly HardeningFactory _factory;
    public HardeningTests(HardeningFactory f) => _factory = f;

    private async Task<HttpClient> AuthClientAsync(string? email = null)
    {
        var c = _factory.CreateClient();
        email ??= $"hard_{Guid.NewGuid():N}@example.com";
        var reg = await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "P@ssw0rd123!", FullName = "Hardening" });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        return c;
    }

    private static byte[] PdfBytes(int size = 400) { var h = Encoding.ASCII.GetBytes("%PDF-1.4\n"); var a = new byte[size]; Array.Copy(h, a, Math.Min(h.Length, size)); for(int i=h.Length;i<size;i++) a[i]=0x20; return a; }
    private static MultipartFormDataContent PdfContent(byte[] b, string name="resume.pdf"){ var c=new MultipartFormDataContent(); var f=new ByteArrayContent(b); f.Headers.ContentType=new MediaTypeHeaderValue("application/pdf"); c.Add(f,"File",name); return c; }

    [Fact]
    public async Task Invalid_Guids_Return_400_Not_500()
    {
        var client = await AuthClientAsync();
        var empty = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/resumes/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/resumes/{empty}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/resumes/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/ats/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/matches/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/chat/sessions/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/jobs/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/applications/{empty}")).StatusCode);
    }

    [Fact]
    public async Task PathTraversal_Filename_Is_Sanitized()
    {
        var client = await AuthClientAsync();
        var pdf = PdfBytes();
        // Try path traversal filename
        var content = PdfContent(pdf, "../../etc/passwd.pdf");
        var resp = await client.PostAsync("/api/resumes", content);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Resumes.ResumeDto>>();
        // OriginalFileName should be sanitized to "passwd.pdf" (GetFileName)
        Assert.Equal("passwd.pdf", body!.Data!.OriginalFileName);
        // Stored FileName should be safe guid, not traversal
        Assert.DoesNotContain("..", body.Data.FileName);
        Assert.DoesNotContain("/", body.Data.FileName);
        // Download should still work
        var dl = await client.GetAsync($"/api/resumes/{body.Data.Id}/download");
        Assert.Equal(HttpStatusCode.OK, dl.StatusCode);
        Assert.Equal("passwd.pdf", dl.Content.Headers.ContentDisposition?.FileNameStar ?? dl.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    [Fact]
    public async Task Oversized_Payload_Rejected_With_400()
    {
        var client = await AuthClientAsync();
        // 6 MB > 5 MB limit
        var large = PdfBytes(6 * 1024 * 1024);
        large[0]=0x25; large[1]=0x50; large[2]=0x44; large[3]=0x46;
        var resp = await client.PostAsync("/api/resumes", PdfContent(large, "large.pdf"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("exceeds limit", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_ContentType_And_MissingFile_Rejected()
    {
        var client = await AuthClientAsync();
        // Text file with .txt extension
        var txt = Encoding.UTF8.GetBytes("not pdf");
        var c1 = new MultipartFormDataContent(); var f1=new ByteArrayContent(txt); f1.Headers.ContentType=new MediaTypeHeaderValue("text/plain"); c1.Add(f1,"File","resume.txt");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/resumes", c1)).StatusCode);
        // PDF extension but wrong magic
        var fake = Encoding.UTF8.GetBytes("NOTPDFCONTENT");
        var c2 = PdfContent(fake, "fake.pdf"); // will be overwritten header check
        // Need to ensure fake doesn't start with %PDF, our helper would still have %PDF if we use PdfBytes, so craft manually
        var c3 = new MultipartFormDataContent(); var f3=new ByteArrayContent(Encoding.ASCII.GetBytes("XXXX")); f3.Headers.ContentType=new MediaTypeHeaderValue("application/pdf"); c3.Add(f3,"File","fake.pdf");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/resumes", c3)).StatusCode);
        // Missing file
        var empty = new MultipartFormDataContent();
        var miss = await client.PostAsync("/api/resumes", empty);
        Assert.True(miss.StatusCode==HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_Enum_Sender_Rejected()
    {
        var client = await AuthClientAsync();
        var sess = await client.PostAsJsonAsync("/api/chat/sessions", new { Title="T"});
        var sessBody = await sess.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Chat.ChatSessionDto>>();
        var id = sessBody!.Data!.Id;
        var bad = await client.PostAsJsonAsync($"/api/chat/sessions/{id}/messages", new { Content="hi", Sender="InvalidRole"});
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Candidate_Ownership_Enforced_Across_All_Resources()
    {
        var clientA = await AuthClientAsync();
        var clientB = await AuthClientAsync();
        // Resume
        var resumeId = (await (await clientA.PostAsync("/api/resumes", PdfContent(PdfBytes()))).Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Resumes.ResumeDto>>())!.Data!.Id;
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.GetAsync($"/api/resumes/{resumeId}")).StatusCode);
        // ATS
        var ats = await clientA.PostAsJsonAsync("/api/ats", new { ResumeId=resumeId, OverallScore=70 });
        var atsBody = await ats.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Ats.AtsAnalysisDto>>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.GetAsync($"/api/ats/{atsBody!.Data!.Id}")).StatusCode);
        // Job application
        var job = await clientA.PostAsJsonAsync("/api/jobs", new CreateJobRequest{ Title="Hardening Job", Description="Hardening job description with enough length", Company="HC", IsActive=true});
        var jobBody = await job.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var jobId = jobBody!.Data!.Id;
        var app = await clientA.PostAsJsonAsync("/api/applications", new { JobId=jobId, ResumeId=resumeId});
        var appBody = await app.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Applications.ApplicationDto>>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.GetAsync($"/api/applications/{appBody!.Data!.Id}")).StatusCode);
        // Match
        var match = await clientA.PostAsJsonAsync("/api/matches", new { ResumeId=resumeId, JobId=jobId, MatchScore=60});
        var matchBody = await match.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Matches.MatchResultDto>>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.GetAsync($"/api/matches/{matchBody!.Data!.Id}")).StatusCode);
        // Chat
        var sess = await clientA.PostAsJsonAsync("/api/chat/sessions", new { Title="S"});
        var sessBody = await sess.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Chat.ChatSessionDto>>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.GetAsync($"/api/chat/sessions/{sessBody!.Data!.Id}")).StatusCode);
    }

    [Fact]
    public async Task Validation_MissingFields_Produces_400()
    {
        var client = await AuthClientAsync();
        // Register missing email
        var anon = _factory.CreateClient();
        var badReg = await anon.PostAsJsonAsync("/api/auth/register", new { Email="", Password="short", FullName=""});
        Assert.Equal(HttpStatusCode.BadRequest, badReg.StatusCode);
        // ATS missing ResumeId empty
        var badAts = await client.PostAsJsonAsync("/api/ats", new { ResumeId=Guid.Empty, OverallScore=50});
        Assert.Equal(HttpStatusCode.BadRequest, badAts.StatusCode);
        // Match missing JobId
        var resumeId = (await (await client.PostAsync("/api/resumes", PdfContent(PdfBytes()))).Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Resumes.ResumeDto>>())!.Data!.Id;
        var job = await client.PostAsJsonAsync("/api/jobs", new CreateJobRequest{ Title="VJob", Description="Valid description with enough length", Company="C", IsActive=true});
        var jobBody = await job.Content.ReadFromJsonAsync<ApiResponse<JobDto>>();
        var badMatch = await client.PostAsJsonAsync("/api/matches", new { ResumeId=resumeId, JobId=Guid.Empty, MatchScore=50});
        Assert.Equal(HttpStatusCode.BadRequest, badMatch.StatusCode);
        // Chat empty message
        var sess = await client.PostAsJsonAsync("/api/chat/sessions", new { Title="T"});
        var sessBody = await sess.Content.ReadFromJsonAsync<ApiResponse<ResumeAnalysis.Api.DTOs.Chat.ChatSessionDto>>();
        var badMsg = await client.PostAsJsonAsync($"/api/chat/sessions/{sessBody!.Data!.Id}/messages", new { Content=""});
        Assert.Equal(HttpStatusCode.BadRequest, badMsg.StatusCode);
    }

    [Fact]
    public async Task No_Sensitive_Data_Exposed_In_Errors()
    {
        var client = await AuthClientAsync();
        // Try to get non-existent resume - should not leak stack trace
        var resp = await client.GetAsync($"/api/resumes/{Guid.NewGuid()}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("at ", body); // no stack trace
        Assert.DoesNotContain("BCrypt", body);
        Assert.DoesNotContain("PasswordHash", body);
        // Login with wrong password - should not leak hash
        var anon = _factory.CreateClient();
        var login = await anon.PostAsJsonAsync("/api/auth/login", new { Email="hard_nonexist@example.com", Password="wrong"});
        var loginBody = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", loginBody);
    }
}

public class HardeningFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbName = $"HardeningDb_{Guid.NewGuid()}";
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"HardeningStorage_{Guid.NewGuid():N}");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var d = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (d!=null) services.Remove(d);
            var dd = services.SingleOrDefault(x => x.ServiceType == typeof(ApplicationDbContext));
            if (dd!=null) services.Remove(dd);
            var n=_dbName; services.AddDbContext<ApplicationDbContext>(o=>o.UseInMemoryDatabase(n));
            services.Configure<StorageSettings>(o=>{o.Path=_storagePath; o.MaxFileSizeBytes=5*1024*1024; o.AllowedExtensions=new[]{".pdf"}; o.AllowedContentTypes=new[]{"application/pdf"};});
            var sd=services.SingleOrDefault(x=>x.ServiceType==typeof(IFileStorage)); if(sd!=null) services.Remove(sd);
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            Directory.CreateDirectory(_storagePath);
        });
    }
    protected override void Dispose(bool disposing){ base.Dispose(disposing); try{if(Directory.Exists(_storagePath)) Directory.Delete(_storagePath,true);}catch{}}
}
