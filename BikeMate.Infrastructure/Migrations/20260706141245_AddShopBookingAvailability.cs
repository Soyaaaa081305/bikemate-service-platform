using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopBookingAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsOnsiteRepair",
                schema: "dbo",
                table: "shops",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsPickup",
                schema: "dbo",
                table: "shops",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsReservations",
                schema: "dbo",
                table: "shops",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsOnsiteRepair",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "AllowsPickup",
                schema: "dbo",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "AllowsReservations",
                schema: "dbo",
                table: "shops");
        }
    }
}
