using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using ProductService.DTOs;
using Testcontainers.MongoDb;
using Xunit;

namespace ProductService.Tests.Integration;

public class ProductAuthorizationTests : IAsyncLifetime
{
    private const string TestJwtKey = "test-signing-key-at-least-32-characters-long";
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder("mongo:7").Build();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MongoDb:ConnectionString", _mongoContainer.GetConnectionString());
            builder.UseSetting("MongoDb:DatabaseName", "ProductServiceTestsDb");
            builder.UseSetting("Jwt:Issuer", "AuthService");
            builder.UseSetting("Jwt:Audience", "ECommerceMicroservices");
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:ExpiresInMinutes", "60");
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }

    private static string GenerateTestToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, "tester@example.com"),
            new(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer: "AuthService",
            audience: "ECommerceMicroservices",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CreateProduct_NoToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Test", "desc", 9.99m, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_CustomerRole_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken("Customer"));

        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Test", "desc", 9.99m, 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AdminRole_ReturnsCreated()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken("Admin"));

        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Test", "desc", 9.99m, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProducts_NoToken_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}