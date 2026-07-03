using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    public partial class AddShopOwnerValidIdUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerValidIdUrl",
                schema: "dbo",
                table: "shops",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerValidIdUrl",
                schema: "dbo",
                table: "shops");
        }
    }
}
