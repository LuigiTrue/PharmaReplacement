using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddItemPriorityToReplenishmentRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemPriority",
                table: "replenishment_rules",
                type: "integer",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemPriority",
                table: "replenishment_rules");
        }
    }
}
