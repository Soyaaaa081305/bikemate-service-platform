USE [BikeMatesDB_Dev];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

DELETE FROM dbo.audit_logs;
DELETE FROM dbo.payment_events;
DELETE FROM dbo.reviews;
DELETE FROM dbo.request_media;
DELETE FROM dbo.live_locations;
DELETE FROM dbo.request_status_history;
DELETE FROM dbo.messages;
DELETE FROM dbo.conversation_participants;
DELETE FROM dbo.conversations;
DELETE FROM dbo.payments;
DELETE FROM dbo.notifications;
DELETE FROM dbo.service_requests;
DELETE FROM dbo.product_images;
DELETE FROM dbo.products;
DELETE FROM dbo.service_images;
DELETE FROM dbo.shop_mechanics;
DELETE FROM dbo.shop_operating_hours;
DELETE FROM dbo.shop_services;
DELETE FROM dbo.motorcycles;
DELETE FROM dbo.client_addresses;
DELETE FROM dbo.clients;
DELETE FROM dbo.mechanic_availability;
DELETE FROM dbo.mechanics;
DELETE FROM dbo.shops;
DELETE FROM dbo.user_device_tokens;
DELETE FROM dbo.otp_verifications;
DELETE FROM dbo.password_reset_tokens;
DELETE FROM dbo.user_auth_providers;
DELETE FROM dbo.user_roles;
DELETE FROM dbo.users;

DBCC CHECKIDENT ('dbo.audit_logs', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.payment_events', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.reviews', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.request_media', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.live_locations', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.request_status_history', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.messages', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.conversations', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.payments', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.notifications', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.service_requests', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.product_images', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.products', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.service_images', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.shop_operating_hours', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.shop_services', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.motorcycles', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.client_addresses', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.clients', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.mechanic_availability', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.mechanics', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.shops', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.user_device_tokens', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.otp_verifications', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.password_reset_tokens', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.user_auth_providers', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.users', RESEED, 0) WITH NO_INFOMSGS;

DECLARE @PasswordHash nvarchar(500) = N'sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16';
DECLARE @Now datetime2 = SYSUTCDATETIME();

SET IDENTITY_INSERT dbo.users ON;
INSERT INTO dbo.users
    (UserId, FirstName, LastName, Email, PhoneNumber, PasswordHash, ProfileImageUrl, EmailVerified, PhoneVerified, AccountStatus, CreatedAt, UpdatedAt)
