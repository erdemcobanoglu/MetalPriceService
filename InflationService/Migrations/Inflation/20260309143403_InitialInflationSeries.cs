using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InflationService.Migrations.Inflation
{
    /// <inheritdoc />
    public partial class InitialInflationSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inflation");

            migrationBuilder.CreateTable(
                name: "InflationSeries",
                schema: "inflation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AnnualRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IndexValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawSourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InflationSeries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_InflationSeries_Source_Year_Month",
                schema: "inflation",
                table: "InflationSeries",
                columns: new[] { "Source", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InflationSeries",
                schema: "inflation");
        }
    }
}
