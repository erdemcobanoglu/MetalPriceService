using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalPrice.Service.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndex_TakenAtDate_RunSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots");

            migrationBuilder.CreateIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots",
                columns: new[] { "TakenAtDate", "RunSlot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots");

            migrationBuilder.CreateIndex(
                name: "UX_MetalPriceSnapshot_TakenAtDate_RunSlot",
                table: "MetalPriceSnapshots",
                columns: new[] { "TakenAtDate", "RunSlot" },
                unique: true);
        }
    }
}
