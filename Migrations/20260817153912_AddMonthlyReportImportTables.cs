using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyReportImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_imports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    ReferenceYear = table.Column<short>(type: "smallint", nullable: false),
                    ReferenceMonth = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalItems = table.Column<int>(type: "integer", nullable: true),
                    ValidItems = table.Column<int>(type: "integer", nullable: true),
                    InvalidItems = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_imports", x => x.Id);
                    table.CheckConstraint("CK_report_imports_reference_month", "\"ReferenceMonth\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_report_imports_reference_year", "\"ReferenceYear\" >= 2000");
                    table.ForeignKey(
                        name: "FK_report_imports_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportImportId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    TotalOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0m),
                    AverageDailyOutput = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    MovementDays = table.Column<short>(type: "smallint", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    AverageUnitCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_items", x => x.Id);
                    table.CheckConstraint("CK_report_items_movement_days", "\"MovementDays\" IS NULL OR \"MovementDays\" BETWEEN 0 AND 31");
                    table.CheckConstraint("CK_report_items_total_output", "\"TotalOutput\" >= 0");
                    table.ForeignKey(
                        name: "FK_report_items_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_items_report_imports_ReportImportId",
                        column: x => x.ReportImportId,
                        principalTable: "report_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_LocationId_ReferenceYear_ReferenceMonth",
                table: "report_imports",
                columns: new[] { "LocationId", "ReferenceYear", "ReferenceMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_items_ItemId",
                table: "report_items",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_report_items_ReportImportId_ItemId",
                table: "report_items",
                columns: new[] { "ReportImportId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_items");

            migrationBuilder.DropTable(
                name: "report_imports");
        }
    }
}
