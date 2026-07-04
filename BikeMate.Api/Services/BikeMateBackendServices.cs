using System.IdentityModel.Tokens.Jwt;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BikeMate.Api.Helpers;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Core.Helpers;
using BikeMate.Infrastructure.Data;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BikeMate.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, IReadOnlyCollection<string> roles, DateTimeOffset expiresAt);
}

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(User user, IReadOnlyCollection<string> roles, DateTimeOffset expiresAt)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "BikeMate";
        var audience = configuration["Jwt:Audience"] ?? "BikeMateMobile";
        var claims = new List<Claim>
        {
            new("sub", user.UserId.ToString()),
            new("user_id", user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string? passwordHash);
}

public sealed class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (passwordHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(passwordHash, HashSha256(password), StringComparison.OrdinalIgnoreCase);
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private static string HashSha256(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return "sha256:" + BitConverter.ToString(hash).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
    }
}

public interface IOtpService
{
    string GenerateCode();
    string HashCode(string code);
    bool VerifyCode(string code, string hash);
}

public sealed class OtpService : IOtpService
{
    public string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    public string HashCode(string code)
    {
        return BCrypt.Net.BCrypt.HashPassword(code);
    }

    public bool VerifyCode(string code, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(code, hash);
    }
}

public interface IEmailService
{
    Task SendOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken);
}

public sealed class EmailService(
    ILogger<EmailService> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IEmailService
{
    public Task SendOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken)
    {
        var message = BuildOtpEmail(user, code, purpose);
        return SendAsync(
            user.Email,
            message.Subject,
            message.PlainText,
            message.Html,
            cancellationToken,
            fallbackLog: "Development OTP for {Email} ({Purpose}): {OtpCode}",
            user.Email,
            purpose,
            code);
    }

    public Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken)
    {
        var message = BuildPasswordResetEmail(user, token);
        return SendAsync(
            user.Email,
            message.Subject,
            message.PlainText,
            message.Html,
            cancellationToken,
            fallbackLog: "Development password reset code for {Email}: {ResetToken}",
            user.Email,
            token);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string plainText,
        string html,
        CancellationToken cancellationToken,
        string fallbackLog,
        params object[] fallbackArgs)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(fallbackLog, fallbackArgs);
            return;
        }

        var fromEmail = configuration["SendGrid:FromEmail"] ?? configuration["Email:From"] ?? "noreply@bikemate.local";
        var fromName = configuration["SendGrid:FromName"] ?? "BikeMate";
        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = toEmail } }
                }
            },
            from = new { email = fromEmail, name = fromName },
            subject,
            content = new[]
            {
                new { type = "text/plain", value = plainText },
                new { type = "text/html", value = html }
            }
        });

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("SendGrid rejected email to {Email}. Status: {Status}. Body: {Body}", toEmail, response.StatusCode, error);
            throw new InvalidOperationException($"Email delivery failed (HTTP {(int)response.StatusCode}). Check the SendGrid API key and sender identity.");
        }
    }

    private static EmailMessage BuildOtpEmail(User user, string code, string purpose)
    {
        var firstName = SafeFirstName(user);
        var details = PurposeDetails(user, purpose);
        var subject = purpose == "email_verification"
            ? "Verify your BikeMate email"
            : "Your BikeMate security code";

        var plainText = string.Join(
            Environment.NewLine,
            [
                $"Hi {firstName},",
                string.Empty,
                details.Context,
                string.Empty,
                $"Your verification code is: {code}",
                string.Empty,
                "This code expires in 10 minutes. If you asked for a new code, use the latest email only.",
                details.NextStep,
                details.ApprovalNote,
                string.Empty,
                "For your safety, do not share this code with anyone. BikeMate staff will never ask for your OTP.",
                "If you did not request this code, you can ignore this email.",
                string.Empty,
                "BikeMate"
            ]);

        var html = BuildHtmlEmail(
            subject,
            firstName,
            details.Context,
            code,
            [
                "This code expires in 10 minutes.",
                "If you asked for a new code, use the latest email only.",
                details.NextStep,
                details.ApprovalNote,
                "Do not share this code with anyone. BikeMate staff will never ask for your OTP.",
                "If you did not request this code, you can ignore this email."
            ]);

        return new EmailMessage(subject, plainText, html);
    }

    private static EmailMessage BuildPasswordResetEmail(User user, string token)
    {
        const string subject = "Reset your BikeMate password";
        var firstName = SafeFirstName(user);
        const string context = "BikeMate received a request to reset the password for your account. This does not change your password yet.";
        var plainText = string.Join(
            Environment.NewLine,
            [
                $"Hi {firstName},",
                string.Empty,
                context,
                string.Empty,
                $"Your password reset code is: {token}",
                string.Empty,
                "This code expires in 15 minutes. If you requested another reset code, use the latest email only.",
                "Enter this code in the BikeMate password reset screen. After the code is accepted, enter and confirm your new password.",
                "Your current password remains active until the new password is saved.",
                "If you did not request a password reset, do not share this code and leave your password unchanged.",
                string.Empty,
                "BikeMate"
            ]);

        var html = BuildHtmlEmail(
            subject,
            firstName,
            context,
            token,
            [
                "This code expires in 15 minutes.",
                "If you requested another reset code, use the latest email only.",
                "Enter this code in the BikeMate password reset screen. After the code is accepted, enter and confirm your new password.",
                "Your current password remains active until the new password is saved.",
                "If you did not request a password reset, do not share this code and leave your password unchanged."
            ]);

        return new EmailMessage(subject, plainText, html);
    }

    private static string BuildHtmlEmail(
        string title,
        string firstName,
        string context,
        string code,
        IReadOnlyCollection<string> notes)
    {
        var body = new StringBuilder();
        body.Append("<div style=\"font-family:Arial,sans-serif;color:#242424;line-height:1.5;max-width:560px\">");
        body.Append("<h2 style=\"color:#ff6b00;margin-bottom:8px\">").Append(WebUtility.HtmlEncode(title)).Append("</h2>");
        body.Append("<p>Hi ").Append(WebUtility.HtmlEncode(firstName)).Append(",</p>");
        body.Append("<p>").Append(WebUtility.HtmlEncode(context)).Append("</p>");
        body.Append("<p style=\"font-size:14px;color:#6e6e6e;margin-bottom:6px\">Your BikeMate code is</p>");
        body.Append("<div style=\"font-size:30px;font-weight:700;letter-spacing:4px;background:#fff7f2;border:1px solid #ffd1bd;border-radius:8px;padding:14px 18px;display:inline-block;color:#242424\">");
        body.Append(WebUtility.HtmlEncode(code));
        body.Append("</div>");
        body.Append("<ul style=\"padding-left:20px;margin-top:18px\">");
        foreach (var note in notes)
        {
            body.Append("<li>").Append(WebUtility.HtmlEncode(note)).Append("</li>");
        }
        body.Append("</ul>");
        body.Append("<p style=\"color:#6e6e6e;font-size:13px\">BikeMate</p>");
        body.Append("</div>");
        return body.ToString();
    }

    private static string SafeFirstName(User user)
    {
        return string.IsNullOrWhiteSpace(user.FirstName)
            ? "there"
            : user.FirstName.Trim();
    }

    private static OtpPurposeDetails PurposeDetails(User user, string purpose)
    {
        var normalized = string.IsNullOrWhiteSpace(purpose)
            ? "email_verification"
            : purpose.Trim().ToLowerInvariant();

        if (normalized == "email_verification")
        {
            var roles = user.UserRoles
                .Select(x => x.Role?.RoleName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (roles.Contains(AppRoles.ShopAdmin))
            {
                normalized = "shop_admin_email_verification";
            }
            else if (roles.Contains(AppRoles.Mechanic))
            {
                normalized = "mechanic_email_verification";
            }
            else if (roles.Contains(AppRoles.Customer))
            {
                normalized = "customer_email_verification";
            }
        }

        return normalized switch
        {
            "customer_email_verification" => new OtpPurposeDetails(
                "BikeMate is verifying your email address before your customer profile can be reviewed.",
                "Enter this code in the BikeMate customer verification screen.",
                "After email verification, BikeMate admin still needs to approve your account before booking repair services is enabled."),
            "mechanic_email_verification" => new OtpPurposeDetails(
                "BikeMate is verifying the mechanic email address before the mechanic profile can be sent for admin review.",
                "Enter this code in the Shop Admin mechanic account creation screen.",
                "After verification, BikeMate admin still needs to approve the submitted identity and mechanic details before the account can receive jobs."),
            "shop_email_verification" or "shop_admin_email_verification" => new OtpPurposeDetails(
                "BikeMate is verifying the shop-admin email address before the bike shop application can move forward.",
                "Enter this code in the Shop Admin application screen.",
                "After verification, BikeMate admin still needs to approve the shop details, address, and submitted business documents."),
            "password_reset" => new OtpPurposeDetails(
                "BikeMate received a password security request for this account.",
                "Enter this code only in the BikeMate password reset screen.",
                "The code does not change your password by itself."),
            _ => new OtpPurposeDetails(
                "BikeMate is checking that this email address belongs to you before the account or application can move forward.",
                "Enter this code only inside the BikeMate app or BikeMate admin page that requested it.",
                "After email verification, customer, shop-admin, and mechanic accounts may still need BikeMate admin approval before all features are enabled.")
        };
    }

    private sealed record EmailMessage(string Subject, string PlainText, string Html);
    private sealed record OtpPurposeDetails(string Context, string NextStep, string ApprovalNote);
}