VALUES
    (1, N'Admin', N'One', N'admin1@bikemate.test', N'+639170000001', @PasswordHash, N'https://placehold.co/160x160/1f2937/ffffff.png?text=A1', 1, 1, N'active', DATEADD(day, -30, @Now), @Now),
    (2, N'Admin', N'Two', N'admin2@bikemate.test', N'+639170000002', @PasswordHash, N'https://placehold.co/160x160/374151/ffffff.png?text=A2', 1, 1, N'active', DATEADD(day, -29, @Now), @Now),
    (3, N'Admin', N'Pending', N'admin3@bikemate.test', N'+639170000003', @PasswordHash, N'https://placehold.co/160x160/f59e0b/111827.png?text=AP', 1, 1, N'pending', DATEADD(day, -28, @Now), @Now),
    (4, N'Admin', N'Suspended', N'admin4@bikemate.test', N'+639170000004', @PasswordHash, N'https://placehold.co/160x160/dc2626/ffffff.png?text=AS', 1, 1, N'suspended', DATEADD(day, -27, @Now), @Now),
    (5, N'Admin', N'Ops', N'admin5@bikemate.test', N'+639170000005', @PasswordHash, N'https://placehold.co/160x160/0f766e/ffffff.png?text=A5', 1, 1, N'active', DATEADD(day, -26, @Now), @Now),

    (101, N'Cora', N'Active', N'customer1@bikemate.test', N'+639181000001', @PasswordHash, N'https://placehold.co/160x160/2563eb/ffffff.png?text=C1', 1, 1, N'active', DATEADD(day, -20, @Now), @Now),
    (102, N'Pia', N'Pending', N'customer2@bikemate.test', N'+639181000002', @PasswordHash, N'https://placehold.co/160x160/f59e0b/111827.png?text=C2', 0, 1, N'pending', DATEADD(day, -19, @Now), @Now),
    (103, N'Rena', N'Rejected', N'customer3@bikemate.test', N'+639181000003', @PasswordHash, N'https://placehold.co/160x160/dc2626/ffffff.png?text=C3', 1, 1, N'rejected', DATEADD(day, -18, @Now), @Now),
    (104, N'Sam', N'Suspended', N'customer4@bikemate.test', N'+639181000004', @PasswordHash, N'https://placehold.co/160x160/7f1d1d/ffffff.png?text=C4', 1, 1, N'suspended', DATEADD(day, -17, @Now), @Now),
    (105, N'Lia', N'Approved', N'customer5@bikemate.test', N'+639181000005', @PasswordHash, N'https://placehold.co/160x160/16a34a/ffffff.png?text=C5', 1, 1, N'active', DATEADD(day, -16, @Now), @Now),

    (201, N'Omar', N'Budget', N'shop1@bikemate.test', N'+639191000001', @PasswordHash, N'https://placehold.co/160x160/f97316/ffffff.png?text=S1', 1, 1, N'active', DATEADD(day, -15, @Now), @Now),
    (202, N'Maya', N'Standard', N'shop2@bikemate.test', N'+639191000002', @PasswordHash, N'https://placehold.co/160x160/0891b2/ffffff.png?text=S2', 1, 1, N'active', DATEADD(day, -14, @Now), @Now),
    (203, N'Noel', N'Pro', N'shop3@bikemate.test', N'+639191000003', @PasswordHash, N'https://placehold.co/160x160/4f46e5/ffffff.png?text=S3', 1, 1, N'active', DATEADD(day, -13, @Now), @Now),
    (204, N'Gina', N'Rapid', N'shop4@bikemate.test', N'+639191000004', @PasswordHash, N'https://placehold.co/160x160/15803d/ffffff.png?text=S4', 1, 1, N'active', DATEADD(day, -12, @Now), @Now),
    (205, N'Tony', N'Premium', N'shop5@bikemate.test', N'+639191000005', @PasswordHash, N'https://placehold.co/160x160/be123c/ffffff.png?text=S5', 1, 1, N'active', DATEADD(day, -11, @Now), @Now),

    (301, N'Miko', N'Junior', N'mechanic1@bikemate.test', N'+639201000001', @PasswordHash, N'https://placehold.co/160x160/0ea5e9/ffffff.png?text=M1', 1, 1, N'active', DATEADD(day, -10, @Now), @Now),
    (302, N'Bea', N'Skilled', N'mechanic2@bikemate.test', N'+639201000002', @PasswordHash, N'https://placehold.co/160x160/14b8a6/ffffff.png?text=M2', 1, 1, N'active', DATEADD(day, -9, @Now), @Now),
    (303, N'Rico', N'Senior', N'mechanic3@bikemate.test', N'+639201000003', @PasswordHash, N'https://placehold.co/160x160/8b5cf6/ffffff.png?text=M3', 1, 1, N'active', DATEADD(day, -8, @Now), @Now),
    (304, N'Ella', N'Expert', N'mechanic4@bikemate.test', N'+639201000004', @PasswordHash, N'https://placehold.co/160x160/ec4899/ffffff.png?text=M4', 1, 1, N'active', DATEADD(day, -7, @Now), @Now),
    (305, N'Dan', N'Master', N'mechanic5@bikemate.test', N'+639201000005', @PasswordHash, N'https://placehold.co/160x160/ea580c/ffffff.png?text=M5', 1, 1, N'active', DATEADD(day, -6, @Now), @Now);
SET IDENTITY_INSERT dbo.users OFF;

INSERT INTO dbo.user_roles (UserId, RoleId, AssignedAt)
SELECT UserId, 4, @Now FROM dbo.users WHERE UserId BETWEEN 1 AND 5
UNION ALL SELECT UserId, 1, @Now FROM dbo.users WHERE UserId BETWEEN 101 AND 105
UNION ALL SELECT UserId, 3, @Now FROM dbo.users WHERE UserId BETWEEN 201 AND 205
UNION ALL SELECT UserId, 2, @Now FROM dbo.users WHERE UserId BETWEEN 301 AND 305;

SET IDENTITY_INSERT dbo.clients ON;
INSERT INTO dbo.clients (ClientId, UserId, MiddleName, Sex, Birthdate, ValidIdImageUrl, CreatedAt)
VALUES
    (101, 101, N'Demo', N'Female', '1996-01-15', N'https://placehold.co/480x300/e0f2fe/075985.png?text=ID+C1', DATEADD(day, -20, @Now)),
    (102, 102, N'Demo', N'Female', '1997-02-16', N'https://placehold.co/480x300/fef3c7/92400e.png?text=ID+C2', DATEADD(day, -19, @Now)),
    (103, 103, N'Demo', N'Female', '1998-03-17', N'https://placehold.co/480x300/fee2e2/991b1b.png?text=ID+C3', DATEADD(day, -18, @Now)),
    (104, 104, N'Demo', N'Male', '1995-04-18', N'https://placehold.co/480x300/f3f4f6/374151.png?text=ID+C4', DATEADD(day, -17, @Now)),
    (105, 105, N'Demo', N'Female', '1999-05-19', N'https://placehold.co/480x300/dcfce7/166534.png?text=ID+C5', DATEADD(day, -16, @Now));
SET IDENTITY_INSERT dbo.clients OFF;

