using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class MergeControlledItemTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE items
                SET "ItemType" = 3
                WHERE "ItemType" = 4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE items
                SET "ItemType" = 4
                WHERE "ItemType" = 3
                  AND UPPER("Name") LIKE ANY (ARRAY[
                      '%CETAMINA%', '%DEXMEDETOMIDINA%', '%ESCETAMINA%', '%ETOMIDATO%', '%KETAMINA%',
                      '%MIDAZOLAM%', '%PROPOFOL%', '%SEVOFLURANO%'
                  ]);
                """);
        }
    }
}