public interface IGoogleAuthService
{
    Task<(string Subject, string Email, string FirstName, string LastName)> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

public sealed class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
{
    public async Task<(string Subject, string Email, string FirstName, string LastName)> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new InvalidOperationException("Google sign-in did not return an ID token.");
        }

        var clientIds = configuration
            .GetSection("GoogleAuth:ClientIds")
            .GetChildren()
            .Select(x => x.Value)
            .Concat([
                configuration["GoogleAuth:ClientId"],
                configuration["GoogleAuth:AndroidClientId"],
                configuration["GoogleAuth:WebClientId"]
            ])
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (clientIds.Length > 0)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = clientIds,
                IssuedAtClockTolerance = TimeSpan.FromMinutes(15),
                ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
            });

            return (payload.Subject, payload.Email, payload.GivenName ?? "Google", payload.FamilyName ?? "User");
        }

        throw new InvalidOperationException("Google sign-in is not configured on the API. Set GoogleAuth:AndroidClientId/WebClientId and try again.");
    }
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken);
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto, CancellationToken cancellationToken);
    Task<UserProfileDto> GetMeAsync(int userId, CancellationToken cancellationToken);
    Task VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken cancellationToken);
    Task ResendOtpAsync(ResendOtpRequestDto dto, CancellationToken cancellationToken);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken cancellationToken);
    Task VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequestDto dto, CancellationToken cancellationToken);
    Task ResendPasswordResetOtpAsync(ResendPasswordResetOtpRequestDto dto, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken cancellationToken);
}