SET IDENTITY_INSERT dbo.client_addresses ON;
INSERT INTO dbo.client_addresses (AddressId, ClientId, Label, AddressLine, Barangay, City, Province, PostalCode, Latitude, Longitude, IsDefault, CreatedAt)
VALUES
    (101, 101, N'Home', N'Pacita Avenue, San Pedro', N'Pacita 1', N'San Pedro', N'Laguna', N'4023', 14.34290000, 121.06010000, 1, @Now),
    (102, 102, N'Home', N'United Bayanihan, San Pedro', N'United Bayanihan', N'San Pedro', N'Laguna', N'4023', 14.34920000, 121.06550000, 1, @Now),
    (103, 103, N'Home', N'Magsaysay Road, San Pedro', N'Magsaysay', N'San Pedro', N'Laguna', N'4023', 14.35180000, 121.05360000, 1, @Now),
    (104, 104, N'Home', N'Calendola Village, San Pedro', N'Calendola', N'San Pedro', N'Laguna', N'4023', 14.33370000, 121.04890000, 1, @Now),
    (105, 105, N'Home', N'Southwoods Avenue, Binan', N'San Francisco', N'Binan', N'Laguna', N'4024', 14.32840000, 121.07420000, 1, @Now);
SET IDENTITY_INSERT dbo.client_addresses OFF;

SET IDENTITY_INSERT dbo.motorcycles ON;
INSERT INTO dbo.motorcycles (MotorcycleId, ClientId, Brand, Model, YearModel, PlateNumber, EngineType, Color, MotorcycleImageUrl, CreatedAt)
VALUES
    (101, 101, N'Honda', N'Click 125i', 2021, N'ABC-1101', N'125cc Scooter', N'Red', N'https://placehold.co/640x420/fef2f2/991b1b.png?text=Honda+Click', @Now),
    (102, 102, N'Yamaha', N'Mio i125', 2020, N'BCD-2202', N'125cc Scooter', N'Blue', N'https://placehold.co/640x420/eff6ff/1d4ed8.png?text=Yamaha+Mio', @Now),
    (103, 103, N'Suzuki', N'Raider 150', 2022, N'CDE-3303', N'150cc Underbone', N'Black', N'https://placehold.co/640x420/f3f4f6/111827.png?text=Raider+150', @Now),
    (104, 104, N'Kawasaki', N'Rouser NS160', 2019, N'DEF-4404', N'160cc Manual', N'Gray', N'https://placehold.co/640x420/e5e7eb/374151.png?text=Rouser', @Now),
    (105, 105, N'Honda', N'Beat', 2023, N'EFG-5505', N'110cc Scooter', N'White', N'https://placehold.co/640x420/f8fafc/0f172a.png?text=Honda+Beat', @Now);
SET IDENTITY_INSERT dbo.motorcycles OFF;

SET IDENTITY_INSERT dbo.mechanics ON;
INSERT INTO dbo.mechanics
    (MechanicId, UserId, MiddleName, Sex, Birthdate, ValidIdImageUrl, AddressLine, Barangay, City, Province, ZipCode, Bio, YearsExperience, CertificationImageUrl, IsVerified, AvailabilityStatus, CurrentLatitude, CurrentLongitude, AverageRating, TotalCompletedJobs, CreatedAt, UpdatedAt)
VALUES
    (301, 301, N'Demo', N'Male', '2001-01-11', N'https://placehold.co/480x300/e0f2fe/075985.png?text=ID+M1', N'Pacita Complex', N'Pacita 1', N'San Pedro', N'Laguna', N'4023', N'Level 1 junior mechanic for quick tire and chain service.', 1, N'https://placehold.co/480x300/ecfeff/155e75.png?text=NCII+M1', 1, N'online', 14.34410000, 121.06140000, 3.80, 4, DATEADD(day, -10, @Now), @Now),
    (302, 302, N'Demo', N'Female', '1999-02-12', N'https://placehold.co/480x300/ccfbf1/115e59.png?text=ID+M2', N'Magsaysay Road', N'Magsaysay', N'San Pedro', N'Laguna', N'4023', N'Level 2 skilled mechanic for brakes and preventive maintenance.', 3, N'https://placehold.co/480x300/ccfbf1/115e59.png?text=NCII+M2', 1, N'online', 14.35130000, 121.05480000, 4.20, 11, DATEADD(day, -9, @Now), @Now),
    (303, 303, N'Demo', N'Male', '1995-03-13', N'https://placehold.co/480x300/ede9fe/5b21b6.png?text=ID+M3', N'United Bayanihan', N'United Bayanihan', N'San Pedro', N'Laguna', N'4023', N'Level 3 senior mechanic for engine and drivetrain diagnostics.', 6, N'https://placehold.co/480x300/ede9fe/5b21b6.png?text=NCII+M3', 1, N'busy', 14.34820000, 121.06640000, 4.60, 24, DATEADD(day, -8, @Now), @Now),
    (304, 304, N'Demo', N'Female', '1993-04-14', N'https://placehold.co/480x300/fce7f3/9d174d.png?text=ID+M4', N'Southwoods Avenue', N'San Francisco', N'Binan', N'Laguna', N'4024', N'Level 4 expert mechanic for electrical and modification work.', 9, N'https://placehold.co/480x300/fce7f3/9d174d.png?text=NCII+M4', 1, N'offline', 14.32980000, 121.07280000, 4.80, 37, DATEADD(day, -7, @Now), @Now),
    (305, 305, N'Demo', N'Male', '1989-05-15', N'https://placehold.co/480x300/ffedd5/9a3412.png?text=ID+M5', N'Calendola Village', N'Calendola', N'San Pedro', N'Laguna', N'4023', N'Level 5 master mechanic for full workshop repair and rescue jobs.', 14, N'https://placehold.co/480x300/ffedd5/9a3412.png?text=NCII+M5', 1, N'online', 14.33450000, 121.04960000, 5.00, 58, DATEADD(day, -6, @Now), @Now);
