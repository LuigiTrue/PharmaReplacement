using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyConsumptionAndReplenishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumptions");

            migrationBuilder.DropTable(
                name: "daily_consumptions");

            migrationBuilder.DropTable(
                name: "item_consumption_averages");

            migrationBuilder.DropTable(
                name: "quotation_x_items");

            migrationBuilder.DropTable(
                name: "replenishment_rules");

            migrationBuilder.DropTable(
                name: "report_items");

            migrationBuilder.DropTable(
                name: "report_imports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    AverageVariation = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    DailyAverage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalDate = table.Column<DateTime>(type: "date", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    InitialDate = table.Column<DateTime>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalConsumption = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumptions", x => x.Id);
                    table.CheckConstraint("CK_consumptions_daily_average", "\"DailyAverage\" >= 0");
                    table.CheckConstraint("CK_consumptions_period", "\"FinalDate\" >= \"InitialDate\"");
                    table.CheckConstraint("CK_consumptions_total_consumption", "\"TotalConsumption\" >= 0");
                    table.ForeignKey(
                        name: "FK_consumptions_items_ProductId",
                        column: x => x.ProductId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_consumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_consumptions_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_consumption_averages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    AveragePeriodKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CoverageDays = table.Column<int>(type: "integer", nullable: false),
                    CurrentAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MonthlyAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ProjectedCoverageDays = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ReportEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StockBalance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    WeeklyAverageOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "quotation_x_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    QuantityRequested = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_x_items", x => x.Id);
                    table.CheckConstraint("CK_quotation_x_items_quantity_requested", "\"QuantityRequested\" >= 0");
                    table.ForeignKey(
                        name: "FK_quotation_x_items_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_x_items_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "replenishment_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    CalculationMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ItemPriority = table.Column<int>(type: "integer", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    SafetyStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    TargetCoverageDays = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replenishment_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_replenishment_rules_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_imports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvalidItems = table.Column<int>(type: "integer", nullable: true),
                    ReferenceMonth = table.Column<short>(type: "smallint", nullable: false),
                    ReferenceYear = table.Column<short>(type: "smallint", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidItems = table.Column<int>(type: "integer", nullable: true)
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
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ReportImportId = table.Column<int>(type: "integer", nullable: false),
                    AverageDailyOutput = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    AverageUnitCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    MovementDays = table.Column<short>(type: "smallint", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TotalOutput = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0m)
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
                name: "IX_consumptions_ProductId_InitialDate_FinalDate_Source",
                table: "consumptions",
                columns: new[] { "ProductId", "InitialDate", "FinalDate", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_consumptions_ItemId",
                table: "daily_consumptions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_item_consumption_averages_ItemCode_ReportStartDate_ReportEn~",
                table: "item_consumption_averages",
                columns: new[] { "ItemCode", "ReportStartDate", "ReportEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_consumption_averages_ItemId",
                table: "item_consumption_averages",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_x_items_ItemId",
                table: "quotation_x_items",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_x_items_QuotationId_ItemId",
                table: "quotation_x_items",
                columns: new[] { "QuotationId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_replenishment_rules_ItemId",
                table: "replenishment_rules",
                column: "ItemId",
                unique: true);

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
    }
}