public sealed class AuthService(
    BikeMateDbContext db,
    IJwtService jwtService,
    IPasswordService passwordService,
    IOtpService otpService,
    IEmailService emailService,
    IGoogleAuthService googleAuthService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        ValidatePassword(dto.Password);
        if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        if (!AppRoles.All.Contains(dto.Role))
        {
            throw new InvalidOperationException("Invalid role.");
        }

        var birthdate = dto.Role == AppRoles.Customer || dto.Role == AppRoles.Mechanic
            ? AgeRequirement.RequireAdult(dto.Birthdate, dto.Role)
            : (DateTime?)null;

        var normalizedEmail = NormalizeEmail(dto.Email);
        var normalizedPhone = NormalizePhilippineMobile(dto.PhoneNumber);
        await DeletedAccountIdentity.ReleaseConflictsAsync(
            db,
            normalizedEmail,
            normalizedPhone,
            cancellationToken);

        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
            await db.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        var role = await db.Roles.SingleAsync(x => x.RoleName == dto.Role, cancellationToken);
        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = normalizedPhone,
            PasswordHash = passwordService.HashPassword(dto.Password),
            AccountStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole { RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
        db.Users.Add(user);
        AddRoleProfile(user, dto.Role, birthdate);
        await db.SaveChangesAsync(cancellationToken);
        await CreateAndSendOtpAsync(user, "email_verification", cancellationToken);

        return await CreateAuthResponseAsync(user.UserId, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (user.AccountStatus is "suspended" or "deleted" or "rejected")
        {
            throw new InvalidOperationException("This account is not active.");
        }

        if (user.AccountStatus == "pending")
        {
            var pendingRole = user.UserRoles.Select(x => x.Role?.RoleName).FirstOrDefault(role => !string.IsNullOrWhiteSpace(role)) ?? "account";
            if (pendingRole == AppRoles.Customer)
            {
                return CreateAuthResponse(user);
            }

            var pendingLabel = pendingRole == AppRoles.ShopAdmin ? "shop application" :
                pendingRole == AppRoles.Mechanic ? "mechanic application" :
                pendingRole == AppRoles.Customer ? "customer account" :
                "account";
            throw new InvalidOperationException($"Your {pendingLabel} is pending BikeMate admin approval.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto, CancellationToken cancellationToken)
    {
        var googleUser = await googleAuthService.ValidateAsync(dto.IdToken, cancellationToken);
        var normalizedEmail = NormalizeEmail(googleUser.Email);
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.AuthProviders)
            .Include(x => x.DeviceTokens)
            .SingleOrDefaultAsync(x => x.AuthProviders.Any(p => p.ProviderName == "google" && p.ProviderSubject == googleUser.Subject)
                || x.Email == normalizedEmail, cancellationToken);

        if (user?.AccountStatus == "deleted")
        {
            DeletedAccountIdentity.Anonymize(user);
            await db.SaveChangesAsync(cancellationToken);
            user = null;
        }

        if (user?.AccountStatus is "suspended" or "rejected")
        {
            throw new InvalidOperationException("This account is not active.");
        }

        if (user is null)
        {
            var roleName = AppRoles.All.Contains(dto.Role ?? string.Empty) ? dto.Role! : AppRoles.Customer;
            var role = await db.Roles.SingleAsync(x => x.RoleName == roleName, cancellationToken);
            user = new User
            {
                FirstName = googleUser.FirstName,
                LastName = googleUser.LastName,
                Email = normalizedEmail,
                EmailVerified = true,
                AccountStatus = "pending",
                CreatedAt = DateTime.UtcNow,
                UserRoles = [new UserRole { RoleId = role.RoleId, AssignedAt = DateTime.UtcNow }],
                AuthProviders = [new UserAuthProvider { ProviderName = "google", ProviderSubject = googleUser.Subject, ProviderEmail = googleUser.Email, CreatedAt = DateTime.UtcNow }]
            };
            db.Users.Add(user);
            AddRoleProfile(user, roleName);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!user.AuthProviders.Any(x =>
                     x.ProviderName == "google" &&
                     x.ProviderSubject == googleUser.Subject))
        {
            user.AuthProviders.Add(new UserAuthProvider
            {
                ProviderName = "google",
                ProviderSubject = googleUser.Subject,
                ProviderEmail = normalizedEmail,
                CreatedAt = DateTime.UtcNow
            });
            user.EmailVerified = true;

            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return await CreateAuthResponseAsync(user.UserId, cancellationToken);
    }

    public async Task<UserProfileDto> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithRolesAsync(userId, cancellationToken);
        return ToProfile(user);
    }

    public async Task VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(dto.Email);
        var purpose = NormalizeOtpPurpose(dto.Purpose);
        var code = NormalizeOtpCode(dto.OtpCode);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var otp = await db.OtpVerifications
            .Where(x => x.UserId == user.UserId && x.Purpose == purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("OTP was not found.");

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("OTP expired.");
        }

        if (otp.Attempts >= 5)
        {
            throw new InvalidOperationException("Too many OTP attempts.");
        }

        otp.Attempts++;
        if (!otpService.VerifyCode(code, otp.OtpHash))
        {
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Invalid OTP.");
        }

        otp.ConsumedAt = DateTime.UtcNow;
        if (purpose == "email_verification")
        {
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendOtpAsync(ResendOtpRequestDto dto, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(dto.Email);
        var purpose = NormalizeOtpPurpose(dto.Purpose);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        await CreateAndSendOtpAsync(user, purpose, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Email == normalizedEmail && x.AccountStatus != "deleted",
            cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Email does not exist in BikeMate.");
        }

        var now = DateTime.UtcNow;
        var openTokens = await db.PasswordResetTokens
            .Where(x => x.UserId == user.UserId && x.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var openToken in openTokens)
        {
            openToken.ConsumedAt = now;
        }

        var token = otpService.GenerateCode();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = passwordService.HashPassword(token),
            ExpiresAt = now.AddMinutes(15),
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordResetAsync(user, token, cancellationToken);
    }

    public async Task VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequestDto dto, CancellationToken cancellationToken)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var code = dto.OtpCode.Trim();
        if (code.Length is < 4 or > 8)
        {
            throw new InvalidOperationException("Enter the reset code from your email.");
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException("Reset code was not found or has expired.");
        var token = await db.PasswordResetTokens
            .Where(x => x.UserId == user.UserId && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Reset code was not found or has expired.");

        if (token.ExpiresAt < DateTime.UtcNow || !passwordService.VerifyPassword(code, token.TokenHash))
        {
            throw new InvalidOperationException("Reset code was not found or has expired.");
        }
    }

    public Task ResendPasswordResetOtpAsync(ResendPasswordResetOtpRequestDto dto, CancellationToken cancellationToken)
    {
        return ForgotPasswordAsync(new ForgotPasswordRequestDto(dto.Email), cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        ValidatePassword(dto.NewPassword);
        if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == dto.Email.ToLower(), cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var token = await db.PasswordResetTokens
            .Where(x => x.UserId == user.UserId && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Reset token was not found.");

        if (token.ExpiresAt < DateTime.UtcNow || !passwordService.VerifyPassword(dto.Token, token.TokenHash))
        {
            throw new InvalidOperationException("Invalid reset token.");
        }

        token.ConsumedAt = DateTime.UtcNow;
        user.PasswordHash = passwordService.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateAndSendOtpAsync(User user, string purpose, CancellationToken cancellationToken)
    {
        var code = otpService.GenerateCode();
        db.OtpVerifications.Add(new OtpVerification
        {
            UserId = user.UserId,
            Purpose = purpose,
            OtpHash = otpService.HashCode(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(user)
            .Collection(x => x.UserRoles)
            .Query()
            .Include(x => x.Role)
            .LoadAsync(cancellationToken);
        await emailService.SendOtpAsync(user, code, purpose, cancellationToken);
    }

    private static string NormalizeOtpPurpose(string? purpose)
    {
        return string.IsNullOrWhiteSpace(purpose)
            ? "email_verification"
            : purpose.Trim().ToLowerInvariant();
    }

    private static string NormalizeOtpCode(string? code)
    {
        var normalized = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("OTP code is required.");
        }

        return normalized;
    }

    public static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Email is required.");
        }

        try
        {
            var address = new MailAddress(normalized);
            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch
        {
            throw new InvalidOperationException("Enter a valid email address.");
        }

        return normalized;
    }

    public static string? NormalizePhilippineMobile(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var compact = Regex.Replace(phoneNumber.Trim(), @"[\s().-]", "");
        if (!Regex.IsMatch(compact, @"^(?:\+?63|0)9\d{9}$"))
        {
            throw new InvalidOperationException("Enter a valid Philippine mobile number, for example 09171234567 or +639171234567.");
        }

        return compact.StartsWith("09", StringComparison.Ordinal)
            ? $"+63{compact[1..]}"
            : compact.StartsWith("+63", StringComparison.Ordinal)
                ? compact
                : $"+{compact}";
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithRolesAsync(userId, cancellationToken);
        return CreateAuthResponse(user);
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var roles = user.UserRoles.Select(x => x.Role?.RoleName ?? string.Empty).Where(x => x.Length > 0).ToArray();
        var expires = DateTimeOffset.UtcNow.AddDays(7);
        return new AuthResponseDto(jwtService.GenerateToken(user, roles, expires), expires, ToProfile(user));
    }

    private async Task<User> LoadUserWithRolesAsync(int userId, CancellationToken cancellationToken)
    {
        return await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleAsync(x => x.UserId == userId, cancellationToken);
    }

    private static UserProfileDto ToProfile(User user)
    {
        return new UserProfileDto(
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.EmailVerified,
            user.AccountStatus,
            user.UserRoles.Select(x => x.Role?.RoleName ?? string.Empty).Where(x => x.Length > 0).ToArray());
    }

    private static void AddRoleProfile(User user, string role, DateTime? birthdate = null)
    {
        if (role == AppRoles.Customer)
        {
            user.Client = new Client { Birthdate = birthdate?.Date, CreatedAt = DateTime.UtcNow };
        }
        else if (role == AppRoles.Mechanic)
        {
            user.Mechanic = new Mechanic { AvailabilityStatus = "offline", CreatedAt = DateTime.UtcNow };
        }
    }
}

public interface IFileStorageService
{
    Task<UploadedFileDto> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken);
}

public sealed class FileStorageService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor) : IFileStorageService
{
    public async Task<UploadedFileDto> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        var upload = UploadFileRules.Validate(file, folder, configuration);
        var dayFolder = DateTime.UtcNow.ToString("yyyyMMdd");
        var storedFileName = $"{Guid.NewGuid():N}{upload.Extension.ToLowerInvariant()}";
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var targetDirectory = Path.Combine(webRoot, "uploads", upload.SafeFolder, dayFolder);
        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, storedFileName);
        await using (var stream = File.Create(targetPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var publicUrl = $"{UploadBaseUrl().TrimEnd('/')}/{upload.SafeFolder}/{dayFolder}/{storedFileName}";
        return new UploadedFileDto(publicUrl, upload.OriginalFileName, upload.ContentType, file.Length);
    }

    private string UploadBaseUrl()
    {
        var configured = configuration["Storage:BaseUrl"];
        var request = httpContextAccessor.HttpContext?.Request;
        if (!string.IsNullOrWhiteSpace(configured) &&
            (!IsLoopbackUrl(configured) || request is null || IsLoopbackHost(request.Host.Host)))
        {
            return configured;
        }

        return request is null
            ? "https://localhost:5001/uploads"
            : $"{request.Scheme}://{request.Host}/uploads";
    }

    private static bool IsLoopbackUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && IsLoopbackHost(uri.Host);
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }

}

public sealed class CloudinaryFileStorageService(IConfiguration configuration) : IFileStorageService
{
    public async Task<UploadedFileDto> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        var upload = UploadFileRules.Validate(file, folder, configuration);
        var cloudinary = CreateCloudinaryClient();
        await using var stream = file.OpenReadStream();

        var publicId = $"{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}";
        var folderName = CloudinaryFolder(upload.SafeFolder);
        var fileDescription = new FileDescription(upload.OriginalFileName, stream);
        RawUploadResult result;

        if (upload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            result = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = fileDescription,
                Folder = folderName,
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false
            }, cancellationToken);
        }
        else if (upload.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            result = await cloudinary.UploadAsync(new VideoUploadParams
            {
                File = fileDescription,
                Folder = folderName,
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false
            }, cancellationToken);
        }
        else
        {
            result = await cloudinary.UploadAsync(new RawUploadParams
            {
                File = fileDescription,
                Folder = folderName,
                PublicId = $"{publicId}{upload.Extension.ToLowerInvariant()}",
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false
            }, "raw", cancellationToken);
        }

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        var publicUrl = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            throw new InvalidOperationException("Cloudinary upload completed without a public URL.");
        }

        return new UploadedFileDto(publicUrl, upload.OriginalFileName, upload.ContentType, file.Length);
    }

    private Cloudinary CreateCloudinaryClient()
    {
        var cloudName = configuration["Cloudinary:CloudName"] ?? configuration["Storage:Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"] ?? configuration["Storage:Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"] ?? configuration["Storage:Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary storage is enabled, but Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret are not configured.");
        }

        return new Cloudinary(new Account(cloudName, apiKey, apiSecret))
        {
            Api = { Secure = true }
        };
    }

    private string CloudinaryFolder(string safeFolder)
    {
        var root = UploadFileRules.SanitizePathSegment(configuration["Cloudinary:Folder"] ?? configuration["Storage:Cloudinary:Folder"] ?? "bikemate");
        return $"{root}/{safeFolder}";
    }
}