SET IDENTITY_INSERT dbo.mechanics OFF;

SET IDENTITY_INSERT dbo.mechanic_availability ON;
INSERT INTO dbo.mechanic_availability (AvailabilityId, MechanicId, DayOfWeek, StartTime, EndTime, IsActive)
SELECT (MechanicId - 300) * 10 + DayOfWeek, MechanicId, DayOfWeek, '08:00', '17:00', 1
FROM (VALUES (301), (302), (303), (304), (305)) AS m(MechanicId)
CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6)) AS d(DayOfWeek);
SET IDENTITY_INSERT dbo.mechanic_availability OFF;

SET IDENTITY_INSERT dbo.shops ON;
INSERT INTO dbo.shops
    (ShopId, OwnerUserId, ShopName, ShopDescription, AddressLine, City, Province, Latitude, Longitude, BusinessPermitUrl, ShopImageUrl, ShopLogoUrl, OwnerValidIdUrl, OwnerMiddleName, OwnerSex, OwnerBirthdate, OwnerAddressLine, OwnerBarangay, OwnerCity, OwnerProvince, OwnerZipCode, ContactNumber, ShopStatus, CreatedAt, UpdatedAt)
VALUES
    (201, 201, N'Level 1 Budget Bike Care', N'Budget-friendly verified shop for simple repairs and commuter scooters.', N'Pacita Avenue', N'San Pedro', N'Laguna', 14.34360000, 121.06070000, N'https://placehold.co/480x300/fff7ed/9a3412.png?text=Permit+S1', N'https://placehold.co/900x540/fff7ed/9a3412.png?text=Budget+Bike+Care', N'https://placehold.co/240x240/f97316/ffffff.png?text=L1', N'https://placehold.co/480x300/fff7ed/9a3412.png?text=Owner+ID+S1', N'Demo', N'Male', '1988-06-10', N'Pacita Avenue', N'Pacita 1', N'San Pedro', N'Laguna', N'4023', N'+639191000001', N'verified', DATEADD(day, -15, @Now), @Now),
    (202, 202, N'Level 2 Standard Moto Hub', N'Standard repair shop with balanced pricing, parts, and roadside support.', N'Magsaysay Road', N'San Pedro', N'Laguna', 14.35100000, 121.05520000, N'https://placehold.co/480x300/ecfeff/155e75.png?text=Permit+S2', N'https://placehold.co/900x540/ecfeff/155e75.png?text=Standard+Moto+Hub', N'https://placehold.co/240x240/0891b2/ffffff.png?text=L2', N'https://placehold.co/480x300/ecfeff/155e75.png?text=Owner+ID+S2', N'Demo', N'Female', '1990-07-11', N'Magsaysay Road', N'Magsaysay', N'San Pedro', N'Laguna', N'4023', N'+639191000002', N'verified', DATEADD(day, -14, @Now), @Now),
    (203, 203, N'Level 3 Pro Performance Garage', N'Pro-level diagnostics, engine work, and performance tuning.', N'United Bayanihan', N'San Pedro', N'Laguna', 14.34860000, 121.06590000, N'https://placehold.co/480x300/e0e7ff/3730a3.png?text=Permit+S3', N'https://placehold.co/900x540/e0e7ff/3730a3.png?text=Pro+Garage', N'https://placehold.co/240x240/4f46e5/ffffff.png?text=L3', N'https://placehold.co/480x300/e0e7ff/3730a3.png?text=Owner+ID+S3', N'Demo', N'Male', '1986-08-12', N'United Bayanihan', N'United Bayanihan', N'San Pedro', N'Laguna', N'4023', N'+639191000003', N'verified', DATEADD(day, -13, @Now), @Now),
    (204, 204, N'Level 4 Rapid Roadside Works', N'Rapid-response shop focused on urgent roadside help and pickup repairs.', N'Calendola Village', N'San Pedro', N'Laguna', 14.33390000, 121.04920000, N'https://placehold.co/480x300/dcfce7/166534.png?text=Permit+S4', N'https://placehold.co/900x540/dcfce7/166534.png?text=Rapid+Roadside', N'https://placehold.co/240x240/15803d/ffffff.png?text=L4', N'https://placehold.co/480x300/dcfce7/166534.png?text=Owner+ID+S4', N'Demo', N'Female', '1991-09-13', N'Calendola Village', N'Calendola', N'San Pedro', N'Laguna', N'4023', N'+639191000004', N'verified', DATEADD(day, -12, @Now), @Now),
    (205, 205, N'Level 5 Premium Cycle Lab', N'Premium workshop for full service, electrical upgrades, and custom modifications.', N'Southwoods Avenue', N'Binan', N'Laguna', 14.32890000, 121.07350000, N'https://placehold.co/480x300/fce7f3/9d174d.png?text=Permit+S5', N'https://placehold.co/900x540/fce7f3/9d174d.png?text=Premium+Cycle+Lab', N'https://placehold.co/240x240/be123c/ffffff.png?text=L5', N'https://placehold.co/480x300/fce7f3/9d174d.png?text=Owner+ID+S5', N'Demo', N'Male', '1984-10-14', N'Southwoods Avenue', N'San Francisco', N'Binan', N'Laguna', N'4024', N'+639191000005', N'verified', DATEADD(day, -11, @Now), @Now);
