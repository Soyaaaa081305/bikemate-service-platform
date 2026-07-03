using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.WebAdmin.Data;

public partial class BikeMateDbContext : DbContext
{
    public BikeMateDbContext()
    {
    }

    public BikeMateDbContext(DbContextOptions<BikeMateDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientAddress> ClientAddresses { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    public virtual DbSet<LiveLocation> LiveLocations { get; set; }

    public virtual DbSet<Mechanic> Mechanics { get; set; }

    public virtual DbSet<MechanicAvailability> MechanicAvailabilities { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Motorcycle> Motorcycles { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OtpVerification> OtpVerifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentEvent> PaymentEvents { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentStatus> PaymentStatuses { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<RequestMedium> RequestMedia { get; set; }

    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

    public virtual DbSet<RequestStatusHistory> RequestStatusHistories { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceImage> ServiceImages { get; set; }

    public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<ShopMechanic> ShopMechanics { get; set; }

    public virtual DbSet<ShopOperatingHour> ShopOperatingHours { get; set; }

    public virtual DbSet<ShopService> ShopServices { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthProvider> UserAuthProviders { get; set; }

    public virtual DbSet<UserDeviceToken> UserDeviceTokens { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.ActorUserId, "IX_audit_logs_ActorUserId");

            entity.Property(e => e.ActionName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");

            entity.HasIndex(e => e.UserId, "IX_clients_UserId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Client).HasForeignKey<Client>(d => d.UserId);
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);

            entity.ToTable("client_addresses");

            entity.HasIndex(e => e.ClientId, "IX_client_addresses_ClientId");

            entity.Property(e => e.AddressLine).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);

            entity.HasOne(d => d.Client).WithMany(p => p.ClientAddresses).HasForeignKey(d => d.ClientId);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");

            entity.HasIndex(e => e.RequestId, "IX_conversations_RequestId");

            entity.Property(e => e.ConversationType)
                .HasMaxLength(50)
                .HasDefaultValue("service_request");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Request).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => new { e.ConversationId, e.UserId });

            entity.ToTable("conversation_participants");

            entity.HasIndex(e => e.UserId, "IX_conversation_participants_UserId");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ConversationParticipants).HasForeignKey(d => d.ConversationId);

            entity.HasOne(d => d.User).WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<LiveLocation>(entity =>
        {
            entity.ToTable("live_locations");

            entity.HasIndex(e => e.MechanicId, "IX_live_locations_MechanicId");

            entity.HasIndex(e => e.RequestId, "IX_live_locations_RequestId");

            entity.HasIndex(e => new { e.RequestId, e.CreatedAt }, "IX_live_locations_RequestId_CreatedAt");

            entity.Property(e => e.AccuracyMeters).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(11, 8)");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.LiveLocations)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Request).WithMany(p => p.LiveLocations)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mechanic>(entity =>
        {
            entity.ToTable("mechanics");

            entity.HasIndex(e => e.UserId, "IX_mechanics_UserId").IsUnique();

            entity.Property(e => e.AvailabilityStatus)
                .HasMaxLength(30)
                .HasDefaultValue("offline");
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CertificationImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrentLatitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.CurrentLongitude).HasColumnType("decimal(11, 8)");

            entity.HasOne(d => d.User).WithOne(p => p.Mechanic).HasForeignKey<Mechanic>(d => d.UserId);
        });

        modelBuilder.Entity<MechanicAvailability>(entity =>
        {
            entity.HasKey(e => e.AvailabilityId);

            entity.ToTable("mechanic_availability");

            entity.HasIndex(e => e.MechanicId, "IX_mechanic_availability_MechanicId");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.MechanicAvailabilities).HasForeignKey(d => d.MechanicId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasIndex(e => e.ConversationId, "IX_messages_ConversationId");

            entity.HasIndex(e => e.SenderUserId, "IX_messages_SenderUserId");

            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages).HasForeignKey(d => d.ConversationId);

            entity.HasOne(d => d.SenderUser).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Motorcycle>(entity =>
        {
            entity.ToTable("motorcycles");

            entity.HasIndex(e => e.ClientId, "IX_motorcycles_ClientId");

            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EngineType).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.MotorcycleImageUrl).HasMaxLength(500);
            entity.Property(e => e.PlateNumber).HasMaxLength(50);

            entity.HasOne(d => d.Client).WithMany(p => p.Motorcycles).HasForeignKey(d => d.ClientId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "IX_notifications_UserId");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_notifications_UserId_IsRead");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NotificationType).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.HasKey(e => e.OtpId);

            entity.ToTable("otp_verifications");

            entity.HasIndex(e => e.UserId, "IX_otp_verifications_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OtpHash).HasMaxLength(500);
            entity.Property(e => e.Purpose)
                .HasMaxLength(50)
                .HasDefaultValue("email_verification");

            entity.HasOne(d => d.User).WithMany(p => p.OtpVerifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.ToTable("password_reset_tokens");

            entity.HasIndex(e => e.UserId, "IX_password_reset_tokens_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasIndex(e => e.ClientId, "IX_payments_ClientId");

            entity.HasIndex(e => e.PaymentMethodId, "IX_payments_PaymentMethodId");

            entity.HasIndex(e => e.PaymentStatusId, "IX_payments_PaymentStatusId");

            entity.HasIndex(e => e.RequestId, "IX_payments_RequestId");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CheckoutUrl).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("PHP");
            entity.Property(e => e.ProviderCheckoutSessionId).HasMaxLength(255);
            entity.Property(e => e.ProviderName)
                .HasMaxLength(50)
                .HasDefaultValue("paymongo");
            entity.Property(e => e.ProviderPaymentId).HasMaxLength(255);
            entity.Property(e => e.ProviderReferenceNumber).HasMaxLength(255);

            entity.HasOne(d => d.Client).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.PaymentStatus).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Request).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.ToTable("payment_events");

            entity.HasIndex(e => e.PaymentId, "IX_payment_events_PaymentId");

            entity.Property(e => e.EventType).HasMaxLength(255);
            entity.Property(e => e.ProviderEventId).HasMaxLength(255);
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentEvents)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");

            entity.HasIndex(e => e.MethodName, "IX_payment_methods_MethodName").IsUnique();

            entity.Property(e => e.MethodName).HasMaxLength(50);
        });

        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.ToTable("payment_statuses");

            entity.HasIndex(e => e.StatusName, "IX_payment_statuses_StatusName").IsUnique();

            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasIndex(e => e.ShopId, "IX_products_ShopId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(255);

            entity.HasOne(d => d.Shop).WithMany(p => p.Products).HasForeignKey(d => d.ShopId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");

            entity.HasIndex(e => e.ProductId, "IX_product_images_ProductId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<RequestMedium>(entity =>
        {
            entity.HasKey(e => e.RequestMediaId);

            entity.ToTable("request_media");

            entity.HasIndex(e => e.RequestId, "IX_request_media_RequestId");

            entity.Property(e => e.Caption).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MediaType)
                .HasMaxLength(50)
                .HasDefaultValue("image");
            entity.Property(e => e.MediaUrl).HasMaxLength(500);

            entity.HasOne(d => d.Request).WithMany(p => p.RequestMedia).HasForeignKey(d => d.RequestId);
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.ToTable("request_statuses");

            entity.HasIndex(e => e.StatusName, "IX_request_statuses_StatusName").IsUnique();

            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.HasKey(e => e.StatusHistoryId);

            entity.ToTable("request_status_history");

            entity.HasIndex(e => e.ChangedByUserId, "IX_request_status_history_ChangedByUserId");

            entity.HasIndex(e => e.NewStatusId, "IX_request_status_history_NewStatusId");

            entity.HasIndex(e => e.OldStatusId, "IX_request_status_history_OldStatusId");

            entity.HasIndex(e => e.RequestId, "IX_request_status_history_RequestId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.RequestStatusHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.NewStatus).WithMany(p => p.RequestStatusHistoryNewStatuses)
                .HasForeignKey(d => d.NewStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OldStatus).WithMany(p => p.RequestStatusHistoryOldStatuses).HasForeignKey(d => d.OldStatusId);

            entity.HasOne(d => d.Request).WithMany(p => p.RequestStatusHistories).HasForeignKey(d => d.RequestId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");

            entity.HasIndex(e => e.ClientId, "IX_reviews_ClientId");

            entity.HasIndex(e => e.MechanicId, "IX_reviews_MechanicId");

            entity.HasIndex(e => e.RequestId, "IX_reviews_RequestId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Client).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Mechanic).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Request).WithOne(p => p.Review)
                .HasForeignKey<Review>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "IX_roles_RoleName").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("service_categories");

            entity.HasIndex(e => e.CategoryName, "IX_service_categories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(150);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ServiceImage>(entity =>
        {
            entity.ToTable("service_images");

            entity.HasIndex(e => e.ShopServiceId, "IX_service_images_ShopServiceId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasOne(d => d.ShopService).WithMany(p => p.ServiceImages).HasForeignKey(d => d.ShopServiceId);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("service_requests");

            entity.HasIndex(e => e.ClientId, "IX_service_requests_ClientId");

            entity.HasIndex(e => new { e.ClientId, e.CurrentStatusId }, "IX_service_requests_ClientId_CurrentStatusId");

            entity.HasIndex(e => e.CurrentStatusId, "IX_service_requests_CurrentStatusId");

            entity.HasIndex(e => e.MechanicId, "IX_service_requests_MechanicId");

            entity.HasIndex(e => e.MotorcycleId, "IX_service_requests_MotorcycleId");

            entity.HasIndex(e => e.ShopId, "IX_service_requests_ShopId");

            entity.HasIndex(e => e.ShopServiceId, "IX_service_requests_ShopServiceId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EstimatedTotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FinalTotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ServiceLatitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.ServiceLocationAddress).HasMaxLength(500);
            entity.Property(e => e.ServiceLongitude).HasColumnType("decimal(11, 8)");

            entity.HasOne(d => d.Client).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CurrentStatus).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.CurrentStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Mechanic).WithMany(p => p.ServiceRequests).HasForeignKey(d => d.MechanicId);

            entity.HasOne(d => d.Motorcycle).WithMany(p => p.ServiceRequests).HasForeignKey(d => d.MotorcycleId);

            entity.HasOne(d => d.Shop).WithMany(p => p.ServiceRequests).HasForeignKey(d => d.ShopId);

            entity.HasOne(d => d.ShopService).WithMany(p => p.ServiceRequests).HasForeignKey(d => d.ShopServiceId);
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("shops");

            entity.HasIndex(e => e.OwnerUserId, "IX_shops_OwnerUserId");

            entity.Property(e => e.AddressLine).HasMaxLength(500);
            entity.Property(e => e.BusinessPermitUrl).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ContactNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.ShopImageUrl).HasMaxLength(500);
            entity.Property(e => e.ShopName).HasMaxLength(255);
            entity.Property(e => e.ShopStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Shops)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ShopMechanic>(entity =>
        {
            entity.HasKey(e => new { e.ShopId, e.MechanicId });

            entity.ToTable("shop_mechanics");

            entity.HasIndex(e => e.MechanicId, "IX_shop_mechanics_MechanicId");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.ShopMechanics)
                .HasForeignKey(d => d.MechanicId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopMechanics).HasForeignKey(d => d.ShopId);
        });

        modelBuilder.Entity<ShopOperatingHour>(entity =>
        {
            entity.HasKey(e => e.OperatingHourId);

            entity.ToTable("shop_operating_hours");

            entity.HasIndex(e => e.ShopId, "IX_shop_operating_hours_ShopId");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopOperatingHours).HasForeignKey(d => d.ShopId);
        });

        modelBuilder.Entity<ShopService>(entity =>
        {
            entity.ToTable("shop_services");

            entity.HasIndex(e => e.CategoryId, "IX_shop_services_CategoryId");

            entity.HasIndex(e => e.ShopId, "IX_shop_services_ShopId");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ServiceName).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.ShopServices)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopServices).HasForeignKey(d => d.ShopId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "IX_users_Email").IsUnique();

            entity.Property(e => e.AccountStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.HasKey(e => e.AuthProviderId);

            entity.ToTable("user_auth_providers");

            entity.HasIndex(e => new { e.ProviderName, e.ProviderSubject }, "IX_user_auth_providers_ProviderName_ProviderSubject")
                .IsUnique()
                .HasFilter("([ProviderSubject] IS NOT NULL)");

            entity.HasIndex(e => e.UserId, "IX_user_auth_providers_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ProviderEmail).HasMaxLength(255);
            entity.Property(e => e.ProviderName).HasMaxLength(50);
            entity.Property(e => e.ProviderSubject).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.UserAuthProviders).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserDeviceToken>(entity =>
        {
            entity.HasKey(e => e.DeviceTokenId);

            entity.ToTable("user_device_tokens");

            entity.HasIndex(e => e.UserId, "IX_user_device_tokens_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DeviceToken).HasMaxLength(500);
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasDefaultValue("android");

            entity.HasOne(d => d.User).WithMany(p => p.UserDeviceTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "IX_user_roles_RoleId");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
