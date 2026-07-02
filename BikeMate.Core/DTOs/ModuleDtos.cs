namespace BikeMate.Core.DTOs;

public sealed record GoogleLoginRequestDto(string IdToken, string? Role);
public sealed record VerifyOtpRequestDto(string Email, string OtpCode, string Purpose);
public sealed record ResendOtpRequestDto(string Email, string Purpose);
public sealed record ForgotPasswordRequestDto(string Email);
public sealed record VerifyPasswordResetOtpRequestDto(string Email, string OtpCode);
public sealed record ResendPasswordResetOtpRequestDto(string Email);
public sealed record ResetPasswordRequestDto(string Email, string Token, string NewPassword, string ConfirmPassword);

public sealed record CustomerAddressDto(
    int AddressId,
    string? Label,
    string AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsDefault);

public sealed record UpsertCustomerAddressDto(
    string? Label,
    string AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsDefault);

public sealed record UpsertCustomerProfileDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? MiddleName = null,
    string? Sex = null,
    DateTime? Birthdate = null);

public sealed record MotorcycleDto(
    int MotorcycleId,
    string Brand,
    string Model,
    int? YearModel,
    string? PlateNumber,
    string? EngineType,
    string? Color,
    string? MotorcycleImageUrl);

public sealed record UpsertMotorcycleDto(
    string Brand,
    string Model,
    int? YearModel,
    string? PlateNumber,
    string? EngineType,
    string? Color,
    string? MotorcycleImageUrl);

public sealed record ServiceCategoryDto(int CategoryId, string CategoryName, string? Description);

public sealed record UpsertServiceCategoryDto(string CategoryName, string? Description);

public sealed record ShopSummaryDto(
    int ShopId,
    string ShopName,
    string? AddressLine,
    string? City,
    string? ContactNumber,
    string ShopStatus,
    decimal? Latitude,
    decimal? Longitude,
    string? ShopImageUrl = null,
    string? ShopLogoUrl = null,
    int ActiveServiceCount = 0,
    decimal? StartingPrice = null);

public sealed record ShopDetailsDto(
    int ShopId,
    string ShopName,
    string? ShopDescription,
    string? AddressLine,
    string? City,
    string? Province,
    string? ContactNumber,
    string ShopStatus,
    decimal? Latitude,
    decimal? Longitude,
    string? ShopImageUrl = null,
    string? ShopLogoUrl = null);

