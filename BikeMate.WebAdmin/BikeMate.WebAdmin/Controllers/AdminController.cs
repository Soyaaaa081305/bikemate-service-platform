#pragma warning disable CS8602

using BCrypt.Net;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using BikeMate.WebAdmin.DTOs;
using BikeMate.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace BikeMate.WebAdmin.Controllers;

[Authorize(Roles = "SystemAdmin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private const string AdminLoginOtpPurpose = "admin_login";
    private const string AdminTrustedBrowserPurpose = "admin_login_trust";
    private const string AdminTrustedBrowserCookie = "bm_admin_otp_trust";
    private static readonly TimeSpan AdminLoginOtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AdminTrustedBrowserLifetime = TimeSpan.FromHours(12);

    private readonly BikeMateDbContext _context;
    private readonly IAdminOtpEmailService _adminOtpEmailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        BikeMateDbContext context,
        IAdminOtpEmailService adminOtpEmailService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _adminOtpEmailService = adminOtpEmailService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        var today = DateTime.UtcNow;
        var sevenDaysAgo = today.AddDays(-7);

        // 1. Fetch raw data for the chart (Users created in the last 7 days)
        var recentUsers = await _context.Clients
            .Where(c => c.CreatedAt >= sevenDaysAgo)
            .Select(c => c.CreatedAt)
            .ToListAsync();

        // 2. Build the last 7 days array so the chart always has 7 bars (even if 0 users joined)
        var weeklyStats = new List<DailyStatsDto>();
        for (int i = 6; i >= 0; i--)
        {
            var targetDate = today.AddDays(-i).Date;
            weeklyStats.Add(new DailyStatsDto
            {
                DayName = targetDate.ToString("ddd"), // e.g., "Mon", "Tue"
                UserCount = recentUsers.Count(date => date.Date == targetDate)
            });
        }

        // 3. Fetch the latest 4 active requests for the quick-view list
        var recentRequests = await _context.ServiceRequests
            .Include(sr => sr.CurrentStatus)
            .Include(sr => sr.ShopService)
            .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(4)
            .Select(sr => new ActiveRequestMiniDto
            {
                RequestId = sr.RequestId,
                ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "General Help",
                Status = sr.CurrentStatus.StatusName,
                TimeAgo = "Recently" // Simplified for this example
            })
            .ToListAsync();

        // 4. Compile the final dashboard package
        var dashboardData = new AdminDashboardDto
        {
            TotalCustomers = await _context.Clients.CountAsync(),
            TotalMechanics = await _context.Mechanics.CountAsync(),
            TotalShops = await _context.Shops.CountAsync(),
            PendingServiceRequests = await _context.ServiceRequests
                .Include(sr => sr.CurrentStatus)
                .CountAsync(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled"),

            // Granular stats for the progress bars
            OnlineMechanics = await _context.Mechanics.CountAsync(m => m.AvailabilityStatus == "online"),
            VerifiedShops = await _context.Shops.CountAsync(s => s.ShopStatus == "verified"),

            WeeklyRegistrations = weeklyStats,
            RecentActiveRequests = recentRequests
        };

        return Ok(dashboardData);
    }

    [HttpGet("users")]
    public async Task<ActionResult<UserDto[]>> GetUsers()
    {
        // Your SQL script sets RoleId 1 as 'Customer'. 
        // This query safely grabs ONLY users assigned the Customer role.
        var users = await _context.Users
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == 1))
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AccountStatus = u.AccountStatus
            })
            .ToArrayAsync();

        return Ok(users);
    }

    [HttpGet("mechanics")]
    public async Task<ActionResult<MechanicDto[]>> GetMechanics()
    {
        // Pulls directly from the mechanics table and includes the related User data
        var mechanics = await _context.Mechanics
            .Include(m => m.User)
            .Select(m => new MechanicDto
            {
                Id = m.MechanicId,
                Name = m.User.FirstName + " " + m.User.LastName,
                Rating = m.AverageRating,
                Status = m.AvailabilityStatus,
                TotalJobs = m.TotalCompletedJobs // Matches the column in your SQL script!
            })
            .ToArrayAsync();

        return Ok(mechanics);
    }

    // 1. This one is for the Pending Approvals page
    [HttpGet("shops/pending")]
    public async Task<ActionResult<ShopDto[]>> GetPendingShops()
    {
        var pendingShops = await _context.Shops
            .Where(s => s.ShopStatus.ToLower().Trim() == "pending")
            .Select(s => new ShopDto
            {
                Id = s.ShopId,
                ShopName = s.ShopName,
                City = s.City ?? string.Empty,
                Status = s.ShopStatus
            })
            .ToArrayAsync();

        return Ok(pendingShops);
    }

    // 2. YOU MUST HAVE THIS ONE for the Shop Directory page!
    [HttpGet("shops")]
    public async Task<ActionResult<ShopDto[]>> GetAllShops()
    {
        var shops = await _context.Shops
            .Select(s => new ShopDto
            {
                Id = s.ShopId,
                ShopName = s.ShopName ?? string.Empty,
                City = s.City ?? string.Empty,
                Status = s.ShopStatus ?? "pending"
            })
            .ToArrayAsync();

        return Ok(shops);
    }

    [HttpGet("shops/{id}")]
    public async Task<ActionResult<ShopDetailsDto>> GetShopDetails(int id)
    {
        var shopDetails = await _context.Shops
            .Include(s => s.Owner) // Get the owner's name
            .Include(s => s.ShopMechanics) // Join the bridge table
                .ThenInclude(sm => sm.Mechanic) // Get the mechanic profile
                    .ThenInclude(m => m.User) // Get the mechanic's actual name
            .Where(s => s.ShopId == id)
            .Select(s => new ShopDetailsDto
            {
                ShopId = s.ShopId,
                ShopName = s.ShopName,
                Description = s.ShopDescription ?? string.Empty,
                FullAddress = $"{s.AddressLine}, {s.City}, {s.Province}",
                ContactNumber = s.ContactNumber ?? string.Empty,
                Status = s.ShopStatus ?? "pending",
                OwnerName = s.Owner!.FirstName + " " + s.Owner.LastName,
                CreatedAt = s.CreatedAt,
                // Map the nested mechanics directly into our DTO list
                Mechanics = s.ShopMechanics.Select(sm => new ShopMechanicDto
                {
                    MechanicId = sm.MechanicId,
                    Name = sm.Mechanic.User.FirstName + " " + sm.Mechanic.User.LastName,
                    Status = sm.Mechanic.AvailabilityStatus,
                    Rating = sm.Mechanic.AverageRating
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (shopDetails == null) return NotFound("Shop not found.");

        return Ok(shopDetails);
    }

    [HttpPut("users/{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateStatusRequest payload)
    {
        if (!IsValidAccountStatus(payload.AccountStatus))
        {
            return BadRequest("Invalid account status.");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        user.AccountStatus = payload.AccountStatus.Trim().ToLowerInvariant();
        await _context.SaveChangesAsync();

        return Ok();
    }
    [HttpGet("requests/active")]
    public async Task<ActionResult<ServiceRequestDto[]>> GetActiveRequests()
    {
        var requests = await _context.ServiceRequests
            .Include(sr => sr.Client).ThenInclude(c => c.User)
            .Include(sr => sr.ShopService)
            .Include(sr => sr.CurrentStatus)
            .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
            .Select(sr => new ServiceRequestDto
            {
                Id = sr.RequestId,
                ClientName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : string.Empty,
                Status = sr.CurrentStatus.StatusName,
                TotalAmount = sr.EstimatedTotal
            })
            .ToArrayAsync();

        return Ok(requests);
    }

    [HttpGet("payments")]
    public async Task<ActionResult<PaymentDto[]>> GetPayments()
    {
        var payments = await _context.Payments
            .Include(p => p.Client).ThenInclude(c => c.User)
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .Select(p => new PaymentDto
            {
                Id = p.PaymentId,
                CustomerName = p.Client.User.FirstName + " " + p.Client.User.LastName,
                Amount = p.Amount,
                Status = p.PaymentStatus.StatusName,
                Method = p.PaymentMethod != null ? p.PaymentMethod.MethodName : "Pending",
                CreatedAt = p.CreatedAt
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToArrayAsync();

        return Ok(payments);
    }

    [HttpGet("payments/{id}")]
    public async Task<ActionResult<PaymentDetailsDto>> GetPaymentDetails(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Client).ThenInclude(c => c.User)
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .Where(p => p.PaymentId == id)
            .Select(p => new PaymentDetailsDto
            {
                PaymentId = p.PaymentId,
                RequestId = p.RequestId,
                CustomerName = p.Client.User.FirstName + " " + p.Client.User.LastName,
                CustomerPhone = p.Client.User.PhoneNumber ?? string.Empty,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.PaymentStatus.StatusName,
                Method = p.PaymentMethod != null ? p.PaymentMethod.MethodName : "Not Selected",
                ProviderName = p.ProviderName,
                ReferenceNumber = p.ProviderReferenceNumber ?? string.Empty,
                CheckoutUrl = p.CheckoutUrl ?? string.Empty,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .FirstOrDefaultAsync();

        if (payment == null) return NotFound("Payment not found.");

        return Ok(payment);
    }

    [HttpGet("mechanics/{id}")]
    public async Task<ActionResult<MechanicProfileDto>> GetMechanicDetails(int id)
    {
        var mechanic = await _context.Mechanics
            .Include(m => m.User)
            .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
            .Include(m => m.AssignedRequests).ThenInclude(sr => sr.CurrentStatus)
            .Include(m => m.AssignedRequests).ThenInclude(sr => sr.ShopService)
            .Include(m => m.Reviews).ThenInclude(r => r.Client).ThenInclude(c => c.User)
            .Where(m => m.MechanicId == id)
            .Select(m => new MechanicProfileDto
            {
                MechanicId = m.MechanicId,
                FullName = m.User.FirstName + " " + m.User.LastName,
                Email = m.User.Email,
                Phone = m.User.PhoneNumber ?? string.Empty,
                Status = m.AvailabilityStatus,
                AverageRating = m.AverageRating,
                TotalJobs = m.TotalCompletedJobs,
                Bio = m.Bio ?? string.Empty,
                YearsExperience = m.YearsExperience ?? 0,

                // Gets the shop they are currently assigned to
                CurrentShopName = m.ShopMechanics
                    .Where(sm => sm.IsActive)
                    .Select(sm => sm.Shop.ShopName)
                    .FirstOrDefault() ?? string.Empty,

                // Grabs their history
                ServiceHistory = m.AssignedRequests.Select(sr => new MechanicServiceHistoryDto
                {
                    RequestId = sr.RequestId,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : string.Empty,
                    Status = sr.CurrentStatus.StatusName,
                    Date = sr.CreatedAt
                }).OrderByDescending(x => x.Date).ToList(),

                // Grabs their reviews
                Reviews = m.Reviews.Select(r => new MechanicReviewDto
                {
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    CustomerName = r.Client.User.FirstName,
                    Date = r.CreatedAt
                }).OrderByDescending(x => x.Date).ToList()
            })
            .FirstOrDefaultAsync();

        if (mechanic == null) return NotFound("Mechanic not found.");
        return Ok(mechanic);
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserEditDto>> GetUserForEdit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserEditDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AccountStatus = user.AccountStatus
        });
    }

    [HttpPut("users/{id}/edit")]
    public async Task<IActionResult> EditUser(int id, [FromBody] UserEditDto dto)
    {
        if (!IsValidAccountStatus(dto.AccountStatus))
        {
            return BadRequest("Invalid account status.");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.AccountStatus = dto.AccountStatus.Trim().ToLowerInvariant();

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("shops/{id}/approve")]
    public async Task<IActionResult> ApproveShop(int id)
    {
        var shop = await _context.Shops.FindAsync(id);
        if (shop == null) return NotFound();

        shop.ShopStatus = "verified";
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("shops/{id}/suspend")]
    public async Task<IActionResult> SuspendShop(int id)
    {
        var shop = await _context.Shops.FindAsync(id);
        if (shop == null) return NotFound();

        shop.ShopStatus = "suspended";
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("mechanics/{id}/suspend")]
    public async Task<IActionResult> SuspendMechanic(int id)
    {
        // Suspending a mechanic means suspending their overarching User account
        var mechanic = await _context.Mechanics
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.MechanicId == id);

        if (mechanic == null) return NotFound();

        mechanic.User.AccountStatus = "suspended";
        mechanic.AvailabilityStatus = "offline"; // Force them offline

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult<ServiceRequestDetailsDto>> GetRequestDetails(int id)
    {
        var request = await _context.ServiceRequests
            .Include(sr => sr.Client).ThenInclude(c => c.User)
            .Include(sr => sr.Mechanic).ThenInclude(m => m!.User)
            .Include(sr => sr.ShopService).ThenInclude(ss => ss!.Shop)
            .Include(sr => sr.CurrentStatus)
            .Include(sr => sr.Payments).ThenInclude(p => p.PaymentStatus)
            .Where(sr => sr.RequestId == id)
            .Select(sr => new ServiceRequestDetailsDto
            {
                RequestId = sr.RequestId,
                ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : string.Empty,
                Status = sr.CurrentStatus.StatusName,
                Description = sr.IssueDescription ?? string.Empty,
                TotalAmount = sr.FinalTotal,
                CreatedAt = sr.CreatedAt,
                Latitude = sr.ServiceLatitude,
                Longitude = sr.ServiceLongitude,

                // Customer mapping
                CustomerId = sr.ClientId,
                CustomerName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                CustomerPhone = sr.Client.User.PhoneNumber ?? string.Empty,

                // Mechanic & Shop mapping
                MechanicId = sr.MechanicId,
                MechanicName = sr.Mechanic != null ? (sr.Mechanic.User.FirstName + " " + sr.Mechanic.User.LastName) : "Pending Assignment",
                MechanicPhone = sr.Mechanic != null ? (sr.Mechanic.User.PhoneNumber ?? string.Empty) : string.Empty,
                ShopName = sr.ShopService != null ? sr.ShopService.Shop.ShopName : string.Empty,

                // Grab the latest payment status if one exists
                PaymentStatus = sr.Payments.OrderByDescending(p => p.CreatedAt)
                                           .Select(p => p.PaymentStatus.StatusName)
                                           .FirstOrDefault() ?? "Unpaid"
            })
            .FirstOrDefaultAsync();

        if (request == null) return NotFound("Service request not found.");

        return Ok(request);
    }

    [HttpPut("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.AccountStatus = "suspended";
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("cookie-login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // Allows standard HTML forms to post securely
    public async Task<IActionResult> CookieLogin(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] bool rememberDevice,
        CancellationToken cancellationToken)
    {
        var user = await ValidateAdminCredentialsAsync(email, password, cancellationToken);
        if (user is null) return Redirect("/login?error=true");

        if (await HasValidTrustedBrowserAsync(user, cancellationToken))
        {
            await SignInAdminAsync(user);
            return Redirect("/");
        }

        try
        {
            await CreateAndSendAdminLoginOtpAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin login OTP could not be sent to {Email}.", user.Email);
            return Redirect(LoginRedirect(email, rememberDevice, "email"));
        }

        return Redirect(LoginRedirect(user.Email, rememberDevice, otpSent: true));
    }

    [HttpPost("cookie-login/verify")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyCookieLoginOtp(
        [FromForm] string email,
        [FromForm] string otpCode,
        [FromForm] bool rememberDevice,
        CancellationToken cancellationToken)
    {
        var user = await GetActiveSystemAdminByEmailAsync(email, cancellationToken);
        if (user is null) return Redirect("/login?error=true");

        var normalizedCode = NormalizeOtpCode(otpCode);
        if (normalizedCode is null)
        {
            return Redirect(LoginRedirect(user.Email, rememberDevice, "otp", otpSent: true));
        }

        var now = DateTime.UtcNow;
        var otp = await _context.OtpVerifications
            .Where(x =>
                x.UserId == user.UserId &&
                x.Purpose == AdminLoginOtpPurpose &&
                x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.ExpiresAt <= now)
        {
            return Redirect(LoginRedirect(user.Email, rememberDevice, "otp_expired", otpSent: true));
        }

        if (otp.Attempts >= 5)
        {
            return Redirect(LoginRedirect(user.Email, rememberDevice, "otp_locked", otpSent: true));
        }

        if (!BCrypt.Net.BCrypt.Verify(normalizedCode, otp.OtpHash))
        {
            otp.Attempts++;
            await _context.SaveChangesAsync(cancellationToken);
            return Redirect(LoginRedirect(user.Email, rememberDevice, "otp", otpSent: true));
        }

        otp.ConsumedAt = now;
        if (rememberDevice)
        {
            await CreateTrustedBrowserAsync(user, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await SignInAdminAsync(user);

        return Redirect("/");
    }

    private async Task<User?> ValidateAdminCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            var user = await GetActiveSystemAdminByEmailAsync(email, cancellationToken);
            return user is not null && PasswordMatches(password, user.PasswordHash)
                ? user
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin login validation failed.");
            return null;
        }
    }

    private async Task<User?> GetActiveSystemAdminByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (user is null) return null;
        if (!string.Equals(user.AccountStatus, "active", StringComparison.OrdinalIgnoreCase)) return null;

        var isSystemAdmin = await _context.UserRoles.AnyAsync(ur =>
            ur.UserId == user.UserId &&
            ur.Role != null &&
            ur.Role.RoleName == "SystemAdmin", cancellationToken);

        return isSystemAdmin ? user : null;
    }

    private async Task CreateAndSendAdminLoginOtpAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await _context.OtpVerifications
            .Where(x => x.UserId == user.UserId && x.Purpose == AdminLoginOtpPurpose && x.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var otp in existing)
        {
            otp.ConsumedAt = now;
        }

        var code = GenerateOtpCode();
        _context.OtpVerifications.Add(new OtpVerification
        {
            UserId = user.UserId,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(code),
            Purpose = AdminLoginOtpPurpose,
            ExpiresAt = now.Add(AdminLoginOtpLifetime),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _adminOtpEmailService.SendLoginOtpAsync(user, code, cancellationToken);
    }

    private async Task<bool> HasValidTrustedBrowserAsync(User user, CancellationToken cancellationToken)
    {
        var cookieValue = Request.Cookies[AdminTrustedBrowserCookie];
        if (string.IsNullOrWhiteSpace(cookieValue)) return false;

        var parts = cookieValue.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var userId) || userId != user.UserId)
        {
            return false;
        }

        var token = parts[1];
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32) return false;

        var now = DateTime.UtcNow;
        var trustedTokens = await _context.OtpVerifications
            .Where(x =>
                x.UserId == user.UserId &&
                x.Purpose == AdminTrustedBrowserPurpose &&
                x.ConsumedAt == null &&
                x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return trustedTokens.Any(x => BCrypt.Net.BCrypt.Verify(token, x.OtpHash));
    }

    private async Task CreateTrustedBrowserAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _context.OtpVerifications.Add(new OtpVerification
        {
            UserId = user.UserId,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(token),
            Purpose = AdminTrustedBrowserPurpose,
            ExpiresAt = now.Add(AdminTrustedBrowserLifetime),
            CreatedAt = now
        });

        Response.Cookies.Append(
            AdminTrustedBrowserCookie,
            $"{user.UserId}:{token}",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.Add(AdminTrustedBrowserLifetime)
            });
    }

    private async Task SignInAdminAsync(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }

    private static string? NormalizeOtpCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = new string(code.Where(char.IsDigit).ToArray());
        return normalized.Length == 6 ? normalized : null;
    }

    private static string LoginRedirect(string email, bool rememberDevice, string? error = null, bool otpSent = false)
    {
        var query = new List<string>();
        if (otpSent) query.Add("otp=sent");
        if (!string.IsNullOrWhiteSpace(error)) query.Add($"error={Uri.EscapeDataString(error)}");
        if (!string.IsNullOrWhiteSpace(email)) query.Add($"email={Uri.EscapeDataString(email.Trim().ToLowerInvariant())}");
        if (rememberDevice) query.Add("remember=true");
        return "/login" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    private static bool IsValidAccountStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        return status.Trim().ToLowerInvariant() is "active" or "pending" or "suspended";
    }

    private static bool PasswordMatches(string password, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;

        if (storedHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            var expectedHash = "sha256:" + HashString(password);
            return string.Equals(storedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static string HashString(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder builder = new StringBuilder();
        foreach (byte b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}

public class UpdateStatusRequest
{
    public string AccountStatus { get; set; } = string.Empty;
}
