using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoinMarketCap.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceToCryptoPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PriceSnapshots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CryptoPrices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "PriceSnapshots");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CryptoPrices");
        }
    }
}
