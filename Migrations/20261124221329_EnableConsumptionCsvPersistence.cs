using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class EnableConsumptionCsvPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_monthly_consumptions_average_daily_consumption",
                table: "monthly_consumptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_monthly_consumptions_total_quantity",
                table: "monthly_consumptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_daily_consumptions_quantity",
                table: "daily_consumptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                table: "monthly_consumptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecordedDays",
                table: "monthly_consumptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Registros anteriores só são completos quando há um diário para cada dia do mês.
            migrationBuilder.Sql("""
                UPDATE monthly_consumptions m
                SET "RecordedDays" = (
                    SELECT COUNT(*)::integer FROM daily_consumptions d
                    WHERE d."ItemId" = m."ItemId"
                      AND EXTRACT(YEAR FROM d."Date") = m."Year"
                      AND EXTRACT(MONTH FROM d."Date") = m."Month"
                );
                UPDATE monthly_consumptions
                SET "IsComplete" = "RecordedDays" = EXTRACT(DAY FROM (
                    make_date("Year", "Month", 1) + INTERVAL '1 month' - INTERVAL '1 day'
                ));
                """);

            migrationBuilder.CreateTable(
                name: "consumption_imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedThrough = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumption_imports", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_monthly_consumptions_recorded_days",
                table: "monthly_consumptions",
                sql: "\"RecordedDays\" BETWEEN 0 AND 31");

            migrationBuilder.CreateIndex(
                name: "IX_consumption_imports_ReferenceMonth_ImportedAt",
                table: "consumption_imports",
                columns: new[] { "ReferenceMonth", "ImportedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumption_imports");

            migrationBuilder.DropCheckConstraint(
                name: "CK_monthly_consumptions_recorded_days",
                table: "monthly_consumptions");

            migrationBuilder.DropColumn(
                name: "IsComplete",
                table: "monthly_consumptions");

            migrationBuilder.DropColumn(
                name: "RecordedDays",
                table: "monthly_consumptions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_monthly_consumptions_average_daily_consumption",
                table: "monthly_consumptions",
                sql: "\"AverageDailyConsumption\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_monthly_consumptions_total_quantity",
                table: "monthly_consumptions",
                sql: "\"TotalQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_daily_consumptions_quantity",
                table: "daily_consumptions",
                sql: "\"Quantity\" >= 0");
        }
    }
}
