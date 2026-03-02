using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TcmbRatesWorker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tcmb");

            migrationBuilder.CreateTable(
                name: "Rates",
                schema: "tcmb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    ForexBuying = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ForexSelling = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BanknoteBuying = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BanknoteSelling = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rates_RateDate_CurrencyCode",
                schema: "tcmb",
                table: "Rates",
                columns: new[] { "RateDate", "CurrencyCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rates",
                schema: "tcmb");
        }
    }
}
