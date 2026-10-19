using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCMobile.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_SalePrice",
                table: "Products",
                sql: "[SalePrice] IS NULL OR ([SalePrice] > 0 AND [SalePrice] < [Price])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_SalePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Products");
        }
    }
}
