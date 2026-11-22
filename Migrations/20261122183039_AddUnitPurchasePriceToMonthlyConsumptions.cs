using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitPurchasePriceToMonthlyConsumptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPurchasePrice",
                table: "monthly_consumptions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_monthly_consumptions_unit_purchase_price",
                table: "monthly_consumptions",
                sql: "\"UnitPurchasePrice\" IS NULL OR \"UnitPurchasePrice\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_monthly_consumptions_unit_purchase_price",
                table: "monthly_consumptions");

            migrationBuilder.DropColumn(
                name: "UnitPurchasePrice",
                table: "monthly_consumptions");
        }
    }
}
