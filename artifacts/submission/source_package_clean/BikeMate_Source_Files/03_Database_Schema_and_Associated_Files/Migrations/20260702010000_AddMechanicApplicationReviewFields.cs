using System;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    [DbContext(typeof(BikeMateDbContext))]
    [Migration("20260702010000_AddMechanicApplicationReviewFields")]
    public partial class AddMechanicApplicationReviewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthdate",
                schema: "dbo",
                table: "mechanics",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidIdImageUrl",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "dbo",
                table: "mechanics",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MiddleName",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "Sex",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "Birthdate",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "ValidIdImageUrl",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "Barangay",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "Province",
                schema: "dbo",
                table: "mechanics");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "dbo",
                table: "mechanics");
        }
    }
}
