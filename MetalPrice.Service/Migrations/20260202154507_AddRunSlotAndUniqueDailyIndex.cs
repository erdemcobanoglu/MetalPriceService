using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalPrice.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddRunSlotAndUniqueDailyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetalPriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TakenAtDate = table.Column<DateTime>(type: "datetime2", nullable: false, computedColumnSql: "CAST([TakenAtUtc] AS date)", stored: true),
                    RunSlot = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BaseCurrency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    XAU = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    XAG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    XPT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    XPD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetalPriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MorningTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EveningTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots",
                columns: new[] { "TakenAtDate", "RunSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetalPriceSnapshots");

            migrationBuilder.DropTable(
                name: "ServiceSchedules");
        }
    }
}
