/*
    BikeMate complete local database setup
    Run this file in SQL Server Management Studio or Azure Data Studio.
    It creates BikeMatesDB when missing, switches to it, then applies the EF-generated schema/seed script.
*/
IF DB_ID(N'BikeMatesDB') IS NULL
BEGIN
    CREATE DATABASE [BikeMatesDB];
END;
GO

USE [BikeMatesDB];
GO
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[payment_methods] (
        [PaymentMethodId] int NOT NULL IDENTITY,
        [MethodName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_payment_methods] PRIMARY KEY ([PaymentMethodId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[payment_statuses] (
        [PaymentStatusId] int NOT NULL IDENTITY,
        [StatusName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_payment_statuses] PRIMARY KEY ([PaymentStatusId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[request_statuses] (
        [StatusId] int NOT NULL IDENTITY,
        [StatusName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_request_statuses] PRIMARY KEY ([StatusId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[roles] (
        [RoleId] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([RoleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[service_categories] (
        [CategoryId] int NOT NULL IDENTITY,
        [CategoryName] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IconUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_service_categories] PRIMARY KEY ([CategoryId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[users] (
        [UserId] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [PasswordHash] nvarchar(500) NULL,
        [ProfileImageUrl] nvarchar(500) NULL,
        [EmailVerified] bit NOT NULL,
        [PhoneVerified] bit NOT NULL,
        [AccountStatus] nvarchar(30) NOT NULL DEFAULT N'pending',
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[audit_logs] (
        [AuditId] int NOT NULL IDENTITY,
        [ActorUserId] int NULL,
        [ActionName] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_audit_logs] PRIMARY KEY ([AuditId]),
        CONSTRAINT [FK_audit_logs_users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[clients] (
        [ClientId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_clients] PRIMARY KEY ([ClientId]),
        CONSTRAINT [FK_clients_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[mechanics] (
        [MechanicId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Bio] nvarchar(max) NULL,
        [YearsExperience] int NULL,
        [CertificationImageUrl] nvarchar(500) NULL,
        [IsVerified] bit NOT NULL,
        [AvailabilityStatus] nvarchar(30) NOT NULL DEFAULT N'offline',
        [CurrentLatitude] decimal(10,8) NULL,
        [CurrentLongitude] decimal(11,8) NULL,
        [AverageRating] decimal(3,2) NOT NULL,
        [TotalCompletedJobs] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_mechanics] PRIMARY KEY ([MechanicId]),
        CONSTRAINT [FK_mechanics_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[notifications] (
        [NotificationId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [NotificationType] nvarchar(100) NULL,
        [Title] nvarchar(255) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [DataJson] nvarchar(max) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_notifications_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[otp_verifications] (
        [OtpId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [OtpHash] nvarchar(500) NOT NULL,
        [Purpose] nvarchar(50) NOT NULL DEFAULT N'email_verification',
        [ExpiresAt] datetime2 NOT NULL,
        [ConsumedAt] datetime2 NULL,
        [Attempts] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_otp_verifications] PRIMARY KEY ([OtpId]),
        CONSTRAINT [FK_otp_verifications_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[password_reset_tokens] (
        [TokenId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(500) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [ConsumedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_password_reset_tokens] PRIMARY KEY ([TokenId]),
        CONSTRAINT [FK_password_reset_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[shops] (
        [ShopId] int NOT NULL IDENTITY,
        [OwnerUserId] int NOT NULL,
        [ShopName] nvarchar(255) NOT NULL,
        [ShopDescription] nvarchar(max) NULL,
        [AddressLine] nvarchar(500) NULL,
        [City] nvarchar(100) NULL,
        [Province] nvarchar(100) NULL,
        [Latitude] decimal(10,8) NULL,
        [Longitude] decimal(11,8) NULL,
        [BusinessPermitUrl] nvarchar(500) NULL,
        [ShopImageUrl] nvarchar(500) NULL,
        [ContactNumber] nvarchar(50) NULL,
        [ShopStatus] nvarchar(30) NOT NULL DEFAULT N'pending',
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_shops] PRIMARY KEY ([ShopId]),
        CONSTRAINT [FK_shops_users_OwnerUserId] FOREIGN KEY ([OwnerUserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[user_auth_providers] (
        [AuthProviderId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ProviderName] nvarchar(50) NOT NULL,
        [ProviderSubject] nvarchar(255) NULL,
        [ProviderEmail] nvarchar(255) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_user_auth_providers] PRIMARY KEY ([AuthProviderId]),
        CONSTRAINT [FK_user_auth_providers_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[user_device_tokens] (
        [DeviceTokenId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [DeviceToken] nvarchar(500) NOT NULL,
        [Platform] nvarchar(50) NOT NULL DEFAULT N'android',
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_user_device_tokens] PRIMARY KEY ([DeviceTokenId]),
        CONSTRAINT [FK_user_device_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[user_roles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_user_roles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_user_roles_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[roles] ([RoleId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_user_roles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[client_addresses] (
        [AddressId] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [Label] nvarchar(100) NULL,
        [AddressLine] nvarchar(500) NOT NULL,
        [City] nvarchar(100) NULL,
        [Province] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Latitude] decimal(10,8) NULL,
        [Longitude] decimal(11,8) NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_client_addresses] PRIMARY KEY ([AddressId]),
        CONSTRAINT [FK_client_addresses_clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[clients] ([ClientId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[motorcycles] (
        [MotorcycleId] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [Brand] nvarchar(100) NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [YearModel] int NULL,
        [PlateNumber] nvarchar(50) NULL,
        [EngineType] nvarchar(100) NULL,
        [Color] nvarchar(50) NULL,
        [MotorcycleImageUrl] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_motorcycles] PRIMARY KEY ([MotorcycleId]),
        CONSTRAINT [FK_motorcycles_clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[clients] ([ClientId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[mechanic_availability] (
        [AvailabilityId] int NOT NULL IDENTITY,
        [MechanicId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_mechanic_availability] PRIMARY KEY ([AvailabilityId]),
        CONSTRAINT [FK_mechanic_availability_mechanics_MechanicId] FOREIGN KEY ([MechanicId]) REFERENCES [dbo].[mechanics] ([MechanicId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[products] (
        [ProductId] int NOT NULL IDENTITY,
        [ShopId] int NOT NULL,
        [ProductName] nvarchar(255) NOT NULL,
        [ProductDescription] nvarchar(max) NULL,
        [Price] decimal(10,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_products] PRIMARY KEY ([ProductId]),
        CONSTRAINT [FK_products_shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [dbo].[shops] ([ShopId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[shop_mechanics] (
        [ShopId] int NOT NULL,
        [MechanicId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_shop_mechanics] PRIMARY KEY ([ShopId], [MechanicId]),
        CONSTRAINT [FK_shop_mechanics_mechanics_MechanicId] FOREIGN KEY ([MechanicId]) REFERENCES [dbo].[mechanics] ([MechanicId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shop_mechanics_shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [dbo].[shops] ([ShopId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[shop_operating_hours] (
        [OperatingHourId] int NOT NULL IDENTITY,
        [ShopId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [OpeningTime] time NOT NULL,
        [ClosingTime] time NOT NULL,
        [IsClosed] bit NOT NULL,
        CONSTRAINT [PK_shop_operating_hours] PRIMARY KEY ([OperatingHourId]),
        CONSTRAINT [FK_shop_operating_hours_shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [dbo].[shops] ([ShopId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[shop_services] (
        [ShopServiceId] int NOT NULL IDENTITY,
        [ShopId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [ServiceName] nvarchar(255) NOT NULL,
        [ServiceDescription] nvarchar(max) NULL,
        [BasePrice] decimal(10,2) NOT NULL,
        [EstimatedMinutes] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_shop_services] PRIMARY KEY ([ShopServiceId]),
        CONSTRAINT [FK_shop_services_service_categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[service_categories] ([CategoryId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shop_services_shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [dbo].[shops] ([ShopId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[product_images] (
        [ProductImageId] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_product_images] PRIMARY KEY ([ProductImageId]),
        CONSTRAINT [FK_product_images_products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[products] ([ProductId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[service_images] (
        [ServiceImageId] int NOT NULL IDENTITY,
        [ShopServiceId] int NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_service_images] PRIMARY KEY ([ServiceImageId]),
        CONSTRAINT [FK_service_images_shop_services_ShopServiceId] FOREIGN KEY ([ShopServiceId]) REFERENCES [dbo].[shop_services] ([ShopServiceId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[service_requests] (
        [RequestId] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [ShopId] int NULL,
        [ShopServiceId] int NULL,
        [MechanicId] int NULL,
        [CurrentStatusId] int NOT NULL,
        [MotorcycleId] int NULL,
        [IssueDescription] nvarchar(max) NOT NULL,
        [ServiceLocationAddress] nvarchar(500) NULL,
        [ServiceLatitude] decimal(10,8) NULL,
        [ServiceLongitude] decimal(11,8) NULL,
        [ScheduledAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AcceptedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [EstimatedTotal] decimal(10,2) NOT NULL,
        [FinalTotal] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_service_requests] PRIMARY KEY ([RequestId]),
        CONSTRAINT [FK_service_requests_clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[clients] ([ClientId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_mechanics_MechanicId] FOREIGN KEY ([MechanicId]) REFERENCES [dbo].[mechanics] ([MechanicId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_motorcycles_MotorcycleId] FOREIGN KEY ([MotorcycleId]) REFERENCES [dbo].[motorcycles] ([MotorcycleId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_request_statuses_CurrentStatusId] FOREIGN KEY ([CurrentStatusId]) REFERENCES [dbo].[request_statuses] ([StatusId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_shop_services_ShopServiceId] FOREIGN KEY ([ShopServiceId]) REFERENCES [dbo].[shop_services] ([ShopServiceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [dbo].[shops] ([ShopId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[conversations] (
        [ConversationId] int NOT NULL IDENTITY,
        [RequestId] int NULL,
        [ConversationType] nvarchar(50) NOT NULL DEFAULT N'service_request',
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastMessageAt] datetime2 NULL,
        CONSTRAINT [PK_conversations] PRIMARY KEY ([ConversationId]),
        CONSTRAINT [FK_conversations_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[live_locations] (
        [LiveLocationId] int NOT NULL IDENTITY,
        [RequestId] int NULL,
        [MechanicId] int NULL,
        [Latitude] decimal(10,8) NOT NULL,
        [Longitude] decimal(11,8) NOT NULL,
        [AccuracyMeters] decimal(10,2) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_live_locations] PRIMARY KEY ([LiveLocationId]),
        CONSTRAINT [FK_live_locations_mechanics_MechanicId] FOREIGN KEY ([MechanicId]) REFERENCES [dbo].[mechanics] ([MechanicId]) ON DELETE SET NULL,
        CONSTRAINT [FK_live_locations_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[payments] (
        [PaymentId] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [ClientId] int NOT NULL,
        [PaymentStatusId] int NOT NULL,
        [PaymentMethodId] int NULL,
        [Amount] decimal(10,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT N'PHP',
        [ProviderName] nvarchar(50) NOT NULL DEFAULT N'paymongo',
        [ProviderCheckoutSessionId] nvarchar(255) NULL,
        [ProviderPaymentId] nvarchar(255) NULL,
        [ProviderReferenceNumber] nvarchar(255) NULL,
        [CheckoutUrl] nvarchar(1000) NULL,
        [PaidAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_payments] PRIMARY KEY ([PaymentId]),
        CONSTRAINT [FK_payments_clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[clients] ([ClientId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_payments_payment_methods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [dbo].[payment_methods] ([PaymentMethodId]) ON DELETE SET NULL,
        CONSTRAINT [FK_payments_payment_statuses_PaymentStatusId] FOREIGN KEY ([PaymentStatusId]) REFERENCES [dbo].[payment_statuses] ([PaymentStatusId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_payments_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[request_media] (
        [RequestMediaId] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [MediaUrl] nvarchar(500) NOT NULL,
        [MediaType] nvarchar(50) NOT NULL DEFAULT N'image',
        [Caption] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_request_media] PRIMARY KEY ([RequestMediaId]),
        CONSTRAINT [FK_request_media_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[request_status_history] (
        [StatusHistoryId] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [OldStatusId] int NULL,
        [NewStatusId] int NOT NULL,
        [ChangedByUserId] int NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_request_status_history] PRIMARY KEY ([StatusHistoryId]),
        CONSTRAINT [FK_request_status_history_request_statuses_NewStatusId] FOREIGN KEY ([NewStatusId]) REFERENCES [dbo].[request_statuses] ([StatusId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_request_status_history_request_statuses_OldStatusId] FOREIGN KEY ([OldStatusId]) REFERENCES [dbo].[request_statuses] ([StatusId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_request_status_history_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE CASCADE,
        CONSTRAINT [FK_request_status_history_users_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[reviews] (
        [ReviewId] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [ClientId] int NOT NULL,
        [MechanicId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_reviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_reviews_clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[clients] ([ClientId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_reviews_mechanics_MechanicId] FOREIGN KEY ([MechanicId]) REFERENCES [dbo].[mechanics] ([MechanicId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_reviews_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[service_requests] ([RequestId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[conversation_participants] (
        [ConversationId] int NOT NULL,
        [UserId] int NOT NULL,
        [JoinedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastReadAt] datetime2 NULL,
        CONSTRAINT [PK_conversation_participants] PRIMARY KEY ([ConversationId], [UserId]),
        CONSTRAINT [FK_conversation_participants_conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[conversations] ([ConversationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_conversation_participants_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[messages] (
        [MessageId] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [SenderUserId] int NOT NULL,
        [MessageText] nvarchar(max) NOT NULL,
        [AttachmentUrl] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ReadAt] datetime2 NULL,
        CONSTRAINT [PK_messages] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_messages_conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[conversations] ([ConversationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_messages_users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[users] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE TABLE [dbo].[payment_events] (
        [PaymentEventId] int NOT NULL IDENTITY,
        [PaymentId] int NULL,
        [ProviderEventId] nvarchar(255) NULL,
        [EventType] nvarchar(255) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ReceivedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_payment_events] PRIMARY KEY ([PaymentEventId]),
        CONSTRAINT [FK_payment_events_payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[payments] ([PaymentId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentMethodId', N'MethodName') AND [object_id] = OBJECT_ID(N'[dbo].[payment_methods]'))
        SET IDENTITY_INSERT [dbo].[payment_methods] ON;
    EXEC(N'INSERT INTO [dbo].[payment_methods] ([PaymentMethodId], [MethodName])
    VALUES (1, N''gcash''),
    (2, N''maya''),
    (3, N''card''),
    (4, N''grab_pay''),
    (5, N''cash''),
    (6, N''bank_transfer'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentMethodId', N'MethodName') AND [object_id] = OBJECT_ID(N'[dbo].[payment_methods]'))
        SET IDENTITY_INSERT [dbo].[payment_methods] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentStatusId', N'StatusName') AND [object_id] = OBJECT_ID(N'[dbo].[payment_statuses]'))
        SET IDENTITY_INSERT [dbo].[payment_statuses] ON;
    EXEC(N'INSERT INTO [dbo].[payment_statuses] ([PaymentStatusId], [StatusName])
    VALUES (1, N''unpaid''),
    (2, N''pending''),
    (3, N''paid''),
    (4, N''failed''),
    (5, N''cancelled''),
    (6, N''refunded'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentStatusId', N'StatusName') AND [object_id] = OBJECT_ID(N'[dbo].[payment_statuses]'))
        SET IDENTITY_INSERT [dbo].[payment_statuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'StatusId', N'StatusName') AND [object_id] = OBJECT_ID(N'[dbo].[request_statuses]'))
        SET IDENTITY_INSERT [dbo].[request_statuses] ON;
    EXEC(N'INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName])
    VALUES (1, N''pending''),
    (2, N''accepted''),
    (3, N''rejected''),
    (4, N''en_route''),
    (5, N''arrived''),
    (6, N''in_progress''),
    (7, N''completed''),
    (8, N''cancelled'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'StatusId', N'StatusName') AND [object_id] = OBJECT_ID(N'[dbo].[request_statuses]'))
        SET IDENTITY_INSERT [dbo].[request_statuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[dbo].[roles]'))
        SET IDENTITY_INSERT [dbo].[roles] ON;
    EXEC(N'INSERT INTO [dbo].[roles] ([RoleId], [RoleName])
    VALUES (1, N''Customer''),
    (2, N''Mechanic''),
    (3, N''ShopAdmin''),
    (4, N''SystemAdmin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[dbo].[roles]'))
        SET IDENTITY_INSERT [dbo].[roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Description', N'IconUrl', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[service_categories]'))
        SET IDENTITY_INSERT [dbo].[service_categories] ON;
    EXEC(N'INSERT INTO [dbo].[service_categories] ([CategoryId], [CategoryName], [Description], [IconUrl], [IsActive])
    VALUES (1, N''Engine Repair'', N''Engine troubleshooting, repair, and maintenance'', NULL, CAST(1 AS bit)),
    (2, N''Tire Service'', N''Flat tire repair, tire replacement, and tire checking'', NULL, CAST(1 AS bit)),
    (3, N''Battery Service'', N''Battery check, replacement, and charging assistance'', NULL, CAST(1 AS bit)),
    (4, N''Oil Change'', N''Motorcycle oil change and fluid maintenance'', NULL, CAST(1 AS bit)),
    (5, N''Brake Service'', N''Brake inspection, cleaning, repair, and replacement'', NULL, CAST(1 AS bit)),
    (6, N''Emergency Roadside Assistance'', N''Urgent roadside motorcycle assistance'', NULL, CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Description', N'IconUrl', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[service_categories]'))
        SET IDENTITY_INSERT [dbo].[service_categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'AccountStatus', N'CreatedAt', N'Email', N'EmailVerified', N'FirstName', N'LastName', N'PasswordHash', N'PhoneNumber', N'PhoneVerified', N'ProfileImageUrl', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[users]'))
        SET IDENTITY_INSERT [dbo].[users] ON;
    EXEC(N'INSERT INTO [dbo].[users] ([UserId], [AccountStatus], [CreatedAt], [Email], [EmailVerified], [FirstName], [LastName], [PasswordHash], [PhoneNumber], [PhoneVerified], [ProfileImageUrl], [UpdatedAt])
    VALUES (1, N''active'', ''2026-06-08T00:00:00.0000000Z'', N''customer@bikemate.test'', CAST(1 AS bit), N''Juan'', N''Customer'', N''sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea'', N''+639171234567'', CAST(1 AS bit), NULL, NULL),
    (2, N''active'', ''2026-06-08T00:00:00.0000000Z'', N''mechanic@bikemate.test'', CAST(1 AS bit), N''Rico'', N''Mechanic'', N''sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea'', N''+639171234567'', CAST(1 AS bit), NULL, NULL),
    (3, N''active'', ''2026-06-08T00:00:00.0000000Z'', N''shop@bikemate.test'', CAST(1 AS bit), N''Maya'', N''ShopAdmin'', N''sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea'', N''+639171234567'', CAST(1 AS bit), NULL, NULL),
    (4, N''active'', ''2026-06-08T00:00:00.0000000Z'', N''admin@bikemate.test'', CAST(1 AS bit), N''Ana'', N''Admin'', N''sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea'', N''+639171234567'', CAST(1 AS bit), NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'AccountStatus', N'CreatedAt', N'Email', N'EmailVerified', N'FirstName', N'LastName', N'PasswordHash', N'PhoneNumber', N'PhoneVerified', N'ProfileImageUrl', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[users]'))
        SET IDENTITY_INSERT [dbo].[users] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ClientId', N'CreatedAt', N'UserId') AND [object_id] = OBJECT_ID(N'[dbo].[clients]'))
        SET IDENTITY_INSERT [dbo].[clients] ON;
    EXEC(N'INSERT INTO [dbo].[clients] ([ClientId], [CreatedAt], [UserId])
    VALUES (1, ''2026-06-08T00:00:00.0000000Z'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ClientId', N'CreatedAt', N'UserId') AND [object_id] = OBJECT_ID(N'[dbo].[clients]'))
        SET IDENTITY_INSERT [dbo].[clients] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MechanicId', N'AvailabilityStatus', N'AverageRating', N'Bio', N'CertificationImageUrl', N'CreatedAt', N'CurrentLatitude', N'CurrentLongitude', N'IsVerified', N'TotalCompletedJobs', N'UpdatedAt', N'UserId', N'YearsExperience') AND [object_id] = OBJECT_ID(N'[dbo].[mechanics]'))
        SET IDENTITY_INSERT [dbo].[mechanics] ON;
    EXEC(N'INSERT INTO [dbo].[mechanics] ([MechanicId], [AvailabilityStatus], [AverageRating], [Bio], [CertificationImageUrl], [CreatedAt], [CurrentLatitude], [CurrentLongitude], [IsVerified], [TotalCompletedJobs], [UpdatedAt], [UserId], [YearsExperience])
    VALUES (1, N''online'', 4.8, N''Certified roadside motorcycle technician.'', NULL, ''2026-06-08T00:00:00.0000000Z'', 14.599512, 120.984222, CAST(1 AS bit), 12, NULL, 2, 5)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MechanicId', N'AvailabilityStatus', N'AverageRating', N'Bio', N'CertificationImageUrl', N'CreatedAt', N'CurrentLatitude', N'CurrentLongitude', N'IsVerified', N'TotalCompletedJobs', N'UpdatedAt', N'UserId', N'YearsExperience') AND [object_id] = OBJECT_ID(N'[dbo].[mechanics]'))
        SET IDENTITY_INSERT [dbo].[mechanics] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'NotificationId', N'CreatedAt', N'DataJson', N'IsRead', N'Message', N'NotificationType', N'Title', N'UserId') AND [object_id] = OBJECT_ID(N'[dbo].[notifications]'))
        SET IDENTITY_INSERT [dbo].[notifications] ON;
    EXEC(N'INSERT INTO [dbo].[notifications] ([NotificationId], [CreatedAt], [DataJson], [IsRead], [Message], [NotificationType], [Title], [UserId])
    VALUES (1, ''2026-06-08T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Rico Mechanic accepted your tire service request.'', N''booking'', N''Mechanic assigned'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'NotificationId', N'CreatedAt', N'DataJson', N'IsRead', N'Message', N'NotificationType', N'Title', N'UserId') AND [object_id] = OBJECT_ID(N'[dbo].[notifications]'))
        SET IDENTITY_INSERT [dbo].[notifications] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ShopId', N'AddressLine', N'BusinessPermitUrl', N'City', N'ContactNumber', N'CreatedAt', N'Latitude', N'Longitude', N'OwnerUserId', N'Province', N'ShopDescription', N'ShopImageUrl', N'ShopName', N'ShopStatus', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[shops]'))
        SET IDENTITY_INSERT [dbo].[shops] ON;
    EXEC(N'INSERT INTO [dbo].[shops] ([ShopId], [AddressLine], [BusinessPermitUrl], [City], [ContactNumber], [CreatedAt], [Latitude], [Longitude], [OwnerUserId], [Province], [ShopDescription], [ShopImageUrl], [ShopName], [ShopStatus], [UpdatedAt])
    VALUES (1, N''Sample shop address, Manila'', NULL, N''Manila'', N''+639171234567'', ''2026-06-08T00:00:00.0000000Z'', 14.6042, 120.9822, 3, N''Metro Manila'', N''Sample verified repair shop for local testing.'', NULL, N''BikeMate Partner Shop'', N''verified'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ShopId', N'AddressLine', N'BusinessPermitUrl', N'City', N'ContactNumber', N'CreatedAt', N'Latitude', N'Longitude', N'OwnerUserId', N'Province', N'ShopDescription', N'ShopImageUrl', N'ShopName', N'ShopStatus', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[shops]'))
        SET IDENTITY_INSERT [dbo].[shops] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId', N'AssignedAt') AND [object_id] = OBJECT_ID(N'[dbo].[user_roles]'))
        SET IDENTITY_INSERT [dbo].[user_roles] ON;
    EXEC(N'INSERT INTO [dbo].[user_roles] ([RoleId], [UserId], [AssignedAt])
    VALUES (1, 1, ''2026-06-08T00:00:00.0000000Z''),
    (2, 2, ''2026-06-08T00:00:00.0000000Z''),
    (3, 3, ''2026-06-08T00:00:00.0000000Z''),
    (4, 4, ''2026-06-08T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId', N'AssignedAt') AND [object_id] = OBJECT_ID(N'[dbo].[user_roles]'))
        SET IDENTITY_INSERT [dbo].[user_roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AddressId', N'AddressLine', N'City', N'ClientId', N'CreatedAt', N'IsDefault', N'Label', N'Latitude', N'Longitude', N'PostalCode', N'Province') AND [object_id] = OBJECT_ID(N'[dbo].[client_addresses]'))
        SET IDENTITY_INSERT [dbo].[client_addresses] ON;
    EXEC(N'INSERT INTO [dbo].[client_addresses] ([AddressId], [AddressLine], [City], [ClientId], [CreatedAt], [IsDefault], [Label], [Latitude], [Longitude], [PostalCode], [Province])
    VALUES (1, N''Sample customer address, Manila'', N''Manila'', 1, ''2026-06-08T00:00:00.0000000Z'', CAST(1 AS bit), N''Home'', 14.599512, 120.984222, N''1000'', N''Metro Manila'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AddressId', N'AddressLine', N'City', N'ClientId', N'CreatedAt', N'IsDefault', N'Label', N'Latitude', N'Longitude', N'PostalCode', N'Province') AND [object_id] = OBJECT_ID(N'[dbo].[client_addresses]'))
        SET IDENTITY_INSERT [dbo].[client_addresses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MotorcycleId', N'Brand', N'ClientId', N'Color', N'CreatedAt', N'EngineType', N'Model', N'MotorcycleImageUrl', N'PlateNumber', N'YearModel') AND [object_id] = OBJECT_ID(N'[dbo].[motorcycles]'))
        SET IDENTITY_INSERT [dbo].[motorcycles] ON;
    EXEC(N'INSERT INTO [dbo].[motorcycles] ([MotorcycleId], [Brand], [ClientId], [Color], [CreatedAt], [EngineType], [Model], [MotorcycleImageUrl], [PlateNumber], [YearModel])
    VALUES (1, N''Honda'', 1, N''Black'', ''2026-06-08T00:00:00.0000000Z'', N''125cc'', N''Click 125i'', NULL, N''BM-1234'', 2022)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MotorcycleId', N'Brand', N'ClientId', N'Color', N'CreatedAt', N'EngineType', N'Model', N'MotorcycleImageUrl', N'PlateNumber', N'YearModel') AND [object_id] = OBJECT_ID(N'[dbo].[motorcycles]'))
        SET IDENTITY_INSERT [dbo].[motorcycles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ProductId', N'CreatedAt', N'IsActive', N'Price', N'ProductDescription', N'ProductName', N'ShopId', N'StockQuantity', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[products]'))
        SET IDENTITY_INSERT [dbo].[products] ON;
    EXEC(N'INSERT INTO [dbo].[products] ([ProductId], [CreatedAt], [IsActive], [Price], [ProductDescription], [ProductName], [ShopId], [StockQuantity], [UpdatedAt])
    VALUES (1, ''2026-06-08T00:00:00.0000000Z'', CAST(1 AS bit), 280.0, N''Sample 10W-40 motorcycle oil.'', N''Engine Oil 1L'', 1, 20, NULL),
    (2, ''2026-06-08T00:00:00.0000000Z'', CAST(1 AS bit), 120.0, N''Patch kit for tubeless motorcycle tires.'', N''Tubeless Tire Patch'', 1, 50, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ProductId', N'CreatedAt', N'IsActive', N'Price', N'ProductDescription', N'ProductName', N'ShopId', N'StockQuantity', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[products]'))
        SET IDENTITY_INSERT [dbo].[products] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MechanicId', N'ShopId', N'AssignedAt', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[shop_mechanics]'))
        SET IDENTITY_INSERT [dbo].[shop_mechanics] ON;
    EXEC(N'INSERT INTO [dbo].[shop_mechanics] ([MechanicId], [ShopId], [AssignedAt], [IsActive])
    VALUES (1, 1, ''2026-06-08T00:00:00.0000000Z'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MechanicId', N'ShopId', N'AssignedAt', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[shop_mechanics]'))
        SET IDENTITY_INSERT [dbo].[shop_mechanics] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ShopServiceId', N'BasePrice', N'CategoryId', N'CreatedAt', N'EstimatedMinutes', N'IsActive', N'ServiceDescription', N'ServiceName', N'ShopId') AND [object_id] = OBJECT_ID(N'[dbo].[shop_services]'))
        SET IDENTITY_INSERT [dbo].[shop_services] ON;
    EXEC(N'INSERT INTO [dbo].[shop_services] ([ShopServiceId], [BasePrice], [CategoryId], [CreatedAt], [EstimatedMinutes], [IsActive], [ServiceDescription], [ServiceName], [ShopId])
    VALUES (1, 350.0, 2, ''2026-06-08T00:00:00.0000000Z'', 45, CAST(1 AS bit), N''On-site tire patching and tire check.'', N''Flat Tire Rescue'', 1),
    (2, 500.0, 4, ''2026-06-08T00:00:00.0000000Z'', 60, CAST(1 AS bit), N''Oil replacement and quick fluid inspection.'', N''Basic Oil Change'', 1),
    (3, 700.0, 6, ''2026-06-08T00:00:00.0000000Z'', 40, CAST(1 AS bit), N''Urgent assistance for breakdowns.'', N''Emergency Roadside Help'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ShopServiceId', N'BasePrice', N'CategoryId', N'CreatedAt', N'EstimatedMinutes', N'IsActive', N'ServiceDescription', N'ServiceName', N'ShopId') AND [object_id] = OBJECT_ID(N'[dbo].[shop_services]'))
        SET IDENTITY_INSERT [dbo].[shop_services] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RequestId', N'AcceptedAt', N'CancelledAt', N'ClientId', N'CompletedAt', N'CreatedAt', N'CurrentStatusId', N'EstimatedTotal', N'FinalTotal', N'IssueDescription', N'MechanicId', N'MotorcycleId', N'ScheduledAt', N'ServiceLatitude', N'ServiceLocationAddress', N'ServiceLongitude', N'ShopId', N'ShopServiceId') AND [object_id] = OBJECT_ID(N'[dbo].[service_requests]'))
        SET IDENTITY_INSERT [dbo].[service_requests] ON;
    EXEC(N'INSERT INTO [dbo].[service_requests] ([RequestId], [AcceptedAt], [CancelledAt], [ClientId], [CompletedAt], [CreatedAt], [CurrentStatusId], [EstimatedTotal], [FinalTotal], [IssueDescription], [MechanicId], [MotorcycleId], [ScheduledAt], [ServiceLatitude], [ServiceLocationAddress], [ServiceLongitude], [ShopId], [ShopServiceId])
    VALUES (1, ''2026-06-08T00:10:00.0000000Z'', NULL, 1, NULL, ''2026-06-08T00:00:00.0000000Z'', 6, 350.0, 350.0, N''Rear tire is flat near home.'', 1, 1, ''2026-06-08T10:00:00.0000000Z'', 14.599512, N''Sample customer address, Manila'', 120.984222, 1, 1),
    (2, NULL, NULL, 1, NULL, ''2026-06-08T00:00:00.0000000Z'', 1, 500.0, 0.0, N''Schedule oil change for next week.'', NULL, 1, ''2026-06-15T00:00:00.0000000Z'', 14.599512, N''Sample customer address, Manila'', 120.984222, 1, 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RequestId', N'AcceptedAt', N'CancelledAt', N'ClientId', N'CompletedAt', N'CreatedAt', N'CurrentStatusId', N'EstimatedTotal', N'FinalTotal', N'IssueDescription', N'MechanicId', N'MotorcycleId', N'ScheduledAt', N'ServiceLatitude', N'ServiceLocationAddress', N'ServiceLongitude', N'ShopId', N'ShopServiceId') AND [object_id] = OBJECT_ID(N'[dbo].[service_requests]'))
        SET IDENTITY_INSERT [dbo].[service_requests] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConversationId', N'ConversationType', N'CreatedAt', N'LastMessageAt', N'RequestId') AND [object_id] = OBJECT_ID(N'[dbo].[conversations]'))
        SET IDENTITY_INSERT [dbo].[conversations] ON;
    EXEC(N'INSERT INTO [dbo].[conversations] ([ConversationId], [ConversationType], [CreatedAt], [LastMessageAt], [RequestId])
    VALUES (1, N''service_request'', ''2026-06-08T00:00:00.0000000Z'', ''2026-06-08T00:20:00.0000000Z'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConversationId', N'ConversationType', N'CreatedAt', N'LastMessageAt', N'RequestId') AND [object_id] = OBJECT_ID(N'[dbo].[conversations]'))
        SET IDENTITY_INSERT [dbo].[conversations] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'LiveLocationId', N'AccuracyMeters', N'CreatedAt', N'Latitude', N'Longitude', N'MechanicId', N'RequestId') AND [object_id] = OBJECT_ID(N'[dbo].[live_locations]'))
        SET IDENTITY_INSERT [dbo].[live_locations] ON;
    EXEC(N'INSERT INTO [dbo].[live_locations] ([LiveLocationId], [AccuracyMeters], [CreatedAt], [Latitude], [Longitude], [MechanicId], [RequestId])
    VALUES (1, 8.0, ''2026-06-08T00:25:00.0000000Z'', 14.601, 120.983, 1, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'LiveLocationId', N'AccuracyMeters', N'CreatedAt', N'Latitude', N'Longitude', N'MechanicId', N'RequestId') AND [object_id] = OBJECT_ID(N'[dbo].[live_locations]'))
        SET IDENTITY_INSERT [dbo].[live_locations] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentId', N'Amount', N'CheckoutUrl', N'ClientId', N'CreatedAt', N'Currency', N'PaidAt', N'PaymentMethodId', N'PaymentStatusId', N'ProviderCheckoutSessionId', N'ProviderName', N'ProviderPaymentId', N'ProviderReferenceNumber', N'RequestId', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[payments]'))
        SET IDENTITY_INSERT [dbo].[payments] ON;
    EXEC(N'INSERT INTO [dbo].[payments] ([PaymentId], [Amount], [CheckoutUrl], [ClientId], [CreatedAt], [Currency], [PaidAt], [PaymentMethodId], [PaymentStatusId], [ProviderCheckoutSessionId], [ProviderName], [ProviderPaymentId], [ProviderReferenceNumber], [RequestId], [UpdatedAt])
    VALUES (1, 350.0, N''https://checkout.paymongo.com/test/bikemate-sample'', 1, ''2026-06-08T00:00:00.0000000Z'', N''PHP'', ''2026-06-08T00:30:00.0000000Z'', 1, 3, NULL, N''paymongo'', NULL, N''BM-PAID-0001'', 1, ''2026-06-08T00:30:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PaymentId', N'Amount', N'CheckoutUrl', N'ClientId', N'CreatedAt', N'Currency', N'PaidAt', N'PaymentMethodId', N'PaymentStatusId', N'ProviderCheckoutSessionId', N'ProviderName', N'ProviderPaymentId', N'ProviderReferenceNumber', N'RequestId', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[dbo].[payments]'))
        SET IDENTITY_INSERT [dbo].[payments] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConversationId', N'UserId', N'JoinedAt', N'LastReadAt') AND [object_id] = OBJECT_ID(N'[dbo].[conversation_participants]'))
        SET IDENTITY_INSERT [dbo].[conversation_participants] ON;
    EXEC(N'INSERT INTO [dbo].[conversation_participants] ([ConversationId], [UserId], [JoinedAt], [LastReadAt])
    VALUES (1, 1, ''2026-06-08T00:00:00.0000000Z'', NULL),
    (1, 2, ''2026-06-08T00:00:00.0000000Z'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConversationId', N'UserId', N'JoinedAt', N'LastReadAt') AND [object_id] = OBJECT_ID(N'[dbo].[conversation_participants]'))
        SET IDENTITY_INSERT [dbo].[conversation_participants] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MessageId', N'AttachmentUrl', N'ConversationId', N'CreatedAt', N'MessageText', N'ReadAt', N'SenderUserId') AND [object_id] = OBJECT_ID(N'[dbo].[messages]'))
        SET IDENTITY_INSERT [dbo].[messages] ON;
    EXEC(N'INSERT INTO [dbo].[messages] ([MessageId], [AttachmentUrl], [ConversationId], [CreatedAt], [MessageText], [ReadAt], [SenderUserId])
    VALUES (1, NULL, 1, ''2026-06-08T00:20:00.0000000Z'', N''I am on the way to your location.'', NULL, 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MessageId', N'AttachmentUrl', N'ConversationId', N'CreatedAt', N'MessageText', N'ReadAt', N'SenderUserId') AND [object_id] = OBJECT_ID(N'[dbo].[messages]'))
        SET IDENTITY_INSERT [dbo].[messages] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_audit_logs_ActorUserId] ON [dbo].[audit_logs] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_client_addresses_ClientId] ON [dbo].[client_addresses] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_clients_UserId] ON [dbo].[clients] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_conversation_participants_UserId] ON [dbo].[conversation_participants] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_conversations_RequestId] ON [dbo].[conversations] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_live_locations_MechanicId] ON [dbo].[live_locations] ([MechanicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_live_locations_RequestId] ON [dbo].[live_locations] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_mechanic_availability_MechanicId] ON [dbo].[mechanic_availability] ([MechanicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_mechanics_UserId] ON [dbo].[mechanics] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_messages_ConversationId] ON [dbo].[messages] ([ConversationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_messages_SenderUserId] ON [dbo].[messages] ([SenderUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_motorcycles_ClientId] ON [dbo].[motorcycles] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_notifications_UserId] ON [dbo].[notifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_otp_verifications_UserId] ON [dbo].[otp_verifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_password_reset_tokens_UserId] ON [dbo].[password_reset_tokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_payment_events_PaymentId] ON [dbo].[payment_events] ([PaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_payment_methods_MethodName] ON [dbo].[payment_methods] ([MethodName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_payment_statuses_StatusName] ON [dbo].[payment_statuses] ([StatusName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_payments_ClientId] ON [dbo].[payments] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_payments_PaymentMethodId] ON [dbo].[payments] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_payments_PaymentStatusId] ON [dbo].[payments] ([PaymentStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_payments_RequestId] ON [dbo].[payments] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_product_images_ProductId] ON [dbo].[product_images] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_products_ShopId] ON [dbo].[products] ([ShopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_request_media_RequestId] ON [dbo].[request_media] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_request_status_history_ChangedByUserId] ON [dbo].[request_status_history] ([ChangedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_request_status_history_NewStatusId] ON [dbo].[request_status_history] ([NewStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_request_status_history_OldStatusId] ON [dbo].[request_status_history] ([OldStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_request_status_history_RequestId] ON [dbo].[request_status_history] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_statuses_StatusName] ON [dbo].[request_statuses] ([StatusName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_reviews_ClientId] ON [dbo].[reviews] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_reviews_MechanicId] ON [dbo].[reviews] ([MechanicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reviews_RequestId] ON [dbo].[reviews] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_roles_RoleName] ON [dbo].[roles] ([RoleName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_categories_CategoryName] ON [dbo].[service_categories] ([CategoryName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_images_ShopServiceId] ON [dbo].[service_images] ([ShopServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_ClientId] ON [dbo].[service_requests] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_CurrentStatusId] ON [dbo].[service_requests] ([CurrentStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_MechanicId] ON [dbo].[service_requests] ([MechanicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_MotorcycleId] ON [dbo].[service_requests] ([MotorcycleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_ShopId] ON [dbo].[service_requests] ([ShopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_service_requests_ShopServiceId] ON [dbo].[service_requests] ([ShopServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_shop_mechanics_MechanicId] ON [dbo].[shop_mechanics] ([MechanicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_shop_operating_hours_ShopId] ON [dbo].[shop_operating_hours] ([ShopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_shop_services_CategoryId] ON [dbo].[shop_services] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_shop_services_ShopId] ON [dbo].[shop_services] ([ShopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_shops_OwnerUserId] ON [dbo].[shops] ([OwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_user_auth_providers_ProviderName_ProviderSubject] ON [dbo].[user_auth_providers] ([ProviderName], [ProviderSubject]) WHERE [ProviderSubject] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_user_auth_providers_UserId] ON [dbo].[user_auth_providers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_user_device_tokens_UserId] ON [dbo].[user_device_tokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE INDEX [IX_user_roles_RoleId] ON [dbo].[user_roles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_Email] ON [dbo].[users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260608143854_InitialBikeMateSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260608143854_InitialBikeMateSchema', N'10.0.8');
END;

COMMIT;
GO


/* Runtime compatibility seed for newer module integration routes. */
IF OBJECT_ID(N'[dbo].[request_statuses]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'emergency_pending')
        INSERT INTO [dbo].[request_statuses] ([StatusName]) VALUES (N'emergency_pending');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'searching_responder')
        INSERT INTO [dbo].[request_statuses] ([StatusName]) VALUES (N'searching_responder');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'call_connecting')
        INSERT INTO [dbo].[request_statuses] ([StatusName]) VALUES (N'call_connecting');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'call_connected')
        INSERT INTO [dbo].[request_statuses] ([StatusName]) VALUES (N'call_connected');
END;
GO

IF OBJECT_ID(N'[dbo].[notifications]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_notifications_UserId_IsRead' AND object_id = OBJECT_ID(N'[dbo].[notifications]'))
BEGIN
    CREATE INDEX [IX_notifications_UserId_IsRead] ON [dbo].[notifications] ([UserId], [IsRead]);
END;
GO

IF OBJECT_ID(N'[dbo].[service_requests]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_service_requests_ClientId_CurrentStatusId' AND object_id = OBJECT_ID(N'[dbo].[service_requests]'))
BEGIN
    CREATE INDEX [IX_service_requests_ClientId_CurrentStatusId] ON [dbo].[service_requests] ([ClientId], [CurrentStatusId]);
END;
GO

IF OBJECT_ID(N'[dbo].[live_locations]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_live_locations_RequestId_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[live_locations]'))
BEGIN
    CREATE INDEX [IX_live_locations_RequestId_CreatedAt] ON [dbo].[live_locations] ([RequestId], [CreatedAt]);
END;
GO

PRINT N'BikeMate database setup completed.';
GO
