#pragma warning disable CS8602

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using BikeMate.WebAdmin.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.WebAdmin.Services;

public class ServiceRequestMechanicCandidateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public int TotalJobs { get; set; }
    public double? DistanceKm { get; set; }
    public bool IsAvailableNow { get; set; }
    public int ActiveRequestCount { get; set; }
    public string CurrentShopName { get; set; } = string.Empty;
}

public class RequestMessageDto
{
    public int MessageId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAdminSender { get; set; }
}

public class AdminApiClient(
    BikeMateDbContext context,
    ILogger<AdminApiClient> logger,
    IAdminOtpEmailService adminOtpEmailService,
    IHttpContextAccessor httpContextAccessor)
{
    private const string AdminLoginOtpPurpose = "admin_login";
    private static readonly TimeSpan AdminLoginOtpLifetime = TimeSpan.FromMinutes(10);

    public string? LastError { get; private set; }

    public async Task<AuditLogDto[]?> GetAuditLogsAsync(int take = 250)
    {
        try
        {
            LastError = null;
            take = Math.Clamp(take, 25, 1000);
            var logs = await context.AuditLogs
                .AsNoTracking()
                .Include(x => x.ActorUser)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .Select(x => new AuditLogDto
                {
                    AuditId = x.AuditId,
                    ActorName = x.ActorUser == null
                        ? "System"
                        : (x.ActorUser.FirstName + " " + x.ActorUser.LastName).Trim(),
                    ActorEmail = x.ActorUser == null ? string.Empty : x.ActorUser.Email,
                    ActionName = x.ActionName,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    OldValuesJson = x.OldValuesJson,
                    NewValuesJson = x.NewValuesJson,
                    CreatedAt = x.CreatedAt
                })
                .ToArrayAsync();

            await TrackViewAsync("ViewAuditTrail", "audit_logs", null, new { Loaded = logs.Length, Take = take });
            return logs;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load audit trail.", Array.Empty<AuditLogDto>());
        }
    }

    public async Task<AdminDashboardDto?> GetDashboardAsync()
    {
        return await GetDashboardDataAsync();
    }

    public async Task<AdminDashboardDto?> GetDashboardDataAsync()
    {
        try
        {
            var today = DateTime.UtcNow;
            var sevenDaysAgo = today.AddDays(-7);
            var recentUsers = await context.Clients
                .Where(c => c.CreatedAt >= sevenDaysAgo)
                .Select(c => c.CreatedAt)
                .ToListAsync();

            var weeklyStats = new List<DailyStatsDto>();
            for (var i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i).Date;
                weeklyStats.Add(new DailyStatsDto
                {
                    DayName = targetDate.ToString("ddd"),
                    UserCount = recentUsers.Count(date => date.Date == targetDate)
                });
            }

            var recentRequests = await context.ServiceRequests
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
                    TimeAgo = "Recently"
                })
                .ToListAsync();

            LastError = null;
            var dashboard = new AdminDashboardDto
            {
                TotalCustomers = await context.Clients.CountAsync(),
                TotalMechanics = await context.Mechanics.CountAsync(),
                TotalShops = await context.Shops.CountAsync(),
                PendingServiceRequests = await context.ServiceRequests
                    .Include(sr => sr.CurrentStatus)
                    .CountAsync(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled"),
                OnlineMechanics = await context.Mechanics.CountAsync(m => m.AvailabilityStatus == "online"),
                VerifiedShops = await context.Shops.CountAsync(s => s.ShopStatus == "verified"),
                WeeklyRegistrations = weeklyStats,
                RecentActiveRequests = recentRequests
            };

            await TrackViewAsync("ViewDashboard", "dashboard", null, new
            {
                dashboard.TotalCustomers,
                dashboard.TotalMechanics,
                dashboard.TotalShops,
                dashboard.PendingServiceRequests
            });
            return dashboard;
        }
        catch (Exception ex)
        {
            return Fail<AdminDashboardDto?>(ex, "Unable to load dashboard data.");
        }
    }

    public async Task<UserDto[]?> GetUsersAsync()
    {
        try
        {
            LastError = null;
            var users = await context.Users
                .Where(u =>
                    u.AccountStatus != "deleted" &&
                    u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Customer"))
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    AccountStatus = u.AccountStatus,
                    EmailVerified = u.EmailVerified,
                    Roles = u.UserRoles
                        .Where(ur => ur.Role != null)
                        .Select(ur => ur.Role!.RoleName)
                        .ToArray()
                })
                .ToArrayAsync();

            await TrackViewAsync("ViewCustomerDirectory", "users", null, new { Loaded = users.Length, Role = "Customer" });
            return users;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load users.", Array.Empty<UserDto>());
        }
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        try
        {
            var normalizedStatus = NormalizeAccountStatus(status);
            var user = await context.Users.FindAsync(userId) ?? throw new InvalidOperationException("User not found.");
            var oldStatus = user.AccountStatus;
            user.AccountStatus = normalizedStatus;
            user.UpdatedAt = DateTime.UtcNow;
            AddAudit("UpdateUserStatus", "users", user.UserId, new { user.Email, AccountStatus = oldStatus }, new { user.Email, AccountStatus = normalizedStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to update user status.");
        }
    }

    public async Task<UserDto> CreateUserAsync(UserCreateDto dto)
    {
        try
        {
            var firstName = Require(dto.FirstName, "First name");
            var lastName = Require(dto.LastName, "Last name");
            var email = NormalizeEmail(dto.Email);
            var accountStatus = NormalizeAccountStatus(dto.AccountStatus);
            ValidatePasswordPair(dto.Password, dto.ConfirmPassword, true);

            if (await EmailInUseAsync(email))
            {
                throw new InvalidOperationException("An account already uses this email address.");
            }

            var customerRoleId = await GetRoleIdAsync("Customer");
            var now = DateTime.UtcNow;
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = CleanOptional(dto.PhoneNumber),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                EmailVerified = dto.EmailVerified,
                AccountStatus = accountStatus,
                CreatedAt = now,
                UserRoles =
                [
                    new UserRole
                    {
                        RoleId = customerRoleId,
                        AssignedAt = now
                    }
                ],
                Client = new Client
                {
                    CreatedAt = now
                }
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            AddAudit("CreateUser", "users", user.UserId, null, new { user.Email, Role = "Customer", user.AccountStatus, user.EmailVerified });
            await context.SaveChangesAsync();
            LastError = null;

            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccountStatus = user.AccountStatus,
                EmailVerified = user.EmailVerified,
                Roles = ["Customer"]
            };
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to create user account.");
        }
    }

    public async Task<MechanicDto[]?> GetMechanicsAsync()
    {
        try
        {
            LastError = null;
            var mechanics = await context.Mechanics
                .Include(m => m.User)
                .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
                .Where(m => m.User != null && m.User.AccountStatus.ToLower().Trim() != "deleted")
                .Select(m => new MechanicDto
                {
                    Id = m.MechanicId,
                    Name = m.User.FirstName + " " + m.User.LastName,
                    Email = m.User.Email,
                    Rating = m.AverageRating,
                    Status = m.AvailabilityStatus,
                    AccountStatus = m.User.AccountStatus,
                    ShopName = m.ShopMechanics
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.Shop!.ShopName)
                        .FirstOrDefault() ?? string.Empty,
                    TotalJobs = m.TotalCompletedJobs
                })
                .ToArrayAsync();

            await TrackViewAsync("ViewMechanicDirectory", "mechanics", null, new { Loaded = mechanics.Length });
            return mechanics;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load mechanics.", Array.Empty<MechanicDto>());
        }
    }

    public async Task<ShopDto[]?> GetPendingShopsAsync()
    {
        try
        {
            LastError = null;
            var shops = await context.Shops
                .AsNoTracking()
                .Include(s => s.Owner)
                .Where(s => s.ShopStatus.ToLower().Trim() == "pending")
                .OrderByDescending(s => s.CreatedAt)
                .ToArrayAsync();

            var result = shops.Select(ToShopDto).ToArray();
            await TrackViewAsync("ViewPendingShops", "shops", null, new { Loaded = result.Length, Status = "pending" });
            return result;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load pending shops.", Array.Empty<ShopDto>());
        }
    }

    public async Task<ShopDto[]?> GetShopsAsync()
    {
        try
        {
            LastError = null;
            var shops = await context.Shops
                .AsNoTracking()
                .Include(s => s.Owner)
                .Where(s => s.ShopStatus.ToLower().Trim() != "deleted")
                .OrderByDescending(s => s.CreatedAt)
                .ToArrayAsync();

            var result = shops.Select(ToShopDto).ToArray();
            await TrackViewAsync("ViewShopDirectory", "shops", null, new { Loaded = result.Length });
            return result;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load shops.", Array.Empty<ShopDto>());
        }
    }

    public async Task<ShopOptionDto[]> GetShopOptionsAsync()
    {
        try
        {
            LastError = null;
            var options = await context.Shops
                .AsNoTracking()
                .Where(shop => shop.ShopStatus.ToLower().Trim() != "deleted")
                .OrderBy(shop => shop.ShopName)
                .Select(shop => new ShopOptionDto
                {
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    City = shop.City ?? string.Empty,
                    Status = shop.ShopStatus
                })
                .ToArrayAsync();

            await TrackViewAsync("LoadShopOptions", "shops", null, new { Loaded = options.Length });
            return options;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load shop options.", Array.Empty<ShopOptionDto>());
        }
    }

    public async Task<ShopApprovalResultDto> CreateShopAsync(ShopRegistrationInputDto dto)
    {
        try
        {
            LastError = null;
            var shopName = Require(dto.ShopName, "Shop name");
            var ownerName = Require(dto.OwnerName, "Owner name");
            var addressLine = Require(dto.AddressLine, "Shop address");
            var city = Require(dto.City, "City");
            var province = Require(dto.Province, "Province");
            var dtiRegistrationNumber = Require(dto.DtiRegistrationNumber, "DTI registration number");

            if (await ShopExistsAsync(shopName, addressLine, city, province))
            {
                throw new InvalidOperationException("This shop is already registered in that location.");
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var shopAdminRoleId = await context.Roles
                .Where(role => role.RoleName == "ShopAdmin")
                .Select(role => role.RoleId)
                .SingleAsync();

            var (firstName, lastName) = SplitName(ownerName);
            var owner = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = await CreatePendingOwnerEmailAsync(),
                AccountStatus = "pending",
                CreatedAt = DateTime.UtcNow,
                UserRoles =
                [
                    new UserRole
                    {
                        RoleId = shopAdminRoleId,
                        AssignedAt = DateTime.UtcNow
                    }
                ]
            };

            var shop = new Shop
            {
                Owner = owner,
                ShopName = shopName,
                ShopDescription = BuildShopDescription(dto.ShopDescription, dtiRegistrationNumber),
                AddressLine = addressLine,
                City = city,
                Province = province,
                ContactNumber = CleanOptional(dto.ContactNumber),
                BusinessPermitUrl = CleanOptional(dto.BusinessPermitUrl),
                ShopImageUrl = CleanOptional(dto.ShopImageUrl),
                ShopStatus = dto.VerifyOnCreate ? "verified" : "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Shops.Add(shop);
            await context.SaveChangesAsync();
            if (dto.VerifyOnCreate)
            {
                owner.AccountStatus = "active";
                owner.EmailVerified = true;
                owner.UpdatedAt = DateTime.UtcNow;
            }

            var result = new ShopApprovalResultDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                ShopStatus = shop.ShopStatus,
                OwnerActivated = string.Equals(owner.AccountStatus, "active", StringComparison.OrdinalIgnoreCase)
            };

            AddAudit("CreateShop", "shops", shop.ShopId, null, new { shop.ShopName, shop.ShopStatus, shop.City, shop.Province, OwnerUserId = owner.UserId });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to register shop.");
        }
    }

    public async Task<ServiceRequestDto[]?> GetActiveRequestsAsync()
    {
        try
        {
            LastError = null;
            var requests = await context.ServiceRequests
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

            await TrackViewAsync("ViewActiveRequests", "service_requests", null, new { Loaded = requests.Length });
            return requests;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load active requests.", Array.Empty<ServiceRequestDto>());
        }
    }

    public async Task<ServiceRequestDto[]?> GetEmergencyRequestsAsync()
    {
        try
        {
            LastError = null;
            var requests = await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.ShopService)
                .Include(sr => sr.CurrentStatus)
                .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
                .Where(sr =>
                    EF.Functions.Like(sr.CurrentStatus.StatusName, "%emergency%") ||
                    EF.Functions.Like(sr.CurrentStatus.StatusName, "%urgent%") ||
                    EF.Functions.Like(sr.IssueDescription, "%emergency%") ||
                    EF.Functions.Like(sr.IssueDescription, "%urgent%") ||
                    EF.Functions.Like(sr.IssueDescription, "%accident%") ||
                    EF.Functions.Like(sr.IssueDescription, "%breakdown%") ||
                    EF.Functions.Like(sr.IssueDescription, "%stranded%") ||
                    EF.Functions.Like(sr.IssueDescription, "%flat tire%") ||
                    EF.Functions.Like(sr.IssueDescription, "%no start%") ||
                    EF.Functions.Like(sr.IssueDescription, "%won't start%") ||
                    EF.Functions.Like(sr.IssueDescription, "%wont start%") ||
                    (sr.ShopService != null && (
                        EF.Functions.Like(sr.ShopService.ServiceName, "%emergency%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%urgent%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%accident%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%breakdown%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%stranded%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%flat tire%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%no start%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%won't start%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%wont start%"))))
                .OrderByDescending(sr => sr.CreatedAt)
                .Select(sr => new ServiceRequestDto
                {
                    Id = sr.RequestId,
                    ClientName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "Emergency Service",
                    Status = sr.CurrentStatus.StatusName,
                    TotalAmount = sr.EstimatedTotal
                })
                .ToArrayAsync();

            await TrackViewAsync("ViewEmergencyRequests", "service_requests", null, new { Loaded = requests.Length, Filter = "emergency" });
            return requests;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load emergency requests.", Array.Empty<ServiceRequestDto>());
        }
    }

    public async Task<ShopDetailsDto?> GetShopDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            var shop = await context.Shops
                .AsNoTracking()
                .Include(s => s.Owner)
                .Include(s => s.OperatingHours)
                .Include(s => s.Services).ThenInclude(service => service.Category)
                .Include(s => s.Services).ThenInclude(service => service.Images)
                .Include(s => s.Products).ThenInclude(product => product.Images)
                .Include(s => s.ShopMechanics).ThenInclude(sm => sm.Mechanic).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(s => s.ShopId == id);

            var details = shop is null ? null : ToShopDetailsDto(shop);
            await TrackViewAsync("ViewShopDetails", "shops", id, new { Found = details is not null, ShopName = details?.ShopName });
            return details;
        }
        catch (Exception ex)
        {
            return Fail<ShopDetailsDto?>(ex, "Unable to load shop details.");
        }
    }

    public async Task<ShopEditDto?> GetShopForEditAsync(int id)
    {
        try
        {
            LastError = null;
            var shop = await context.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShopId == id);

            if (shop is null)
            {
                LastError = "Shop not found.";
                await TrackViewAsync("OpenShopEdit", "shops", id, new { Found = false });
                return null;
            }

            var description = shop.ShopDescription ?? string.Empty;
            var dto = new ShopEditDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                ShopDescription = CleanShopDescription(description),
                AddressLine = shop.AddressLine ?? string.Empty,
                City = shop.City ?? string.Empty,
                Province = shop.Province ?? string.Empty,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                ContactNumber = shop.ContactNumber,
                BusinessPermitUrl = shop.BusinessPermitUrl,
                ShopImageUrl = shop.ShopImageUrl,
                ShopLogoUrl = shop.ShopLogoUrl,
                OwnerValidIdUrl = shop.OwnerValidIdUrl,
                DtiRegistrationNumber = ExtractDtiRegistration(description),
                ShopStatus = shop.ShopStatus
            };

            await TrackViewAsync("OpenShopEdit", "shops", id, new { Found = true, shop.ShopName, shop.ShopStatus });
            return dto;
        }
        catch (Exception ex)
        {
            return Fail<ShopEditDto?>(ex, "Unable to load shop for editing.");
        }
    }

    public async Task UpdateShopAsync(int id, ShopEditDto dto)
    {
        try
        {
            var shop = await context.Shops
                .FirstOrDefaultAsync(s => s.ShopId == id)
                ?? throw new InvalidOperationException("Shop not found.");

            var shopName = Require(dto.ShopName, "Shop name");
            var addressLine = Require(dto.AddressLine, "Shop address");
            var city = Require(dto.City, "City");
            var province = Require(dto.Province, "Province");
            var status = NormalizeShopStatus(dto.ShopStatus);
            var oldValues = new { shop.ShopName, shop.ShopStatus, shop.AddressLine, shop.City, shop.Province, shop.ContactNumber };

            if (await ShopExistsAsync(shopName, addressLine, city, province, id))
            {
                throw new InvalidOperationException("Another shop already uses this name and location.");
            }

            shop.ShopName = shopName;
            shop.ShopDescription = BuildShopDescription(dto.ShopDescription, dto.DtiRegistrationNumber);
            shop.AddressLine = addressLine;
            shop.City = city;
            shop.Province = province;
            shop.Latitude = dto.Latitude;
            shop.Longitude = dto.Longitude;
            shop.ContactNumber = CleanOptional(dto.ContactNumber);
            shop.BusinessPermitUrl = CleanOptional(dto.BusinessPermitUrl);
            shop.ShopImageUrl = CleanOptional(dto.ShopImageUrl);
            shop.ShopLogoUrl = CleanOptional(dto.ShopLogoUrl);
            shop.OwnerValidIdUrl = CleanOptional(dto.OwnerValidIdUrl);
            shop.ShopStatus = status;
            shop.UpdatedAt = DateTime.UtcNow;

            AddAudit("UpdateShop", "shops", shop.ShopId, oldValues, new { shop.ShopName, shop.ShopStatus, shop.AddressLine, shop.City, shop.Province, shop.ContactNumber });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to update shop.");
        }
    }

    public async Task<PaymentDto[]?> GetPaymentsAsync()
    {
        try
        {
            LastError = null;
            var payments = await context.Payments
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

            await TrackViewAsync("ViewPayments", "payments", null, new { Loaded = payments.Length });
            return payments;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load payments.", Array.Empty<PaymentDto>());
        }
    }

    public async Task<PaymentDetailsDto?> GetPaymentDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            var payment = await context.Payments
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

            await TrackViewAsync("ViewPaymentDetails", "payments", id, new { Found = payment is not null, RequestId = payment?.RequestId, payment?.Status });
            return payment;
        }
        catch (Exception ex)
        {
            return Fail<PaymentDetailsDto?>(ex, "Unable to load payment details.");
        }
    }

    public async Task<MechanicProfileDto?> GetMechanicDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            var mechanic = await context.Mechanics
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
                    AccountStatus = m.User.AccountStatus,
                    IsVerified = m.IsVerified,
                    AverageRating = m.AverageRating,
                    TotalJobs = m.TotalCompletedJobs,
                    Bio = m.Bio ?? string.Empty,
                    YearsExperience = m.YearsExperience ?? 0,
                    CurrentShopName = m.ShopMechanics
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.Shop.ShopName)
                        .FirstOrDefault() ?? string.Empty,
                    ServiceHistory = m.AssignedRequests.Select(sr => new MechanicServiceHistoryDto
                    {
                        RequestId = sr.RequestId,
                        ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : string.Empty,
                        Status = sr.CurrentStatus.StatusName,
                        Date = sr.CreatedAt
                    }).OrderByDescending(x => x.Date).ToList(),
                    Reviews = m.Reviews.Select(r => new MechanicReviewDto
                    {
                        Rating = r.Rating,
                        Comment = r.Comment ?? string.Empty,
                        CustomerName = r.Client.User.FirstName,
                        Date = r.CreatedAt
                    }).OrderByDescending(x => x.Date).ToList()
                })
                .FirstOrDefaultAsync();

            await TrackViewAsync("ViewMechanicDetails", "mechanics", id, new { Found = mechanic is not null, Name = mechanic?.FullName, mechanic?.AccountStatus });
            return mechanic;
        }
        catch (Exception ex)
        {
            return Fail<MechanicProfileDto?>(ex, "Unable to load mechanic details.");
        }
    }

    public async Task<MechanicEditDto?> GetMechanicForEditAsync(int id)
    {
        try
        {
            LastError = null;
            var mechanic = await context.Mechanics
                .AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
                .FirstOrDefaultAsync(m => m.MechanicId == id);

            if (mechanic is null)
            {
                LastError = "Mechanic not found.";
                await TrackViewAsync("OpenMechanicEdit", "mechanics", id, new { Found = false });
                return null;
            }

            var dto = ToMechanicEditDto(mechanic);
            await TrackViewAsync("OpenMechanicEdit", "mechanics", id, new { Found = true, dto.Email, dto.AccountStatus, dto.IsVerified });
            return dto;
        }
        catch (Exception ex)
        {
            return Fail<MechanicEditDto?>(ex, "Unable to load mechanic for editing.");
        }
    }

    public async Task<MechanicEditDto> CreateMechanicAsync(MechanicEditDto dto)
    {
        try
        {
            var firstName = Require(dto.FirstName, "First name");
            var lastName = Require(dto.LastName, "Last name");
            var email = NormalizeEmail(dto.Email);
            var accountStatus = NormalizeAccountStatus(dto.AccountStatus);
            var availabilityStatus = NormalizeMechanicStatus(dto.AvailabilityStatus);
            ValidatePasswordPair(dto.Password, dto.ConfirmPassword, true);

            if (await EmailInUseAsync(email))
            {
                throw new InvalidOperationException("An account already uses this email address.");
            }

            var mechanicRoleId = await GetRoleIdAsync("Mechanic");
            var now = DateTime.UtcNow;
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = CleanOptional(dto.PhoneNumber),
                ProfileImageUrl = CleanOptional(dto.ProfileImageUrl),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password!),
                EmailVerified = dto.EmailVerified,
                AccountStatus = accountStatus,
                CreatedAt = now,
                UserRoles =
                [
                    new UserRole
                    {
                        RoleId = mechanicRoleId,
                        AssignedAt = now
                    }
                ]
            };

            var mechanic = new Mechanic
            {
                User = user,
                MiddleName = CleanOptional(dto.MiddleName),
                Sex = CleanOptional(dto.Sex),
                Birthdate = dto.Birthdate,
                AddressLine = CleanOptional(dto.AddressLine),
                Barangay = CleanOptional(dto.Barangay),
                City = CleanOptional(dto.City),
                Province = CleanOptional(dto.Province),
                ZipCode = CleanOptional(dto.ZipCode),
                ValidIdImageUrl = CleanOptional(dto.ValidIdImageUrl),
                CertificationImageUrl = CleanOptional(dto.CertificationImageUrl),
                Bio = CleanOptional(dto.Bio),
                YearsExperience = dto.YearsExperience,
                IsVerified = dto.IsVerified,
                AvailabilityStatus = string.Equals(accountStatus, "active", StringComparison.OrdinalIgnoreCase)
                    ? availabilityStatus
                    : "offline",
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Mechanics.Add(mechanic);
            await context.SaveChangesAsync();
            await UpsertMechanicShopAssignmentAsync(mechanic, dto.ShopId, dto.AssignmentActive);
            AddAudit("CreateMechanic", "mechanics", mechanic.MechanicId, null, new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.AccountStatus, mechanic.IsVerified, mechanic.AvailabilityStatus, dto.ShopId });
            await context.SaveChangesAsync();
            LastError = null;

            return await GetMechanicForEditAsync(mechanic.MechanicId)
                ?? throw new InvalidOperationException("Mechanic was created but could not be reloaded.");
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to create mechanic.");
        }
    }

    public async Task UpdateMechanicAsync(int id, MechanicEditDto dto)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(m => m.User)
                .Include(m => m.ShopMechanics)
                .FirstOrDefaultAsync(m => m.MechanicId == id)
                ?? throw new InvalidOperationException("Mechanic not found.");

            if (mechanic.User is null)
            {
                throw new InvalidOperationException("Mechanic user account is missing.");
            }

            var firstName = Require(dto.FirstName, "First name");
            var lastName = Require(dto.LastName, "Last name");
            var email = NormalizeEmail(dto.Email);
            var accountStatus = NormalizeAccountStatus(dto.AccountStatus);
            var availabilityStatus = NormalizeMechanicStatus(dto.AvailabilityStatus);
            ValidatePasswordPair(dto.Password, dto.ConfirmPassword, false);
            var oldValues = new
            {
                mechanic.User.Email,
                Name = $"{mechanic.User.FirstName} {mechanic.User.LastName}",
                mechanic.User.AccountStatus,
                mechanic.IsVerified,
                mechanic.AvailabilityStatus,
                ShopIds = mechanic.ShopMechanics.Where(x => x.IsActive).Select(x => x.ShopId).ToArray()
            };

            if (!string.Equals(mechanic.User.Email, email, StringComparison.OrdinalIgnoreCase) &&
                await EmailInUseAsync(email, mechanic.UserId))
            {
                throw new InvalidOperationException("Another account already uses this email address.");
            }

            mechanic.User.FirstName = firstName;
            mechanic.User.LastName = lastName;
            mechanic.User.Email = email;
            mechanic.User.PhoneNumber = CleanOptional(dto.PhoneNumber);
            mechanic.User.ProfileImageUrl = CleanOptional(dto.ProfileImageUrl);
            mechanic.User.EmailVerified = dto.EmailVerified;
            mechanic.User.AccountStatus = accountStatus;
            mechanic.User.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                mechanic.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            mechanic.MiddleName = CleanOptional(dto.MiddleName);
            mechanic.Sex = CleanOptional(dto.Sex);
            mechanic.Birthdate = dto.Birthdate;
            mechanic.AddressLine = CleanOptional(dto.AddressLine);
            mechanic.Barangay = CleanOptional(dto.Barangay);
            mechanic.City = CleanOptional(dto.City);
            mechanic.Province = CleanOptional(dto.Province);
            mechanic.ZipCode = CleanOptional(dto.ZipCode);
            mechanic.ValidIdImageUrl = CleanOptional(dto.ValidIdImageUrl);
            mechanic.CertificationImageUrl = CleanOptional(dto.CertificationImageUrl);
            mechanic.Bio = CleanOptional(dto.Bio);
            mechanic.YearsExperience = dto.YearsExperience;
            mechanic.IsVerified = dto.IsVerified;
            mechanic.AvailabilityStatus = string.Equals(accountStatus, "active", StringComparison.OrdinalIgnoreCase)
                ? availabilityStatus
                : "offline";
            mechanic.UpdatedAt = DateTime.UtcNow;

            await UpsertMechanicShopAssignmentAsync(mechanic, dto.ShopId, dto.AssignmentActive);
            AddAudit("UpdateMechanic", "mechanics", mechanic.MechanicId, oldValues, new
            {
                mechanic.User.Email,
                Name = $"{mechanic.User.FirstName} {mechanic.User.LastName}",
                mechanic.User.AccountStatus,
                mechanic.IsVerified,
                mechanic.AvailabilityStatus,
                dto.ShopId,
                dto.AssignmentActive
            });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to update mechanic.");
        }
    }

    public async Task DeleteMechanicAsync(int id)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(m => m.User).ThenInclude(u => u!.UserRoles).ThenInclude(ur => ur.Role)
                .Include(m => m.User).ThenInclude(u => u!.AuthProviders)
                .Include(m => m.User).ThenInclude(u => u!.DeviceTokens)
                .Include(m => m.ShopMechanics)
                .FirstOrDefaultAsync(m => m.MechanicId == id)
                ?? throw new InvalidOperationException("Mechanic not found.");
            var oldValues = new
            {
                mechanic.User?.Email,
                Name = mechanic.User is null ? string.Empty : $"{mechanic.User.FirstName} {mechanic.User.LastName}",
                mechanic.User?.AccountStatus,
                mechanic.IsVerified,
                mechanic.AvailabilityStatus
            };

            mechanic.IsVerified = false;
            mechanic.AvailabilityStatus = "offline";
            mechanic.UpdatedAt = DateTime.UtcNow;

            foreach (var assignment in mechanic.ShopMechanics)
            {
                assignment.IsActive = false;
            }

            if (mechanic.User is not null)
            {
                AnonymizeDeletedUser(mechanic.User);
            }

            AddAudit("DeleteMechanic", "mechanics", mechanic.MechanicId, oldValues, new { AccountStatus = mechanic.User?.AccountStatus, mechanic.IsVerified, mechanic.AvailabilityStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to delete mechanic.");
        }
    }

    public async Task<UserEditDto?> GetUserForEditAsync(int id)
    {
        try
        {
            var user = await context.Users.FindAsync(id);
            LastError = user == null ? "User not found." : null;
            if (user is null)
            {
                await TrackViewAsync("OpenUserEdit", "users", id, new { Found = false });
                return null;
            }

            var dto = new UserEditDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccountStatus = user.AccountStatus
            };

            await TrackViewAsync("OpenUserEdit", "users", id, new { Found = true, user.Email, user.AccountStatus });
            return dto;
        }
        catch (Exception ex)
        {
            return Fail<UserEditDto?>(ex, "Unable to load user details.");
        }
    }

    public async Task UpdateUserAsync(int id, UserEditDto userDto)
    {
        try
        {
            var normalizedStatus = NormalizeAccountStatus(userDto.AccountStatus);
            var user = await context.Users.FindAsync(id) ?? throw new InvalidOperationException("User not found.");
            var oldValues = new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus };
            user.FirstName = Require(userDto.FirstName, "First name");
            user.LastName = Require(userDto.LastName, "Last name");
            user.PhoneNumber = CleanOptional(userDto.PhoneNumber);
            user.AccountStatus = normalizedStatus;
            user.UpdatedAt = DateTime.UtcNow;
            AddAudit("UpdateUser", "users", user.UserId, oldValues, new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to update user.");
        }
    }

    public async Task DeleteUserAsync(int id)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.AuthProviders)
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(u => u.UserId == id)
                ?? throw new InvalidOperationException("User not found.");

            if (user.UserRoles.Any(ur => string.Equals(ur.Role?.RoleName, "SystemAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("System admin accounts cannot be deleted from the user directory.");
            }

            if (string.Equals(user.AccountStatus, "deleted", StringComparison.OrdinalIgnoreCase))
            {
                LastError = null;
                return;
            }

            var oldValues = new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus };
            AnonymizeDeletedUser(user);
            AddAudit("DeleteUser", "users", id, oldValues, new { user.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to delete user account.");
        }
    }

    public async Task<AdminAccountDto[]?> GetAdminAccountsAsync()
    {
        try
        {
            LastError = null;
            var admins = await context.Users
                .AsNoTracking()
                .Where(user =>
                    user.AccountStatus.ToLower().Trim() != "deleted" &&
                    user.UserRoles.Any(role =>
                        role.Role != null &&
                        role.Role.RoleName == "SystemAdmin"))
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToArrayAsync();

            var result = admins.Select(ToAdminAccountDto).ToArray();
            await TrackViewAsync("ViewAdminAccounts", "users", null, new { Loaded = result.Length, Role = "SystemAdmin" });
            return result;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load admin accounts.", Array.Empty<AdminAccountDto>());
        }
    }

    public async Task<AdminAccountEditDto?> GetAdminAccountForEditAsync(int id)
    {
        try
        {
            LastError = null;
            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(user => user.UserId == id)
                ?? throw new InvalidOperationException("Admin account not found.");

            EnsureSystemAdminUser(user);

            var dto = new AdminAccountEditDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccountStatus = user.AccountStatus
            };

            await TrackViewAsync("OpenAdminEdit", "users", id, new { Found = true, user.Email, user.AccountStatus });
            return dto;
        }
        catch (Exception ex)
        {
            return Fail<AdminAccountEditDto?>(ex, "Unable to load admin account.");
        }
    }

    public async Task<AdminAccountDto> CreateAdminAccountAsync(AdminAccountCreateDto dto)
    {
        try
        {
            var firstName = Require(dto.FirstName, "First name");
            var lastName = Require(dto.LastName, "Last name");
            var email = NormalizeEmail(dto.Email);
            var password = Require(dto.Password, "Password");

            if (password.Length < 8)
            {
                throw new InvalidOperationException("Password must be at least 8 characters long.");
            }

            if (!string.Equals(password, dto.ConfirmPassword, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Password and confirmation password do not match.");
            }

            var emailExists = await context.Users.AnyAsync(user =>
                user.AccountStatus.ToLower().Trim() != "deleted" &&
                user.Email.ToLower() == email);

            if (emailExists)
            {
                throw new InvalidOperationException("An active account already uses this email address.");
            }

            var systemAdminRole = await context.Roles
                .SingleOrDefaultAsync(role => role.RoleName == "SystemAdmin")
                ?? throw new InvalidOperationException("SystemAdmin role is missing from the database.");

            var now = DateTime.UtcNow;
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = CleanOptional(dto.PhoneNumber),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                EmailVerified = true,
                PhoneVerified = false,
                AccountStatus = "active",
                CreatedAt = now
            };

            user.UserRoles.Add(new UserRole
            {
                RoleId = systemAdminRole.RoleId,
                AssignedAt = now
            });

            context.Users.Add(user);
            await context.SaveChangesAsync();
            AddAudit("CreateAdmin", "users", user.UserId, null, new { user.Email, Role = "SystemAdmin", user.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;

            return ToAdminAccountDto(user);
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to create admin account.");
        }
    }

    public async Task SendAdminLoginCodeAsync(int id)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(user => user.UserId == id)
                ?? throw new InvalidOperationException("Admin account not found.");

            EnsureSystemAdminUser(user);
            if (!string.Equals(user.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only active admin accounts can receive login codes.");
            }

            var now = DateTime.UtcNow;
            var existing = await context.OtpVerifications
                .Where(x => x.UserId == user.UserId && x.Purpose == AdminLoginOtpPurpose && x.ConsumedAt == null)
                .ToListAsync();

            foreach (var otp in existing)
            {
                otp.ConsumedAt = now;
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            context.OtpVerifications.Add(new OtpVerification
            {
                UserId = user.UserId,
                OtpHash = BCrypt.Net.BCrypt.HashPassword(code),
                Purpose = AdminLoginOtpPurpose,
                ExpiresAt = now.Add(AdminLoginOtpLifetime),
                CreatedAt = now
            });

            await context.SaveChangesAsync();
            await adminOtpEmailService.SendLoginOtpAsync(user, code, CancellationToken.None);
            AddAudit("SendAdminLoginCode", "users", user.UserId, null, new { user.Email, Purpose = AdminLoginOtpPurpose, ExpiresAt = now.Add(AdminLoginOtpLifetime) });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to send admin login code.");
        }
    }

    public async Task UpdateAdminAccountAsync(int id, AdminAccountEditDto dto)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(user => user.UserId == id)
                ?? throw new InvalidOperationException("Admin account not found.");

            EnsureSystemAdminUser(user);
            var oldValues = new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus };

            var firstName = Require(dto.FirstName, "First name");
            var lastName = Require(dto.LastName, "Last name");
            var normalizedStatus = NormalizeAccountStatus(dto.AccountStatus);

            if (!string.Equals(normalizedStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(user.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
            {
                var remainingActiveAdmins = await context.Users.CountAsync(candidate =>
                    candidate.UserId != id &&
                    candidate.AccountStatus.ToLower().Trim() == "active" &&
                    candidate.UserRoles.Any(role =>
                        role.Role != null &&
                        role.Role.RoleName == "SystemAdmin"));

                if (remainingActiveAdmins == 0)
                {
                    throw new InvalidOperationException("At least one active system admin must remain.");
                }
            }

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Admin email addresses cannot be changed here. Create a new admin account or contact the database administrator.");
            }

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (dto.NewPassword.Length < 8)
                {
                    throw new InvalidOperationException("New password must be at least 8 characters long.");
                }

                if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("New password and confirmation password do not match.");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = CleanOptional(dto.PhoneNumber);
            user.AccountStatus = normalizedStatus;
            user.UpdatedAt = DateTime.UtcNow;

            AddAudit("UpdateAdmin", "users", user.UserId, oldValues, new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus, PasswordChanged = !string.IsNullOrWhiteSpace(dto.NewPassword) });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to update admin account.");
        }
    }

    public async Task DeleteAdminAccountAsync(int id)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.AuthProviders)
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(user => user.UserId == id)
                ?? throw new InvalidOperationException("Admin account not found.");

            EnsureSystemAdminUser(user);

            if (string.Equals(user.AccountStatus, "deleted", StringComparison.OrdinalIgnoreCase))
            {
                LastError = null;
                return;
            }

            var remainingActiveAdmins = await context.Users.CountAsync(candidate =>
                candidate.UserId != id &&
                candidate.AccountStatus.ToLower().Trim() == "active" &&
                candidate.UserRoles.Any(role =>
                    role.Role != null &&
                    role.Role.RoleName == "SystemAdmin"));

            if (remainingActiveAdmins == 0)
            {
                throw new InvalidOperationException("Create or activate another system admin before deleting this account.");
            }

            var oldValues = new { user.Email, Name = $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.AccountStatus };
            AnonymizeDeletedUser(user);
            AddAudit("DeleteAdmin", "users", id, oldValues, new { user.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to delete admin account.");
        }
    }

    public async Task<ShopApprovalResultDto> ApproveShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.ShopId == id)
                ?? throw new InvalidOperationException("Shop not found.");

            if (shop.Owner is null)
            {
                throw new InvalidOperationException("This shop does not have an owner account to activate.");
            }

            if (!shop.Owner.EmailVerified)
            {
                throw new InvalidOperationException("The owner must verify the email OTP before this shop can be approved.");
            }

            shop.ShopStatus = "verified";
            shop.UpdatedAt = DateTime.UtcNow;
            shop.Owner.AccountStatus = "active";
            shop.Owner.UpdatedAt = DateTime.UtcNow;

            AddAudit("ApproveShop", "shops", shop.ShopId, new { ShopStatus = "pending", OwnerStatus = "pending" }, new { shop.ShopName, shop.ShopStatus, OwnerUserId = shop.OwnerUserId, OwnerStatus = shop.Owner.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
            return new ShopApprovalResultDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                ShopStatus = shop.ShopStatus,
                OwnerActivated = true
            };
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to approve shop.");
        }
    }

    public async Task SuspendShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.ShopId == id)
                ?? throw new InvalidOperationException("Shop not found.");
            var oldValues = new { shop.ShopName, shop.ShopStatus, OwnerStatus = shop.Owner?.AccountStatus };

            shop.ShopStatus = "suspended";
            shop.UpdatedAt = DateTime.UtcNow;
            if (shop.Owner is not null && !string.Equals(shop.Owner.AccountStatus, "deleted", StringComparison.OrdinalIgnoreCase))
            {
                shop.Owner.AccountStatus = "suspended";
                shop.Owner.UpdatedAt = DateTime.UtcNow;
            }

            AddAudit("SuspendShop", "shops", shop.ShopId, oldValues, new { shop.ShopName, shop.ShopStatus, OwnerStatus = shop.Owner?.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to suspend shop.");
        }
    }

    public async Task RejectShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.ShopId == id)
                ?? throw new InvalidOperationException("Shop not found.");
            var oldValues = new { shop.ShopName, shop.ShopStatus, OwnerStatus = shop.Owner?.AccountStatus };
            shop.ShopStatus = "rejected";
            shop.UpdatedAt = DateTime.UtcNow;
            if (shop.Owner is not null)
            {
                shop.Owner.AccountStatus = "rejected";
                shop.Owner.UpdatedAt = DateTime.UtcNow;
            }

            AddAudit("RejectShop", "shops", shop.ShopId, oldValues, new { shop.ShopName, shop.ShopStatus, OwnerStatus = shop.Owner?.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to reject shop.");
        }
    }

    public async Task<CustomerApplicationDto[]?> GetPendingCustomerApplicationsAsync()
    {
        try
        {
            LastError = null;
            var customers = await context.Clients
                .AsNoTracking()
                .Include(client => client.User)
                .Include(client => client.Addresses)
                .Where(client => client.User != null && client.User.AccountStatus.ToLower().Trim() == "pending")
                .OrderByDescending(client => client.CreatedAt)
                .ToArrayAsync();

            var result = customers.Select(ToCustomerApplicationDto).ToArray();
            await TrackViewAsync("ViewPendingCustomers", "clients", null, new { Loaded = result.Length, Status = "pending" });
            return result;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load pending customer applications.", Array.Empty<CustomerApplicationDto>());
        }
    }

    public async Task ApproveCustomerAsync(int clientId)
    {
        try
        {
            var customer = await context.Clients
                .Include(client => client.User)
                .FirstOrDefaultAsync(client => client.ClientId == clientId)
                ?? throw new InvalidOperationException("Customer account not found.");

            if (customer.User is null)
            {
                throw new InvalidOperationException("This customer does not have a user account to activate.");
            }

            if (!customer.User.EmailVerified)
            {
                throw new InvalidOperationException("The customer must verify the email OTP before approval.");
            }

            if (string.IsNullOrWhiteSpace(customer.ValidIdImageUrl))
            {
                throw new InvalidOperationException("A valid ID image is required before customer approval.");
            }

            customer.User.AccountStatus = "active";
            customer.User.UpdatedAt = DateTime.UtcNow;
            AddAudit("ApproveCustomer", "clients", customer.ClientId, new { AccountStatus = "pending" }, new { customer.User.Email, customer.User.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to approve customer.");
        }
    }

    public async Task RejectCustomerAsync(int clientId)
    {
        try
        {
            var customer = await context.Clients
                .Include(client => client.User)
                .FirstOrDefaultAsync(client => client.ClientId == clientId)
                ?? throw new InvalidOperationException("Customer account not found.");

            if (customer.User is not null)
            {
                var oldValues = new { customer.User.Email, customer.User.AccountStatus };
                customer.User.AccountStatus = "rejected";
                customer.User.UpdatedAt = DateTime.UtcNow;
                AddAudit("RejectCustomer", "clients", customer.ClientId, oldValues, new { customer.User.Email, customer.User.AccountStatus });
            }

            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to reject customer.");
        }
    }

    public async Task<MechanicApplicationDto[]?> GetPendingMechanicApplicationsAsync()
    {
        try
        {
            LastError = null;
            var mechanics = await context.Mechanics
                .AsNoTracking()
                .Include(mechanic => mechanic.User)
                .Include(mechanic => mechanic.ShopMechanics).ThenInclude(assignment => assignment.Shop)
                .Where(mechanic =>
                    mechanic.User != null &&
                    mechanic.User.AccountStatus.ToLower().Trim() != "deleted" &&
                    mechanic.User.AccountStatus.ToLower().Trim() != "rejected" &&
                    (!mechanic.IsVerified || mechanic.User.AccountStatus.ToLower().Trim() == "pending"))
                .OrderByDescending(mechanic => mechanic.CreatedAt)
                .ToArrayAsync();

            var result = mechanics.Select(ToMechanicApplicationDto).ToArray();
            await TrackViewAsync("ViewPendingMechanics", "mechanics", null, new { Loaded = result.Length, Status = "pending_review" });
            return result;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load pending mechanic applications.", Array.Empty<MechanicApplicationDto>());
        }
    }

    public async Task ApproveMechanicAsync(int mechanicId)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(item => item.User)
                .Include(item => item.ShopMechanics)
                .FirstOrDefaultAsync(item => item.MechanicId == mechanicId)
                ?? throw new InvalidOperationException("Mechanic account not found.");

            if (mechanic.User is null)
            {
                throw new InvalidOperationException("This mechanic does not have a user account to activate.");
            }

            if (!mechanic.User.EmailVerified)
            {
                throw new InvalidOperationException("The mechanic must verify the email OTP before approval.");
            }

            if (string.IsNullOrWhiteSpace(mechanic.ValidIdImageUrl) ||
                string.IsNullOrWhiteSpace(mechanic.CertificationImageUrl))
            {
                throw new InvalidOperationException("Valid ID and mechanic certification/license files are required before approval.");
            }

            mechanic.IsVerified = true;
            mechanic.AvailabilityStatus = string.IsNullOrWhiteSpace(mechanic.AvailabilityStatus)
                ? "offline"
                : mechanic.AvailabilityStatus;
            mechanic.User.AccountStatus = "active";
            mechanic.UpdatedAt = DateTime.UtcNow;
            mechanic.User.UpdatedAt = DateTime.UtcNow;

            foreach (var assignment in mechanic.ShopMechanics)
            {
                assignment.IsActive = true;
            }

            AddAudit("ApproveMechanic", "mechanics", mechanic.MechanicId, new { mechanic.User.Email, AccountStatus = "pending", mechanic.IsVerified }, new { mechanic.User.Email, mechanic.User.AccountStatus, mechanic.IsVerified, mechanic.AvailabilityStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to approve mechanic.");
        }
    }

    public async Task RejectMechanicAsync(int mechanicId)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(item => item.User)
                .Include(item => item.ShopMechanics)
                .FirstOrDefaultAsync(item => item.MechanicId == mechanicId)
                ?? throw new InvalidOperationException("Mechanic account not found.");
            var oldValues = new { mechanic.User?.Email, mechanic.User?.AccountStatus, mechanic.IsVerified, mechanic.AvailabilityStatus };

            mechanic.IsVerified = false;
            mechanic.UpdatedAt = DateTime.UtcNow;
            if (mechanic.User is not null)
            {
                mechanic.User.AccountStatus = "rejected";
                mechanic.User.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var assignment in mechanic.ShopMechanics)
            {
                assignment.IsActive = false;
            }

            AddAudit("RejectMechanic", "mechanics", mechanic.MechanicId, oldValues, new { mechanic.User?.Email, mechanic.User?.AccountStatus, mechanic.IsVerified });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to reject mechanic.");
        }
    }

    public async Task DeleteShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops
                .Include(s => s.Services)
                .Include(s => s.Products)
                .Include(s => s.ShopMechanics)
                .Include(s => s.Owner).ThenInclude(o => o.UserRoles).ThenInclude(ur => ur.Role)
                .Include(s => s.Owner).ThenInclude(o => o.AuthProviders)
                .Include(s => s.Owner).ThenInclude(o => o.DeviceTokens)
                .FirstOrDefaultAsync(s => s.ShopId == id)
                ?? throw new InvalidOperationException("Shop not found.");

            if (string.Equals(shop.ShopStatus, "deleted", StringComparison.OrdinalIgnoreCase))
            {
                LastError = null;
                return;
            }

            var oldValues = new { shop.ShopName, shop.ShopStatus, OwnerEmail = shop.Owner?.Email, OwnerStatus = shop.Owner?.AccountStatus };
            var now = DateTime.UtcNow;
            shop.ShopStatus = "deleted";
            shop.UpdatedAt = now;
            ScrubDeletedShopDetails(shop);

            foreach (var service in shop.Services)
            {
                service.IsActive = false;
            }

            foreach (var product in shop.Products)
            {
                product.IsActive = false;
                product.UpdatedAt = now;
            }

            foreach (var assignment in shop.ShopMechanics)
            {
                assignment.IsActive = false;
            }

            if (shop.Owner is not null)
            {
                var ownerIsSystemAdmin = shop.Owner.UserRoles.Any(ur =>
                    string.Equals(ur.Role?.RoleName, "SystemAdmin", StringComparison.OrdinalIgnoreCase));

                var ownerHasOtherOpenShop = await context.Shops.AnyAsync(candidate =>
                    candidate.ShopId != shop.ShopId &&
                    candidate.OwnerUserId == shop.OwnerUserId &&
                    candidate.ShopStatus.ToLower().Trim() != "deleted");

                if (!ownerIsSystemAdmin && !ownerHasOtherOpenShop)
                {
                    AnonymizeDeletedUser(shop.Owner);
                }
            }

            AddAudit("DeleteShop", "shops", shop.ShopId, oldValues, new { shop.ShopStatus, OwnerStatus = shop.Owner?.AccountStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, ex is InvalidOperationException ? ex.Message : "Unable to delete shop account.");
        }
    }

    public async Task SuspendMechanicAsync(int id)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MechanicId == id)
                ?? throw new InvalidOperationException("Mechanic not found.");
            var oldValues = new { mechanic.User.Email, mechanic.User.AccountStatus, mechanic.AvailabilityStatus };

            mechanic.User.AccountStatus = "suspended";
            mechanic.AvailabilityStatus = "offline";
            mechanic.UpdatedAt = DateTime.UtcNow;
            AddAudit("SuspendMechanic", "mechanics", mechanic.MechanicId, oldValues, new { mechanic.User.Email, mechanic.User.AccountStatus, mechanic.AvailabilityStatus });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to suspend mechanic.");
        }
    }

    public async Task<ServiceRequestDetailsDto?> GetServiceRequestDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            var request = await context.ServiceRequests
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
                    CustomerId = sr.ClientId,
                    CustomerName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                    CustomerPhone = sr.Client.User.PhoneNumber ?? string.Empty,
                    MechanicId = sr.MechanicId,
                    MechanicName = sr.Mechanic != null ? sr.Mechanic.User.FirstName + " " + sr.Mechanic.User.LastName : "Pending Assignment",
                    MechanicPhone = sr.Mechanic != null ? sr.Mechanic.User.PhoneNumber ?? string.Empty : string.Empty,
                    ShopName = sr.ShopService != null ? sr.ShopService.Shop.ShopName : string.Empty,
                    PaymentStatus = sr.Payments.OrderByDescending(p => p.CreatedAt)
                        .Select(p => p.PaymentStatus.StatusName)
                        .FirstOrDefault() ?? "Unpaid"
                })
                .FirstOrDefaultAsync();

            await TrackViewAsync("ViewServiceRequestDetails", "service_requests", id, new { Found = request is not null, request?.Status, request?.PaymentStatus });
            return request;
        }
        catch (Exception ex)
        {
            return Fail<ServiceRequestDetailsDto?>(ex, "Unable to load service request details.");
        }
    }

    public async Task<ServiceRequestMechanicCandidateDto[]> GetMechanicCandidatesForRequestAsync(int requestId)
    {
        try
        {
            var request = await context.ServiceRequests
                .AsNoTracking()
                .Where(sr => sr.RequestId == requestId)
                .Select(sr => new
                {
                    sr.ServiceLatitude,
                    sr.ServiceLongitude
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                LastError = "Service request not found.";
                await TrackViewAsync("CheckEmergencyMechanicCandidates", "service_requests", requestId, new { Found = false, Loaded = 0 });
                return Array.Empty<ServiceRequestMechanicCandidateDto>();
            }

            var now = DateTime.UtcNow;
            var dayOfWeek = (int)now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            var mechanics = await context.Mechanics
                .AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.Availability)
                .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
                .Include(m => m.AssignedRequests).ThenInclude(sr => sr.CurrentStatus)
                .Where(m => m.User.AccountStatus == "active")
                .Select(m => new
                {
                    m.MechanicId,
                    m.User.FirstName,
                    m.User.LastName,
                    m.User.PhoneNumber,
                    m.AvailabilityStatus,
                    m.CurrentLatitude,
                    m.CurrentLongitude,
                    m.AverageRating,
                    m.TotalCompletedJobs,
                    OpenRequestCount = m.AssignedRequests.Count(sr =>
                        sr.CurrentStatus.StatusName != "completed" &&
                        sr.CurrentStatus.StatusName != "cancelled"),
                    ScheduleCount = m.Availability.Count(a => a.IsActive),
                    HasScheduleNow = m.Availability.Any(a =>
                        a.IsActive &&
                        a.DayOfWeek == dayOfWeek &&
                        a.StartTime <= currentTime &&
                        a.EndTime >= currentTime),
                    CurrentShopName = m.ShopMechanics
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.Shop.ShopName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            LastError = null;
            var candidates = mechanics
                .Select(m => new ServiceRequestMechanicCandidateDto
                {
                    Id = m.MechanicId,
                    Name = $"{m.FirstName} {m.LastName}",
                    Phone = m.PhoneNumber ?? string.Empty,
                    Status = m.AvailabilityStatus,
                    Rating = m.AverageRating,
                    TotalJobs = m.TotalCompletedJobs,
                    DistanceKm = CalculateDistanceKm(request.ServiceLatitude, request.ServiceLongitude, m.CurrentLatitude, m.CurrentLongitude),
                    IsAvailableNow = IsOnlineStatus(m.AvailabilityStatus) && m.OpenRequestCount == 0 && (m.ScheduleCount == 0 || m.HasScheduleNow),
                    ActiveRequestCount = m.OpenRequestCount,
                    CurrentShopName = m.CurrentShopName ?? string.Empty
                })
                .OrderByDescending(m => m.IsAvailableNow)
                .ThenBy(m => m.DistanceKm ?? double.MaxValue)
                .ThenByDescending(m => m.Rating ?? 0)
                .ThenBy(m => m.Name)
                .ToArray();

            await TrackViewAsync("CheckEmergencyMechanicCandidates", "service_requests", requestId, new
            {
                Found = true,
                Loaded = candidates.Length,
                AvailableNow = candidates.Count(x => x.IsAvailableNow)
            });
            return candidates;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load mechanic candidates.", Array.Empty<ServiceRequestMechanicCandidateDto>());
        }
    }

    public async Task AssignEmergencyMechanicAsync(int requestId, int mechanicId, string? adminNote)
    {
        try
        {
            var serviceRequest = await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.CurrentStatus)
                .FirstOrDefaultAsync(sr => sr.RequestId == requestId)
                ?? throw new InvalidOperationException("Service request not found.");

            var mechanic = await context.Mechanics
                .Include(m => m.User)
                .Include(m => m.ShopMechanics)
                .FirstOrDefaultAsync(m => m.MechanicId == mechanicId)
                ?? throw new InvalidOperationException("Mechanic not found.");

            var adminUserId = await GetSystemAdminUserIdAsync();
            var oldStatusId = serviceRequest.CurrentStatusId;
            var assignedStatus = await context.RequestStatuses
                .FirstOrDefaultAsync(s => s.StatusName == "assigned" || s.StatusName == "in progress");

            serviceRequest.MechanicId = mechanic.MechanicId;
            serviceRequest.ShopId ??= mechanic.ShopMechanics
                .Where(sm => sm.IsActive)
                .Select(sm => (int?)sm.ShopId)
                .FirstOrDefault();
            serviceRequest.AcceptedAt ??= DateTime.UtcNow;

            if (assignedStatus != null)
            {
                serviceRequest.CurrentStatusId = assignedStatus.StatusId;
                context.RequestStatusHistory.Add(new RequestStatusHistory
                {
                    RequestId = serviceRequest.RequestId,
                    OldStatusId = oldStatusId,
                    NewStatusId = assignedStatus.StatusId,
                    ChangedByUserId = adminUserId,
                    Notes = $"Emergency assignment to {mechanic.User.FirstName} {mechanic.User.LastName}.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            mechanic.AvailabilityStatus = "busy";
            mechanic.UpdatedAt = DateTime.UtcNow;

            var conversation = await EnsureRequestConversationAsync(serviceRequest.RequestId, "emergency_service");
            await EnsureParticipantAsync(conversation.ConversationId, serviceRequest.Client.UserId);
            await EnsureParticipantAsync(conversation.ConversationId, mechanic.UserId);
            if (adminUserId.HasValue)
            {
                await EnsureParticipantAsync(conversation.ConversationId, adminUserId.Value);
                context.Messages.Add(new Message
                {
                    ConversationId = conversation.ConversationId,
                    SenderUserId = adminUserId.Value,
                    MessageText = BuildAssignmentMessage(mechanic.User.FirstName, mechanic.User.LastName, mechanic.User.PhoneNumber, adminNote),
                    CreatedAt = DateTime.UtcNow
                });
                conversation.LastMessageAt = DateTime.UtcNow;
            }

            AddAssignmentNotification(serviceRequest.Client.UserId, serviceRequest.RequestId, "Mechanic assigned",
                $"{mechanic.User.FirstName} {mechanic.User.LastName} has been assigned to your emergency request.");
            AddAssignmentNotification(mechanic.UserId, serviceRequest.RequestId, "Emergency request assigned",
                $"You have been assigned to emergency request #{serviceRequest.RequestId}.");

            AddAudit("AssignEmergencyMechanic", "service_requests", serviceRequest.RequestId, new { MechanicId = (int?)null, StatusId = oldStatusId }, new { MechanicId = mechanic.MechanicId, MechanicName = $"{mechanic.User.FirstName} {mechanic.User.LastName}", serviceRequest.CurrentStatusId, adminNote });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to assign mechanic to emergency request.");
        }
    }

    public async Task<RequestMessageDto[]> GetRequestMessagesAsync(int requestId)
    {
        try
        {
            var adminUserIds = await GetSystemAdminUserIdsAsync();
            LastError = null;

            var messages = await context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.Conversation.RequestId == requestId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new RequestMessageDto
                {
                    MessageId = m.MessageId,
                    SenderUserId = m.SenderUserId,
                    SenderName = m.Sender!.FirstName + " " + m.Sender.LastName,
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsAdminSender = adminUserIds.Contains(m.SenderUserId)
                })
                .ToArrayAsync();

            await TrackViewAsync("ViewRequestMessages", "service_requests", requestId, new { Loaded = messages.Length });
            return messages;
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load request messages.", Array.Empty<RequestMessageDto>());
        }
    }

    public async Task SendAdminMessageAsync(int requestId, string messageText)
    {
        try
        {
            var cleanMessage = messageText.Trim();
            if (string.IsNullOrWhiteSpace(cleanMessage))
            {
                throw new InvalidOperationException("Message cannot be empty.");
            }

            var adminUserId = await GetSystemAdminUserIdAsync()
                ?? throw new InvalidOperationException("No system admin user found.");

            var request = await context.ServiceRequests
                .Include(sr => sr.Client)
                .FirstOrDefaultAsync(sr => sr.RequestId == requestId)
                ?? throw new InvalidOperationException("Service request not found.");

            var conversation = await EnsureRequestConversationAsync(requestId, "emergency_service");
            await EnsureParticipantAsync(conversation.ConversationId, adminUserId);
            await EnsureParticipantAsync(conversation.ConversationId, request.Client.UserId);

            if (request.MechanicId.HasValue)
            {
                var mechanicUserId = await context.Mechanics
                    .Where(m => m.MechanicId == request.MechanicId.Value)
                    .Select(m => m.UserId)
                    .FirstOrDefaultAsync();

                if (mechanicUserId > 0)
                {
                    await EnsureParticipantAsync(conversation.ConversationId, mechanicUserId);
                }
            }

            context.Messages.Add(new Message
            {
                ConversationId = conversation.ConversationId,
                SenderUserId = adminUserId,
                MessageText = cleanMessage,
                CreatedAt = DateTime.UtcNow
            });
            conversation.LastMessageAt = DateTime.UtcNow;

            AddAssignmentNotification(request.Client.UserId, requestId, "Admin message", cleanMessage);
            AddAudit("SendAdminMessage", "service_requests", requestId, null, new { MessagePreview = cleanMessage.Length > 160 ? cleanMessage[..160] : cleanMessage });
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to send admin message.");
        }
    }

    public async Task SuspendUserAsync(int userId)
    {
        await UpdateUserStatusAsync(userId, "suspended");
    }

    public async Task<bool> ValidateAdminLoginAsync(string email, string password)
    {
        try
        {
            var normalizedEmail = NormalizeEmail(email);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null) return false;
            if (!string.Equals(user.AccountStatus, "active", StringComparison.OrdinalIgnoreCase)) return false;

            var isSystemAdmin = await context.UserRoles.AnyAsync(ur =>
                ur.UserId == user.UserId &&
                ur.Role != null &&
                ur.Role.RoleName == "SystemAdmin");

            return isSystemAdmin && PasswordMatches(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            LastError = "Unable to validate login.";
            logger.LogWarning(ex, "Admin login validation failed.");
            return false;
        }
    }

    private T Fail<T>(Exception ex, string message, T fallback = default!)
    {
        LastError = message;
        logger.LogError(ex, message);
        return fallback;
    }

    private InvalidOperationException FailForWrite(Exception ex, string message)
    {
        LastError = message;
        logger.LogError(ex, message);
        return new InvalidOperationException(message, ex);
    }

    private async Task TrackViewAsync(string actionName, string entityName, object? entityId = null, object? details = null)
    {
        if (GetCurrentAdminUserId() is null)
        {
            return;
        }

        AddAudit(actionName, entityName, entityId, null, details);
        await context.SaveChangesAsync();
    }

    private void AddAudit(string actionName, string entityName, object? entityId, object? oldValues = null, object? newValues = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = GetCurrentAdminUserId(),
            ActionName = actionName,
            EntityName = entityName,
            EntityId = entityId?.ToString(),
            OldValuesJson = ToAuditJson(oldValues),
            NewValuesJson = ToAuditJson(newValues),
            CreatedAt = DateTime.UtcNow
        });
    }

    private int? GetCurrentAdminUserId()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static string? ToAuditJson(object? values)
    {
        if (values is null) return null;
        if (values is string text) return text;

        return JsonSerializer.Serialize(values, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private async Task<bool> ShopExistsAsync(string shopName, string addressLine, string city, string province, int? excludeShopId = null)
    {
        var normalizedShopName = shopName.Trim().ToLowerInvariant();
        var shops = await context.Shops
            .Where(shop => shop.ShopStatus.ToLower().Trim() != "deleted" &&
                shop.ShopStatus.ToLower().Trim() != "rejected")
            .Where(shop => !excludeShopId.HasValue || shop.ShopId != excludeShopId.Value)
            .Where(shop => shop.ShopName.ToLower() == normalizedShopName)
            .ToArrayAsync();

        return shops.Any(shop =>
            string.Equals(shop.AddressLine?.Trim(), addressLine.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(shop.City?.Trim(), city.Trim(), StringComparison.OrdinalIgnoreCase) &&
             string.Equals(shop.Province?.Trim(), province.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<int> GetRoleIdAsync(string roleName)
    {
        var roleId = await context.Roles
            .Where(role => role.RoleName == roleName)
            .Select(role => role.RoleId)
            .SingleOrDefaultAsync();

        return roleId > 0
            ? roleId
            : throw new InvalidOperationException($"{roleName} role is missing from the database.");
    }

    private async Task<bool> EmailInUseAsync(string normalizedEmail, int? excludeUserId = null)
    {
        return await context.Users.AnyAsync(user =>
            user.Email.ToLower() == normalizedEmail &&
            (!excludeUserId.HasValue || user.UserId != excludeUserId.Value));
    }

    private async Task UpsertMechanicShopAssignmentAsync(Mechanic mechanic, int? shopId, bool assignmentActive)
    {
        foreach (var assignment in mechanic.ShopMechanics)
        {
            assignment.IsActive = false;
        }

        if (!shopId.HasValue)
        {
            return;
        }

        var shopExists = await context.Shops.AnyAsync(shop =>
            shop.ShopId == shopId.Value &&
            shop.ShopStatus.ToLower().Trim() != "deleted");

        if (!shopExists)
        {
            throw new InvalidOperationException("Selected shop does not exist.");
        }

        var existing = mechanic.ShopMechanics.FirstOrDefault(assignment => assignment.ShopId == shopId.Value);
        if (existing is not null)
        {
            existing.IsActive = assignmentActive;
            return;
        }

        mechanic.ShopMechanics.Add(new ShopMechanic
        {
            ShopId = shopId.Value,
            MechanicId = mechanic.MechanicId,
            AssignedAt = DateTime.UtcNow,
            IsActive = assignmentActive
        });
    }

    private static void ValidatePasswordPair(string? password, string? confirmPassword, bool required)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            if (required)
            {
                throw new InvalidOperationException("Password is required.");
            }

            return;
        }

        if (password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters long.");
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password and confirmation password do not match.");
        }
    }

    private async Task<string> CreatePendingOwnerEmailAsync()
    {
        string email;
        do
        {
            email = $"pending-shop-admin-{Guid.NewGuid():N}@bikemates.local";
        }
        while (await context.Users.AnyAsync(user => user.Email == email));

        return email;
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

    private static string NormalizeEmail(string? value)
    {
        var email = Require(value, "Email address").ToLowerInvariant();
        if (!email.Contains('@') || !email.Contains('.'))
        {
            throw new InvalidOperationException("Enter a valid email address.");
        }

        return email;
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

    private static AdminAccountDto ToAdminAccountDto(User user)
    {
        return new AdminAccountDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AccountStatus = user.AccountStatus,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static ShopDto ToShopDto(Shop shop)
    {
        var description = shop.ShopDescription ?? string.Empty;
        return new ShopDto
        {
            Id = shop.ShopId,
            ShopName = shop.ShopName ?? string.Empty,
            Description = CleanShopDescription(description),
            AddressLine = shop.AddressLine ?? string.Empty,
            City = shop.City ?? string.Empty,
            Province = shop.Province ?? string.Empty,
            FullAddress = BuildFullAddress(shop.AddressLine, shop.City, shop.Province),
            ContactNumber = shop.ContactNumber ?? string.Empty,
            OwnerName = shop.Owner is null ? string.Empty : $"{shop.Owner.FirstName} {shop.Owner.LastName}".Trim(),
            OwnerMiddleName = shop.OwnerMiddleName ?? string.Empty,
            OwnerEmail = shop.Owner?.Email ?? string.Empty,
            OwnerPhoneNumber = shop.Owner?.PhoneNumber ?? string.Empty,
            OwnerSex = shop.OwnerSex ?? string.Empty,
            OwnerBirthdate = shop.OwnerBirthdate?.ToString("MMM dd, yyyy") ?? string.Empty,
            OwnerFullAddress = BuildFullAddress(shop.OwnerAddressLine, shop.OwnerBarangay, shop.OwnerCity, shop.OwnerProvince, shop.OwnerZipCode),
            OwnerEmailVerified = shop.Owner?.EmailVerified == true,
            BusinessPermitUrl = shop.BusinessPermitUrl ?? string.Empty,
            ShopImageUrl = shop.ShopImageUrl ?? string.Empty,
            OwnerValidIdUrl = shop.OwnerValidIdUrl ?? string.Empty,
            DtiRegistrationNumber = ExtractDtiRegistration(description),
            Status = shop.ShopStatus ?? "pending",
            CreatedAt = shop.CreatedAt
        };
    }

    private static ShopDetailsDto ToShopDetailsDto(Shop shop)
    {
        var description = shop.ShopDescription ?? string.Empty;
        var ownerName = shop.Owner is null
            ? string.Empty
            : $"{shop.Owner.FirstName} {shop.Owner.LastName}".Trim();

        return new ShopDetailsDto
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName ?? string.Empty,
            Description = CleanShopDescription(description),
            AddressLine = shop.AddressLine ?? string.Empty,
            City = shop.City ?? string.Empty,
            Province = shop.Province ?? string.Empty,
            FullAddress = BuildFullAddress(shop.AddressLine, shop.City, shop.Province),
            ContactNumber = shop.ContactNumber ?? string.Empty,
            Latitude = shop.Latitude,
            Longitude = shop.Longitude,
            BusinessPermitUrl = shop.BusinessPermitUrl ?? string.Empty,
            ShopImageUrl = shop.ShopImageUrl ?? string.Empty,
            ShopLogoUrl = shop.ShopLogoUrl ?? string.Empty,
            OwnerValidIdUrl = shop.OwnerValidIdUrl ?? string.Empty,
            DtiRegistrationNumber = ExtractDtiRegistration(description),
            Status = shop.ShopStatus ?? "pending",
            OwnerName = ownerName,
            OwnerMiddleName = shop.OwnerMiddleName ?? string.Empty,
            OwnerEmail = shop.Owner?.Email ?? string.Empty,
            OwnerPhoneNumber = shop.Owner?.PhoneNumber ?? string.Empty,
            OwnerSex = shop.OwnerSex ?? string.Empty,
            OwnerBirthdate = shop.OwnerBirthdate,
            OwnerFullAddress = BuildFullAddress(shop.OwnerAddressLine, shop.OwnerBarangay, shop.OwnerCity, shop.OwnerProvince, shop.OwnerZipCode),
            OwnerEmailVerified = shop.Owner?.EmailVerified == true,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt,
            OperatingHours = shop.OperatingHours
                .OrderBy(hour => hour.DayOfWeek)
                .Select(hour => new ShopOperatingHourDto
                {
                    DayOfWeek = hour.DayOfWeek,
                    DayName = DayName(hour.DayOfWeek),
                    OpeningTime = FormatTime(hour.OpeningTime),
                    ClosingTime = FormatTime(hour.ClosingTime),
                    IsClosed = hour.IsClosed
                })
                .ToList(),
            Services = shop.Services
                .OrderByDescending(service => service.IsActive)
                .ThenBy(service => service.ServiceName)
                .Select(service => new ShopDetailServiceDto
                {
                    ShopServiceId = service.ShopServiceId,
                    ServiceName = service.ServiceName,
                    CategoryName = service.Category?.CategoryName ?? string.Empty,
                    Description = service.ServiceDescription ?? string.Empty,
                    BasePrice = service.BasePrice,
                    EstimatedMinutes = service.EstimatedMinutes,
                    IsActive = service.IsActive,
                    CreatedAt = service.CreatedAt,
                    ImageUrls = service.Images
                        .OrderBy(image => image.CreatedAt)
                        .Select(image => image.ImageUrl)
                        .ToList()
                })
                .ToList(),
            Products = shop.Products
                .OrderByDescending(product => product.IsActive)
                .ThenBy(product => product.ProductName)
                .Select(product => new ShopDetailProductDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Description = product.ProductDescription ?? string.Empty,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ImageUrls = product.Images
                        .OrderBy(image => image.CreatedAt)
                        .Select(image => image.ImageUrl)
                        .ToList()
                })
                .ToList(),
            Mechanics = shop.ShopMechanics
                .OrderByDescending(assignment => assignment.IsActive)
                .ThenByDescending(assignment => assignment.AssignedAt)
                .Select(assignment => new ShopMechanicDto
                {
                    MechanicId = assignment.MechanicId,
                    Name = assignment.Mechanic?.User is null
                        ? "Mechanic account"
                        : $"{assignment.Mechanic.User.FirstName} {assignment.Mechanic.User.LastName}".Trim(),
                    Status = assignment.Mechanic?.AvailabilityStatus ?? "offline",
                    Rating = assignment.Mechanic?.AverageRating ?? 0,
                    IsActive = assignment.IsActive,
                    AssignedAt = assignment.AssignedAt
                })
                .ToList()
        };
    }

    private static CustomerApplicationDto ToCustomerApplicationDto(Client client)
    {
        var user = client.User;
        var address = client.Addresses
            .OrderByDescending(item => item.IsDefault)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        return new CustomerApplicationDto
        {
            ClientId = client.ClientId,
            UserId = client.UserId,
            FirstName = user?.FirstName ?? string.Empty,
            MiddleName = client.MiddleName ?? string.Empty,
            LastName = user?.LastName ?? string.Empty,
            FullName = user is null ? "Customer account" : $"{user.FirstName} {user.LastName}".Trim(),
            Email = user?.Email ?? string.Empty,
            PhoneNumber = user?.PhoneNumber ?? string.Empty,
            AccountStatus = user?.AccountStatus ?? "pending",
            EmailVerified = user?.EmailVerified == true,
            Sex = client.Sex ?? string.Empty,
            Birthdate = client.Birthdate,
            ProfileImageUrl = user?.ProfileImageUrl ?? string.Empty,
            ValidIdImageUrl = client.ValidIdImageUrl ?? string.Empty,
            AddressLine = address?.AddressLine ?? string.Empty,
            Barangay = address?.Barangay ?? string.Empty,
            City = address?.City ?? string.Empty,
            Province = address?.Province ?? string.Empty,
            ZipCode = address?.PostalCode ?? string.Empty,
            CreatedAt = client.CreatedAt,
            UpdatedAt = user?.UpdatedAt
        };
    }

    private static MechanicApplicationDto ToMechanicApplicationDto(Mechanic mechanic)
    {
        var user = mechanic.User;
        var assignment = mechanic.ShopMechanics
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.AssignedAt)
            .FirstOrDefault();

        return new MechanicApplicationDto
        {
            MechanicId = mechanic.MechanicId,
            UserId = mechanic.UserId,
            FirstName = user?.FirstName ?? string.Empty,
            MiddleName = mechanic.MiddleName ?? string.Empty,
            LastName = user?.LastName ?? string.Empty,
            FullName = user is null ? "Mechanic account" : $"{user.FirstName} {user.LastName}".Trim(),
            Email = user?.Email ?? string.Empty,
            PhoneNumber = user?.PhoneNumber ?? string.Empty,
            AccountStatus = user?.AccountStatus ?? "pending",
            EmailVerified = user?.EmailVerified == true,
            IsVerified = mechanic.IsVerified,
            AvailabilityStatus = mechanic.AvailabilityStatus,
            Sex = mechanic.Sex ?? string.Empty,
            Birthdate = mechanic.Birthdate,
            AddressLine = mechanic.AddressLine ?? string.Empty,
            Barangay = mechanic.Barangay ?? string.Empty,
            City = mechanic.City ?? string.Empty,
            Province = mechanic.Province ?? string.Empty,
            ZipCode = mechanic.ZipCode ?? string.Empty,
            ProfileImageUrl = user?.ProfileImageUrl ?? string.Empty,
            ValidIdImageUrl = mechanic.ValidIdImageUrl ?? string.Empty,
            CertificationImageUrl = mechanic.CertificationImageUrl ?? string.Empty,
            Bio = mechanic.Bio ?? string.Empty,
            YearsExperience = mechanic.YearsExperience,
            ShopId = assignment?.ShopId,
            ShopName = assignment?.Shop?.ShopName ?? string.Empty,
            IsAssignedToShop = assignment?.IsActive == true,
            CreatedAt = mechanic.CreatedAt,
            UpdatedAt = mechanic.UpdatedAt ?? user?.UpdatedAt
        };
    }

    private static MechanicEditDto ToMechanicEditDto(Mechanic mechanic)
    {
        var user = mechanic.User;
        var assignment = mechanic.ShopMechanics
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.AssignedAt)
            .FirstOrDefault();

        return new MechanicEditDto
        {
            MechanicId = mechanic.MechanicId,
            FirstName = user?.FirstName ?? string.Empty,
            MiddleName = mechanic.MiddleName ?? string.Empty,
            LastName = user?.LastName ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            PhoneNumber = user?.PhoneNumber,
            AccountStatus = user?.AccountStatus ?? "pending",
            EmailVerified = user?.EmailVerified == true,
            IsVerified = mechanic.IsVerified,
            AvailabilityStatus = mechanic.AvailabilityStatus,
            Sex = mechanic.Sex ?? string.Empty,
            Birthdate = mechanic.Birthdate,
            AddressLine = mechanic.AddressLine ?? string.Empty,
            Barangay = mechanic.Barangay ?? string.Empty,
            City = mechanic.City ?? string.Empty,
            Province = mechanic.Province ?? string.Empty,
            ZipCode = mechanic.ZipCode ?? string.Empty,
            ProfileImageUrl = user?.ProfileImageUrl ?? string.Empty,
            ValidIdImageUrl = mechanic.ValidIdImageUrl ?? string.Empty,
            CertificationImageUrl = mechanic.CertificationImageUrl ?? string.Empty,
            Bio = mechanic.Bio ?? string.Empty,
            YearsExperience = mechanic.YearsExperience,
            ShopId = assignment?.IsActive == true ? assignment.ShopId : null,
            AssignmentActive = assignment?.IsActive == true
        };
    }

    private static string BuildFullAddress(params string?[] parts)
    {
        var address = string.Join(", ", parts
            .Select(CleanOptional)
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(address) ? string.Empty : address;
    }

    private static string DayName(int dayOfWeek)
    {
        return dayOfWeek >= 0 && dayOfWeek <= 6
            ? CultureInfo.InvariantCulture.DateTimeFormat.GetDayName((DayOfWeek)dayOfWeek)
            : $"Day {dayOfWeek}";
    }

    private static string FormatTime(TimeSpan value)
    {
        return DateTime.Today.Add(value).ToString("h:mm tt", CultureInfo.InvariantCulture);
    }

    private static string CleanShopDescription(string description)
    {
        var lines = description
            .Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("DTI Registration:", StringComparison.OrdinalIgnoreCase));

        var value = string.Join(Environment.NewLine, lines);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    private static string ExtractDtiRegistration(string description)
    {
        var line = description
            .Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(item => item.StartsWith("DTI Registration:", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(line)
            ? string.Empty
            : line["DTI Registration:".Length..].Trim();
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? (parts[0], "Owner")
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private async Task<Conversation> EnsureRequestConversationAsync(int requestId, string conversationType)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c => c.RequestId == requestId);

        if (conversation != null)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            RequestId = requestId,
            ConversationType = conversationType,
            CreatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return conversation;
    }

    private async Task EnsureParticipantAsync(int conversationId, int userId)
    {
        var exists = await context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!exists)
        {
            context.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<int?> GetSystemAdminUserIdAsync()
    {
        return await context.UserRoles
            .Where(ur => ur.Role != null && (ur.Role.RoleName == "SystemAdmin" || ur.Role.RoleName == "admin" || ur.Role.RoleName == "system_admin"))
            .OrderBy(ur => ur.UserId)
            .Select(ur => (int?)ur.UserId)
            .FirstOrDefaultAsync();
    }

    private async Task<int[]> GetSystemAdminUserIdsAsync()
    {
        return await context.UserRoles
            .Where(ur => ur.Role != null && (ur.Role.RoleName == "SystemAdmin" || ur.Role.RoleName == "admin" || ur.Role.RoleName == "system_admin"))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToArrayAsync();
    }

    private void AddAssignmentNotification(int userId, int requestId, string title, string message)
    {
        context.Notifications.Add(new Notification
        {
            UserId = userId,
            NotificationType = "service_request",
            Title = title,
            Message = message,
            DataJson = JsonSerializer.Serialize(new { requestId }),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildAssignmentMessage(string firstName, string lastName, string? phone, string? adminNote)
    {
        var message = $"Emergency mechanic assigned: {firstName} {lastName}";
        if (!string.IsNullOrWhiteSpace(phone))
        {
            message += $" ({phone})";
        }

        if (!string.IsNullOrWhiteSpace(adminNote))
        {
            message += $". Admin note: {adminNote.Trim()}";
        }

        return message;
    }

    private static bool IsOnlineStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() is "online" or "available";
    }

    private static double? CalculateDistanceKm(decimal? fromLatitude, decimal? fromLongitude, decimal? toLatitude, decimal? toLongitude)
    {
        if (!fromLatitude.HasValue || !fromLongitude.HasValue || !toLatitude.HasValue || !toLongitude.HasValue)
        {
            return null;
        }

        const double earthRadiusKm = 6371;
        var fromLat = DegreesToRadians((double)fromLatitude.Value);
        var toLat = DegreesToRadians((double)toLatitude.Value);
        var latDelta = DegreesToRadians((double)(toLatitude.Value - fromLatitude.Value));
        var lonDelta = DegreesToRadians((double)(toLongitude.Value - fromLongitude.Value));

        var a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
            Math.Cos(fromLat) * Math.Cos(toLat) *
            Math.Sin(lonDelta / 2) * Math.Sin(lonDelta / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private static string NormalizeAccountStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized is "active" or "pending" or "suspended"
            ? normalized
            : throw new InvalidOperationException("Invalid account status.");
    }

    private static string NormalizeShopStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "pending" => "pending",
            "verified" => "verified",
            "active" => "verified",
            "suspended" => "suspended",
            "rejected" => "rejected",
            "deleted" => "deleted",
            _ => throw new InvalidOperationException("Invalid shop status.")
        };
    }

    private static string NormalizeMechanicStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "online" => "online",
            "available" => "online",
            "offline" => "offline",
            "busy" => "busy",
            "on_route" => "busy",
            "on route" => "busy",
            _ => "offline"
        };
    }

    private static void EnsureSystemAdminUser(User user)
    {
        if (!user.UserRoles.Any(role =>
            string.Equals(role.Role?.RoleName, "SystemAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("This account is not a system admin.");
        }
    }

    private static void ScrubDeletedShopDetails(Shop shop)
    {
        shop.ShopName = $"Deleted shop #{shop.ShopId}";
        shop.ShopDescription = null;
        shop.AddressLine = null;
        shop.City = null;
        shop.Province = null;
        shop.Latitude = null;
        shop.Longitude = null;
        shop.BusinessPermitUrl = null;
        shop.ShopImageUrl = null;
        shop.OwnerValidIdUrl = null;
        shop.OwnerMiddleName = null;
        shop.OwnerSex = null;
        shop.OwnerBirthdate = null;
        shop.OwnerAddressLine = null;
        shop.OwnerBarangay = null;
        shop.OwnerCity = null;
        shop.OwnerProvince = null;
        shop.OwnerZipCode = null;
        shop.ContactNumber = null;
    }

    private static void AnonymizeDeletedUser(User user)
    {
        var now = DateTime.UtcNow;
        var marker = $"{user.UserId}-{Guid.NewGuid():N}";

        user.FirstName = "Deleted";
        user.LastName = "Account";
        user.Email = $"deleted-{marker}@deleted.bikemate.invalid";
        user.PhoneNumber = null;
        user.PasswordHash = null;
        user.ProfileImageUrl = null;
        user.EmailVerified = false;
        user.PhoneVerified = false;
        user.AccountStatus = "deleted";
        user.UpdatedAt = now;

        foreach (var provider in user.AuthProviders)
        {
            provider.ProviderSubject = null;
            provider.ProviderEmail = null;
        }

        foreach (var token in user.DeviceTokens)
        {
            token.IsActive = false;
            token.UpdatedAt = now;
        }
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
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}