internal static class UploadFileRules
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "video/mp4",
        "video/webm",
        "video/quicktime",
        "video/3gpp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf",
        ".txt",
        ".doc",
        ".docx",
        ".mp4",
        ".webm",
        ".mov",
        ".3gp"
    };

    public static UploadFileInfo Validate(IFormFile file, string folder, IConfiguration configuration)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Select a file before uploading.");
        }

        var maxBytes = configuration.GetValue<long?>("Storage:MaxFileBytes") ?? 50 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException($"The selected file is too large. Maximum size is {maxBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? GuessContentType(extension)
            : file.ContentType;

        if (!AllowedContentTypes.Contains(contentType) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("BikeMate accepts JPG, PNG, WEBP, PDF, TXT, DOC, DOCX, MP4, WEBM, MOV, and 3GP files.");
        }

        return new UploadFileInfo(
            SanitizePathSegment(folder),
            Path.GetFileName(file.FileName),
            extension,
            contentType);
    }

    public static string SanitizePathSegment(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "general" : value.Trim().ToLowerInvariant();
        var safe = new string(source.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "general" : safe;
    }

    private static string GuessContentType(string extension)
    {
        return ContentTypeHelper.GuessFromExtension(extension);
    }
}

internal sealed record UploadFileInfo(
    string SafeFolder,
    string OriginalFileName,
    string Extension,
    string ContentType);

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto, CancellationToken cancellationToken);
    Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, int? changedByUserId, string? notes, CancellationToken cancellationToken);
    IQueryable<ServiceRequest> Query();
    IQueryable<ServiceRequest> QueryForDto();
}

public sealed class ServiceRequestService(BikeMateDbContext db) : IServiceRequestService
{
    public IQueryable<ServiceRequest> Query()
    {
        return db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Include(x => x.LineItems);
    }

    public IQueryable<ServiceRequest> QueryForDto()
    {
        return db.ServiceRequests.AsNoTracking();
    }

