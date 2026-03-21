using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoinMarketCap.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddRankAndListingsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConvertCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(38,18)", precision: 38, scale: 18, nullable: false),
                    MarketCap = table.Column<decimal>(type: "decimal(38,8)", precision: 38, scale: 8, nullable: true),
                    Volume24h = table.Column<decimal>(type: "decimal(38,8)", precision: 38, scale: 8, nullable: true),
                    PercentChange1h = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    PercentChange24h = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    PercentChange7d = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoPrices_PriceSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "PriceSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoPrices_LastUpdatedUtc",
                table: "CryptoPrices",
                column: "LastUpdatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoPrices_SnapshotId",
                table: "CryptoPrices",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoPrices_Symbol_ConvertCurrency",
                table: "CryptoPrices",
                columns: new[] { "Symbol", "ConvertCurrency" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CryptoPrices");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");
        }
    }
}
