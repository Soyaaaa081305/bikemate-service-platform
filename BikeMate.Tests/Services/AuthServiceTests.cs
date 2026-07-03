using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BikeMate.Tests.Services;

public sealed class AuthServiceTests : IDisposable
{
    private static DateTime AdultBirthdate => DateTime.Today.AddYears(-21);
    private static DateTime UnderageBirthdate => DateTime.Today.AddYears(-17);

    private readonly BikeMateDbContext _db;
    private readonly AuthService _sut;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IGoogleAuthService> _googleAuthService = new();

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<BikeMateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BikeMateDbContext(options);

        // Seed required roles
        _db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = AppRoles.Customer },
            new Role { RoleId = 2, RoleName = AppRoles.Mechanic },
            new Role { RoleId = 3, RoleName = AppRoles.ShopAdmin },
            new Role { RoleId = 4, RoleName = AppRoles.SystemAdmin });
        _db.SaveChanges();

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<DateTimeOffset>()))
            .Returns("test-token");

        _sut = new AuthService(
            _db,
            jwtService.Object,
            new PasswordService(),
            new OtpService(),
            _emailService.Object,
            _googleAuthService.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task RegisterAsync_CreatesUserWithCorrectData()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass1234!", "Pass1234!", "09171234567", "Customer", AdultBirthdate);

        var result = await _sut.RegisterAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal("jane@test.com", result.User.Email);
        Assert.Equal("Jane", result.User.FirstName);
        Assert.Contains("Customer", result.User.Roles);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenPasswordsDoNotMatch()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass1234!", "DifferentPass!", null, "Customer", AdultBirthdate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("Passwords do not match", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsForInvalidRole()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass1234!", "Pass1234!", null, "InvalidRole");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid role", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenCustomerIsUnderage()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "underage@test.com", "Pass1234!", "Pass1234!", null, "Customer", UnderageBirthdate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("at least 18", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenMechanicIsUnderage()
    {
        var dto = new RegisterRequestDto("Mike", "Mech", "underage-mechanic@test.com", "Pass1234!", "Pass1234!", null, "Mechanic", UnderageBirthdate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("at least 18", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenEmailAlreadyRegistered()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "dupe@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(dto, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmail()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", " UPPER@TEST.COM  ", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);

        var result = await _sut.RegisterAsync(dto, CancellationToken.None);

        Assert.Equal("upper@test.com", result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_CreatesClientProfileForCustomerRole()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "customer@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);

        await _sut.RegisterAsync(dto, CancellationToken.None);

        var user = await _db.Users.Include(u => u.Client).SingleAsync(u => u.Email == "customer@test.com");
        Assert.NotNull(user.Client);
        Assert.Equal(AdultBirthdate, user.Client.Birthdate);
    }

    [Fact]
    public async Task RegisterAsync_CreatesMechanicProfileForMechanicRole()
    {
        var dto = new RegisterRequestDto("Mike", "Mech", "mechanic@test.com", "Pass1234!", "Pass1234!", null, "Mechanic", AdultBirthdate);

        await _sut.RegisterAsync(dto, CancellationToken.None);

        var user = await _db.Users.Include(u => u.Mechanic).SingleAsync(u => u.Email == "mechanic@test.com");
        Assert.NotNull(user.Mechanic);
        Assert.Equal("offline", user.Mechanic.AvailabilityStatus);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenForValidCredentials()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "login@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(registerDto, CancellationToken.None);
        var user = await _db.Users.SingleAsync(u => u.Email == "login@test.com");
        user.AccountStatus = "active";
        user.EmailVerified = true;
        await _db.SaveChangesAsync();

        var loginDto = new LoginRequestDto("login@test.com", "Pass1234!");
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_AllowsPendingCustomerToFinishSetup()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "pending-login@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var loginDto = new LoginRequestDto("pending-login@test.com", "Pass1234!");
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal("pending", result.User.AccountStatus);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForInvalidPassword()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "login2@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var loginDto = new LoginRequestDto("login2@test.com", "WrongPassword!");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForNonExistentUser()
    {
        var loginDto = new LoginRequestDto("ghost@test.com", "Pass1234!");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForSuspendedAccount()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "suspended@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var user = await _db.Users.SingleAsync(u => u.Email == "suspended@test.com");
        user.AccountStatus = "suspended";
        await _db.SaveChangesAsync();

        var loginDto = new LoginRequestDto("suspended@test.com", "Pass1234!");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("not active", ex.Message);
    }

    [Fact]
    public async Task GetMeAsync_ReturnsUserProfile()
    {
        var registerDto = new RegisterRequestDto("Get", "Me", "getme@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);
        await _sut.RegisterAsync(registerDto, CancellationToken.None);
        var user = await _db.Users.SingleAsync(u => u.Email == "getme@test.com");

        var profile = await _sut.GetMeAsync(user.UserId, CancellationToken.None);

        Assert.Equal("Get", profile.FirstName);
        Assert.Equal("Me", profile.LastName);
        Assert.Equal("getme@test.com", profile.Email);
    }

    [Fact]
    public async Task RegisterAsync_SendsOtpEmail()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "otp@test.com", "Pass1234!", "Pass1234!", null, "Customer", AdultBirthdate);

        await _sut.RegisterAsync(dto, CancellationToken.None);

        _emailService.Verify(
            e => e.SendOtpAsync(It.IsAny<User>(), It.IsAny<string>(), "email_verification", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReleasesEmailAndPhoneFromDeletedAccount()
    {
        var deleted = new User
        {
            FirstName = "Old",
            LastName = "Account",
            Email = "reuse@test.com",
            PhoneNumber = "+639171234567",
            PasswordHash = "old-hash",
            AccountStatus = "deleted",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            AuthProviders =
            [
                new UserAuthProvider
                {
                    ProviderName = "google",
                    ProviderSubject = "old-google-subject",
                    ProviderEmail = "reuse@test.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                }
            ]
        };
        _db.Users.Add(deleted);
        await _db.SaveChangesAsync();

        var result = await _sut.RegisterAsync(
            new RegisterRequestDto(
                "New",
                "Account",
                "reuse@test.com",
                "Password123!",
                "Password123!",
                "09171234567",
                AppRoles.Customer,
                AdultBirthdate),
            CancellationToken.None);

        var users = await _db.Users
            .Include(x => x.AuthProviders)
            .OrderBy(x => x.UserId)
            .ToArrayAsync();
        var archived = users.Single(x => x.UserId == deleted.UserId);
        var replacement = users.Single(x => x.Email == "reuse@test.com");

        Assert.Equal("deleted", archived.AccountStatus);
        Assert.StartsWith("deleted-", archived.Email);
        Assert.Null(archived.PhoneNumber);
        Assert.Null(archived.PasswordHash);
        Assert.All(archived.AuthProviders, provider =>
        {
            Assert.Null(provider.ProviderSubject);
            Assert.Null(provider.ProviderEmail);
        });
        Assert.Equal(replacement.UserId, result.User.UserId);
        Assert.Equal("+639171234567", replacement.PhoneNumber);
    }
}