public sealed record ShopApplicationDetailsDto(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string? ShopDescription,
    string? ShopAddressLine,
    string? ShopBarangay,
    string? ShopCity,
    string? ShopProvince,
    string? ShopZipCode,
    string? ContactNumber,
    string? BusinessPermitUrl,
    string? ShopImageUrl,
    string? OwnerValidIdUrl,
    string? DtiRegistrationNumber,
    string? OwnerFirstName,
    string? OwnerMiddleName,
    string? OwnerLastName,
    string? OwnerEmail,
    string? OwnerPhoneNumber,
    string? OwnerSex,
    DateTime? OwnerBirthdate,
    string? OwnerAddressLine,
    string? OwnerBarangay,
    string? OwnerCity,
    string? OwnerProvince,
    string? OwnerZipCode,
    bool OwnerEmailVerified,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record UpsertShopDto(
    string ShopName,
    string? ShopDescription,
    string? AddressLine,
    string? City,
    string? Province,
    decimal? Latitude,
    decimal? Longitude,
    string? ContactNumber);

public sealed record ShopServiceDto(
    int ShopServiceId,
    int ShopId,
    int CategoryId,
    string CategoryName,
    string ServiceName,
    string? ServiceDescription,
    decimal BasePrice,
    int EstimatedMinutes,
    bool IsActive);

public sealed record UpsertShopServiceDto(
    int CategoryId,
    string ServiceName,
    string? ServiceDescription,
    decimal BasePrice,
    int EstimatedMinutes,
    bool IsActive);

public sealed record ProductDto(
    int ProductId,
    int ShopId,
    string ProductName,
    string? ProductDescription,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    string? ProductImageUrl);

public sealed record UpsertProductDto(
    string ProductName,
    string? ProductDescription,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    string? ProductImageUrl);

public sealed record CreateServiceRequestDto(
    int? ShopId,
    int? ShopServiceId,
    int? MotorcycleId,
    string IssueDescription,
    string? ServiceLocationAddress,
    decimal? ServiceLatitude,
    decimal? ServiceLongitude,
    DateTime? ScheduledAt);

public sealed record ServiceRequestDto(
    int RequestId,
    string CurrentStatus,
    string CustomerName,
    string? MechanicName,
    string? ShopName,
    string? ServiceName,
    string IssueDescription,
    string? ServiceLocationAddress,
    DateTime? ScheduledAt,
    decimal EstimatedTotal,
    decimal FinalTotal,
    DateTime CreatedAt,
    decimal? ServiceLatitude = null,
    decimal? ServiceLongitude = null,
    decimal? DistanceKm = null);

public sealed record UpdateRequestStatusDto(string Status, string? Notes);
public sealed record AssignMechanicDto(int MechanicId);
public sealed record UploadMediaDto(string MediaUrl, string MediaType, string? Caption);
public sealed record UploadedFileDto(string Url, string FileName, string ContentType, long SizeBytes);
public sealed record SelectShopDto(int ShopId, int? ShopServiceId, int? ProductId = null);

public sealed record CustomerMeDto(
    int ClientId,
    int UserId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfileImageUrl,
    bool EmailVerified,
    bool PhoneVerified,
    string AccountStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? Sex,
    DateTime? Birthdate,
    string? ValidIdImageUrl,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<MotorcycleDto> Motorcycles);

public sealed record CustomerApplicationDto(
    int ClientId,
    int UserId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string AccountStatus,
    bool EmailVerified,
    string? Sex,
    DateTime? Birthdate,
    string? ProfileImageUrl,
    string? ValidIdImageUrl,
    string? AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? ZipCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record MechanicProfileDto(
    int MechanicId,
    string FullName,
    string? ProfileImageUrl,
    string? Bio,
    int? YearsExperience,
    bool IsVerified,
    string AvailabilityStatus,
    decimal AverageRating,
    int TotalCompletedJobs);

public sealed record MechanicApplicationDto(
    int MechanicId,
    int UserId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string AccountStatus,
    bool EmailVerified,
    bool IsVerified,
    string AvailabilityStatus,
    string? Sex,
    DateTime? Birthdate,
    string? AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? ZipCode,
    string? ProfileImageUrl,
    string? ValidIdImageUrl,
    string? CertificationImageUrl,
    string? Bio,
    int? YearsExperience,
    int? ShopId,
    string? ShopName,
    bool IsAssignedToShop,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateMechanicApplicationDto(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Sex,
    string? Birthdate,
    string Email,
    string PhoneNumber,
    string Password,
    string? AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? ZipCode,
    string ValidIdImageUrl,
    string CertificationImageUrl,
    string? ProfileImageUrl,
    string? Bio,
    int? YearsExperience);

public sealed record UpdateMechanicProfileDto(
    string? Bio,
    int? YearsExperience,
    string AvailabilityStatus,
    decimal? CurrentLatitude,
    decimal? CurrentLongitude);

public sealed record LocationUpdateDto(int? RequestId, int? MechanicId, decimal Latitude, decimal Longitude, decimal? AccuracyMeters);
public sealed record LiveLocationDto(int LiveLocationId, int? RequestId, int? MechanicId, decimal Latitude, decimal Longitude, DateTime CreatedAt);
public sealed record MapPointDto(decimal Latitude, decimal Longitude, string Address, string? PlaceId);
public sealed record MapPlaceSuggestionDto(string PlaceId, string Description);
public sealed record MapDirectionsDto(string DistanceText, string DurationText, int? DistanceMeters, int? DurationSeconds, string? EncodedPolyline);

public sealed record PhilippineRegionDto(string Code, string Name);
public sealed record PhilippineLocalityDto(string Code, string Name, string Type, string RegionCode, string? Province);
public sealed record PhilippineBarangayDto(string Code, string Name, string LocalityCode);
public sealed record PhilippineLocationMatchDto(PhilippineRegionDto Region, PhilippineLocalityDto Locality);

public sealed record ShopReputationDto(
    int ShopId,
    decimal AverageRating,
    int ReviewCount,
    int CompletedJobs,
    IReadOnlyList<ShopTechnicianSummaryDto> TopTechnicians,
    IReadOnlyList<ShopCustomerReviewDto> RecentReviews);

public sealed record ShopTechnicianSummaryDto(
    int MechanicId,
    string FullName,
    string? ProfileImageUrl,
    decimal AverageRating,
    int ReviewCount,
    int CompletedJobs,
    int? YearsExperience,
    string AvailabilityStatus,
    bool IsVerified);

public sealed record ShopCustomerReviewDto(
    int ReviewId,
    int Rating,
    string? Comment,
    string CustomerName,
    string TechnicianName,
    string? ServiceName,
    DateTime CreatedAt);

public sealed record CreateEmergencyRequestDto(
    decimal Latitude,
    decimal Longitude,
    string ServiceLocation,
    string? Notes,
    string? EmergencyLevel);

public sealed record NearbyResponderDto(
    int MechanicId,
    string FullName,
    string? ProfileImageUrl,
    decimal? Latitude,
    decimal? Longitude,
    decimal? DistanceKm,
    decimal AverageRating,
    string AvailabilityStatus);

public sealed record EmergencyRequestStatusDto(
    int RequestId,
    string Status,
    string Message,
    int? AssignedMechanicId,
    string? AssignedMechanicName,
    string? AssignedMechanicPhone,
    decimal CustomerLatitude,
    decimal CustomerLongitude,
    string ServiceLocation,
    decimal? MechanicLatitude,
    decimal? MechanicLongitude,
    IReadOnlyCollection<NearbyResponderDto> NearbyResponders,
    DateTime CreatedAt);

public sealed record EmergencyCallSessionDto(
    int RequestId,
    string CallStatus,
    DateTime StartedAt,
    DateTime? EndedAt,
    string Message,
    string? AppId = null,
    string? ChannelName = null,
    uint? Uid = null,
    string? Token = null,
    DateTime? ExpiresAt = null);

public sealed record ConversationDto(int ConversationId, int? RequestId, string ConversationType, DateTime? LastMessageAt);
public sealed record ConversationSummaryDto(
    int ConversationId,
    int? RequestId,
    string ConversationType,
    DateTime? LastMessageAt,
    string Title,
    string? Subtitle,
    int? OtherUserId,
    string? OtherProfileImageUrl,
    string? LastMessageText,
    int UnreadCount = 0,
    string? BookingStatus = null,
    DateTime? ScheduledAt = null);
public sealed record StartConversationDto(int? RequestId, IReadOnlyCollection<int> ParticipantUserIds);
public sealed record MessageDto(int MessageId, int ConversationId, int SenderUserId, string MessageText, string? AttachmentUrl, DateTime CreatedAt, DateTime? ReadAt);
public sealed record SendMessageDto(string MessageText, string? AttachmentUrl);

public sealed record CreateCheckoutSessionDto(int RequestId, decimal? Amount);
public sealed record PaymentDto(
    int PaymentId,
    int RequestId,
    string Status,
    decimal Amount,
    string Currency,
    string ProviderName,
    string? CheckoutUrl,
    string? ReferenceNumber,
    DateTime CreatedAt,
    DateTime? PaidAt);

public sealed record CreateReviewDto(int RequestId, int Rating, string? Comment);
public sealed record ReviewDto(int ReviewId, int RequestId, int MechanicId, int Rating, string? Comment, DateTime CreatedAt);

public sealed record RegisterDeviceTokenDto(string DeviceToken, string Platform);

public sealed record NotificationDto(
    int NotificationId,
    int UserId,
    string? NotificationType,
    string Title,
    string Message,
    string? DataJson,
    bool IsRead,
    DateTime CreatedAt);

public sealed record AdminDashboardDto(
    int TotalUsers,
    int TotalCustomers,
    int TotalMechanics,
    int TotalShops,
    int PendingRequests,
    int CompletedRequests,
    decimal Revenue,
    int PendingVerifications);

public sealed record UpdateUserStatusDto(string AccountStatus);
public sealed record VerificationDecisionDto(string? Notes);
public sealed record AdminAnnouncementDto(string Title, string Message);

public sealed record RevenueReportDto(DateTime From, DateTime To, decimal Revenue, int PaidPayments);
public sealed record TopServiceDto(string ServiceName, int RequestCount);
public sealed record TopMechanicDto(string MechanicName, decimal AverageRating, int CompletedJobs);
