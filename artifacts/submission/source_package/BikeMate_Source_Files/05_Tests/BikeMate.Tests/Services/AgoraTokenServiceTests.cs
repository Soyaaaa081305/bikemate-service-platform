using BikeMate.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BikeMate.Tests.Services;

public sealed class AgoraTokenServiceTests
{
    private static AgoraTokenService CreateService(string? appId = null, string? certificate = null, int? lifetimeSeconds = null)
    {
        var configValues = new Dictionary<string, string?>();
        if (appId is not null) configValues["Agora:AppId"] = appId;
        if (certificate is not null) configValues["Agora:PrimaryCertificate"] = certificate;
        if (lifetimeSeconds is not null) configValues["Agora:TokenLifetimeSeconds"] = lifetimeSeconds.Value.ToString();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AgoraTokenService>>();
        return new AgoraTokenService(config, logger.Object);
    }

    [Fact]
    public void CreateEmergencyCallSession_ReturnsConfigurationMissing_WhenAppIdMissing()
    {
        var sut = CreateService(appId: null, certificate: null);

        var result = sut.CreateEmergencyCallSession(1, 10, DateTime.UtcNow);

        Assert.Equal("ConfigurationMissing", result.CallStatus);
        Assert.Contains("not configured", result.Message);
        Assert.Null(result.Token);
    }

    [Fact]
    public void CreateEmergencyCallSession_ReturnsConfigurationMissing_WhenAppIdStartsWithYOUR()
    {
        var sut = CreateService(appId: "YOUR_APP_ID_HERE_placeholder1234", certificate: "abcdef01234567890abcdef012345678");

        var result = sut.CreateEmergencyCallSession(1, 10, DateTime.UtcNow);

        Assert.Equal("ConfigurationMissing", result.CallStatus);
    }

    [Fact]
    public void CreateEmergencyCallSession_ReturnsConfigurationMissing_WhenAppIdNotExactly32HexChars()
    {
        var sut = CreateService(appId: "tooshort", certificate: "abcdef01234567890abcdef012345678");

        var result = sut.CreateEmergencyCallSession(1, 10, DateTime.UtcNow);

        Assert.Equal("ConfigurationMissing", result.CallStatus);
    }

    [Fact]
    public void CreateEmergencyCallSession_ReturnsTokenReady_WhenConfigured()
    {
        // Valid 32-character hex strings
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210");

        var result = sut.CreateEmergencyCallSession(42, 10, DateTime.UtcNow);

        Assert.Equal("TokenReady", result.CallStatus);
        Assert.NotNull(result.Token);
        Assert.StartsWith("007", result.Token);
        Assert.Equal(42, result.RequestId);
    }

    [Fact]
    public void CreateEmergencyCallSession_SetsChannelNameWithRequestId()
    {
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210");

        var result = sut.CreateEmergencyCallSession(99, 5, DateTime.UtcNow);

        Assert.Equal("bikemate-emergency-99", result.ChannelName);
    }

    [Fact]
    public void CreateEmergencyCallSession_SetsUidFromUserId()
    {
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210");

        var result = sut.CreateEmergencyCallSession(1, 42, DateTime.UtcNow);

        Assert.Equal(42u, result.Uid);
    }

    [Fact]
    public void CreateEmergencyCallSession_UsesRequestIdAsUid_WhenUserIdIsZeroOrNegative()
    {
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210");

        var result = sut.CreateEmergencyCallSession(55, 0, DateTime.UtcNow);

        Assert.Equal(55u, result.Uid);
    }

    [Fact]
    public void CreateEmergencyCallSession_SetsExpiresAt()
    {
        var startTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210",
            lifetimeSeconds: 3600);

        var result = sut.CreateEmergencyCallSession(1, 10, startTime);

        Assert.Equal(startTime.AddSeconds(3600), result.ExpiresAt);
    }

    [Fact]
    public void CreateEmergencyCallSession_DefaultLifetimeIs1800Seconds()
    {
        var startTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210");

        var result = sut.CreateEmergencyCallSession(1, 10, startTime);

        Assert.Equal(startTime.AddSeconds(1800), result.ExpiresAt);
    }

    [Fact]
    public void CreateEmergencyCallSession_ClampsLifetimeToMinimum60()
    {
        var startTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210",
            lifetimeSeconds: 5); // Below minimum

        var result = sut.CreateEmergencyCallSession(1, 10, startTime);

        Assert.Equal(startTime.AddSeconds(60), result.ExpiresAt);
    }

    [Fact]
    public void CreateEmergencyCallSession_ClampsLifetimeToMaximum86400()
    {
        var startTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var sut = CreateService(
            appId: "0123456789abcdef0123456789abcdef",
            certificate: "fedcba9876543210fedcba9876543210",
            lifetimeSeconds: 200000); // Above maximum

        var result = sut.CreateEmergencyCallSession(1, 10, startTime);

        Assert.Equal(startTime.AddSeconds(86400), result.ExpiresAt);
    }
}
