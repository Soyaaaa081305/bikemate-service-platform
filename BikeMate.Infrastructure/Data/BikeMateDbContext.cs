using BikeMate.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Infrastructure.Data;

public sealed class BikeMateDbContext(DbContextOptions<BikeMateDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserAuthProvider> UserAuthProviders => Set<UserAuthProvider>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Motorcycle> Motorcycles => Set<Motorcycle>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<MechanicAvailability> MechanicAvailability => Set<MechanicAvailability>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopOperatingHour> ShopOperatingHours => Set<ShopOperatingHour>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ShopService> ShopServices => Set<ShopService>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ShopMechanic> ShopMechanics => Set<ShopMechanic>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestLineItem> ServiceRequestLineItems => Set<ServiceRequestLineItem>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();
    public DbSet<RequestMedia> RequestMedia => Set<RequestMedia>();
    public DbSet<LiveLocation> LiveLocations => Set<LiveLocation>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");

        ConfigureUsers(modelBuilder);
        ConfigureCustomerData(modelBuilder);
        ConfigureMechanics(modelBuilder);
        ConfigureShopsAndServices(modelBuilder);
        ConfigureRequests(modelBuilder);
        ConfigureMessaging(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureReviewsAndLogs(modelBuilder);
        Seed(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.RoleId);
            entity.HasIndex(x => x.RoleName).IsUnique();
            entity.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(50);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(500);
            entity.Property(x => x.AccountStatus).HasMaxLength(30).HasDefaultValue("pending");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.ToTable("user_auth_providers");
            entity.HasKey(x => x.AuthProviderId);
            entity.HasIndex(x => new { x.ProviderName, x.ProviderSubject }).IsUnique().HasFilter("[ProviderSubject] IS NOT NULL");
            entity.Property(x => x.ProviderName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ProviderSubject).HasMaxLength(255);
            entity.Property(x => x.ProviderEmail).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.AuthProviders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.ToTable("otp_verifications");
            entity.HasKey(x => x.OtpId);
            entity.Property(x => x.OtpHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(50).HasDefaultValue("email_verification");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.OtpVerifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDeviceToken>(entity =>
        {
            entity.ToTable("user_device_tokens");
            entity.HasKey(x => x.DeviceTokenId);
            entity.Property(x => x.DeviceToken).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("android");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.DeviceTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCustomerData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(x => x.ClientId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.MiddleName).HasMaxLength(100);
            entity.Property(x => x.Sex).HasMaxLength(30);
            entity.Property(x => x.ValidIdImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithOne(x => x.Client).HasForeignKey<Client>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.ToTable("client_addresses");
            entity.HasKey(x => x.AddressId);
            entity.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(100);
            entity.Property(x => x.Barangay).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.Addresses).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Motorcycle>(entity =>
        {
            entity.ToTable("motorcycles");
            entity.HasKey(x => x.MotorcycleId);
            entity.Property(x => x.Brand).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PlateNumber).HasMaxLength(50);
            entity.Property(x => x.EngineType).HasMaxLength(100);
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.MotorcycleImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.Motorcycles).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMechanics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mechanic>(entity =>
        {
            entity.ToTable("mechanics");
            entity.HasKey(x => x.MechanicId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.MiddleName).HasMaxLength(100);
            entity.Property(x => x.Sex).HasMaxLength(30);
            entity.Property(x => x.ValidIdImageUrl).HasMaxLength(500);
            entity.Property(x => x.AddressLine).HasMaxLength(500);
            entity.Property(x => x.Barangay).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.ZipCode).HasMaxLength(10);
            entity.Property(x => x.CertificationImageUrl).HasMaxLength(500);
            entity.Property(x => x.AvailabilityStatus).HasMaxLength(30).HasDefaultValue("offline");
            entity.Property(x => x.CurrentLatitude).HasPrecision(10, 8);
            entity.Property(x => x.CurrentLongitude).HasPrecision(11, 8);
            entity.Property(x => x.AverageRating).HasPrecision(3, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithOne(x => x.Mechanic).HasForeignKey<Mechanic>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MechanicAvailability>(entity =>
        {
            entity.ToTable("mechanic_availability");
            entity.HasKey(x => x.AvailabilityId);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.Availability).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureShopsAndServices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("shops");
            entity.HasKey(x => x.ShopId);
            entity.Property(x => x.ShopName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.BusinessPermitUrl).HasMaxLength(500);
            entity.Property(x => x.ShopImageUrl).HasMaxLength(500);
            entity.Property(x => x.ShopLogoUrl).HasMaxLength(500);
            entity.Property(x => x.OwnerValidIdUrl).HasMaxLength(500);
            entity.Property(x => x.OwnerMiddleName).HasMaxLength(100);
            entity.Property(x => x.OwnerSex).HasMaxLength(30);
            entity.Property(x => x.OwnerAddressLine).HasMaxLength(500);
            entity.Property(x => x.OwnerBarangay).HasMaxLength(100);
            entity.Property(x => x.OwnerCity).HasMaxLength(100);
            entity.Property(x => x.OwnerProvince).HasMaxLength(100);
            entity.Property(x => x.OwnerZipCode).HasMaxLength(10);
            entity.Property(x => x.ContactNumber).HasMaxLength(50);
            entity.Property(x => x.ShopStatus).HasMaxLength(30).HasDefaultValue("pending");
            entity.Property(x => x.AllowsReservations).HasDefaultValue(true);
            entity.Property(x => x.AllowsPickup).HasDefaultValue(true);
            entity.Property(x => x.AllowsOnsiteRepair).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Owner).WithMany(x => x.OwnedShops).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShopOperatingHour>(entity =>
        {
            entity.ToTable("shop_operating_hours");
            entity.HasKey(x => x.OperatingHourId);
            entity.HasOne(x => x.Shop).WithMany(x => x.OperatingHours).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("service_categories");
            entity.HasKey(x => x.CategoryId);
            entity.HasIndex(x => x.CategoryName).IsUnique();
            entity.Property(x => x.CategoryName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IconUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ShopService>(entity =>
        {
            entity.ToTable("shop_services");
            entity.HasKey(x => x.ShopServiceId);
            entity.Property(x => x.ServiceName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BasePrice).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.Services).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany(x => x.ShopServices).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceImage>(entity =>
        {
            entity.ToTable("service_images");
            entity.HasKey(x => x.ServiceImageId);
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.ShopService).WithMany(x => x.Images).HasForeignKey(x => x.ShopServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.Products).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(x => x.ProductImageId);
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShopMechanic>(entity =>
        {
            entity.ToTable("shop_mechanics");
            entity.HasKey(x => new { x.ShopId, x.MechanicId });
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.ShopMechanics).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.ShopMechanics).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.ToTable("request_statuses");
            entity.HasKey(x => x.StatusId);
            entity.HasIndex(x => x.StatusName).IsUnique();
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("service_requests", table =>
            {
                table.HasTrigger("trg_requests_update_completed_jobs");
                table.HasTrigger("trg_service_request_status_history");
            });
            entity.HasKey(x => x.RequestId);
            entity.Property(x => x.IssueDescription).IsRequired();
            entity.Property(x => x.ServiceLocationAddress).HasMaxLength(500);
            entity.Property(x => x.ServiceLatitude).HasPrecision(10, 8);
            entity.Property(x => x.ServiceLongitude).HasPrecision(11, 8);
            entity.Property(x => x.EstimatedTotal).HasPrecision(10, 2);
            entity.Property(x => x.FinalTotal).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Shop).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ShopService).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ShopServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.AssignedRequests).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CurrentStatus).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.CurrentStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Motorcycle).WithMany().HasForeignKey(x => x.MotorcycleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceRequestLineItem>(entity =>
        {
            entity.ToTable("service_request_line_items");
            entity.HasKey(x => x.LineItemId);
            entity.Property(x => x.ItemType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Quantity).HasDefaultValue(1);
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2);
            entity.Property(x => x.LineTotal).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.LineItems).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ShopService).WithMany().HasForeignKey(x => x.ShopServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany(x => x.RequestLineItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.ToTable("request_status_history");
            entity.HasKey(x => x.StatusHistoryId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.StatusHistory).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.OldStatus).WithMany().HasForeignKey(x => x.OldStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NewStatus).WithMany().HasForeignKey(x => x.NewStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RequestMedia>(entity =>
        {
            entity.ToTable("request_media");
            entity.HasKey(x => x.RequestMediaId);
            entity.Property(x => x.MediaUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(50).HasDefaultValue("image");
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.Media).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LiveLocation>(entity =>
        {
            entity.ToTable("live_locations");
            entity.HasKey(x => x.LiveLocationId);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.AccuracyMeters).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.LiveLocations).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.LiveLocations).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureMessaging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(x => x.ConversationId);
            entity.Property(x => x.ConversationType).HasMaxLength(50).HasDefaultValue("service_request");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("conversation_participants");
            entity.HasKey(x => new { x.ConversationId, x.UserId });
            entity.Property(x => x.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Conversation).WithMany(x => x.Participants).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(x => x.MessageId);
            entity.Property(x => x.AttachmentUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.ToTable("payment_statuses");
            entity.HasKey(x => x.PaymentStatusId);
            entity.HasIndex(x => x.StatusName).IsUnique();
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.HasKey(x => x.PaymentMethodId);
            entity.HasIndex(x => x.MethodName).IsUnique();
            entity.Property(x => x.MethodName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("PHP");
            entity.Property(x => x.ProviderName).HasMaxLength(50).HasDefaultValue("paymongo");
            entity.Property(x => x.ProviderCheckoutSessionId).HasMaxLength(255);
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(255);
            entity.Property(x => x.ProviderReferenceNumber).HasMaxLength(255);
            entity.Property(x => x.CheckoutUrl).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.Payments).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Client).WithMany(x => x.Payments).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentStatus).WithMany(x => x.Payments).HasForeignKey(x => x.PaymentStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentMethod).WithMany(x => x.Payments).HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.ToTable("payment_events");
            entity.HasKey(x => x.PaymentEventId);
            entity.Property(x => x.ProviderEventId).HasMaxLength(255);
            entity.Property(x => x.EventType).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.ReceivedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Payment).WithMany(x => x.Events).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureReviewsAndLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews", table => table.HasTrigger("trg_reviews_update_mechanic_rating"));
            entity.HasKey(x => x.ReviewId);
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithOne(x => x.Review).HasForeignKey<Review>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.Reviews).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.NotificationType).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.ActionName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Customer" },
            new Role { RoleId = 2, RoleName = "Mechanic" },
            new Role { RoleId = 3, RoleName = "ShopAdmin" },
            new Role { RoleId = 4, RoleName = "SystemAdmin" });

        modelBuilder.Entity<RequestStatus>().HasData(
            new RequestStatus { StatusId = 1, StatusName = "pending" },
            new RequestStatus { StatusId = 2, StatusName = "accepted" },
            new RequestStatus { StatusId = 3, StatusName = "rejected" },
            new RequestStatus { StatusId = 4, StatusName = "en_route" },
            new RequestStatus { StatusId = 5, StatusName = "arrived" },
            new RequestStatus { StatusId = 6, StatusName = "in_progress" },
            new RequestStatus { StatusId = 7, StatusName = "completed" },
            new RequestStatus { StatusId = 8, StatusName = "cancelled" },
            new RequestStatus { StatusId = 9, StatusName = "emergency_pending" },
            new RequestStatus { StatusId = 10, StatusName = "call_connecting" },
            new RequestStatus { StatusId = 11, StatusName = "searching_responder" },
            new RequestStatus { StatusId = 12, StatusName = "payment_pending" },
            new RequestStatus { StatusId = 13, StatusName = "paid" },
            new RequestStatus { StatusId = 14, StatusName = "call_connected" });

        modelBuilder.Entity<PaymentStatus>().HasData(
            new PaymentStatus { PaymentStatusId = 1, StatusName = "unpaid" },
            new PaymentStatus { PaymentStatusId = 2, StatusName = "pending" },
            new PaymentStatus { PaymentStatusId = 3, StatusName = "paid" },
            new PaymentStatus { PaymentStatusId = 4, StatusName = "failed" },
            new PaymentStatus { PaymentStatusId = 5, StatusName = "cancelled" },
            new PaymentStatus { PaymentStatusId = 6, StatusName = "refunded" });

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { PaymentMethodId = 1, MethodName = "gcash" },
            new PaymentMethod { PaymentMethodId = 2, MethodName = "maya" },
            new PaymentMethod { PaymentMethodId = 3, MethodName = "card" },
            new PaymentMethod { PaymentMethodId = 4, MethodName = "grab_pay" },
            new PaymentMethod { PaymentMethodId = 5, MethodName = "cash" },
            new PaymentMethod { PaymentMethodId = 6, MethodName = "bank_transfer" });

        modelBuilder.Entity<ServiceCategory>().HasData(
            new ServiceCategory { CategoryId = 1, CategoryName = "Engine Repair", Description = "Engine troubleshooting, repair, and maintenance", IsActive = true },
            new ServiceCategory { CategoryId = 2, CategoryName = "Tire Service", Description = "Flat tire repair, tire replacement, and tire checking", IsActive = true },
            new ServiceCategory { CategoryId = 3, CategoryName = "Battery Service", Description = "Battery check, replacement, and charging assistance", IsActive = true },
            new ServiceCategory { CategoryId = 4, CategoryName = "Oil Change", Description = "Motorcycle oil change and fluid maintenance", IsActive = true },
            new ServiceCategory { CategoryId = 5, CategoryName = "Brake Service", Description = "Brake inspection, cleaning, repair, and replacement", IsActive = true },
            new ServiceCategory { CategoryId = 6, CategoryName = "Emergency Roadside Assistance", Description = "Urgent roadside motorcycle assistance", IsActive = true },
            new ServiceCategory { CategoryId = 7, CategoryName = "Drivetrain & Gear Service", Description = "Gear shifting, transmission, derailleur, and drivetrain diagnostics", IsActive = true },
            new ServiceCategory { CategoryId = 8, CategoryName = "Chain & Sprocket Service", Description = "Drive chain cleaning, tensioning, replacement, and sprocket inspection", IsActive = true },
            new ServiceCategory { CategoryId = 9, CategoryName = "Accessories & Electrical Installation", Description = "Safe installation of approved motorcycle accessories and electrical upgrades", IsActive = true },
            new ServiceCategory { CategoryId = 10, CategoryName = "Preventive Maintenance & Tune-up", Description = "Periodic inspection, adjustment, and complete motorcycle tune-up", IsActive = true });

    }
}
