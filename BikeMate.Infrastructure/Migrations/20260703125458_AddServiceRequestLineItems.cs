using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_request_line_items",
                schema: "dbo",
                columns: table => new
                {
                    LineItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShopServiceId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_line_items", x => x.LineItemId);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "dbo",
                        principalTable: "service_requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_request_line_items_shop_services_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalSchema: "dbo",
                        principalTable: "shop_services",
                        principalColumn: "ShopServiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_ProductId",
                schema: "dbo",
                table: "service_request_line_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_RequestId",
                schema: "dbo",
                table: "service_request_line_items",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_line_items_ShopServiceId",
                schema: "dbo",
                table: "service_request_line_items",
                column: "ShopServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_request_line_items",
                schema: "dbo");
        }
    }
}
