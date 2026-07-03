using System;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    [DbContext(typeof(BikeMateDbContext))]
    [Migration("20260630230000_AddShopApplicationReviewFields")]
    public partial class AddShopApplicationReviewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerMiddleName",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSex",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerBirthdate",
                schema: "dbo",
                table: "shops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerAddressLine",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerBarangay",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerCity",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerProvince",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerZipCode",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerMiddleName",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerSex",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerBirthdate",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerAddressLine",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerBarangay",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerCity",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerProvince",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "OwnerZipCode",
                schema: "dbo",
                table: "shops");
        }
    }
}
