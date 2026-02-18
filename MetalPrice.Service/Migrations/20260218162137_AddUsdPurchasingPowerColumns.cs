using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalPrice.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddUsdPurchasingPowerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots");

            migrationBuilder.AlterColumn<decimal>(
                name: "XPT",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "XPD",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "XAU",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "XAG",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "XAG_PerUsd",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "XAU_PerUsd",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "XPD_PerUsd",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "XPT_PerUsd",
                table: "MetalPriceSnapshots",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots",
                columns: new[] { "TakenAtDate", "RunSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "XAG_PerUsd",
                table: "MetalPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "XAU_PerUsd",
                table: "MetalPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "XPD_PerUsd",
                table: "MetalPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "XPT_PerUsd",
                table: "MetalPriceSnapshots");

            migrationBuilder.AlterColumn<decimal>(
                name: "XPT",
                table: "MetalPriceSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "XPD",
                table: "MetalPriceSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "XAU",
                table: "MetalPriceSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "XAG",
                table: "MetalPriceSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.CreateIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots",
                columns: new[] { "TakenAtDate", "RunSlot" });
        }
    }
}
