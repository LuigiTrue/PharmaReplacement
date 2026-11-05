using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTrackingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    Observations = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotation_x_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
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
                name: "danfes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    DanfeNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Boleto = table.Column<bool>(type: "boolean", nullable: false),
                    UserReceiver = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "date", nullable: true),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    IsThermolabile = table.Column<bool>(type: "boolean", nullable: false),
                    ItemsQuantity = table.Column<int>(type: "integer", nullable: false),
                    ContainsPurchase = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentDueAt = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_danfes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_danfes_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_danfes_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_danfes_QuotationId",
                table: "danfes",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_danfes_SupplierId",
                table: "danfes",
                column: "SupplierId");

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
                name: "IX_quotations_QuotationNumber",
                table: "quotations",
                column: "QuotationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Cnpj",
                table: "suppliers",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "danfes");

            migrationBuilder.DropTable(
                name: "quotation_x_items");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "quotations");
        }
    }
}
