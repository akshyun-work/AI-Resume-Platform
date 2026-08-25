using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Candidates;
using ResumeAnalysis.Api.DTOs.Common;
using ResumeAnalysis.Api.DTOs.Resumes;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Services.Storage;

public class CandidateResumeTests : IClassFixture<ResumeTestFactory>
{
    private readonly ResumeTestFactory _factory;
    public CandidateResumeTests(ResumeTestFactory factory) => _factory = factory;

    private async Task<(HttpClient client, string email, string token)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"cr_{Guid.NewGuid():N}@example.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "P@ssw0rd123!",
            FullName = "Initial Name",
            Phone = "111"
        });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = body!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email, token);
    }

    private static byte[] MinimalPdfBytes(int size = 200)
    {
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n%Test\n1 0 obj\n<<>>\nendobj\n");
        if (size <= header.Length) return header.Take(size).ToArray();
        var arr = new byte[size];
        Array.Copy(header, arr, header.Length);
        for (int i = header.Length; i < size; i++) arr[i] = 0x20; // space
        return arr;
    }

    private static MultipartFormDataContent CreatePdfContent(byte[] bytes, string fileName = "resume.pdf")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Candidate_GetMe_And_UpdateMe_Works()
    {
        var (client, email, _) = await CreateAuthenticatedClientAsync();

        // GET me
        var get = await client.GetAsync("/api/candidates/me");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadFromJsonAsync<ApiResponse<CandidateDto>>();
        Assert.Equal("Initial Name", getBody!.Data!.FullName);

        // PUT me
        var put = await client.PutAsJsonAsync("/api/candidates/me", new UpdateCandidateRequest
        {
            FullName = "Updated Name",
            Phone = "999888777"
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var putBody = await put.Content.ReadFromJsonAsync<ApiResponse<CandidateDto>>();
        Assert.Equal("Updated Name", putBody!.Data!.FullName);
        Assert.Equal("999888777", putBody.Data.Phone);

        // Verify GET again
        var get2 = await client.GetAsync("/api/candidates/me");
        var get2Body = await get2.Content.ReadFromJsonAsync<ApiResponse<CandidateDto>>();
        Assert.Equal("Updated Name", get2Body!.Data!.FullName);

        // Validation: short name -> 400
        var bad = await client.PutAsJsonAsync("/api/candidates/me", new UpdateCandidateRequest { FullName = "A", Phone = null });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        // Unauthorized: no token
        var anon = _factory.CreateClient();
        var unauth = await anon.GetAsync("/api/candidates/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        var unauthPut = await anon.PutAsJsonAsync("/api/candidates/me", new UpdateCandidateRequest { FullName = "Nope", Phone = null });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthPut.StatusCode);
    }

    [Fact]
    public async Task Resume_Upload_List_Versioning_Download_Delete_Works()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();

        // Upload first version
        var pdf1 = MinimalPdfBytes(500);
        var content1 = CreatePdfContent(pdf1, "my_resume.pdf");
        var upload1 = await client.PostAsync("/api/resumes", content1);
        Assert.Equal(HttpStatusCode.Created, upload1.StatusCode);
        var dto1 = await upload1.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>();
        Assert.NotNull(dto1!.Data);
        Assert.Equal(1, dto1.Data!.VersionNumber);
        Assert.True(dto1.Data.IsLatest);
        Assert.Equal("Uploaded", dto1.Data.Status);
        var id1 = dto1.Data.Id;

        // Upload second version (same candidate)
        var pdf2 = MinimalPdfBytes(800);
        var content2 = CreatePdfContent(pdf2, "my_resume_v2.pdf");
        var upload2 = await client.PostAsync("/api/resumes", content2);
        Assert.Equal(HttpStatusCode.Created, upload2.StatusCode);
        var dto2 = await upload2.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>();
        Assert.Equal(2, dto2!.Data!.VersionNumber);
        Assert.True(dto2.Data.IsLatest);
        var id2 = dto2.Data.Id;

        // List should have 2, ordered desc version
        var listResp = await client.GetAsync("/api/resumes");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var listBody = await listResp.Content.ReadFromJsonAsync<ApiResponse<List<ResumeDto>>>();
        Assert.Equal(2, listBody!.Data!.Count);
        Assert.Equal(2, listBody.Data[0].VersionNumber);
        Assert.True(listBody.Data[0].IsLatest);
        Assert.False(listBody.Data[1].IsLatest);
        Assert.Equal(1, listBody.Data[1].VersionNumber);

        // Get latest endpoint
        var latestResp = await client.GetAsync("/api/resumes/latest");
        Assert.Equal(HttpStatusCode.OK, latestResp.StatusCode);
        var latestBody = await latestResp.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>();
        Assert.Equal(id2, latestBody!.Data!.Id);

        // Get single by id
        var get1 = await client.GetAsync($"/api/resumes/{id1}");
        Assert.Equal(HttpStatusCode.OK, get1.StatusCode);
        var get1Body = await get1.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>();
        Assert.Equal(1, get1Body!.Data!.VersionNumber);

        // Download
        var dl = await client.GetAsync($"/api/resumes/{id1}/download");
        Assert.Equal(HttpStatusCode.OK, dl.StatusCode);
        Assert.Equal("application/pdf", dl.Content.Headers.ContentType!.MediaType);
        var dlBytes = await dl.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdf1.Length, dlBytes.Length);
        Assert.Equal((byte)'%', dlBytes[0]);

        // Delete latest (id2) -> id1 should become latest
        var del = await client.DeleteAsync($"/api/resumes/{id2}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var listAfterDel = await client.GetAsync("/api/resumes");
        var listAfterBody = await listAfterDel.Content.ReadFromJsonAsync<ApiResponse<List<ResumeDto>>>();
        Assert.Single(listAfterBody!.Data!);
        Assert.Equal(id1, listAfterBody.Data[0].Id);
        Assert.True(listAfterBody.Data[0].IsLatest);

        // Download deleted should be 404
        var dlDeleted = await client.GetAsync($"/api/resumes/{id2}/download");
        Assert.Equal(HttpStatusCode.NotFound, dlDeleted.StatusCode);
    }

    [Fact]
    public async Task Resume_Validation_And_Security()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _, _) = await CreateAuthenticatedClientAsync();

        // Invalid file type (txt)
        var txtContent = new MultipartFormDataContent();
        var txtBytes = Encoding.UTF8.GetBytes("hello not pdf");
        var txtFile = new ByteArrayContent(txtBytes);
        txtFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        txtContent.Add(txtFile, "file", "resume.txt");
        var badType = await clientA.PostAsync("/api/resumes", txtContent);
        Assert.Equal(HttpStatusCode.BadRequest, badType.StatusCode);

        // Invalid magic bytes (pdf extension but not pdf content)
        var fakePdf = Encoding.UTF8.GetBytes("This is not a pdf");
        var fakeContent = CreatePdfContent(fakePdf, "fake.pdf");
        var badMagic = await clientA.PostAsync("/api/resumes", fakeContent);
        Assert.Equal(HttpStatusCode.BadRequest, badMagic.StatusCode);

        // File too large (6 MB > 5 MB)
        var large = MinimalPdfBytes(6 * 1024 * 1024);
        // Ensure header still %PDF
        large[0] = 0x25; large[1] = 0x50; large[2] = 0x44; large[3] = 0x46;
        var largeContent = CreatePdfContent(large, "large.pdf");
        var tooLarge = await clientA.PostAsync("/api/resumes", largeContent);
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);

        // Missing file -> 400
        var empty = new MultipartFormDataContent();
        var missing = await clientA.PostAsync("/api/resumes", empty);
        // Could be 400 BadRequest due to model binding
        Assert.True(missing.StatusCode == HttpStatusCode.BadRequest || missing.StatusCode == HttpStatusCode.UnsupportedMediaType);

        // Valid upload for A
        var pdf = MinimalPdfBytes(400);
        var okContent = CreatePdfContent(pdf, "valid.pdf");
        var ok = await clientA.PostAsync("/api/resumes", okContent);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var okBody = await ok.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>();
        var resumeId = okBody!.Data!.Id;

        // B cannot access A's resume
        var getByB = await clientB.GetAsync($"/api/resumes/{resumeId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByB.StatusCode);

        var dlByB = await clientB.GetAsync($"/api/resumes/{resumeId}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, dlByB.StatusCode);

        var delByB = await clientB.DeleteAsync($"/api/resumes/{resumeId}");
        Assert.Equal(HttpStatusCode.Unauthorized, delByB.StatusCode);

        // B's list should be empty
        var listB = await clientB.GetAsync("/api/resumes");
        var listBBody = await listB.Content.ReadFromJsonAsync<ApiResponse<List<ResumeDto>>>();
        Assert.Empty(listBBody!.Data!);

        // Unauthenticated -> 401
        var anon = _factory.CreateClient();
        var unauthUpload = await anon.PostAsync("/api/resumes", CreatePdfContent(pdf, "anon.pdf"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthUpload.StatusCode);
        var unauthList = await anon.GetAsync("/api/resumes");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);

        // Invalid GUID -> 404
        var fakeId = Guid.NewGuid();
        var notFound = await clientA.GetAsync($"/api/resumes/{fakeId}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        var delNotFound = await clientA.DeleteAsync($"/api/resumes/{fakeId}");
        Assert.Equal(HttpStatusCode.NotFound, delNotFound.StatusCode);
    }
}

public class ResumeTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbName = $"ResumeTestDb_{Guid.NewGuid()}";
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"ResumeTestStorage_{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            var dbName = _dbName;
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));

            // Override storage settings to use temp path + small cleanup
            services.Configure<StorageSettings>(o =>
            {
                o.Path = _storagePath;
                o.MaxFileSizeBytes = 5 * 1024 * 1024;
                o.AllowedExtensions = new[] { ".pdf" };
                o.AllowedContentTypes = new[] { "application/pdf" };
            });

            // Ensure IFileStorage uses new settings: remove old and re-add
            var storageDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorage));
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
