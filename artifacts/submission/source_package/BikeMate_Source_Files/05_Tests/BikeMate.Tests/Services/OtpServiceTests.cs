using BikeMate.Api.Services;

namespace BikeMate.Tests.Services;

public sealed class OtpServiceTests
{
    private readonly OtpService _sut = new();

    [Fact]
    public void GenerateCode_ReturnsSixDigitString()
    {
        var code = _sut.GenerateCode();

        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out var numericValue));
        Assert.InRange(numericValue, 100000, 999999);
    }

    [Fact]
    public void GenerateCode_ReturnsVaryingCodes()
    {
        var codes = Enumerable.Range(0, 20).Select(_ => _sut.GenerateCode()).ToHashSet();

        // With 20 random codes, we expect more than 1 distinct value
        Assert.True(codes.Count > 1);
    }

    [Fact]
    public void HashCode_ReturnsNonEmptyString()
    {
        var hash = _sut.HashCode("123456");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void HashCode_ReturnsBcryptFormat()
    {
        var hash = _sut.HashCode("654321");

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void VerifyCode_ReturnsTrueForMatchingCode()
    {
        var code = "123456";
        var hash = _sut.HashCode(code);

        Assert.True(_sut.VerifyCode(code, hash));
    }

    [Fact]
    public void VerifyCode_ReturnsFalseForMismatchedCode()
    {
        var hash = _sut.HashCode("123456");

        Assert.False(_sut.VerifyCode("654321", hash));
    }

    [Fact]
    public void HashCode_ProducesDifferentHashesForSameInput()
    {
        var code = "111111";
        var hash1 = _sut.HashCode(code);
        var hash2 = _sut.HashCode(code);

        // BCrypt salts produce different hashes
        Assert.NotEqual(hash1, hash2);
        // But both still verify
        Assert.True(_sut.VerifyCode(code, hash1));
        Assert.True(_sut.VerifyCode(code, hash2));
    }
}
