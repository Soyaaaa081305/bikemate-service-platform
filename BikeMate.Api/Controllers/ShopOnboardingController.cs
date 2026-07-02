using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/shop-onboarding")]
public sealed class ShopOnboardingController(
    BikeMateDbContext db,
    IPasswordService passwordService,
    IOtpService otpService,
    IEmailService emailService) : ControllerBase
{
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopApplicationResponse>> Apply(
        ShopOwnerApplicationRequest dto,
        CancellationToken cancellationToken)
    {
        var firstName = Require(dto.FirstName, "First name");
        var lastName = Require(dto.LastName, "Last name");
        var email = AuthService.NormalizeEmail(dto.Email);
        var phoneNumber = Require(dto.PhoneNumber, "Phone number");
        var password = Require(dto.Password, "Password");
        var shopName = Require(dto.ShopName, "Shop name");
        var shopAddress = Require(dto.ShopAddress, "Shop address");
        var city = Require(dto.ShopCity, "Shop city");
        var province = Require(dto.ShopProvince, "Shop province");
        var validIdPath = Require(dto.ValidIdPath, "Valid ID");
        var businessPermitPath = Require(dto.BusinessPermitPath, "Business permit");
        var shopImagePath = Require(dto.ShopImagePath, "Shop image");
        var dtiRegistrationNumber = Require(dto.DtiRegistrationNumber, "DTI registration number");
        var ownerBirthdate = AgeRequirement.RequireAdult(dto.Birthdate, "Shop-admin owner");

        if (password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Users.AnyAsync(x => x.Email == email && x.AccountStatus != "deleted", cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (await db.Users.AnyAsync(x => x.PhoneNumber == phoneNumber && x.AccountStatus != "deleted", cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        if (await ShopExistsAsync(shopName, shopAddress, city, province, cancellationToken))
        {
            throw new InvalidOperationException("This shop is already registered.");
        }

        var roleId = await db.Roles
            .Where(x => x.RoleName == AppRoles.ShopAdmin)
            .Select(x => x.RoleId)
            .SingleAsync(cancellationToken);

        var owner = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordService.HashPassword(password),
            EmailVerified = false,
            AccountStatus = "pending",
            CreatedAt = DateTime.UtcNow,
            UserRoles =
            [
                new UserRole
                {
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                }
            ]
        };

        var shop = new Shop
        {
            Owner = owner,
            ShopName = shopName,
            ShopDescription = BuildShopDescription(dto.ShopDescription, dtiRegistrationNumber),
            AddressLine = BuildAddress(dto.ShopAddress, dto.ShopBarangay, dto.ShopCity, dto.ShopProvince, dto.ShopZipCode),
            City = city,
            Province = province,
            ContactNumber = phoneNumber,
            BusinessPermitUrl = businessPermitPath,
            ShopImageUrl = shopImagePath,
            OwnerValidIdUrl = validIdPath,
            OwnerMiddleName = CleanOptional(dto.MiddleName),
            OwnerSex = CleanOptional(dto.Sex),
            OwnerBirthdate = ownerBirthdate,
            OwnerAddressLine = CleanOptional(dto.Address),
            OwnerBarangay = CleanOptional(dto.Barangay),
            OwnerCity = CleanOptional(dto.City),
            OwnerProvince = CleanOptional(dto.Province),
            OwnerZipCode = CleanOptional(dto.ZipCode),
            ShopStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };

        db.Shops.Add(shop);
        await db.SaveChangesAsync(cancellationToken);

        var otpCode = otpService.GenerateCode();
        db.OtpVerifications.Add(new OtpVerification
        {
            UserId = owner.UserId,
            OtpHash = otpService.HashCode(otpCode),
            Purpose = "email_verification",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendOtpAsync(owner, otpCode, "email_verification", cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new ShopApplicationResponse(
            shop.ShopId,
            shop.ShopName,
            shop.ShopStatus,
            "Your shop application was submitted for BikeMate admin approval."));
    }

    [HttpPost("register-shop")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<ShopRegistrationResponse>> RegisterShop(
        ShopRegistrationRequest dto,
        CancellationToken cancellationToken)
    {
        var shopName = Require(dto.ShopName, "Shop name");
        var ownerName = Require(dto.OwnerName, "Shop owner");
        var shopAddress = Require(dto.ShopAddress, "Shop address");
        var city = Require(dto.City, "City");
        var province = Require(dto.Province, "Province");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await ShopExistsAsync(shopName, shopAddress, city, province, cancellationToken))
        {
            throw new InvalidOperationException("This shop is already registered.");
        }

        var roleId = await db.Roles
            .Where(x => x.RoleName == AppRoles.ShopAdmin)
            .Select(x => x.RoleId)
            .SingleAsync(cancellationToken);
        var (firstName, lastName) = SplitName(ownerName);
        var owner = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = await CreatePendingOwnerEmailAsync(cancellationToken),
            AccountStatus = "pending",
            CreatedAt = DateTime.UtcNow,
            UserRoles =
            [
                new UserRole
                {
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                }
            ]
        };

        var shop = new Shop
        {
            Owner = owner,
            ShopName = shopName,
            ShopDescription = BuildShopDescription(dto.ShopDescription, dto.DtiRegistrationNumber),
            AddressLine = shopAddress,
            City = city,
            Province = province,
            BusinessPermitUrl = CleanOptional(dto.BusinessPermitPath),
            ShopImageUrl = CleanOptional(dto.ShopImagePath),
            ShopStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };

        db.Shops.Add(shop);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new ShopRegistrationResponse(
            shop.ShopId,
            shop.ShopName,
            shop.ShopStatus,
            "Shop was registered for approval."));
    }

    [HttpPost("shop-exists")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopExistsResponse>> ShopExists(
        ShopExistsRequest dto,
        CancellationToken cancellationToken)
    {
        var shopName = Require(dto.ShopName, "Shop name");
        var shopAddress = BuildAddress(dto.ShopAddress, dto.ShopBarangay, dto.ShopCity, dto.ShopProvince, dto.ShopZipCode);
        var exists = await ShopExistsAsync(
            shopName,
            shopAddress,
            dto.ShopCity,
            dto.ShopProvince,
            cancellationToken,
            requireVerified: true);
        return Ok(new ShopExistsResponse(exists));
    }

    [HttpGet("application-status")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopApplicationStatusResponse>> ApplicationStatus(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = AuthService.NormalizeEmail(email);
        var application = await db.Shops
            .AsNoTracking()
            .Include(x => x.Owner)
            .Where(x => x.Owner != null && x.Owner.Email == normalizedEmail)
            .Where(x => x.ShopStatus != "deleted")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ShopApplicationStatusResponse(
                x.ShopId,
                x.ShopName,
                x.ShopStatus,
                x.Owner!.AccountStatus,
                x.Owner.EmailVerified,
                x.UpdatedAt ?? x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return application is null
            ? NotFound(new { error = "No shop application was found for this email address." })
            : Ok(application);
    }

    private async Task<bool> ShopExistsAsync(
        string shopName,
        string? shopAddress,
        string? city,
        string? province,
        CancellationToken cancellationToken,
        bool requireVerified = false)
    {
        var normalizedShopName = shopName.Trim().ToLowerInvariant();
        var shops = await db.Shops
            .Where(x => x.ShopName.ToLower() == normalizedShopName)
            .Where(x =>
                x.ShopStatus.ToLower().Trim() == "pending" ||
                x.ShopStatus.ToLower().Trim() == "verified" ||
                x.ShopStatus.ToLower().Trim() == "suspended")
            .ToArrayAsync(cancellationToken);

        return shops.Any(shop =>
            (!requireVerified || IsVerifiedShop(shop)) &&
            ShopMatchesLocation(shop, shopAddress, city, province));
    }

    private async Task<string> CreatePendingOwnerEmailAsync(CancellationToken cancellationToken)
    {
        string email;
        do
        {
            email = $"pending-shop-admin-{Guid.NewGuid():N}@bikemates.local";
        }
        while (await db.Users.AnyAsync(x => x.Email == email, cancellationToken));

        return email;
    }

    private static bool ShopMatchesLocation(Shop shop, string? shopAddress, string? city, string? province)
    {
        var address = CleanOptional(shopAddress);
        return string.IsNullOrWhiteSpace(address) ||
            string.Equals(shop.AddressLine?.Trim(), address, StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(shop.City?.Trim(), city?.Trim(), StringComparison.OrdinalIgnoreCase) &&
             string.Equals(shop.Province?.Trim(), province?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsVerifiedShop(Shop shop)
    {
        return string.Equals(shop.ShopStatus?.Trim(), "verified", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAddress(params string?[] parts)
    {
        return string.Join(", ", parts
            .Select(part => CleanOptional(part))
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? BuildShopDescription(string? description, string? dtiRegistrationNumber)
    {
        var cleanDescription = CleanOptional(description);
        var cleanDti = CleanOptional(dtiRegistrationNumber);
        if (string.IsNullOrWhiteSpace(cleanDti))
        {
            return cleanDescription;
        }

        if (string.IsNullOrWhiteSpace(cleanDescription))
        {
            return $"DTI Registration: {cleanDti}";
        }

        return $"{cleanDescription}\nDTI Registration: {cleanDti}";
    }

    private static string Require(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? (parts[0], "Admin")
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }
}

public sealed record ShopRegistrationRequest(
    string ShopName,
    string OwnerName,
    string? ShopDescription,
    string ShopAddress,
    string City,
    string Province,
    string? BusinessPermitPath,
    string? ShopImagePath,
    string? DtiRegistrationNumber);

public sealed record ShopRegistrationResponse(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string Message);

public sealed record ShopOwnerApplicationRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Sex,
    string? Birthdate,
    string Email,
    string PhoneNumber,
    string Password,
    string? Province,
    string? City,
    string? Barangay,
    string? Address,
    string? ZipCode,
    string ValidIdPath,
    string ShopName,
    string? ShopDescription,
    string? ShopProvince,
    string? ShopCity,
    string? ShopBarangay,
    string? ShopAddress,
    string? ShopZipCode,
    string BusinessPermitPath,
    string ShopImagePath,
    string DtiRegistrationNumber);

public sealed record ShopApplicationResponse(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string Message);

public sealed record ShopApplicationStatusResponse(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string AccountStatus,
    bool EmailVerified,
    DateTime UpdatedAt);

public sealed record ShopExistsRequest(
    string ShopName,
    string? ShopProvince,
    string? ShopCity,
    string? ShopBarangay,
    string? ShopAddress,
    string? ShopZipCode);

public sealed record ShopExistsResponse(bool Exists);
