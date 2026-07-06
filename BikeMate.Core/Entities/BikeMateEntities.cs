namespace BikeMate.Core.Entities;

public sealed class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<UserRole> UserRoles { get; set; } = [];
}

public sealed class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PasswordHash { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public string AccountStatus { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Client? Client { get; set; }
    public Mechanic? Mechanic { get; set; }
    public List<UserRole> UserRoles { get; set; } = [];
    public List<UserAuthProvider> AuthProviders { get; set; } = [];
    public List<OtpVerification> OtpVerifications { get; set; } = [];
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public List<UserDeviceToken> DeviceTokens { get; set; } = [];
    public List<Shop> OwnedShops { get; set; } = [];
}

public sealed class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}

public sealed class UserAuthProvider
{
    public int AuthProviderId { get; set; }
    public int UserId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderSubject { get; set; }
    public string? ProviderEmail { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}

public sealed class OtpVerification
{
    public int OtpId { get; set; }
    public int UserId { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = "email_verification";
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}

public sealed class PasswordResetToken
{
    public int TokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}

public sealed class UserDeviceToken
{
    public int DeviceTokenId { get; set; }
    public int UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
}

public sealed class Client
{
    public int ClientId { get; set; }
    public int UserId { get; set; }
    public string? MiddleName { get; set; }
    public string? Sex { get; set; }
    public DateTime? Birthdate { get; set; }
    public string? ValidIdImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public List<ClientAddress> Addresses { get; set; } = [];
    public List<Motorcycle> Motorcycles { get; set; } = [];
    public List<ServiceRequest> ServiceRequests { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public sealed class ClientAddress
{
    public int AddressId { get; set; }
    public int ClientId { get; set; }
    public string? Label { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }

    public Client? Client { get; set; }
}

public sealed class Motorcycle
{
    public int MotorcycleId { get; set; }
    public int ClientId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? YearModel { get; set; }
    public string? PlateNumber { get; set; }
    public string? EngineType { get; set; }
    public string? Color { get; set; }
    public string? MotorcycleImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public Client? Client { get; set; }
}

public sealed class Mechanic
{
    public int MechanicId { get; set; }
    public int UserId { get; set; }
    public string? MiddleName { get; set; }
    public string? Sex { get; set; }
    public DateTime? Birthdate { get; set; }
    public string? ValidIdImageUrl { get; set; }
    public string? AddressLine { get; set; }
    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ZipCode { get; set; }
    public string? Bio { get; set; }
    public int? YearsExperience { get; set; }
    public string? CertificationImageUrl { get; set; }
    public bool IsVerified { get; set; }
    public string AvailabilityStatus { get; set; } = "offline";
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalCompletedJobs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public List<MechanicAvailability> Availability { get; set; } = [];
    public List<ShopMechanic> ShopMechanics { get; set; } = [];
    public List<ServiceRequest> AssignedRequests { get; set; } = [];
    public List<LiveLocation> LiveLocations { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}

public sealed class MechanicAvailability
{
    public int AvailabilityId { get; set; }
    public int MechanicId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public Mechanic? Mechanic { get; set; }
}

public sealed class Shop
{
    public int ShopId { get; set; }
    public int OwnerUserId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopDescription { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BusinessPermitUrl { get; set; }
    public string? ShopImageUrl { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string? OwnerValidIdUrl { get; set; }
    public string? OwnerMiddleName { get; set; }
    public string? OwnerSex { get; set; }
    public DateTime? OwnerBirthdate { get; set; }
    public string? OwnerAddressLine { get; set; }
    public string? OwnerBarangay { get; set; }
    public string? OwnerCity { get; set; }
    public string? OwnerProvince { get; set; }
    public string? OwnerZipCode { get; set; }
    public string? ContactNumber { get; set; }
    public string ShopStatus { get; set; } = "pending";
    public bool AllowsReservations { get; set; } = true;
    public bool AllowsPickup { get; set; } = true;
    public bool AllowsOnsiteRepair { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? Owner { get; set; }
    public List<ShopOperatingHour> OperatingHours { get; set; } = [];
    public List<ShopService> Services { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<ShopMechanic> ShopMechanics { get; set; } = [];
    public List<ServiceRequest> ServiceRequests { get; set; } = [];
}

public sealed class ShopOperatingHour
{
    public int OperatingHourId { get; set; }
    public int ShopId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    public bool IsClosed { get; set; }

    public Shop? Shop { get; set; }
}

public sealed class ServiceCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ShopService> ShopServices { get; set; } = [];
}

public sealed class ShopService
{
    public int ShopServiceId { get; set; }
    public int ShopId { get; set; }
    public int CategoryId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public decimal BasePrice { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Shop? Shop { get; set; }
    public ServiceCategory? Category { get; set; }
    public List<ServiceImage> Images { get; set; } = [];
    public List<ServiceRequest> ServiceRequests { get; set; } = [];
}

public sealed class ServiceImage
{
    public int ServiceImageId { get; set; }
    public int ShopServiceId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ShopService? ShopService { get; set; }
}

public sealed class Product
{
    public int ProductId { get; set; }
    public int ShopId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Shop? Shop { get; set; }
    public List<ProductImage> Images { get; set; } = [];
    public List<ServiceRequestLineItem> RequestLineItems { get; set; } = [];
}

public sealed class ProductImage
{
    public int ProductImageId { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Product? Product { get; set; }
}

public sealed class ShopMechanic
{
    public int ShopId { get; set; }
    public int MechanicId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Shop? Shop { get; set; }
    public Mechanic? Mechanic { get; set; }
}

public sealed class RequestStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<ServiceRequest> ServiceRequests { get; set; } = [];
}

public sealed class ServiceRequest
{
    public int RequestId { get; set; }
    public int ClientId { get; set; }
    public int? ShopId { get; set; }
    public int? ShopServiceId { get; set; }
    public int? MechanicId { get; set; }
    public int CurrentStatusId { get; set; }
    public int? MotorcycleId { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public string? ServiceLocationAddress { get; set; }
    public decimal? ServiceLatitude { get; set; }
    public decimal? ServiceLongitude { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal EstimatedTotal { get; set; }
    public decimal FinalTotal { get; set; }

    public Client? Client { get; set; }
    public Shop? Shop { get; set; }
    public ShopService? ShopService { get; set; }
    public Mechanic? Mechanic { get; set; }
    public RequestStatus? CurrentStatus { get; set; }
    public Motorcycle? Motorcycle { get; set; }
    public List<RequestStatusHistory> StatusHistory { get; set; } = [];
    public List<RequestMedia> Media { get; set; } = [];
    public List<LiveLocation> LiveLocations { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<ServiceRequestLineItem> LineItems { get; set; } = [];
    public Review? Review { get; set; }
}

public sealed class ServiceRequestLineItem
{
    public int LineItemId { get; set; }
    public int RequestId { get; set; }
    public string ItemType { get; set; } = "service";
    public int? ShopServiceId { get; set; }
    public int? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public ShopService? ShopService { get; set; }
    public Product? Product { get; set; }
}

public sealed class RequestStatusHistory
{
    public int StatusHistoryId { get; set; }
    public int RequestId { get; set; }
    public int? OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public RequestStatus? OldStatus { get; set; }
    public RequestStatus? NewStatus { get; set; }
    public User? ChangedByUser { get; set; }
}

public sealed class RequestMedia
{
    public int RequestMediaId { get; set; }
    public int RequestId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
}

public sealed class LiveLocation
{
    public int LiveLocationId { get; set; }
    public int? RequestId { get; set; }
    public int? MechanicId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public Mechanic? Mechanic { get; set; }
}

public sealed class Conversation
{
    public int ConversationId { get; set; }
    public int? RequestId { get; set; }
    public string ConversationType { get; set; } = "service_request";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public List<ConversationParticipant> Participants { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}

public sealed class ConversationParticipant
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }

    public Conversation? Conversation { get; set; }
    public User? User { get; set; }
}

public sealed class Message
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public Conversation? Conversation { get; set; }
    public User? Sender { get; set; }
}

public sealed class PaymentStatus
{
    public int PaymentStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<Payment> Payments { get; set; } = [];
}

public sealed class PaymentMethod
{
    public int PaymentMethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public List<Payment> Payments { get; set; } = [];
}

public sealed class Payment
{
    public int PaymentId { get; set; }
    public int RequestId { get; set; }
    public int ClientId { get; set; }
    public int PaymentStatusId { get; set; }
    public int? PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public string ProviderName { get; set; } = "paymongo";
    public string? ProviderCheckoutSessionId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderReferenceNumber { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public Client? Client { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public List<PaymentEvent> Events { get; set; } = [];
}

public sealed class PaymentEvent
{
    public int PaymentEventId { get; set; }
    public int? PaymentId { get; set; }
    public string? ProviderEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }

    public Payment? Payment { get; set; }
}

public sealed class Review
{
    public int ReviewId { get; set; }
    public int RequestId { get; set; }
    public int ClientId { get; set; }
    public int MechanicId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceRequest? Request { get; set; }
    public Client? Client { get; set; }
    public Mechanic? Mechanic { get; set; }
}

public sealed class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string? NotificationType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}

public sealed class AuditLog
{
    public int AuditId { get; set; }
    public int? ActorUserId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? ActorUser { get; set; }
}