    public async Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .Include(x => x.User)
            .SingleAsync(x => x.UserId == userId, cancellationToken);
        if (!string.Equals(client.User?.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Your customer account is pending BikeMate admin approval. Finish your profile setup and wait for approval before booking a repair.");
        }

        var pendingStatusId = await db.RequestStatuses.Where(x => x.StatusName == "pending").Select(x => x.StatusId).SingleAsync(cancellationToken);
        var serviceIds = MergeIds(dto.ShopServiceId, dto.ShopServiceIds);
        var selectedServices = serviceIds.Count == 0
            ? []
            : await db.ShopServices
                .Where(x => serviceIds.Contains(x.ShopServiceId) && x.IsActive)
                .OrderBy(x => x.ServiceName)
                .ToListAsync(cancellationToken);
        if (selectedServices.Count != serviceIds.Count)
        {
            throw new InvalidOperationException("Select available services before booking.");
        }

        if (dto.ShopId is not null && selectedServices.Any(x => x.ShopId != dto.ShopId))
        {
            throw new InvalidOperationException("Selected services do not belong to the selected shop.");
        }

        ValidateScheduledAt(dto.ScheduledAt);

        var selectedShopId = dto.ShopId ?? selectedServices.Select(x => (int?)x.ShopId).Distinct().SingleOrDefault();
        if (selectedShopId is not null && selectedServices.Select(x => x.ShopId).Distinct().Count() > 1)
        {
            throw new InvalidOperationException("Selected services must come from one shop.");
        }

        var productIds = MergeIds(null, dto.ProductIds);
        var selectedProducts = productIds.Count == 0 || selectedShopId is null
            ? []
            : await db.Products
                .Where(x => productIds.Contains(x.ProductId) && x.ShopId == selectedShopId && x.IsActive)
                .OrderBy(x => x.ProductName)
                .ToListAsync(cancellationToken);
        if (productIds.Count > 0 && selectedProducts.Count != productIds.Count)
        {
            throw new InvalidOperationException("Select available products from the selected shop.");
        }

        if (selectedProducts.Any(x => x.StockQuantity <= 0))
        {
            throw new InvalidOperationException("One or more selected products are out of stock.");
        }

        var estimatedTotal = selectedServices.Sum(x => x.BasePrice) + selectedProducts.Sum(x => x.Price);
        var primaryService = selectedServices.FirstOrDefault();

        var request = new ServiceRequest
        {
            ClientId = client.ClientId,
            ShopId = selectedShopId,
            ShopServiceId = primaryService?.ShopServiceId,
            MotorcycleId = dto.MotorcycleId,
            CurrentStatusId = pendingStatusId,
            IssueDescription = dto.IssueDescription,
            ServiceLocationAddress = dto.ServiceLocationAddress,
            ServiceLatitude = dto.ServiceLatitude,
            ServiceLongitude = dto.ServiceLongitude,
            ScheduledAt = dto.ScheduledAt,
            EstimatedTotal = estimatedTotal,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var service in selectedServices)
        {
            request.LineItems.Add(new ServiceRequestLineItem
            {
                ItemType = "service",
                ShopServiceId = service.ShopServiceId,
                ItemName = service.ServiceName,
                Quantity = 1,
                UnitPrice = service.BasePrice,
                LineTotal = service.BasePrice,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var product in selectedProducts)
        {
            request.LineItems.Add(new ServiceRequestLineItem
            {
                ItemType = "product",
                ProductId = product.ProductId,
                ItemName = product.ProductName,
                Quantity = 1,
                UnitPrice = product.Price,
                LineTotal = product.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return await Query().Where(x => x.RequestId == request.RequestId).Select(ToDtoExpression()).SingleAsync(cancellationToken);
    }

    public static IReadOnlyCollection<int> MergeIds(int? singleId, IReadOnlyCollection<int>? ids)
    {
        var merged = new List<int>();
        if (ids is not null)
        {
            merged.AddRange(ids.Where(id => id > 0));
        }

        if (singleId is > 0)
        {
            merged.Add(singleId.Value);
        }

        return merged.Distinct().ToArray();
    }

    private static void ValidateScheduledAt(DateTime? scheduledAt)
    {
        if (scheduledAt is null)
        {
            return;
        }

        var scheduledUtc = scheduledAt.Value.ToUniversalTime();
        var nowUtc = DateTime.UtcNow;
        if (scheduledUtc <= nowUtc)
        {
            throw new InvalidOperationException("Choose a future service date and time before booking.");
        }

        if (scheduledUtc > nowUtc.AddDays(7))
        {
            throw new InvalidOperationException("Bookings are only available up to 7 days in advance.");
        }

        var localSchedule = ToPhilippineLocalTime(scheduledUtc);
        var localSlot = new TimeSpan(localSchedule.Hour, localSchedule.Minute, 0);
        var allowedSlots = new[]
        {
            new TimeSpan(8, 0, 0),
            new TimeSpan(9, 30, 0),
            new TimeSpan(11, 0, 0),
            new TimeSpan(12, 30, 0),
            new TimeSpan(14, 0, 0),
            new TimeSpan(15, 30, 0),
            new TimeSpan(17, 0, 0)
        };

        if (localSchedule.Second != 0 || !allowedSlots.Contains(localSlot))
        {
            throw new InvalidOperationException("Choose an available service time from 8:00 AM to 5:00 PM in 1 hour 30 minute intervals.");
        }
    }

    private static DateTime ToPhilippineLocalTime(DateTime scheduledUtc)
    {
        var utc = scheduledUtc.Kind == DateTimeKind.Utc
            ? scheduledUtc
            : DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, PhilippineTimeZone);
    }

    private static readonly TimeZoneInfo PhilippineTimeZone = ResolvePhilippineTimeZone();

    private static TimeZoneInfo ResolvePhilippineTimeZone()
    {
        foreach (var id in new[] { "Asia/Manila", "Singapore Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    public async Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, int? changedByUserId, string? notes, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == requestId, cancellationToken);
        var oldStatusId = request.CurrentStatusId;
        var newStatusId = await db.RequestStatuses.Where(x => x.StatusName == status).Select(x => x.StatusId).SingleAsync(cancellationToken);
        request.CurrentStatusId = newStatusId;
        request.CompletedAt = status == "completed" ? DateTime.UtcNow : request.CompletedAt;
        request.CancelledAt = status == "cancelled" ? DateTime.UtcNow : request.CancelledAt;
        request.AcceptedAt = status == "accepted" ? DateTime.UtcNow : request.AcceptedAt;

        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = requestId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return await Query().Where(x => x.RequestId == requestId).Select(ToDtoExpression()).SingleAsync(cancellationToken);
    }

    public static System.Linq.Expressions.Expression<Func<ServiceRequest, ServiceRequestDto>> ToDtoExpression()
    {
        return x => new ServiceRequestDto(
            x.RequestId,
            x.CurrentStatus!.StatusName,
            x.Client!.User!.FirstName + " " + x.Client.User.LastName,
            x.Mechanic == null ? null : x.Mechanic.User!.FirstName + " " + x.Mechanic.User.LastName,
            x.Shop == null ? null : x.Shop.ShopName,
            x.ShopService == null ? null : x.ShopService.ServiceName,
            x.IssueDescription,
            x.ServiceLocationAddress,
            x.ScheduledAt,
            x.EstimatedTotal,
            x.FinalTotal,
            x.CreatedAt,
            x.ServiceLatitude,
            x.ServiceLongitude,
            null,
            x.Shop == null ? null : x.Shop.ShopImageUrl,
            x.Shop == null ? null : x.Shop.ShopLogoUrl,
            x.CompletedAt,
            x.LineItems
                .OrderBy(item => item.LineItemId)
                .Select(item => new ServiceRequestLineItemDto(
                    item.LineItemId,
                    item.ItemType,
                    item.ShopServiceId,
                    item.ProductId,
                    item.ItemName,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal))
                .ToList());
    }
}

public interface IMessageService
{
    Task<MessageDto> SendAsync(int userId, int conversationId, SendMessageDto dto, CancellationToken cancellationToken);
}

public sealed class MessageService(BikeMateDbContext db) : IMessageService
{
    public async Task<MessageDto> SendAsync(int userId, int conversationId, SendMessageDto dto, CancellationToken cancellationToken)
    {
        var canSend = await db.ConversationParticipants
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
        if (!canSend)
        {
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            MessageText = dto.MessageText,
            AttachmentUrl = dto.AttachmentUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Messages.Add(message);

        var conversation = await db.Conversations.SingleAsync(x => x.ConversationId == conversationId, cancellationToken);
        conversation.LastMessageAt = message.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);

        return new MessageDto(message.MessageId, message.ConversationId, message.SenderUserId, message.MessageText, message.AttachmentUrl, message.CreatedAt, message.ReadAt);
    }
}

public interface IBookingConversationService
{
    Task SyncForUserAsync(int userId, CancellationToken cancellationToken);
    Task SyncRequestAsync(int requestId, CancellationToken cancellationToken);
    Task<int?> EnsureEmergencySupportConversationAsync(int requestId, CancellationToken cancellationToken);
}

public sealed class BookingConversationService(BikeMateDbContext db) : IBookingConversationService
{
    public async Task SyncForUserAsync(int userId, CancellationToken cancellationToken)
    {
        var requestIds = await db.ServiceRequests
            .Where(x =>
                x.Client!.UserId == userId ||
                (x.ShopId != null && x.Shop!.OwnerUserId == userId) ||
                (x.MechanicId != null && x.Mechanic!.UserId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.RequestId)
            .Take(30)
            .ToArrayAsync(cancellationToken);

        foreach (var requestId in requestIds)
        {
            await SyncRequestAsync(requestId, cancellationToken);
        }
    }

    public async Task SyncRequestAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Shop).ThenInclude(x => x!.Owner)
            .Include(x => x.ShopService)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.CurrentStatus)
            .SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (request?.Client?.User is null)
        {
            return;
        }

        if (IsEmergencyRequest(request))
        {
            await EnsureEmergencySupportConversationAsync(requestId, cancellationToken);
            return;
        }

        if (request.Shop?.Owner is not null)
        {
            await EnsureConversationAsync(
                request,
                "booking_shop",
                request.Shop.OwnerUserId,
                BuildShopMessage(request),
                cancellationToken);
        }

        if (request.Mechanic?.User is not null)
        {
            await EnsureConversationAsync(
                request,
                "booking_mechanic",
                request.Mechanic.UserId,
                BuildMechanicMessage(request),
                cancellationToken);
        }
    }

    public async Task<int?> EnsureEmergencySupportConversationAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (request?.Client?.User is null)
        {
            return null;
        }

        var supportUserId = await db.UserRoles
            .Where(x => x.Role!.RoleName == AppRoles.SystemAdmin)
            .OrderBy(x => x.UserId)
            .Select(x => (int?)x.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (supportUserId is null)
        {
            return null;
        }

        var conversation = await db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .OrderByDescending(x => x.ConversationType == "emergency_support")
            .FirstOrDefaultAsync(x =>
                x.RequestId == requestId &&
                (x.ConversationType == "emergency_support" ||
                 x.ConversationType == "emergency_request"),
                cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                RequestId = requestId,
                ConversationType = "emergency_support",
                CreatedAt = request.CreatedAt,
                LastMessageAt = request.CreatedAt,
                Participants =
                [
                    new ConversationParticipant { UserId = request.Client.UserId, JoinedAt = request.CreatedAt },
                    new ConversationParticipant { UserId = supportUserId.Value, JoinedAt = request.CreatedAt }
                ]
            };
            AddAutomatedMessage(
                conversation,
                supportUserId.Value,
                BuildEmergencySupportMessage(request),
                request.CreatedAt);
            db.Conversations.Add(conversation);
        }
        else
        {
            conversation.ConversationType = "emergency_support";
        }

        if (conversation.Participants.All(x => x.UserId != request.Client.UserId))
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = request.Client.UserId,
                JoinedAt = DateTime.UtcNow
            });
        }

        if (conversation.Participants.All(x => x.UserId != supportUserId.Value))
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = supportUserId.Value,
                JoinedAt = DateTime.UtcNow
            });
        }

        if (request.Mechanic?.User is not null &&
            conversation.Participants.All(x => x.UserId != request.Mechanic.UserId))
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = request.Mechanic.UserId,
                JoinedAt = DateTime.UtcNow
            });
            AddAutomatedMessage(
                conversation,
                supportUserId.Value,
                $"{request.Mechanic.User.FirstName} {request.Mechanic.User.LastName} has joined as your assigned roadside responder.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return conversation.ConversationId;
    }

    private async Task EnsureConversationAsync(
        ServiceRequest request,
        string conversationType,
        int partnerUserId,
        string automatedMessage,
        CancellationToken cancellationToken)
    {
        var customerUserId = request.Client!.UserId;
        var conversationTime = conversationType == "booking_mechanic"
            ? request.AcceptedAt ?? DateTime.UtcNow
            : request.CreatedAt;
        var existing = await db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x =>
                x.RequestId == request.RequestId &&
                x.ConversationType == conversationType,
                cancellationToken);
        if (existing is not null)
        {
            EnsureParticipants(existing, customerUserId, partnerUserId);
            var onlyMessage = existing.Messages.Count == 1 ? existing.Messages[0] : null;
            if (onlyMessage is not null && IsAutomatedMessage(onlyMessage.MessageText))
            {
                onlyMessage.MessageText = automatedMessage;
                onlyMessage.CreatedAt = conversationTime;
                existing.CreatedAt = conversationTime;
                existing.LastMessageAt = conversationTime;
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var reusable = await db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .Where(x =>
                x.ConversationType == conversationType &&
                x.Participants.Any(p => p.UserId == customerUserId) &&
                x.Participants.Any(p => p.UserId == partnerUserId))
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (reusable is not null)
        {
            reusable.RequestId = request.RequestId;
            reusable.ConversationType = conversationType;
            EnsureParticipants(reusable, customerUserId, partnerUserId);
            if (!reusable.Messages.Any(x => x.MessageText.Contains($"BM-{request.RequestId:000000}", StringComparison.Ordinal)))
            {
                AddAutomatedMessage(reusable, partnerUserId, automatedMessage, conversationTime);
            }
            else if (reusable.LastMessageAt is null || reusable.LastMessageAt < conversationTime)
            {
                reusable.LastMessageAt = conversationTime;
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var legacy = await db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x =>
                x.RequestId == request.RequestId &&
                (x.ConversationType == "service_request" || x.ConversationType == "emergency_request") &&
                x.Participants.Any(p => p.UserId == customerUserId) &&
                x.Participants.Any(p => p.UserId == partnerUserId),
                cancellationToken);
        if (legacy is not null)
        {
            legacy.ConversationType = conversationType;
            if (legacy.Messages.Count == 0)
            {
                AddAutomatedMessage(legacy, partnerUserId, automatedMessage);
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            RequestId = request.RequestId,
            ConversationType = conversationType,
            CreatedAt = conversationTime,
            LastMessageAt = conversationTime,
            Participants =
            [
                new ConversationParticipant { UserId = customerUserId, JoinedAt = now },
                new ConversationParticipant { UserId = partnerUserId, JoinedAt = now }
            ]
        };
        AddAutomatedMessage(conversation, partnerUserId, automatedMessage, conversationTime);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureParticipants(Conversation conversation, int customerUserId, int partnerUserId)
    {
        var now = DateTime.UtcNow;
        if (conversation.Participants.All(x => x.UserId != customerUserId))
        {
            conversation.Participants.Add(new ConversationParticipant { UserId = customerUserId, JoinedAt = now });
        }

        if (conversation.Participants.All(x => x.UserId != partnerUserId))
        {
            conversation.Participants.Add(new ConversationParticipant { UserId = partnerUserId, JoinedAt = now });
        }
    }

    private static void AddAutomatedMessage(
        Conversation conversation,
        int senderUserId,
        string text,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        conversation.LastMessageAt = now;
        conversation.Messages.Add(new Message
        {
            SenderUserId = senderUserId,
            MessageText = text,
            CreatedAt = now
        });
    }

    private static string BuildShopMessage(ServiceRequest request)
    {
        return
            $"Booking BM-{request.RequestId:000000} received.\n\n" +
            $"Service: {ServiceName(request)}\n" +
            $"Schedule: {Schedule(request)}\n" +
            $"Location: {request.ServiceLocationAddress ?? "To be confirmed"}\n" +
            $"Concern: {request.IssueDescription}\n" +
            $"Estimated total: PHP {request.EstimatedTotal:N2}\n\n" +
            $"{request.Shop!.ShopName} will use this chat for service updates, pricing questions, and preparation details.";
    }

    private static string BuildMechanicMessage(ServiceRequest request)
    {
        var mechanicName = $"{request.Mechanic!.User!.FirstName} {request.Mechanic.User.LastName}".Trim();
        return
            $"Hi {request.Client!.User!.FirstName}, I’m {mechanicName}, the mechanic assigned to booking BM-{request.RequestId:000000}.\n\n" +
            $"Service: {ServiceName(request)}\n" +
            $"Schedule: {Schedule(request)}\n" +
            $"Location: {request.ServiceLocationAddress ?? "To be confirmed"}\n" +
            $"Current status: {FormatStatus(request.CurrentStatus?.StatusName)}\n\n" +
            "Use this chat for arrival updates, location details, and repair questions.";
    }

    private static string BuildEmergencySupportMessage(ServiceRequest request)
    {
        var concern = request.IssueDescription
            .Replace("[EMERGENCY]", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return
            $"BikeMate Emergency received roadside request BM-{request.RequestId:000000}.\n\n" +
            $"Location: {request.ServiceLocationAddress ?? "Current location"}\n" +
            $"Concern: {(string.IsNullOrWhiteSpace(concern) ? "Roadside assistance needed." : concern)}\n\n" +
            "No payment is required to request or track emergency help. Use this chat for safety updates, landmarks, and responder coordination.";
    }

    private static bool IsEmergencyRequest(ServiceRequest request)
    {
        return request.IssueDescription.StartsWith("[EMERGENCY]", StringComparison.OrdinalIgnoreCase) ||
               request.CurrentStatus?.StatusName is "emergency_pending" or "searching_responder" or "call_connecting" or "call_connected";
    }

    private static string ServiceName(ServiceRequest request)
    {
        return request.ShopService?.ServiceName ?? "Bike repair service";
    }

    private static string Schedule(ServiceRequest request)
    {
        return request.ScheduledAt?.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)
            ?? "To be confirmed";
    }

    private static bool IsAutomatedMessage(string text)
    {
        return text.StartsWith("Booking BM-", StringComparison.Ordinal) ||
               text.StartsWith("BikeMate Emergency received", StringComparison.Ordinal) ||
               (text.StartsWith("Hi ", StringComparison.Ordinal) &&
                text.Contains("assigned to booking BM-", StringComparison.Ordinal));
    }

    private static string FormatStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "Pending"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace("_", " "));
    }
}

public interface ILocationService
{
    Task<LiveLocationDto> UpdateAsync(LocationUpdateDto dto, CancellationToken cancellationToken);
}

public sealed class LocationService(BikeMateDbContext db) : ILocationService
{
    public async Task<LiveLocationDto> UpdateAsync(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var location = new LiveLocation
        {
            RequestId = dto.RequestId,
            MechanicId = dto.MechanicId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AccuracyMeters = dto.AccuracyMeters,
            CreatedAt = DateTime.UtcNow
        };
        db.LiveLocations.Add(location);

        if (dto.MechanicId is not null)
        {
            var mechanic = await db.Mechanics.FindAsync([dto.MechanicId.Value], cancellationToken);
            if (mechanic is not null)
            {
                mechanic.CurrentLatitude = dto.Latitude;
                mechanic.CurrentLongitude = dto.Longitude;
                mechanic.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new LiveLocationDto(location.LiveLocationId, location.RequestId, location.MechanicId, location.Latitude, location.Longitude, location.CreatedAt);
    }
}

public interface IAgoraTokenService
{
    EmergencyCallSessionDto CreateEmergencyCallSession(int requestId, int userId, DateTime startedAt);
}

public sealed class AgoraTokenService(IConfiguration configuration, ILogger<AgoraTokenService> logger) : IAgoraTokenService
{
    public EmergencyCallSessionDto CreateEmergencyCallSession(int requestId, int userId, DateTime startedAt)
    {
        var appId = configuration["Agora:AppId"];
        var certificate = GetAppCertificate();
        var channelName = $"bikemate-emergency-{requestId}";
        var uid = userId <= 0 ? (uint)requestId : (uint)userId;
        var tokenLifetimeSeconds = GetTokenLifetimeSeconds();
        var expiresAt = startedAt.AddSeconds(tokenLifetimeSeconds);

        if (!IsAgoraId(appId) || !IsAgoraId(certificate))
        {
            logger.LogWarning("Agora emergency call session requested for {RequestId}, but Agora configuration is missing or invalid.", requestId);
            return new EmergencyCallSessionDto(
                requestId,
                "ConfigurationMissing",
                startedAt,
                null,
                "Agora calling is not configured on the API. Set Agora:AppId and an Agora certificate, then retry.",
                appId,
                channelName,
                uid,
                null,
                expiresAt);
        }

        var token = AgoraRtcTokenBuilder.BuildTokenWithUid(
            appId!,
            certificate!,
            channelName,
            uid,
            tokenLifetimeSeconds,
            tokenLifetimeSeconds);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Agora token generation returned an empty token for emergency request {RequestId}.", requestId);
            return new EmergencyCallSessionDto(
                requestId,
                "TokenUnavailable",
                startedAt,
                null,
                "Agora token generation failed. Check the API Agora App ID and certificate.",
                appId,
                channelName,
                uid,
                null,
                expiresAt);
        }

        return new EmergencyCallSessionDto(
            requestId,
            "TokenReady",
            startedAt,
            null,
            "Agora session token is ready. Join the returned channel with the native Agora RTC SDK.",
            appId,
            channelName,
            uid,
            token,
            expiresAt);
    }

    private uint GetTokenLifetimeSeconds()
    {
        var configured = configuration.GetValue<int?>("Agora:TokenLifetimeSeconds") ?? 1800;
        return (uint)Math.Clamp(configured, 60, 86400);
    }

    private string? GetAppCertificate()
    {
        return configuration["Agora:PrimaryCertificate"]
            ?? configuration["Agora:AppCertificate"]
            ?? configuration["Agora:SecondaryCertificate"];
    }

    private static bool IsAgoraId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length == 32 &&
            value.All(Uri.IsHexDigit) &&
            !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
    }

    private static class AgoraRtcTokenBuilder
    {
        private const ushort ServiceTypeRtc = 1;
        private const ushort PrivilegeJoinChannel = 1;
        private const ushort PrivilegePublishAudioStream = 2;
        private const ushort PrivilegePublishVideoStream = 3;
        private const ushort PrivilegePublishDataStream = 4;

        public static string BuildTokenWithUid(
            string appId,
            string appCertificate,
            string channelName,
            uint uid,
            uint tokenExpireSeconds,
            uint privilegeExpireSeconds)
        {
            var issueTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var salt = (uint)RandomNumberGenerator.GetInt32(1, 99_999_999);
            var appCertificateBytes = Encoding.UTF8.GetBytes(appCertificate);
            var signing = Hmac(PackUInt32(issueTs), appCertificateBytes);
            signing = Hmac(PackUInt32(salt), signing);

            var serviceRtc = PackServiceRtc(channelName, uid, privilegeExpireSeconds);
            var signingInfo = Concat(
                PackString(Encoding.UTF8.GetBytes(appId)),
                PackUInt32(issueTs),
                PackUInt32(tokenExpireSeconds),
                PackUInt32(salt),
                PackUInt16(1),
                serviceRtc);

            var signature = Hmac(signing, signingInfo);
            var content = Concat(PackString(signature), signingInfo);
            return "007" + Convert.ToBase64String(Compress(content));
        }

        private static byte[] PackServiceRtc(string channelName, uint uid, uint privilegeExpireSeconds)
        {
            var privileges = new SortedDictionary<ushort, uint>
            {
                [PrivilegeJoinChannel] = privilegeExpireSeconds,
                [PrivilegePublishAudioStream] = privilegeExpireSeconds,
                [PrivilegePublishVideoStream] = privilegeExpireSeconds,
                [PrivilegePublishDataStream] = privilegeExpireSeconds
            };

            return Concat(
                PackUInt16(ServiceTypeRtc),
                PackPrivilegeMap(privileges),
                PackString(Encoding.UTF8.GetBytes(channelName)),
                PackString(Encoding.UTF8.GetBytes(uid == 0 ? string.Empty : uid.ToString())));
        }

        private static byte[] PackPrivilegeMap(SortedDictionary<ushort, uint> privileges)
        {
            using var buffer = new MemoryStream();
            buffer.Write(PackUInt16((ushort)privileges.Count));
            foreach (var privilege in privileges)
            {
                buffer.Write(PackUInt16(privilege.Key));
                buffer.Write(PackUInt32(privilege.Value));
            }

            return buffer.ToArray();
        }

        private static byte[] PackString(byte[] value)
        {
            return Concat(PackUInt16((ushort)value.Length), value);
        }

        private static byte[] PackUInt16(ushort value)
        {
            var bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
            return bytes;
        }

        private static byte[] PackUInt32(uint value)
        {
            var bytes = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return bytes;
        }

        private static byte[] Hmac(byte[] key, byte[] value)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(value);
        }

        private static byte[] Compress(byte[] value)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(value, 0, value.Length);
            }

            return output.ToArray();
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var length = parts.Sum(x => x.Length);
            var output = new byte[length];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, output, offset, part.Length);
                offset += part.Length;
            }

            return output;
        }
    }
}