SET IDENTITY_INSERT dbo.shops OFF;

SET IDENTITY_INSERT dbo.shop_operating_hours ON;
INSERT INTO dbo.shop_operating_hours (OperatingHourId, ShopId, DayOfWeek, OpeningTime, ClosingTime, IsClosed)
SELECT (ShopId - 200) * 10 + DayOfWeek, ShopId, DayOfWeek, '08:00', '18:00', CASE WHEN DayOfWeek = 0 THEN 1 ELSE 0 END
FROM (VALUES (201), (202), (203), (204), (205)) AS s(ShopId)
CROSS JOIN (VALUES (0), (1), (2), (3), (4), (5), (6)) AS d(DayOfWeek);
SET IDENTITY_INSERT dbo.shop_operating_hours OFF;

INSERT INTO dbo.shop_mechanics (ShopId, MechanicId, AssignedAt, IsActive)
VALUES
    (201, 301, @Now, 1), (202, 302, @Now, 1), (203, 303, @Now, 1), (204, 304, @Now, 1), (205, 305, @Now, 1),
    (201, 302, @Now, 1), (202, 303, @Now, 1), (203, 304, @Now, 1), (204, 305, @Now, 1), (205, 301, @Now, 1);

SET IDENTITY_INSERT dbo.shop_services ON;
INSERT INTO dbo.shop_services (ShopServiceId, ShopId, CategoryId, ServiceName, ServiceDescription, BasePrice, EstimatedMinutes, IsActive, CreatedAt)
SELECT
    ((s.ShopId - 200) * 10) + v.LevelNo,
    s.ShopId,
    v.CategoryId,
    CONCAT(N'Level ', v.LevelNo, N' ', v.ServiceName),
    CONCAT(v.ServiceDescription, N' Offered by ', sh.ShopName, N'.'),
    v.BasePrice + ((s.ShopId - 201) * 80),
    v.EstimatedMinutes + ((s.ShopId - 201) * 5),
    1,
    @Now
FROM (VALUES (201), (202), (203), (204), (205)) AS s(ShopId)
INNER JOIN dbo.shops sh ON sh.ShopId = s.ShopId
CROSS JOIN (VALUES
    (1, 2, N'Tire Patch and Pressure Check', N'Fast tire inspection, patching, and pressure balancing.', 180.00, 25),
    (2, 5, N'Brake Cleaning and Adjustment', N'Brake pad check, cable tuning, and road-safe adjustment.', 260.00, 35),
    (3, 4, N'Oil Change and Fluid Check', N'Engine oil replacement plus quick fluid inspection.', 390.00, 45),
    (4, 8, N'Chain and Sprocket Service', N'Chain cleaning, tensioning, and sprocket wear inspection.', 480.00, 55),
    (5, 9, N'Electrical Accessory Install', N'Clean install for approved lights, chargers, and basic wiring.', 620.00, 70)
) AS v(LevelNo, CategoryId, ServiceName, ServiceDescription, BasePrice, EstimatedMinutes);
SET IDENTITY_INSERT dbo.shop_services OFF;

SET IDENTITY_INSERT dbo.products ON;
INSERT INTO dbo.products (ProductId, ShopId, ProductName, ProductDescription, Price, StockQuantity, IsActive, CreatedAt, UpdatedAt)
SELECT
    ((s.ShopId - 200) * 10) + v.LevelNo,
    s.ShopId,
    CONCAT(N'Level ', v.LevelNo, N' ', v.ProductName),
    CONCAT(v.ProductDescription, N' Stocked by ', sh.ShopName, N'.'),
    v.Price + ((s.ShopId - 201) * 100),
    v.StockQuantity,
    1,
    @Now,
    @Now
FROM (VALUES (201), (202), (203), (204), (205)) AS s(ShopId)
INNER JOIN dbo.shops sh ON sh.ShopId = s.ShopId
CROSS JOIN (VALUES
    (1, N'Inner Tube', N'Budget replacement tube for common scooter tire sizes.', 180.00, 10),
    (2, N'Brake Pad Set', N'Standard brake pad set for daily commuter motorcycles.', 320.00, 8),
    (3, N'Semi-Synthetic Oil', N'One liter semi-synthetic oil for tune-up service.', 420.00, 7),
    (4, N'Chain Lube Kit', N'Chain cleaner and lubricant bundle.', 560.00, 5),
    (5, N'LED Auxiliary Light', N'Premium compact auxiliary light kit.', 980.00, 3)
) AS v(LevelNo, ProductName, ProductDescription, Price, StockQuantity);
SET IDENTITY_INSERT dbo.products OFF;

