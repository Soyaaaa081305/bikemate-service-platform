using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "service_categories",
                columns: new[] { "CategoryId", "CategoryName", "Description", "IconUrl", "IsActive" },
                values: new object[,]
                {
                    { 7, "Drivetrain & Gear Service", "Gear shifting, transmission, derailleur, and drivetrain diagnostics", null, true },
                    { 8, "Chain & Sprocket Service", "Drive chain cleaning, tensioning, replacement, and sprocket inspection", null, true },
                    { 9, "Accessories & Electrical Installation", "Safe installation of approved motorcycle accessories and electrical upgrades", null, true },
                    { 10, "Preventive Maintenance & Tune-up", "Periodic inspection, adjustment, and complete motorcycle tune-up", null, true }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "users",
                columns: new[] { "UserId", "AccountStatus", "CreatedAt", "Email", "EmailVerified", "FirstName", "LastName", "PasswordHash", "PhoneNumber", "PhoneVerified", "ProfileImageUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 101, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "southside.owner@bikemate.test", true, "Sofia", "Mendoza", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010101", true, null, null },
                    { 102, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "alabang.owner@bikemate.test", true, "Marco", "Reyes", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010102", true, null, null },
                    { 103, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "laspinas.owner@bikemate.test", true, "Lea", "Santos", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010103", true, null, null },
                    { 104, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "binan.owner@bikemate.test", true, "Paolo", "Cruz", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010104", true, null, null },
                    { 111, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "daniel.ramos@bikemate.test", true, "Daniel", "Ramos", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010111", true, null, null },
                    { 112, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "ken.bautista@bikemate.test", true, "Ken", "Bautista", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010112", true, null, null },
                    { 113, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "miguel.flores@bikemate.test", true, "Miguel", "Flores", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010113", true, null, null },
                    { 114, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "carlo.navarro@bikemate.test", true, "Carlo", "Navarro", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010114", true, null, null },
                    { 115, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "nina.garcia@bikemate.test", true, "Nina", "Garcia", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010115", true, null, null },
                    { 116, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "jomar.villanueva@bikemate.test", true, "Jomar", "Villanueva", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010116", true, null, null },
                    { 117, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "ella.torres@bikemate.test", true, "Ella", "Torres", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010117", true, null, null },
                    { 118, "active", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), "anton.lim@bikemate.test", true, "Anton", "Lim", "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea", "+639181010118", true, null, null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "mechanics",
                columns: new[] { "MechanicId", "AvailabilityStatus", "AverageRating", "Bio", "CertificationImageUrl", "CreatedAt", "CurrentLatitude", "CurrentLongitude", "IsVerified", "TotalCompletedJobs", "UpdatedAt", "UserId", "YearsExperience" },
                values: new object[,]
                {
                    { 101, "online", 4.90m, "Drivetrain and transmission specialist.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3591m, 121.0579m, true, 184, null, 111, 8 },
                    { 102, "online", 4.80m, "Chain, brake, and preventive maintenance technician.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3587m, 121.0572m, true, 139, null, 112, 6 },
                    { 103, "online", 4.90m, "Motorcycle electrical and accessory installation specialist.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4239m, 121.0310m, true, 171, null, 113, 7 },
                    { 104, "online", 4.70m, "Gearbox, brake, and roadworthiness technician.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4234m, 121.0304m, true, 206, null, 114, 9 },
                    { 105, "online", 4.90m, "Tire, wheel, and roadside repair specialist.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4455m, 120.9833m, true, 128, null, 115, 5 },
                    { 106, "online", 4.80m, "Tune-up, chain, and brake service technician.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4451m, 120.9827m, true, 163, null, 116, 7 },
                    { 107, "online", 4.90m, "Engine, drivetrain, and periodic maintenance specialist.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3335m, 121.0832m, true, 244, null, 117, 10 },
                    { 108, "online", 4.70m, "Electrical accessories, tires, and roadside support technician.", null, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3329m, 121.0827m, true, 151, null, 118, 6 }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shops",
                columns: new[] { "ShopId", "AddressLine", "BusinessPermitUrl", "City", "ContactNumber", "CreatedAt", "Latitude", "Longitude", "OwnerUserId", "Province", "ShopDescription", "ShopImageUrl", "ShopName", "ShopStatus", "UpdatedAt" },
                values: new object[,]
                {
                    { 101, "National Highway, Barangay Nueva", null, "San Pedro", "+639181100101", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3589m, 121.0575m, 101, "Laguna", "Full-service motorcycle repair with dedicated drivetrain, chain, brake, tire, and tune-up bays.", null, "Southside MotoCare San Pedro", "verified", null },
                    { 102, "Montillano Street, Alabang", null, "Muntinlupa", "+639181100102", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4237m, 121.0307m, 102, "Metro Manila", "Verified workshop specializing in gear diagnostics, brakes, electrical accessories, batteries, and scheduled maintenance.", null, "Alabang CycleWorks", "verified", null },
                    { 103, "Alabang-Zapote Road, Pamplona", null, "Las Pinas", "+639181100103", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.4453m, 120.9830m, 103, "Metro Manila", "Roadside-ready repair center for tires, brakes, chains, tune-ups, and emergency assistance.", null, "Las Pinas MotoLab", "verified", null },
                    { 104, "Manila South Road, Barangay San Antonio", null, "Binan", "+639181100104", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 14.3332m, 121.0830m, 104, "Laguna", "Experienced engine and drivetrain team with chain, tire, oil, and accessory installation services.", null, "RoadReady Garage Binan", "verified", null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "user_roles",
                columns: new[] { "RoleId", "UserId", "AssignedAt" },
                values: new object[,]
                {
                    { 3, 101, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 102, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 103, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 104, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 111, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 112, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 113, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 114, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 115, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 116, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 117, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 118, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shop_mechanics",
                columns: new[] { "MechanicId", "ShopId", "AssignedAt", "IsActive" },
                values: new object[,]
                {
                    { 101, 101, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 102, 101, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 103, 102, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 104, 102, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 105, 103, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 106, 103, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 107, 104, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true },
                    { 108, 104, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), true }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "shop_services",
                columns: new[] { "ShopServiceId", "BasePrice", "CategoryId", "CreatedAt", "EstimatedMinutes", "IsActive", "ServiceDescription", "ServiceName", "ShopId" },
                values: new object[,]
                {
                    { 101, 650m, 7, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 75, true, "Diagnoses hard shifting, false neutrals, cable play, and drivetrain alignment.", "Precision Gear Tuning", 101 },
                    { 102, 450m, 8, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 55, true, "Cleans, lubricates, aligns, and adjusts the drive chain and inspects both sprockets.", "Chain Cleaning and Tensioning", 101 },
                    { 103, 950m, 10, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 120, true, "Full preventive inspection with adjustment of controls, fluids, ignition, and roadworthiness items.", "Complete Motorcycle Tune-up", 101 },
                    { 104, 400m, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 45, true, "Puncture assessment, professional patching, pressure correction, and wheel safety check.", "Tubeless Tire and Puncture Repair", 101 },
                    { 105, 550m, 5, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Front and rear brake inspection, cleaning, adjustment, and wear report.", "Brake Cleaning and Adjustment", 101 },
                    { 106, 800m, 7, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 90, true, "Systematic inspection of shifting controls, clutch adjustment, transmission behavior, and drivetrain noise.", "Gearbox and Shifting Diagnostics", 102 },
                    { 107, 650m, 5, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 75, true, "Brake adjustment, pad inspection, cleaning, and hydraulic safety check where applicable.", "Brake System Service", 102 },
                    { 108, 700m, 9, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 90, true, "Safe fused installation of lights, horns, chargers, cameras, and approved accessories.", "Electrical Accessory Installation", 102 },
                    { 109, 450m, 3, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 45, true, "Battery load test, charging-system test, terminal service, and replacement assessment.", "Battery Health and Charging Check", 102 },
                    { 110, 1100m, 10, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 135, true, "Periodic maintenance inspection based on mileage and manufacturer service points.", "Scheduled Preventive Maintenance", 102 },
                    { 111, 380m, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 50, true, "Flat tire repair, valve inspection, tire replacement, pressure setting, and wheel check.", "Tire Repair and Replacement", 103 },
                    { 112, 500m, 5, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Brake response diagnosis, adjustment, cleaning, and component wear inspection.", "Brake Adjustment and Safety Check", 103 },
                    { 113, 480m, 8, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Chain cleaning, lubrication, slack correction, alignment, and sprocket wear assessment.", "Chain and Sprocket Care", 103 },
                    { 114, 900m, 10, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 120, true, "Complete tune-up covering controls, fluids, fasteners, tires, brakes, and running condition.", "General Tune-up and Inspection", 103 },
                    { 115, 750m, 6, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 45, true, "Dispatch support for breakdowns, no-start conditions, and minor roadside repairs.", "24/7 Roadside Motorcycle Assistance", 103 },
                    { 116, 850m, 1, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 90, true, "Troubleshooting for difficult starting, poor idle, power loss, smoke, and unusual engine noise.", "Engine Performance Diagnosis", 104 },
                    { 117, 500m, 8, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 65, true, "Complete chain maintenance with tension, alignment, lubrication, and replacement advice.", "Drive Chain and Sprocket Service", 104 },
                    { 118, 750m, 9, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 95, true, "Professional installation and wiring of utility, safety, and touring accessories.", "Motorcycle Accessory Fitment", 104 },
                    { 119, 600m, 4, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Engine oil replacement plus leak, level, and fluid-condition inspection.", "Oil and Fluid Maintenance", 104 },
                    { 120, 900m, 7, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 105, true, "Diagnosis and repair planning for shifting faults, clutch issues, gear noise, and drivetrain vibration.", "Drivetrain and Gear Repair", 104 },
                    { 121, 450m, 2, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Utc), 50, true, "Mobile puncture repair, inflation, valve check, and tire condition assessment.", "Roadside Flat Tire Service", 104 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 101, 101 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 102, 101 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 103, 102 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 104, 102 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 105, 103 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 106, 103 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 107, 104 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_mechanics",
                keyColumns: new[] { "MechanicId", "ShopId" },
                keyValues: new object[] { 108, 104 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 105);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 107);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 108);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 109);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 110);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 111);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 112);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 113);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 114);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 115);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 116);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 117);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 118);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 119);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 120);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shop_services",
                keyColumn: "ShopServiceId",
                keyValue: 121);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 3, 101 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 3, 102 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 3, 103 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 3, 104 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 111 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 112 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 113 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 114 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 115 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 116 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 117 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 118 });

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 105);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 107);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "mechanics",
                keyColumn: "MechanicId",
                keyValue: 108);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "service_categories",
                keyColumn: "CategoryId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "service_categories",
                keyColumn: "CategoryId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "service_categories",
                keyColumn: "CategoryId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "service_categories",
                keyColumn: "CategoryId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shops",
                keyColumn: "ShopId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shops",
                keyColumn: "ShopId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shops",
                keyColumn: "ShopId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "shops",
                keyColumn: "ShopId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 111);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 112);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 113);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 114);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 115);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 116);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 117);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "users",
                keyColumn: "UserId",
                keyValue: 118);
        }
    }
}
