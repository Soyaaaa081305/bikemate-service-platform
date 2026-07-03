using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAccountDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Birthdate",
                schema: "dbo",
                table: "clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                schema: "dbo",
                table: "clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                schema: "dbo",
                table: "clients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidIdImageUrl",
                schema: "dbo",
                table: "clients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                schema: "dbo",
                table: "client_addresses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "client_addresses",
                keyColumn: "AddressId",
                keyValue: 1,
                column: "Barangay",
                value: null);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "clients",
                keyColumn: "ClientId",
                keyValue: 1,
                columns: new[] { "Birthdate", "MiddleName", "Sex", "ValidIdImageUrl" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birthdate",
                schema: "dbo",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                schema: "dbo",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "Sex",
                schema: "dbo",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "ValidIdImageUrl",
                schema: "dbo",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "Barangay",
                schema: "dbo",
                table: "client_addresses");
        }
    }
}
