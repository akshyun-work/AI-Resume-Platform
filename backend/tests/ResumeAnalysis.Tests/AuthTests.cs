using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Auth;
using ResumeAnalysis.Api.DTOs.Common;

public class AuthTests : IClassFixture<CustomWebFactory>
{
    private readonly CustomWebFactory _factory;
    public AuthTests(CustomWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Login_And_GetMe_Works()
    {
        var client = _factory.CreateClient();
        var email = $"test_{Guid.NewGuid():N}@example.com";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "P@ssw0rd123!",
            FullName = "Test User",
            Phone = "1234567890"
        });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);
        var regBody = await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(regBody); Assert.True(regBody.Success); Assert.NotNull(regBody.Data?.Token);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "P@ssw0rd123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(loginBody?.Data?.Token);
        var token = loginBody!.Data!.Token;

        // Duplicate should be 409
        var dup = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "P@ssw0rd123!", FullName = "Dup" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // Wrong password -> 401
        var badLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        // Validation -> 400
        var badReg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = "bad", Password = "short", FullName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badReg.StatusCode);

        // GetMe with token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/api/candidates/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<ApiResponse<CandidateDto>>();
        Assert.Equal(email.ToLowerInvariant(), meBody!.Data!.Email);

        // No token -> 401
        var unauthClient = _factory.CreateClient();
        var noAuth = await unauthClient.GetAsync("/api/candidates/me");
        Assert.Equal(HttpStatusCode.Unauthorized, noAuth.StatusCode);
    }

    [Fact]
    public async Task Health_And_Swagger_Available()
    {
        var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
    }
}

public class CustomWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";
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
        });
    }
}
