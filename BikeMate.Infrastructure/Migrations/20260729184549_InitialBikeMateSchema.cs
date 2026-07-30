using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.PaymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "payment_statuses",
                columns: table => new
                {
                    PaymentStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatusName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_statuses", x => x.PaymentStatusId);
                });

            migrationBuilder.CreateTable(
                name: "request_statuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatusName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_statuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AccountStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    AuditId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldValuesJson = table.Column<string>(type: "text", nullable: true),
                    NewValuesJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sex = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Birthdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidIdImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_clients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mechanics",
                columns: table => new
                {
                    MechanicId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sex = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Birthdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidIdImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Barangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    YearsExperience = table.Column<int>(type: "integer", nullable: true),
                    CertificationImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "offline"),
                    CurrentLatitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    CurrentLongitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalCompletedJobs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mechanics", x => x.MechanicId);
                    table.ForeignKey(
                        name: "FK_mechanics_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                columns: table => new
                {
                    OtpId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "email_verification"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_verifications", x => x.OtpId);
                    table.ForeignKey(
                        name: "FK_otp_verifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shops",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    ShopName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ShopDescription = table.Column<string>(type: "text", nullable: true),
                    AddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    BusinessPermitUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShopImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShopLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerValidIdUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerMiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerSex = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OwnerBirthdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OwnerAddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerBarangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ContactNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShopStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    AllowsReservations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowsPickup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowsOnsiteRepair = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shops", x => x.ShopId);
                    table.ForeignKey(
                        name: "FK_shops_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_providers",
                columns: table => new
                {
                    AuthProviderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth_providers", x => x.AuthProviderId);
                    table.ForeignKey(
                        name: "FK_user_auth_providers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_device_tokens",
                columns: table => new
                {
                    DeviceTokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "android"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_device_tokens", x => x.DeviceTokenId);
                    table.ForeignKey(
                        name: "FK_user_device_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_addresses",
                columns: table => new
                {
                    AddressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Barangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_addresses", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_client_addresses_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motorcycles",
                columns: table => new
                {
                    MotorcycleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    YearModel = table.Column<int>(type: "integer", nullable: true),
                    PlateNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EngineType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MotorcycleImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorcycles", x => x.MotorcycleId);
                    table.ForeignKey(
                        name: "FK_motorcycles_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mechanic_availability",
                columns: table => new
                {
                    AvailabilityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MechanicId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mechanic_availability", x => x.AvailabilityId);
                    table.ForeignKey(
                        name: "FK_mechanic_availability_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProductDescription = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_products_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_mechanics",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    MechanicId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_mechanics", x => new { x.ShopId, x.MechanicId });
                    table.ForeignKey(
                        name: "FK_shop_mechanics_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_mechanics_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_operating_hours",
                columns: table => new
                {
                    OperatingHourId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    OpeningTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ClosingTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_operating_hours", x => x.OperatingHourId);
                    table.ForeignKey(
                        name: "FK_shop_operating_hours_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_services",
                columns: table => new
                {
                    ShopServiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServiceDescription = table.Column<string>(type: "text", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_services", x => x.ShopServiceId);
                    table.ForeignKey(
                        name: "FK_shop_services_service_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "service_categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_services_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    ProductImageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.ProductImageId);
                    table.ForeignKey(
                        name: "FK_product_images_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_images",
                columns: table => new
                {
                    ServiceImageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopServiceId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_images", x => x.ServiceImageId);
                    table.ForeignKey(
                        name: "FK_service_images_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ShopId = table.Column<int>(type: "integer", nullable: true),
                    ShopServiceId = table.Column<int>(type: "integer", nullable: true),
                    MechanicId = table.Column<int>(type: "integer", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "integer", nullable: false),
                    MotorcycleId = table.Column<int>(type: "integer", nullable: true),
                    IssueDescription = table.Column<string>(type: "text", nullable: false),
                    ServiceLocationAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ServiceLatitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    ServiceLongitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EstimatedTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FinalTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_service_requests_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "motorcycles",
                        principalColumn: "MotorcycleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_request_statuses_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "ShopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: true),
                    ConversationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "service_request"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_conversations_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "live_locations",
                columns: table => new
                {
                    LiveLocationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: true),
                    MechanicId = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: false),
                    AccuracyMeters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_locations", x => x.LiveLocationId);
                    table.ForeignKey(
                        name: "FK_live_locations_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_live_locations_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatusId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "PHP"),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "paymongo"),
                    ProviderCheckoutSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderReferenceNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_payments_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_payment_methods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "payment_methods",
                        principalColumn: "PaymentMethodId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payments_payment_statuses_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalTable: "payment_statuses",
                        principalColumn: "PaymentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_media",
                columns: table => new
                {
                    RequestMediaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "image"),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_media", x => x.RequestMediaId);
                    table.ForeignKey(
                        name: "FK_request_media_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_status_history",
                columns: table => new
                {
                    StatusHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    OldStatusId = table.Column<int>(type: "integer", nullable: true),
                    NewStatusId = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_status_history", x => x.StatusHistoryId);
                    table.ForeignKey(
                        name: "FK_request_status_history_request_statuses_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_status_history_request_statuses_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "request_statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_status_history_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_status_history_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    MechanicId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_reviews_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviews_mechanics_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "mechanics",
                        principalColumn: "MechanicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviews_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_line_items",
                columns: table => new
                {
                    LineItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ShopServiceId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_line_items", x => x.LineItemId);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversation_participants",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_participants", x => new { x.ConversationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_conversation_participants_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversation_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                columns: table => new
                {
                    PaymentEventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EventType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.PaymentEventId);
                    table.ForeignKey(
                        name: "FK_payment_events_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
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
                    { 8, "cancelled" },
                    { 9, "emergency_pending" },
                    { 10, "call_connecting" },
                    { 11, "searching_responder" },
                    { 12, "payment_pending" },
                    { 13, "paid" },
                    { 14, "call_connected" }
                });

            migrationBuilder.InsertData(
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
                table: "service_categories",
                columns: new[] { "CategoryId", "CategoryName", "Description", "IconUrl", "IsActive" },
                values: new object[,]
                {
                    { 1, "Engine Repair", "Engine troubleshooting, repair, and maintenance", null, true },
                    { 2, "Tire Service", "Flat tire repair, tire replacement, and tire checking", null, true },
                    { 3, "Battery Service", "Battery check, replacement, and charging assistance", null, true },
                    { 4, "Oil Change", "Motorcycle oil change and fluid maintenance", null, true },
                    { 5, "Brake Service", "Brake inspection, cleaning, repair, and replacement", null, true },
                    { 6, "Emergency Roadside Assistance", "Urgent roadside motorcycle assistance", null, true },
                    { 7, "Drivetrain & Gear Service", "Gear shifting, transmission, derailleur, and drivetrain diagnostics", null, true },
                    { 8, "Chain & Sprocket Service", "Drive chain cleaning, tensioning, replacement, and sprocket inspection", null, true },
                    { 9, "Accessories & Electrical Installation", "Safe installation of approved motorcycle accessories and electrical upgrades", null, true },
                    { 10, "Preventive Maintenance & Tune-up", "Periodic inspection, adjustment, and complete motorcycle tune-up", null, true }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "UserId", "AccountStatus", "CreatedAt", "Email", "EmailVerified", "FirstName", "LastName", "PasswordHash", "PhoneNumber", "PhoneVerified", "ProfileImageUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "active", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "isaiahandreinoda@gmail.com", true, "Isaiah", "Noda", "sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16", "+639170000001", true, null, null },
                    { 2, "active", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "customer1@bikemate.test", true, "John", "Doe", "sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16", "+639170000002", true, null, null },
                    { 3, "active", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "mechanic1@bikemate.test", true, "Jane", "Smith", "sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16", "+639170000003", true, null, null },
                    { 4, "active", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "shop1@bikemate.test", true, "Bob", "Williams", "sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16", "+639170000004", true, null, null }
                });

            migrationBuilder.InsertData(
                table: "clients",
                columns: new[] { "ClientId", "Birthdate", "CreatedAt", "MiddleName", "Sex", "UserId", "ValidIdImageUrl" },
                values: new object[] { 1, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, 2, null });

            migrationBuilder.InsertData(
                table: "mechanics",
                columns: new[] { "MechanicId", "AddressLine", "AvailabilityStatus", "AverageRating", "Barangay", "Bio", "Birthdate", "CertificationImageUrl", "City", "CreatedAt", "CurrentLatitude", "CurrentLongitude", "IsVerified", "MiddleName", "Province", "Sex", "TotalCompletedJobs", "UpdatedAt", "UserId", "ValidIdImageUrl", "YearsExperience", "ZipCode" },
                values: new object[] { 1, null, "online", 0m, null, null, null, null, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, null, null, 0, null, 3, null, null, null });

            migrationBuilder.InsertData(
                table: "shops",
                columns: new[] { "ShopId", "AddressLine", "AllowsOnsiteRepair", "AllowsPickup", "AllowsReservations", "BusinessPermitUrl", "City", "ContactNumber", "CreatedAt", "Latitude", "Longitude", "OwnerAddressLine", "OwnerBarangay", "OwnerBirthdate", "OwnerCity", "OwnerMiddleName", "OwnerProvince", "OwnerSex", "OwnerUserId", "OwnerValidIdUrl", "OwnerZipCode", "Province", "ShopDescription", "ShopImageUrl", "ShopLogoUrl", "ShopName", "ShopStatus", "UpdatedAt" },
                values: new object[] { 1, null, true, true, true, null, "Manila", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, null, null, null, null, null, null, 4, null, null, "NCR", null, null, null, "Demo Bike Shop", "active", null });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "RoleId", "UserId", "AssignedAt" },
                values: new object[,]
                {
                    { 4, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 1, 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 3, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 4, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ActorUserId",
                table: "audit_logs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_client_addresses_ClientId",
                table: "client_addresses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_clients_UserId",
                table: "clients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversation_participants_UserId",
                table: "conversation_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_RequestId",
                table: "conversations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_live_locations_MechanicId",
                table: "live_locations",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_live_locations_RequestId",
                table: "live_locations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_mechanic_availability_MechanicId",
                table: "mechanic_availability",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_mechanics_UserId",
                table: "mechanics",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderUserId",
                table: "messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_motorcycles_ClientId",
                table: "motorcycles",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_otp_verifications_UserId",
                table: "otp_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_PaymentId",
                table: "payment_events",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_MethodName",
                table: "payment_methods",
                column: "MethodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_statuses_StatusName",
                table: "payment_statuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ClientId",
                table: "payments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentMethodId",
                table: "payments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentStatusId",
                table: "payments",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_RequestId",
                table: "payments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_ProductId",
                table: "product_images",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_products_ShopId",
                table: "products",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_request_media_RequestId",
                table: "request_media",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_ChangedByUserId",
                table: "request_status_history",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_NewStatusId",
                table: "request_status_history",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_OldStatusId",
                table: "request_status_history",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_RequestId",
                table: "request_status_history",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_statuses_StatusName",
                table: "request_statuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_ClientId",
                table: "reviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_MechanicId",
                table: "reviews",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_RequestId",
                table: "reviews",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleName",
                table: "roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_CategoryName",
                table: "service_categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_images_ShopServiceId",
                table: "service_images",
                column: "ShopServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_ProductId",
                table: "service_request_line_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_RequestId",
                table: "service_request_line_items",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_ShopServiceId",
                table: "service_request_line_items",
                column: "ShopServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ClientId",
                table: "service_requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_CurrentStatusId",
                table: "service_requests",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_MechanicId",
                table: "service_requests",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_MotorcycleId",
                table: "service_requests",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ShopId",
                table: "service_requests",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_ShopServiceId",
                table: "service_requests",
                column: "ShopServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_mechanics_MechanicId",
                table: "shop_mechanics",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_operating_hours_ShopId",
                table: "shop_operating_hours",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_services_CategoryId",
                table: "shop_services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_services_ShopId",
                table: "shop_services",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_shops_OwnerUserId",
                table: "shops",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_ProviderName_ProviderSubject",
                table: "user_auth_providers",
                columns: new[] { "ProviderName", "ProviderSubject" },
                unique: true,
                filter: "\"ProviderSubject\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_UserId",
                table: "user_auth_providers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_device_tokens_UserId",
                table: "user_device_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "client_addresses");

            migrationBuilder.DropTable(
                name: "conversation_participants");

            migrationBuilder.DropTable(
                name: "live_locations");

            migrationBuilder.DropTable(
                name: "mechanic_availability");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "otp_verifications");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "payment_events");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "request_media");

            migrationBuilder.DropTable(
                name: "request_status_history");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "service_images");

            migrationBuilder.DropTable(
                name: "service_request_line_items");

            migrationBuilder.DropTable(
                name: "shop_mechanics");

            migrationBuilder.DropTable(
                name: "shop_operating_hours");

            migrationBuilder.DropTable(
                name: "user_auth_providers");

            migrationBuilder.DropTable(
                name: "user_device_tokens");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payment_statuses");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "mechanics");

            migrationBuilder.DropTable(
                name: "motorcycles");

            migrationBuilder.DropTable(
                name: "request_statuses");

            migrationBuilder.DropTable(
                name: "shop_services");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "service_categories");

            migrationBuilder.DropTable(
                name: "shops");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
