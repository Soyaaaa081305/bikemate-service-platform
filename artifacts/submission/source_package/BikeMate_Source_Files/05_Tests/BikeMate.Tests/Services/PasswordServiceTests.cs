using BikeMate.Api.Services;

namespace BikeMate.Tests.Services;

public sealed class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var hash = _sut.HashPassword("TestPassword123!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void HashPassword_ReturnsDifferentHashesForSameInput()
    {
        var hash1 = _sut.HashPassword("TestPassword123!");
        var hash2 = _sut.HashPassword("TestPassword123!");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var password = "MySecurePassword!";
        var hash = _sut.HashPassword(password);

        Assert.True(_sut.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var hash = _sut.HashPassword("CorrectPassword");

        Assert.False(_sut.VerifyPassword("WrongPassword", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPassword_ReturnsFalseForNullOrWhitespaceHash(string? hash)
    {
        Assert.False(_sut.VerifyPassword("AnyPassword", hash));
    }

    [Fact]
    public void VerifyPassword_HandlesSha256PrefixedHash()
    {
        // The service supports legacy sha256: prefixed hashes
        var password = "test123";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var sha256Hash = "sha256:" + BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        Assert.True(_sut.VerifyPassword(password, sha256Hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForWrongSha256Hash()
    {
        var sha256Hash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

        Assert.False(_sut.VerifyPassword("test123", sha256Hash));
    }

    [Fact]
    public void HashPassword_ProducesBcryptFormat()
    {
        var hash = _sut.HashPassword("SomePassword");

        Assert.StartsWith("$2", hash);
    }
}