SET IDENTITY_INSERT dbo.product_images ON;
INSERT INTO dbo.product_images (ProductImageId, ProductId, ImageUrl, CreatedAt)
SELECT ProductId, ProductId, CONCAT(N'https://placehold.co/640x420/f8fafc/0f172a.png?text=Product+', ProductId), @Now
FROM dbo.products;
SET IDENTITY_INSERT dbo.product_images OFF;

SET IDENTITY_INSERT dbo.service_images ON;
INSERT INTO dbo.service_images (ServiceImageId, ShopServiceId, ImageUrl, CreatedAt)
SELECT ShopServiceId, ShopServiceId, CONCAT(N'https://placehold.co/640x420/fff7ed/9a3412.png?text=Service+', ShopServiceId), @Now
FROM dbo.shop_services;
SET IDENTITY_INSERT dbo.service_images OFF;

SET IDENTITY_INSERT dbo.service_requests ON;
INSERT INTO dbo.service_requests
    (RequestId, ClientId, ShopId, ShopServiceId, MechanicId, CurrentStatusId, MotorcycleId, IssueDescription, ServiceLocationAddress, ServiceLatitude, ServiceLongitude, ScheduledAt, CreatedAt, AcceptedAt, CompletedAt, CancelledAt, EstimatedTotal, FinalTotal)
VALUES
    (1, 101, 201, 11, 301, 12, 101, N'Booking type: Repair. Tire keeps losing pressure. Assistance method: On-site Repair.', N'Pacita Avenue, San Pedro', 14.34290000, 121.06010000, NULL, DATEADD(hour, -10, @Now), NULL, NULL, NULL, 180.00, 180.00),
    (2, 102, 202, 22, 302, 1, 102, N'Booking type: Repair. Brake lever feels loose. Assistance method: Pick-up Repair.', N'United Bayanihan, San Pedro', 14.34920000, 121.06550000, NULL, DATEADD(hour, -8, @Now), NULL, NULL, NULL, 340.00, 340.00),
    (3, 103, 203, 33, 303, 2, 103, N'Booking type: Reservation. Oil change and inspection appointment.', N'Magsaysay Road, San Pedro', 14.35180000, 121.05360000, DATEADD(day, 1, @Now), DATEADD(hour, -6, @Now), DATEADD(hour, -5, @Now), NULL, NULL, 550.00, 550.00),
    (4, 104, 204, 44, 304, 4, 104, N'Booking type: Repair. Chain noise during acceleration. Assistance method: On-site Repair.', N'Calendola Village, San Pedro', 14.33370000, 121.04890000, NULL, DATEADD(hour, -4, @Now), DATEADD(hour, -3, @Now), NULL, NULL, 720.00, 720.00),
    (5, 105, 205, 55, 305, 6, 105, N'Booking type: Modification. Install auxiliary light kit. Assistance method: Reservation.', N'Southwoods Avenue, Binan', 14.32840000, 121.07420000, DATEADD(day, 2, @Now), DATEADD(hour, -2, @Now), DATEADD(hour, -2, @Now), NULL, NULL, 940.00, 1920.00),
    (6, 101, 201, 11, 301, 7, 101, N'Completed tire patch demo review level 1.', N'Pacita Avenue, San Pedro', 14.34290000, 121.06010000, DATEADD(day, -5, @Now), DATEADD(day, -6, @Now), DATEADD(day, -6, @Now), DATEADD(day, -5, @Now), NULL, 180.00, 180.00),
    (7, 102, 202, 22, 302, 7, 102, N'Completed brake service demo review level 2.', N'United Bayanihan, San Pedro', 14.34920000, 121.06550000, DATEADD(day, -4, @Now), DATEADD(day, -5, @Now), DATEADD(day, -5, @Now), DATEADD(day, -4, @Now), NULL, 340.00, 340.00),
    (8, 103, 203, 33, 303, 7, 103, N'Completed oil change demo review level 3.', N'Magsaysay Road, San Pedro', 14.35180000, 121.05360000, DATEADD(day, -3, @Now), DATEADD(day, -4, @Now), DATEADD(day, -4, @Now), DATEADD(day, -3, @Now), NULL, 550.00, 550.00),
    (9, 104, 204, 44, 304, 7, 104, N'Completed chain service demo review level 4.', N'Calendola Village, San Pedro', 14.33370000, 121.04890000, DATEADD(day, -2, @Now), DATEADD(day, -3, @Now), DATEADD(day, -3, @Now), DATEADD(day, -2, @Now), NULL, 720.00, 720.00),
    (10, 105, 205, 55, 305, 7, 105, N'Completed electrical install demo review level 5.', N'Southwoods Avenue, Binan', 14.32840000, 121.07420000, DATEADD(day, -1, @Now), DATEADD(day, -2, @Now), DATEADD(day, -2, @Now), DATEADD(day, -1, @Now), NULL, 940.00, 1920.00);
