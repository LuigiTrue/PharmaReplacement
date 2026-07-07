using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddItemConsumptionAverages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_consumption_averages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    ItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReportStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CoverageDays = table.Column<int>(type: "integer", nullable: false),
                    AveragePeriodKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MonthlyAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    WeeklyAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    CurrentAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    StockBalance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ProjectedCoverageDays = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_consumption_averages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_consumption_averages_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_consumption_averages_ItemCode_ReportStartDate_ReportEn~",
                table: "item_consumption_averages",
                columns: new[] { "ItemCode", "ReportStartDate", "ReportEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_consumption_averages_ItemId",
                table: "item_consumption_averages",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_consumption_averages");
        }
    }
}
