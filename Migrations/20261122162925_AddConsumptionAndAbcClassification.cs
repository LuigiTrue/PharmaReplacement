using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionAndAbcClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abc_classification_results",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConsumptionValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AccumulatedPercentage = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    Classification = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abc_classification_results", x => x.Id);
                    table.CheckConstraint("CK_abc_classification_results_accumulated_percentage", "\"AccumulatedPercentage\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_abc_classification_results_analysis_id", "\"AnalysisId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("CK_abc_classification_results_classification", "\"Classification\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_abc_classification_results_consumption_value", "\"ConsumptionValue\" >= 0");
                    table.CheckConstraint("CK_abc_classification_results_period", "\"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_abc_classification_results_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_consumptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_consumptions", x => x.Id);
                    table.CheckConstraint("CK_daily_consumptions_quantity", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_daily_consumptions_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "monthly_consumptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AverageDailyConsumption = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_consumptions", x => x.Id);
                    table.CheckConstraint("CK_monthly_consumptions_average_daily_consumption", "\"AverageDailyConsumption\" >= 0");
                    table.CheckConstraint("CK_monthly_consumptions_month", "\"Month\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_monthly_consumptions_total_quantity", "\"TotalQuantity\" >= 0");
                    table.CheckConstraint("CK_monthly_consumptions_year", "\"Year\" BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "FK_monthly_consumptions_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abc_classification_results_AnalysisId_ItemId",
                table: "abc_classification_results",
                columns: new[] { "AnalysisId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_abc_classification_results_ItemId",
                table: "abc_classification_results",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_abc_classification_results_StartDate_EndDate",
                table: "abc_classification_results",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_consumptions_ItemId_Date",
                table: "daily_consumptions",
                columns: new[] { "ItemId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monthly_consumptions_ItemId_Year_Month",
                table: "monthly_consumptions",
                columns: new[] { "ItemId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abc_classification_results");

            migrationBuilder.DropTable(
                name: "daily_consumptions");

            migrationBuilder.DropTable(
                name: "monthly_consumptions");
        }
    }
}
