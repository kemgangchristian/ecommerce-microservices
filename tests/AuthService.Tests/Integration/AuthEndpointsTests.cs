using System.Net;
using System.Net.Http.Json;
using AuthService.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace AuthService.Tests.Integration;

public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("AuthServiceTestsDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:AuthDb", _postgresContainer.GetConnectionString());
            builder.UseSetting("Jwt:Issuer", "AuthService");
            builder.UseSetting("Jwt:Audience", "ECommerceMicroservices");
            builder.UseSetting("Jwt:Key", "test-signing-key-at-least-32-characters-long");
            builder.UseSetting("Jwt:ExpiresInMinutes", "60");
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated_WithoutAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("newuser@example.com", "Test1234!", "New User"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.Equal("newuser@example.com", body!.Email);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login@example.com", "Test1234!", "Login User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login@example.com", "Test1234!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("wrongpass@example.com", "Test1234!", "User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wrongpass@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}