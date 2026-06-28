using System.Security.Cryptography;
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
    IPasswordService passwordService) : ControllerBase
{
    private const string ShopAccessPurposePrefix = "shop_access_code:";

    [HttpPost("register-shop")]
    [AllowAnonymous]
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

        var accessCode = GenerateAccessCode();
        db.OtpVerifications.Add(new OtpVerification
        {
            UserId = owner.UserId,
            OtpHash = passwordService.HashPassword(accessCode),
            Purpose = ShopAccessPurposePrefix + shop.ShopId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new ShopRegistrationResponse(shop.ShopId, shop.ShopName, accessCode, string.Empty));
    }

    [HttpPost("shop-exists")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopExistsResponse>> ShopExists(
        ShopExistsRequest dto,
        CancellationToken cancellationToken)
    {
        var shopName = Require(dto.ShopName, "Shop name");
        var shopAddress = BuildAddress(dto.ShopAddress, dto.ShopBarangay, dto.ShopCity, dto.ShopProvince, dto.ShopZipCode);
        var exists = await ShopExistsAsync(shopName, shopAddress, dto.ShopCity, dto.ShopProvince, cancellationToken);
        return Ok(new ShopExistsResponse(exists));
    }

    [HttpPost("create-account")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopAdminAccountResponse>> CreateAccount(
        ShopAdminAccountRequest dto,
        CancellationToken cancellationToken)
    {
        var firstName = Require(dto.FirstName, "First name");
        var lastName = Require(dto.LastName, "Last name");
        var email = AuthService.NormalizeEmail(dto.Email);
        var phoneNumber = Require(dto.PhoneNumber, "Phone number");
        var password = Require(dto.Password, "Password");
        var shopName = Require(dto.ShopName, "Shop name");
        var accessCode = Require(dto.AccessCode, "Access code");
        var shopAddress = BuildAddress(dto.ShopAddress, dto.ShopBarangay, dto.ShopCity, dto.ShopProvince, dto.ShopZipCode);

        if (password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var candidates = await LoadAccessCodeCandidatesAsync(shopName, shopAddress, dto.ShopCity, dto.ShopProvince, cancellationToken);
        var matched = candidates.FirstOrDefault(x => passwordService.VerifyPassword(accessCode, x.Otp.OtpHash));
        if (matched is null)
        {
            foreach (var candidate in candidates)
            {
                candidate.Otp.Attempts++;
            }

            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Bike shop not found, or the access code is invalid.");
        }

        var existingUser = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existingUser is not null && existingUser.UserId != matched.Shop.OwnerUserId)
        {
            throw new InvalidOperationException("That email is already used by another account.");
        }

        var owner = await db.Users.SingleAsync(x => x.UserId == matched.Shop.OwnerUserId, cancellationToken);
        owner.FirstName = firstName;
        owner.LastName = lastName;
        owner.Email = email;
        owner.PhoneNumber = phoneNumber;
        owner.PasswordHash = passwordService.HashPassword(password);
        owner.EmailVerified = true;
        owner.AccountStatus = "active";
        owner.UpdatedAt = DateTime.UtcNow;

        await EnsureShopAdminRoleAsync(owner.UserId, cancellationToken);
        matched.Otp.ConsumedAt = DateTime.UtcNow;
        matched.Otp.Attempts++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new ShopAdminAccountResponse(
            owner.UserId,
            owner.FirstName,
            owner.LastName,
            owner.Email,
            matched.Shop.ShopId,
            matched.Shop.ShopName,
            matched.Shop.ShopStatus));
    }

    private async Task<bool> ShopExistsAsync(
        string shopName,
        string? shopAddress,
        string? city,
        string? province,
        CancellationToken cancellationToken)
    {
        var normalizedShopName = shopName.Trim().ToLowerInvariant();
        var shops = await db.Shops
            .Where(x => x.ShopName.ToLower() == normalizedShopName)
            .ToArrayAsync(cancellationToken);

        return shops.Any(shop => ShopMatchesLocation(shop, shopAddress, city, province));
    }

    private async Task<List<ShopAccessCodeCandidate>> LoadAccessCodeCandidatesAsync(
        string shopName,
        string shopAddress,
        string? city,
        string? province,
        CancellationToken cancellationToken)
    {
        var normalizedShopName = shopName.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var candidates = await (
            from otp in db.OtpVerifications
            join shop in db.Shops on otp.UserId equals shop.OwnerUserId
            where otp.ConsumedAt == null
                && otp.ExpiresAt > now
                && otp.Purpose.StartsWith(ShopAccessPurposePrefix)
                && shop.ShopName.ToLower() == normalizedShopName
            select new ShopAccessCodeCandidate(otp, shop))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(x => string.Equals(x.Otp.Purpose, ShopAccessPurposePrefix + x.Shop.ShopId, StringComparison.OrdinalIgnoreCase))
            .Where(x => ShopMatchesLocation(x.Shop, shopAddress, city, province))
            .ToList();
    }

    private async Task EnsureShopAdminRoleAsync(int userId, CancellationToken cancellationToken)
    {
        var roleId = await db.Roles
            .Where(x => x.RoleName == AppRoles.ShopAdmin)
            .Select(x => x.RoleId)
            .SingleAsync(cancellationToken);

        if (await db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken))
        {
            return;
        }

        db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        });
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

    private static string GenerateAccessCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> code = stackalloc char[9];
        for (var i = 0; i < code.Length; i++)
        {
            code[i] = i == 4 ? '-' : alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(code);
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

    private sealed record ShopAccessCodeCandidate(OtpVerification Otp, Shop Shop);
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
    string AccessCode,
    string AdminEmail);

public sealed record ShopExistsRequest(
    string ShopName,
    string? ShopProvince,
    string? ShopCity,
    string? ShopBarangay,
    string? ShopAddress,
    string? ShopZipCode);

public sealed record ShopExistsResponse(bool Exists);

public sealed record ShopAdminAccountRequest(
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
    string? ValidIdPath,
    string ShopName,
    string? ShopProvince,
    string? ShopCity,
    string? ShopBarangay,
    string? ShopAddress,
    string? ShopZipCode,
    string AccessCode);

public sealed record ShopAdminAccountResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    int ShopId,
    string ShopName,
    string ShopStatus);
