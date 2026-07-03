namespace BikeMate.WebAdmin.DTOs;

public class AdminDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalMechanics { get; set; }
    public int TotalShops { get; set; }
    public int PendingServiceRequests { get; set; }
    public int OnlineMechanics { get; set; }
    public int VerifiedShops { get; set; }
    public List<DailyStatsDto> WeeklyRegistrations { get; set; } = new();
    public List<ActiveRequestMiniDto> RecentActiveRequests { get; set; } = new();
}

public class DailyStatsDto
{
    public string DayName { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class ActiveRequestMiniDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}

public class AdminLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminAccountDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminAccountCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminAccountEditDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = "active";
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string[] Roles { get; set; } = [];
}

public class UserCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "pending";
    public bool EmailVerified { get; set; }
}

public class MechanicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
}

public class CustomerApplicationDto
{
    public int ClientId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Sex { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string ValidIdImageUrl { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MechanicApplicationDto
{
    public int MechanicId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool IsVerified { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string ValidIdImageUrl { get; set; } = string.Empty;
    public string CertificationImageUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int? YearsExperience { get; set; }
    public int? ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public bool IsAssignedToShop { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ShopDto
{
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerMiddleName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPhoneNumber { get; set; } = string.Empty;
    public string OwnerSex { get; set; } = string.Empty;
    public string OwnerBirthdate { get; set; } = string.Empty;
    public string OwnerFullAddress { get; set; } = string.Empty;
    public bool OwnerEmailVerified { get; set; }
    public string BusinessPermitUrl { get; set; } = string.Empty;
    public string ShopImageUrl { get; set; } = string.Empty;
    public string OwnerValidIdUrl { get; set; } = string.Empty;
    public string DtiRegistrationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ShopRegistrationInputDto
{
    public string ShopName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? ShopDescription { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? BusinessPermitUrl { get; set; }
    public string? ShopImageUrl { get; set; }
    public string? DtiRegistrationNumber { get; set; }
    public bool VerifyOnCreate { get; set; }
}

public class ShopEditDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopDescription { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ContactNumber { get; set; }
    public string? BusinessPermitUrl { get; set; }
    public string? ShopImageUrl { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string? OwnerValidIdUrl { get; set; }
    public string? DtiRegistrationNumber { get; set; }
    public string ShopStatus { get; set; } = "pending";
}

public class ShopOptionDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ShopApprovalResultDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopStatus { get; set; } = string.Empty;
    public bool OwnerActivated { get; set; }
}

public class ShopDetailsDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string BusinessPermitUrl { get; set; } = string.Empty;
    public string ShopImageUrl { get; set; } = string.Empty;
    public string ShopLogoUrl { get; set; } = string.Empty;
    public string OwnerValidIdUrl { get; set; } = string.Empty;
    public string DtiRegistrationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerMiddleName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPhoneNumber { get; set; } = string.Empty;
    public string OwnerSex { get; set; } = string.Empty;
    public DateTime? OwnerBirthdate { get; set; }
    public string OwnerFullAddress { get; set; } = string.Empty;
    public bool OwnerEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ShopOperatingHourDto> OperatingHours { get; set; } = new();
    public List<ShopDetailServiceDto> Services { get; set; } = new();
    public List<ShopDetailProductDto> Products { get; set; } = new();
    public List<ShopMechanicDto> Mechanics { get; set; } = new();
}

public class ShopOperatingHourDto
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
}

public class ShopDetailServiceDto
{
    public int ShopServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class ShopDetailProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class ShopMechanicDto
{
    public int MechanicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class ServiceRequestDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class ServiceRequestDetailsDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int? MechanicId { get; set; }
    public string MechanicName { get; set; } = "Pending Assignment";
    public string MechanicPhone { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Pending";
}

public class PaymentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaymentDetailsDto
{
    public int PaymentId { get; set; }
    public int RequestId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = "Not Selected";
    public string ProviderName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class MechanicProfileDto
{
    public int MechanicId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalJobs { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int YearsExperience { get; set; }
    public string? CurrentShopName { get; set; }
    public List<MechanicServiceHistoryDto> ServiceHistory { get; set; } = new();
    public List<MechanicReviewDto> Reviews { get; set; } = new();
}

public class MechanicServiceHistoryDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class MechanicReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class UserEditDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
}

public class MechanicEditDto
{
    public int MechanicId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string AccountStatus { get; set; } = "pending";
    public bool EmailVerified { get; set; }
    public bool IsVerified { get; set; }
    public string AvailabilityStatus { get; set; } = "offline";
    public string Sex { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string ValidIdImageUrl { get; set; } = string.Empty;
    public string CertificationImageUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int? YearsExperience { get; set; }
    public int? ShopId { get; set; }
    public bool AssignmentActive { get; set; } = true;
}