SET IDENTITY_INSERT dbo.service_requests OFF;

SET IDENTITY_INSERT dbo.request_status_history ON;
INSERT INTO dbo.request_status_history (StatusHistoryId, RequestId, OldStatusId, NewStatusId, ChangedByUserId, Notes, CreatedAt)
SELECT RequestId, RequestId, NULL, CurrentStatusId, NULL, N'Seeded demo status.', CreatedAt
FROM dbo.service_requests;
SET IDENTITY_INSERT dbo.request_status_history OFF;

SET IDENTITY_INSERT dbo.request_media ON;
INSERT INTO dbo.request_media (RequestMediaId, RequestId, MediaUrl, MediaType, Caption, CreatedAt)
SELECT RequestId, RequestId, CONCAT(N'https://placehold.co/800x520/e5e7eb/111827.png?text=Request+', RequestId), N'image', N'Demo request photo', CreatedAt
FROM dbo.service_requests;
SET IDENTITY_INSERT dbo.request_media OFF;

SET IDENTITY_INSERT dbo.live_locations ON;
INSERT INTO dbo.live_locations (LiveLocationId, RequestId, MechanicId, Latitude, Longitude, AccuracyMeters, CreatedAt)
VALUES
    (1, 3, 303, 14.34990000, 121.05990000, 8.00, DATEADD(minute, -18, @Now)),
    (2, 4, 304, 14.33750000, 121.05140000, 6.00, DATEADD(minute, -8, @Now)),
    (3, NULL, 301, 14.34410000, 121.06140000, 12.00, DATEADD(minute, -3, @Now)),
    (4, NULL, 302, 14.35130000, 121.05480000, 10.00, DATEADD(minute, -4, @Now)),
    (5, NULL, 305, 14.33450000, 121.04960000, 11.00, DATEADD(minute, -5, @Now));
SET IDENTITY_INSERT dbo.live_locations OFF;

SET IDENTITY_INSERT dbo.payments ON;
INSERT INTO dbo.payments
    (PaymentId, RequestId, ClientId, PaymentStatusId, PaymentMethodId, Amount, Currency, ProviderName, ProviderCheckoutSessionId, ProviderPaymentId, ProviderReferenceNumber, CheckoutUrl, PaidAt, CreatedAt, UpdatedAt)
VALUES
    (2, 2, 102, 3, 2, 340.00, N'PHP', N'paymongo', N'demo_checkout_002', N'demo_payment_002', N'BM-PAY-002', N'https://checkout.test/002', DATEADD(hour, -7, @Now), DATEADD(hour, -8, @Now), @Now),
    (3, 3, 103, 3, 3, 550.00, N'PHP', N'paymongo', N'demo_checkout_003', N'demo_payment_003', N'BM-PAY-003', N'https://checkout.test/003', DATEADD(hour, -5, @Now), DATEADD(hour, -6, @Now), @Now),
    (4, 4, 104, 3, 1, 720.00, N'PHP', N'paymongo', N'demo_checkout_004', N'demo_payment_004', N'BM-PAY-004', N'https://checkout.test/004', DATEADD(hour, -3, @Now), DATEADD(hour, -4, @Now), @Now),
    (6, 6, 101, 3, 1, 180.00, N'PHP', N'paymongo', N'demo_checkout_006', N'demo_payment_006', N'BM-PAY-006', N'https://checkout.test/006', DATEADD(day, -5, @Now), DATEADD(day, -6, @Now), @Now),
    (7, 7, 102, 3, 2, 340.00, N'PHP', N'paymongo', N'demo_checkout_007', N'demo_payment_007', N'BM-PAY-007', N'https://checkout.test/007', DATEADD(day, -4, @Now), DATEADD(day, -5, @Now), @Now),
    (8, 8, 103, 3, 3, 550.00, N'PHP', N'paymongo', N'demo_checkout_008', N'demo_payment_008', N'BM-PAY-008', N'https://checkout.test/008', DATEADD(day, -3, @Now), DATEADD(day, -4, @Now), @Now),
    (9, 9, 104, 3, 1, 720.00, N'PHP', N'paymongo', N'demo_checkout_009', N'demo_payment_009', N'BM-PAY-009', N'https://checkout.test/009', DATEADD(day, -2, @Now), DATEADD(day, -3, @Now), @Now),
    (10, 10, 105, 3, 4, 1920.00, N'PHP', N'paymongo', N'demo_checkout_010', N'demo_payment_010', N'BM-PAY-010', N'https://checkout.test/010', DATEADD(day, -1, @Now), DATEADD(day, -2, @Now), @Now);
SET IDENTITY_INSERT dbo.payments OFF;

