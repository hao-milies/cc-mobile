using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCMobile.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToPhones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaptopSpecifications");

            migrationBuilder.CreateTable(
                name: "PhoneSpecifications",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Chipset = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RamGb = table.Column<int>(type: "int", nullable: false),
                    StorageGb = table.Column<int>(type: "int", nullable: false),
                    ScreenSizeInches = table.Column<double>(type: "float", nullable: false),
                    ScreenResolution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PanelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RefreshRateHz = table.Column<int>(type: "int", nullable: true),
                    RearCamera = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FrontCamera = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BatteryMah = table.Column<int>(type: "int", nullable: true),
                    ChargingWatts = table.Column<int>(type: "int", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WeightGrams = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneSpecifications", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_PhoneSpecifications_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneSpecifications");

            migrationBuilder.CreateTable(
                name: "LaptopSpecifications",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Battery = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cpu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Gpu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PanelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ports = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RamGb = table.Column<int>(type: "int", nullable: false),
                    RamType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RefreshRateHz = table.Column<int>(type: "int", nullable: true),
                    ScreenResolution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScreenSizeInches = table.Column<double>(type: "float", nullable: false),
                    SsdGb = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaptopSpecifications", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_LaptopSpecifications_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
