using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AuthService.Tests.Services;

public class JwtTokenServiceTests
{
    private static IJwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AuthService",
                ["Jwt:Audience"] = "ECommerceMicroservices",
                ["Jwt:Key"] = "test-signing-key-at-least-32-characters-long",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_IncludesEmailAndFullNameClaims()
    {
        var service = CreateService();
        var user = new ApplicationUser { Id = "user-1", Email = "test@example.com", FullName = "Test User" };

        var (token, expiresAt) = service.GenerateToken(user, new List<string> { "Customer" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == "fullName").Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Customer");
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_MultipleRoles_AllIncludedAsClaims()
    {
        var service = CreateService();
        var user = new ApplicationUser { Id = "user-2", Email = "admin@example.com", FullName = "Admin User" };

        var (token, _) = service.GenerateToken(user, new List<string> { "Admin", "Customer" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Customer", roleClaims);
    }
}