SET IDENTITY_INSERT dbo.reviews ON;
INSERT INTO dbo.reviews (ReviewId, RequestId, ClientId, MechanicId, Rating, Comment, CreatedAt)
VALUES
    (1, 6, 101, 301, 1, N'Level 1 review: repair finished, but the timing can improve.', DATEADD(day, -5, @Now)),
    (2, 7, 102, 302, 2, N'Level 2 review: service was okay and the mechanic explained the brake issue.', DATEADD(day, -4, @Now)),
    (3, 8, 103, 303, 3, N'Level 3 review: solid oil change and inspection.', DATEADD(day, -3, @Now)),
    (4, 9, 104, 304, 4, N'Level 4 review: fast chain service and clear updates.', DATEADD(day, -2, @Now)),
    (5, 10, 105, 305, 5, N'Level 5 review: excellent premium install and clean wiring.', DATEADD(day, -1, @Now));
SET IDENTITY_INSERT dbo.reviews OFF;

SET IDENTITY_INSERT dbo.conversations ON;
INSERT INTO dbo.conversations (ConversationId, RequestId, ConversationType, CreatedAt, LastMessageAt)
SELECT RequestId, RequestId, N'service_request', CreatedAt, DATEADD(minute, 15, CreatedAt)
FROM dbo.service_requests
WHERE RequestId BETWEEN 1 AND 5;
SET IDENTITY_INSERT dbo.conversations OFF;

INSERT INTO dbo.conversation_participants (ConversationId, UserId, JoinedAt, LastReadAt)
VALUES
    (1, 101, @Now, @Now), (1, 301, @Now, NULL), (1, 201, @Now, NULL),
    (2, 102, @Now, @Now), (2, 302, @Now, NULL), (2, 202, @Now, NULL),
    (3, 103, @Now, @Now), (3, 303, @Now, NULL), (3, 203, @Now, NULL),
    (4, 104, @Now, @Now), (4, 304, @Now, NULL), (4, 204, @Now, NULL),
    (5, 105, @Now, @Now), (5, 305, @Now, NULL), (5, 205, @Now, NULL);

SET IDENTITY_INSERT dbo.messages ON;
INSERT INTO dbo.messages (MessageId, ConversationId, SenderUserId, MessageText, AttachmentUrl, CreatedAt, ReadAt)
VALUES
    (1, 1, 101, N'Hi, the tire keeps losing air near Pacita.', NULL, DATEADD(hour, -10, @Now), @Now),
    (2, 1, 301, N'I can check the valve and tube once payment clears.', NULL, DATEADD(hour, -9, @Now), NULL),
    (3, 2, 202, N'We received your pickup repair request.', NULL, DATEADD(hour, -8, @Now), @Now),
    (4, 2, 102, N'Thank you, the bike is at the guard house.', NULL, DATEADD(hour, -7, @Now), NULL),
    (5, 3, 303, N'Reservation accepted. Please arrive ten minutes early.', NULL, DATEADD(hour, -5, @Now), @Now),
    (6, 4, 304, N'I am on the way. You can track my location now.', NULL, DATEADD(hour, -3, @Now), NULL),
    (7, 5, 205, N'Electrical install request is queued for workshop confirmation.', NULL, DATEADD(hour, -1, @Now), NULL);
SET IDENTITY_INSERT dbo.messages OFF;

SET IDENTITY_INSERT dbo.notifications ON;
INSERT INTO dbo.notifications (NotificationId, UserId, NotificationType, Title, Message, DataJson, IsRead, CreatedAt)
VALUES
    (1, 101, N'booking', N'Payment pending', N'Complete checkout for BM-000001.', N'{"requestId":1}', 0, DATEADD(hour, -10, @Now)),
    (2, 102, N'approval', N'Account pending', N'Your customer account is waiting for OTP/admin approval.', N'{"clientId":102}', 0, DATEADD(day, -19, @Now)),
    (3, 301, N'job', N'New tire request', N'You have a level 1 service request.', N'{"requestId":1}', 0, DATEADD(hour, -9, @Now)),
    (4, 304, N'job', N'Go to customer', N'Location tracking is active for BM-000004.', N'{"requestId":4}', 0, DATEADD(hour, -3, @Now)),
    (5, 205, N'shop', N'Premium booking', N'A level 5 modification request is waiting.', N'{"requestId":5}', 0, DATEADD(hour, -1, @Now));
SET IDENTITY_INSERT dbo.notifications OFF;

COMMIT TRANSACTION;

SELECT 'users' AS [table], COUNT(*) AS [rows] FROM dbo.users
UNION ALL SELECT 'customers', COUNT(*) FROM dbo.clients
UNION ALL SELECT 'shops', COUNT(*) FROM dbo.shops
UNION ALL SELECT 'mechanics', COUNT(*) FROM dbo.mechanics
UNION ALL SELECT 'shop_services', COUNT(*) FROM dbo.shop_services
UNION ALL SELECT 'products', COUNT(*) FROM dbo.products
UNION ALL SELECT 'service_requests', COUNT(*) FROM dbo.service_requests
UNION ALL SELECT 'reviews', COUNT(*) FROM dbo.reviews
UNION ALL SELECT 'payments', COUNT(*) FROM dbo.payments;

SELECT Email, AccountStatus FROM dbo.users ORDER BY UserId;