public interface IPayMongoService
{
    Task<(string SessionId, string CheckoutUrl, string ReferenceNumber)> CreateCheckoutAsync(int requestId, decimal amount, CancellationToken cancellationToken);
    Task<PayMongoCheckoutStatus> GetCheckoutStatusAsync(string checkoutSessionId, CancellationToken cancellationToken);
}

public sealed record PayMongoCheckoutStatus(bool IsPaid, string? Status, string? ProviderPaymentId, string? PayloadJson);

public sealed class PayMongoService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<PayMongoService> logger) : IPayMongoService
{
    public async Task<(string SessionId, string CheckoutUrl, string ReferenceNumber)> CreateCheckoutAsync(int requestId, decimal amount, CancellationToken cancellationToken)
    {
        var secretKey = configuration["PayMongo:SecretKey"];
        var reference = $"BM-{requestId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (string.IsNullOrWhiteSpace(secretKey) ||
            !secretKey.StartsWith("sk_", StringComparison.OrdinalIgnoreCase) ||
            secretKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PayMongo checkout is not configured on the API. Set PayMongo:SecretKey on the server and try again.");
        }

        var amountInCentavos = Convert.ToInt32(Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.paymongo.com/v1/checkout_sessions");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:")));
        request.Content = JsonContent.Create(new
        {
            data = new
            {
                attributes = new
                {
                    description = $"BikeMate service request #{requestId}",
                    reference_number = reference,
                    send_email_receipt = true,
                    show_description = true,
                    show_line_items = true,
                    success_url = configuration["PayMongo:SuccessUrl"] ?? "bikemate://payment-success",
                    cancel_url = configuration["PayMongo:CancelUrl"] ?? "bikemate://payment-cancelled",
                    payment_method_types = new[] { "card", "gcash", "paymaya" },
                    line_items = new[]
                    {
                        new
                        {
                            name = "BikeMate Service",
                            description = $"Service request #{requestId}",
                            amount = amountInCentavos,
                            currency = "PHP",
                            quantity = 1
                        }
                    },
                    metadata = new Dictionary<string, string>
                    {
                        ["request_id"] = requestId.ToString(),
                        ["reference_number"] = reference
                    }
                }
            }
        });

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("PayMongo checkout failed for request {RequestId}. Status: {Status}. Body: {Body}", requestId, response.StatusCode, payload);
            throw new InvalidOperationException("PayMongo checkout could not be created.");
        }

        using var document = JsonDocument.Parse(payload);
        var data = document.RootElement.GetProperty("data");
        var attributes = data.GetProperty("attributes");
        var sessionId = data.GetProperty("id").GetString() ?? $"cs_{reference.ToLowerInvariant()}";
        var checkoutUrl = attributes.GetProperty("checkout_url").GetString() ?? $"https://checkout.paymongo.com/pay/{reference}";
        var responseReference = attributes.TryGetProperty("reference_number", out var refElement)
            ? refElement.GetString() ?? reference
            : reference;

        return (sessionId, checkoutUrl, responseReference);
    }

    public async Task<PayMongoCheckoutStatus> GetCheckoutStatusAsync(string checkoutSessionId, CancellationToken cancellationToken)
    {
        var secretKey = configuration["PayMongo:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) ||
            !secretKey.StartsWith("sk_", StringComparison.OrdinalIgnoreCase) ||
            secretKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PayMongo checkout is not configured on the API. Set PayMongo:SecretKey on the server and try again.");
        }

        var escapedSessionId = Uri.EscapeDataString(checkoutSessionId);
        var endpoints = new[]
        {
            $"https://api.paymongo.com/v2/checkout_sessions/{escapedSessionId}",
            $"https://api.paymongo.com/v1/checkout_sessions/{escapedSessionId}"
        };
        var client = httpClientFactory.CreateClient();
        string? lastNotFoundPayload = null;

        foreach (var endpoint in endpoints)
        {
            using var request = CreatePayMongoRequest(System.Net.Http.HttpMethod.Get, endpoint, secretKey);
            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                lastNotFoundPayload = payload;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PayMongo checkout status failed for session {SessionId}. Status: {Status}. Body: {Body}", checkoutSessionId, response.StatusCode, payload);
                throw new InvalidOperationException("PayMongo checkout status could not be refreshed.");
            }

            return ParseCheckoutStatus(payload);
        }

        logger.LogWarning("PayMongo checkout session {SessionId} was not found from either PayMongo status endpoint. Body: {Body}", checkoutSessionId, lastNotFoundPayload);
        return new PayMongoCheckoutStatus(false, "not_found", null, string.IsNullOrWhiteSpace(lastNotFoundPayload) ? "{}" : lastNotFoundPayload);
    }

    private static HttpRequestMessage CreatePayMongoRequest(System.Net.Http.HttpMethod method, string url, string secretKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:")));
        return request;
    }

    private static PayMongoCheckoutStatus ParseCheckoutStatus(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var status = FindString(root, "status", "payment_status");
        var paid = string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase) ||
            HasPaidPayment(root);
        var paymentId = FindString(root, "payment_id", "payment_intent_id");
        return new PayMongoCheckoutStatus(paid, status, paymentId, payload);
    }

    private static bool HasPaidPayment(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, "status", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number &&
                    (string.Equals(property.Value.ToString(), "paid", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(property.Value.ToString(), "succeeded", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (HasPaidPayment(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (HasPaidPayment(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? FindString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                {
                    return property.Value.ToString();
                }

                var nested = FindString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}

public interface IPaymentService
{
    Task<PaymentDto> CreateCheckoutAsync(int userId, CreateCheckoutSessionDto dto, CancellationToken cancellationToken);
}

public sealed class PaymentService(BikeMateDbContext db, IPayMongoService payMongoService) : IPaymentService
{
    public async Task<PaymentDto> CreateCheckoutAsync(int userId, CreateCheckoutSessionDto dto, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests
            .Include(x => x.Client)
            .Include(x => x.CurrentStatus)
            .Include(x => x.ShopService)
            .Include(x => x.LineItems)
            .Include(x => x.Payments).ThenInclude(x => x.PaymentStatus)
            .SingleAsync(x => x.RequestId == dto.RequestId, cancellationToken);
        if (request.Client!.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only pay for your own request.");
        }

        if (request.IssueDescription.StartsWith("[EMERGENCY]", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Emergency roadside help does not require checkout.");
        }

        var hasLineItems = request.LineItems.Count > 0;
        if (request.ShopId is null || (!hasLineItems && (request.ShopServiceId is null || request.ShopService is null)))
        {
            throw new InvalidOperationException("Select a repair shop and service before secure payment.");
        }

        if (!hasLineItems && (request.ShopService!.ShopId != request.ShopId || !request.ShopService.IsActive))
        {
            throw new InvalidOperationException("The selected shop service is no longer available. Choose another service before payment.");
        }

        var lineItemTotal = request.LineItems.Sum(x => x.LineTotal);
        var amount = request.FinalTotal > 0
            ? request.FinalTotal
            : lineItemTotal > 0
                ? lineItemTotal
                : request.ShopService!.BasePrice;
        request.EstimatedTotal = amount;
        var pendingStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "pending").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var existingPending = request.Payments
            .Where(x => x.PaymentStatus?.StatusName == PaymentStatuses.Pending &&
                        !string.IsNullOrWhiteSpace(x.CheckoutUrl) &&
                        x.Amount == amount)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (existingPending is not null)
        {
            await MoveRequestStatusAsync(request, RequestStatuses.PaymentPending, "Existing PayMongo checkout is still pending.", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return ToPaymentDto(existingPending, PaymentStatuses.Pending);
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        var checkout = await payMongoService.CreateCheckoutAsync(request.RequestId, amount, cancellationToken);
        var payment = new Payment
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            PaymentStatusId = pendingStatusId,
            Amount = amount,
            Currency = "PHP",
            ProviderName = "paymongo",
            ProviderCheckoutSessionId = checkout.SessionId,
            ProviderReferenceNumber = checkout.ReferenceNumber,
            CheckoutUrl = checkout.CheckoutUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await MoveRequestStatusAsync(request, RequestStatuses.PaymentPending, "PayMongo checkout created and waiting for payment confirmation.", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToPaymentDto(payment, PaymentStatuses.Pending);
    }

    private async Task MoveRequestStatusAsync(ServiceRequest request, string statusName, string notes, CancellationToken cancellationToken)
    {
        if (request.CurrentStatus?.StatusName == statusName)
        {
            return;
        }

        var oldStatusId = request.CurrentStatusId;
        var newStatusId = await GetOrCreateRequestStatusIdAsync(statusName, cancellationToken);
        request.CurrentStatusId = newStatusId;
        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = request.RequestId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<int> GetOrCreateRequestStatusIdAsync(string statusName, CancellationToken cancellationToken)
    {
        var existingStatusId = await db.RequestStatuses
            .Where(x => x.StatusName == statusName)
            .Select(x => (int?)x.StatusId)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingStatusId is not null)
        {
            return existingStatusId.Value;
        }

        var status = new RequestStatus { StatusName = statusName };
        db.RequestStatuses.Add(status);
        await db.SaveChangesAsync(cancellationToken);
        return status.StatusId;
    }

    private static PaymentDto ToPaymentDto(Payment payment, string statusName)
    {
        return new PaymentDto(
            payment.PaymentId,
            payment.RequestId,
            statusName,
            payment.Amount,
            payment.Currency,
            payment.ProviderName,
            payment.CheckoutUrl,
            payment.ProviderReferenceNumber,
            payment.CreatedAt,
            payment.PaidAt);
    }
}

public interface INotificationService
{
    Task AddAsync(int userId, string type, string title, string message, CancellationToken cancellationToken);
}

public sealed class NotificationService(BikeMateDbContext db) : INotificationService
{
    public async Task AddAsync(int userId, string type, string title, string message, CancellationToken cancellationToken)
    {
        db.Notifications.Add(new Notification { UserId = userId, NotificationType = type, Title = title, Message = message, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface IAdminReportService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TopServiceDto>> GetTopServicesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TopMechanicDto>> GetTopMechanicsAsync(CancellationToken cancellationToken);
}

public sealed class AdminReportService(BikeMateDbContext db) : IAdminReportService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var pendingRequestStatusId = await db.RequestStatuses.Where(x => x.StatusName == "pending").Select(x => x.StatusId).SingleAsync(cancellationToken);
        var completedStatusId = await db.RequestStatuses.Where(x => x.StatusName == "completed").Select(x => x.StatusId).SingleAsync(cancellationToken);

        return new AdminDashboardDto(
            await db.Users.CountAsync(cancellationToken),
            await db.Clients.CountAsync(cancellationToken),
            await db.Mechanics.CountAsync(cancellationToken),
            await db.Shops.CountAsync(cancellationToken),
            await db.ServiceRequests.CountAsync(x => x.CurrentStatusId == pendingRequestStatusId, cancellationToken),
            await db.ServiceRequests.CountAsync(x => x.CurrentStatusId == completedStatusId, cancellationToken),
            await db.Payments.Where(x => x.PaymentStatusId == paidStatusId).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            await db.Mechanics.CountAsync(x => !x.IsVerified, cancellationToken) + await db.Shops.CountAsync(x => x.ShopStatus == "pending", cancellationToken));
    }

    public async Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var query = db.Payments.Where(x => x.PaymentStatusId == paidStatusId && x.PaidAt >= from && x.PaidAt <= to);
        return new RevenueReportDto(from, to, await query.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m, await query.CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyCollection<TopServiceDto>> GetTopServicesAsync(CancellationToken cancellationToken)
    {
        return await db.ServiceRequests
            .Join(
                db.ShopServices,
                request => request.ShopServiceId,
                service => service.ShopServiceId,
                (request, service) => service.ServiceName)
            .GroupBy(serviceName => serviceName)
            .Select(group => new
            {
                ServiceName = group.Key,
                RequestCount = group.Count()
            })
            .OrderByDescending(x => x.RequestCount)
            .Take(10)
            .Select(x => new TopServiceDto(x.ServiceName, x.RequestCount))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TopMechanicDto>> GetTopMechanicsAsync(CancellationToken cancellationToken)
    {
        return await db.Mechanics
            .Include(x => x.User)
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.TotalCompletedJobs)
            .Take(10)
            .Select(x => new TopMechanicDto(x.User!.FirstName + " " + x.User.LastName, x.AverageRating, x.TotalCompletedJobs))
            .ToArrayAsync(cancellationToken);
    }
}
