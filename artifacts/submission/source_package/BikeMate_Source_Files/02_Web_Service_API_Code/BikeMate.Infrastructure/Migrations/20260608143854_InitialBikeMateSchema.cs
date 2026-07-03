using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBikeMateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "dbo",
                columns: table => new
                {
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.PaymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "payment_statuses",
                schema: "dbo",
                columns: table => new
                {
                    PaymentStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_statuses", x => x.PaymentStatusId);
                });

            migrationBuilder.CreateTable(
                name: "request_statuses",
                schema: "dbo",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_statuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                schema: "dbo",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "dbo",
                columns: table => new
                {
                    AuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                schema: "dbo",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_clients_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mechanics",
                schema: "dbo",
                columns: table => new
                {
                    MechanicId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearsExperience = table.Column<int>(type: "int", nullable: true),
                    CertificationImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "offline"),
                    CurrentLatitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    CurrentLongitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalCompletedJobs = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mechanics", x => x.MechanicId);
                    table.ForeignKey(
                        name: "FK_mechanics_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "dbo",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                schema: "dbo",
                columns: table => new
                {
                    OtpId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "email_verification"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_verifications", x => x.OtpId);
                    table.ForeignKey(
                        name: "FK_otp_verifications_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "dbo",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shops",
                schema: "dbo",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    ShopName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShopDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    BusinessPermitUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShopImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShopStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shops", x => x.ShopId);
                    table.ForeignKey(
                        name: "FK_shops_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_providers",
                schema: "dbo",
                columns: table => new
                {
                    AuthProviderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderSubject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth_providers", x => x.AuthProviderId);
                    table.ForeignKey(
                        name: "FK_user_auth_providers_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_device_tokens",
                schema: "dbo",
                columns: table => new
                {
                    DeviceTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "android"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_device_tokens", x => x.DeviceTokenId);
                    table.ForeignKey(
                        name: "FK_user_device_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_addresses",
                schema: "dbo",
                columns: table => new
                {
                    AddressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_addresses", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_client_addresses_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motorcycles",
                schema: "dbo",
                columns: table => new
                {
                    MotorcycleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    YearModel = table.Column<int>(type: "int", nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EngineType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MotorcycleImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorcycles", x => x.MotorcycleId);
                    table.ForeignKey(
                        name: "FK_motorcycles_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mechanic_availability",
                schema: "dbo",
                columns: table => new
                {
                    AvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MechanicId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mechanic_availability", x => x.AvailabilityId);
                    table.ForeignKey(
                        name: "FK_mechanic_availability_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalSchema: "dbo",
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "dbo",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_products_shops_ShopId",
                        column: x => x.ShopId,
                        principalSchema: "dbo",
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_mechanics",
                schema: "dbo",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    MechanicId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_mechanics", x => new { x.ShopId, x.MechanicId });
                    table.ForeignKey(
                        name: "FK_shop_mechanics_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalSchema: "dbo",
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_mechanics_shops_ShopId",
                        column: x => x.ShopId,
                        principalSchema: "dbo",
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_operating_hours",
                schema: "dbo",
                columns: table => new
                {
                    OperatingHourId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpeningTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ClosingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_operating_hours", x => x.OperatingHourId);
                    table.ForeignKey(
                        name: "FK_shop_operating_hours_shops_ShopId",
                        column: x => x.ShopId,
                        principalSchema: "dbo",
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_services",
                schema: "dbo",
                columns: table => new
                {
                    ShopServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_services", x => x.ShopServiceId);
                    table.ForeignKey(
                        name: "FK_shop_services_service_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "service_categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_services_shops_ShopId",
                        column: x => x.ShopId,
                        principalSchema: "dbo",
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "dbo",
                columns: table => new
                {
                    ProductImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.ProductImageId);
                    table.ForeignKey(
                        name: "FK_product_images_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_images",
                schema: "dbo",
                columns: table => new
                {
                    ServiceImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopServiceId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_images", x => x.ServiceImageId);
                    table.ForeignKey(
                        name: "FK_service_images_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalSchema: "dbo",
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                schema: "dbo",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<int>(type: "int", nullable: true),
                    ShopServiceId = table.Column<int>(type: "int", nullable: true),
                    MechanicId = table.Column<int>(type: "int", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    MotorcycleId = table.Column<int>(type: "int", nullable: true),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceLocationAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceLatitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    ServiceLongitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedTotal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FinalTotal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_service_requests_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalSchema: "dbo",
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalSchema: "dbo",
                        principalTable: "motorcycles",
                        principalColumn: "MotorcycleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_request_statuses_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalSchema: "dbo",
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalSchema: "dbo",
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_shops_ShopId",
                        column: x => x.ShopId,
                        principalSchema: "dbo",
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "dbo",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    ConversationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "service_request"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_conversations_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "live_locations",
                schema: "dbo",
                columns: table => new
                {
                    LiveLocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    MechanicId = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    AccuracyMeters = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_locations", x => x.LiveLocationId);
                    table.ForeignKey(
                        name: "FK_live_locations_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalSchema: "dbo",
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_live_locations_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "dbo",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    PaymentStatusId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "PHP"),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "paymongo"),
                    ProviderCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderReferenceNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_payments_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_payment_methods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "dbo",
                        principalTable: "payment_methods",
                        principalColumn: "PaymentMethodId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payments_payment_statuses_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalSchema: "dbo",
                        principalTable: "payment_statuses",
                        principalColumn: "PaymentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_media",
                schema: "dbo",
                columns: table => new
                {
                    RequestMediaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "image"),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_media", x => x.RequestMediaId);
                    table.ForeignKey(
                        name: "FK_request_media_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_status_history",
                schema: "dbo",
                columns: table => new
                {
                    StatusHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: true),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_status_history", x => x.StatusHistoryId);
                    table.ForeignKey(
                        name: "FK_request_status_history_request_statuses_NewStatusId",
                        column: x => x.NewStatusId,
                        principalSchema: "dbo",
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_status_history_request_statuses_OldStatusId",
                        column: x => x.OldStatusId,
                        principalSchema: "dbo",
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_status_history_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_status_history_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                schema: "dbo",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    MechanicId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_reviews_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviews_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalSchema: "dbo",
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviews_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversation_participants",
                schema: "dbo",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_participants", x => new { x.ConversationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_conversation_participants_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "dbo",
                        principalTable: "conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversation_participants_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "dbo",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "dbo",
                        principalTable: "conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                schema: "dbo",
                columns: table => new
                {
                    PaymentEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    ProviderEventId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.PaymentEventId);
                    table.ForeignKey(
                        name: "FK_payment_events_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "dbo",
                        principalTable: "payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "payment_methods",
                columns: new[] { "PaymentMethodId", "MethodName" },
                values: new object[,]
                {
                    { 1, "gcash" },
                    { 2, "maya" },
                    { 3, "card" },
                    { 4, "grab_pay" },
                    { 5, "cash" },
                    { 6, "bank_transfer" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "payment_statuses",
                columns: new[] { "PaymentStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "unpaid" },
                    { 2, "pending" },
                    { 3, "paid" },
                    { 4, "failed" },
                    { 5, "cancelled" },
                    { 6, "refunded" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "request_statuses",
                columns: new[] { "StatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "pending" },
                    { 2, "accepted" },
                    { 3, "rejected" },
                    { 4, "en_route" },
                    { 5, "arrived" },
                    { 6, "in_progress" },
                    { 7, "completed" },
                    { 8, "cancelled" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Customer" },
                    { 2, "Mechanic" },
                    { 3, "ShopAdmin" },
                    { 4, "SystemAdmin" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "service_categories",
                columns: new[] { "CategoryId", "CategoryName", "Description", "IconUrl", "IsActive" },
                values: new object[,]
                {
                    { 1, "Engine Repair", "Engine troubleshooting, repair, and maintenance", null, true },
                    { 2, "Tire Service", "Flat tire repair, tire replacement, and tire checking", null, true },
                    { 3, "Battery Service", "Battery check, replacement, and charging assistance", null, true },
                    { 4, "Oil Change", "Motorcycle oil change and fluid maintenance", null, true },
                    { 5, "Brake Service", "Brake inspection, cleaning, repair, and replacement", null, true },
                    { 6, "Emergency Roadside Assistance", "Urgent roadside motorcycle assistance", null, true }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "users",
                columns: new[] { "UserId", "AccountStatus", "CreatedAt", "Email", "EmailVerified", "FirstName", "LastName", "PasswordHash", "PhoneNumber", "PhoneVerified", "ProfileImageUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "customer@bikemate.test", true, "Juan", "Customer", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639171234567", true, null, null },
                    { 2, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "mechanic@bikemate.test", true, "Rico", "Mechanic", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639171234567", true, null, null },
                    { 3, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "shop@bikemate.test", true, "Maya", "ShopAdmin", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639171234567", true, null, null },
                    { 4, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "admin@bikemate.test", true, "Ana", "Admin", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639171234567", true, null, null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "clients",
                columns: new[] { "ClientId", "CreatedAt", "UserId" },
                values: new object[] { 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "mechanics",
                columns: new[] { "MechanicId", "AvailabilityStatus", "AverageRating", "Bio", "CertificationImageUrl", "CreatedAt", "CurrentLatitude", "CurrentLongitude", "IsVerified", "TotalCompletedJobs", "UpdatedAt", "UserId", "YearsExperience" },
                values: new object[] { 1, "online", 4.80m, "Certified roadside motorcycle technician.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.599512m, 120.984222m, true, 12, null, 2, 5 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "notifications",
                columns: new[] { "NotificationId", "CreatedAt", "DataJson", "IsRead", "Message", "NotificationType", "Title", "UserId" },
                values: new object[] { 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Rico Mechanic accepted your tire service request.", "booking", "Mechanic assigned", 1 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shops",
                columns: new[] { "ShopId", "AddressLine", "BusinessPermitUrl", "City", "ContactNumber", "CreatedAt", "Latitude", "Longitude", "OwnerUserId", "Province", "ShopDescription", "ShopImageUrl", "ShopName", "ShopStatus", "UpdatedAt" },
                values: new object[] { 1, "Sample shop address, Manila", null, "Manila", "+639171234567", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.6042m, 120.9822m, 3, "Metro Manila", "Sample verified repair shop for local testing.", null, "BikeMate Partner Shop", "verified", null });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "user_roles",
                columns: new[] { "RoleId", "UserId", "AssignedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 3, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 4, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "client_addresses",
                columns: new[] { "AddressId", "AddressLine", "City", "ClientId", "CreatedAt", "IsDefault", "Label", "Latitude", "Longitude", "PostalCode", "Province" },
                values: new object[] { 1, "Sample customer address, Manila", "Manila", 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Home", 14.599512m, 120.984222m, "1000", "Metro Manila" });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "motorcycles",
                columns: new[] { "MotorcycleId", "Brand", "ClientId", "Color", "CreatedAt", "EngineType", "Model", "MotorcycleImageUrl", "PlateNumber", "YearModel" },
                values: new object[] { 1, "Honda", 1, "Black", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "125cc", "Click 125i", null, "BM-1234", 2022 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "products",
                columns: new[] { "ProductId", "CreatedAt", "IsActive", "Price", "ProductDescription", "ProductName", "ShopId", "StockQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, 280m, "Sample 10W-40 motorcycle oil.", "Engine Oil 1L", 1, 20, null },
                    { 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, 120m, "Patch kit for tubeless motorcycle tires.", "Tubeless Tire Patch", 1, 50, null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shop_mechanics",
                columns: new[] { "MechanicId", "ShopId", "AssignedAt", "IsActive" },
                values: new object[] { 1, 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shop_services",
                columns: new[] { "ShopServiceId", "BasePrice", "CategoryId", "CreatedAt", "EstimatedMinutes", "IsActive", "ServiceDescription", "ServiceName", "ShopId" },
                values: new object[,]
                {
                    { 1, 350m, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 45, true, "On-site tire patching and tire check.", "Flat Tire Rescue", 1 },
                    { 2, 500m, 4, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Oil replacement and quick fluid inspection.", "Basic Oil Change", 1 },
                    { 3, 700m, 6, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 40, true, "Urgent assistance for breakdowns.", "Emergency Roadside Help", 1 }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "service_requests",
                columns: new[] { "RequestId", "AcceptedAt", "CancelledAt", "ClientId", "CompletedAt", "CreatedAt", "CurrentStatusId", "EstimatedTotal", "FinalTotal", "IssueDescription", "MechanicId", "MotorcycleId", "ScheduledAt", "ServiceLatitude", "ServiceLocationAddress", "ServiceLongitude", "ShopId", "ShopServiceId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 8, 0, 10, 0, 0, DateTimeKind.Utc), null, 1, null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 6, 350m, 350m, "Rear tire is flat near home.", 1, 1, new DateTime(2026, 6, 8, 10, 0, 0, 0, DateTimeKind.Utc), 14.599512m, "Sample customer address, Manila", 120.984222m, 1, 1 },
                    { 2, null, null, 1, null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1, 500m, 0m, "Schedule oil change for next week.", null, 1, new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), 14.599512m, "Sample customer address, Manila", 120.984222m, 1, 2 }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "conversations",
                columns: new[] { "ConversationId", "ConversationType", "CreatedAt", "LastMessageAt", "RequestId" },
                values: new object[] { 1, "service_request", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 8, 0, 20, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "live_locations",
                columns: new[] { "LiveLocationId", "AccuracyMeters", "CreatedAt", "Latitude", "Longitude", "MechanicId", "RequestId" },
                values: new object[] { 1, 8m, new DateTime(2026, 6, 8, 0, 25, 0, 0, DateTimeKind.Utc), 14.6010m, 120.9830m, 1, 1 });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "payments",
                columns: new[] { "PaymentId", "Amount", "CheckoutUrl", "ClientId", "CreatedAt", "Currency", "PaidAt", "PaymentMethodId", "PaymentStatusId", "ProviderCheckoutSessionId", "ProviderName", "ProviderPaymentId", "ProviderReferenceNumber", "RequestId", "UpdatedAt" },
                values: new object[] { 1, 350m, "https://checkout.paymongo.com/test/bikemate-sample", 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", new DateTime(2026, 6, 8, 0, 30, 0, 0, DateTimeKind.Utc), 1, 3, null, "paymongo", null, "BM-PAID-0001", 1, new DateTime(2026, 6, 8, 0, 30, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "conversation_participants",
                columns: new[] { "ConversationId", "UserId", "JoinedAt", "LastReadAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "messages",
                columns: new[] { "MessageId", "AttachmentUrl", "ConversationId", "CreatedAt", "MessageText", "ReadAt", "SenderUserId" },
                values: new object[] { 1, null, 1, new DateTime(2026, 6, 8, 0, 20, 0, 0, DateTimeKind.Utc), "I am on the way to your location.", null, 2 });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ActorUserId",
                schema: "dbo",
                table: "audit_logs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_client_addresses_ClientId",
                schema: "dbo",
                table: "client_addresses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_clients_UserId",
                schema: "dbo",
                table: "clients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversation_participants_UserId",
                schema: "dbo",
                table: "conversation_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_RequestId",
                schema: "dbo",
                table: "conversations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_live_locations_MechanicId",
                schema: "dbo",
                table: "live_locations",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_live_locations_RequestId",
                schema: "dbo",
                table: "live_locations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_mechanic_availability_MechanicId",
                schema: "dbo",
                table: "mechanic_availability",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_mechanics_UserId",
                schema: "dbo",
                table: "mechanics",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "dbo",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderUserId",
                schema: "dbo",
                table: "messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_motorcycles_ClientId",
                schema: "dbo",
                table: "motorcycles",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                schema: "dbo",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_otp_verifications_UserId",
                schema: "dbo",
                table: "otp_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                schema: "dbo",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_PaymentId",
                schema: "dbo",
                table: "payment_events",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_MethodName",
                schema: "dbo",
                table: "payment_methods",
                column: "MethodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_statuses_StatusName",
                schema: "dbo",
                table: "payment_statuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ClientId",
                schema: "dbo",
                table: "payments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentMethodId",
                schema: "dbo",
                table: "payments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentStatusId",
                schema: "dbo",
                table: "payments",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_RequestId",
                schema: "dbo",
                table: "payments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_ProductId",
                schema: "dbo",
                table: "product_images",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_products_ShopId",
                schema: "dbo",
                table: "products",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_request_media_RequestId",
                schema: "dbo",
                table: "request_media",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_ChangedByUserId",
                schema: "dbo",
                table: "request_status_history",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_NewStatusId",
                schema: "dbo",
                table: "request_status_history",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_OldStatusId",
                schema: "dbo",
                table: "request_status_history",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_RequestId",
                schema: "dbo",
                table: "request_status_history",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_statuses_StatusName",
                schema: "dbo",
                table: "request_statuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_ClientId",
                schema: "dbo",
                table: "reviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_MechanicId",
                schema: "dbo",
                table: "reviews",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_RequestId",
                schema: "dbo",
                table: "reviews",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleName",
                schema: "dbo",
                table: "roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_CategoryName",
                schema: "dbo",
                table: "service_categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_images_ShopServiceId",
                schema: "dbo",
                table: "service_images",
                column: "ShopServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ClientId",
                schema: "dbo",
                table: "service_requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_CurrentStatusId",
                schema: "dbo",
                table: "service_requests",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_MechanicId",
                schema: "dbo",
                table: "service_requests",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_MotorcycleId",
                schema: "dbo",
                table: "service_requests",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ShopId",
                schema: "dbo",
                table: "service_requests",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ShopServiceId",
                schema: "dbo",
                table: "service_requests",
                column: "ShopServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_mechanics_MechanicId",
                schema: "dbo",
                table: "shop_mechanics",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_operating_hours_ShopId",
                schema: "dbo",
                table: "shop_operating_hours",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_services_CategoryId",
                schema: "dbo",
                table: "shop_services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_services_ShopId",
                schema: "dbo",
                table: "shop_services",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_shops_OwnerUserId",
                schema: "dbo",
                table: "shops",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_ProviderName_ProviderSubject",
                schema: "dbo",
                table: "user_auth_providers",
                columns: new[] { "ProviderName", "ProviderSubject" },
                unique: true,
                filter: "[ProviderSubject] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_UserId",
                schema: "dbo",
                table: "user_auth_providers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_device_tokens_UserId",
                schema: "dbo",
                table: "user_device_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                schema: "dbo",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "dbo",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "client_addresses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "conversation_participants",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "live_locations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "mechanic_availability",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "otp_verifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payment_events",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "product_images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "request_media",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "request_status_history",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "reviews",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "service_images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "shop_mechanics",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "shop_operating_hours",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "user_auth_providers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "user_device_tokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payment_statuses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "service_requests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "mechanics",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "motorcycles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "request_statuses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "shop_services",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "clients",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "service_categories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "shops",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "users",
                schema: "dbo");
        }
    }
}
