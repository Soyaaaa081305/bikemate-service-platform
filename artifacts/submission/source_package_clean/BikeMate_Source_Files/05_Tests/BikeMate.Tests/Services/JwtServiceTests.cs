using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BikeMate.Api.Services;
using BikeMate.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace BikeMate.Tests.Services;

public sealed class JwtServiceTests
{
    private static JwtService CreateService(string key = "ThisIsASecretKeyForTestingPurposesOnlyXXX", string issuer = "TestIssuer", string audience = "TestAudience")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = issuer,
                ["Jwt:Audience"] = audience
            })
            .Build();
        return new JwtService(config);
    }

    private static User CreateUser(int userId = 1, string firstName = "John", string lastName = "Doe", string email = "john@example.com")
    {
        return new User
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var sut = CreateService();
        var user = CreateUser();

        var token = sut.GenerateToken(user, ["Customer"], DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var sut = CreateService();
        var user = CreateUser(userId: 42, email: "test@bikemate.com");

        var tokenString = sut.GenerateToken(user, ["Customer", "Mechanic"], DateTimeOffset.UtcNow.AddHours(1));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("42", token.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("42", token.Claims.First(c => c.Type == "user_id").Value);
        Assert.Equal("test@bikemate.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("John", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.GivenName).Value);
        Assert.Equal("Doe", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.FamilyName).Value);
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaims()
    {
        var sut = CreateService();
        var user = CreateUser();
        var roles = new[] { "Customer", "ShopAdmin" };

        var tokenString = sut.GenerateToken(user, roles, DateTimeOffset.UtcNow.AddHours(1));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        var roleClaims = token.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();

        Assert.Contains("Customer", roleClaims);
        Assert.Contains("ShopAdmin", roleClaims);
        Assert.Equal(2, roleClaims.Length);
    }

    [Fact]
    public void GenerateToken_SetsCorrectExpiration()
    {
        var sut = CreateService();
        var user = CreateUser();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var tokenString = sut.GenerateToken(user, ["Customer"], expiresAt);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // JWT timestamps are second-precision
        Assert.Equal(expiresAt.UtcDateTime.Date, token.ValidTo.Date);
    }

    [Fact]
    public void GenerateToken_SetsCorrectIssuerAndAudience()
    {
        var sut = CreateService(issuer: "BikeMate", audience: "BikeMateMobile");
        var user = CreateUser();

        var tokenString = sut.GenerateToken(user, ["Customer"], DateTimeOffset.UtcNow.AddHours(1));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("BikeMate", token.Issuer);
        Assert.Contains("BikeMateMobile", token.Audiences);
    }

    [Fact]
    public void GenerateToken_ThrowsWhenKeyIsMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var sut = new JwtService(config);
        var user = CreateUser();

        var exception = Assert.Throws<InvalidOperationException>(
            () => sut.GenerateToken(user, ["Customer"], DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Contains("Jwt:Key is not configured", exception.Message);
    }

    [Fact]
    public void GenerateToken_WithEmptyRoles_ProducesTokenWithNoRoleClaims()
    {
        var sut = CreateService();
        var user = CreateUser();

        var tokenString = sut.GenerateToken(user, Array.Empty<string>(), DateTimeOffset.UtcNow.AddHours(1));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        var roleClaims = token.Claims.Where(c => c.Type == "role").ToArray();

        Assert.Empty(roleClaims);
    }
}
