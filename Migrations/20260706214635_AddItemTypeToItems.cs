using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepyPharma.Migrations
{
    /// <inheritdoc />
    public partial class AddItemTypeToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE items
                SET "ItemType" = CASE
                    WHEN UPPER("Name") LIKE ANY (ARRAY[
                        '%ADRENALINA%', '%AMIODARONA%', '%DOBUTAMINA%', '%DOPAMINA%', '%ENOXAPARINA%',
                        '%EPINEFRINA%', '%FENTANIL%', '%FENTANILA%', '%GLUCONATO DE CALCIO%', '%HEPARINA%',
                        '%INSULINA%', '%KCL%', '%MORFINA%', '%NITROGLICERINA%', '%NITROPRUSSIATO%',
                        '%NORADRENALINA%', '%NOREPINEFRINA%', '%POTASSIO%', '%SULFATO DE MAGNESIO%'
                    ]) THEN 2
                    WHEN UPPER("Name") LIKE ANY (ARRAY[
                        '%AMITRIPTILINA%', '%BIPERIDENO%', '%CARBAMAZEPINA%', '%CLONAZEPAM%', '%CLORPROMAZINA%',
                        '%DIAZEPAM%', '%FENITOINA%', '%FENOBARBITAL%', '%FLUOXETINA%', '%HALOPERIDOL%',
                        '%LEVOMEPROMAZINA%', '%LITIO%', '%OLANZAPINA%', '%QUETIAPINA%', '%RISPERIDONA%',
                        '%SERTRALINA%', '%VALPROATO%'
                    ]) THEN 3
                    WHEN UPPER("Name") LIKE ANY (ARRAY[
                        '%CETAMINA%', '%DEXMEDETOMIDINA%', '%ESCETAMINA%', '%ETOMIDATO%', '%KETAMINA%',
                        '%MIDAZOLAM%', '%PROPOFOL%', '%SEVOFLURANO%'
                    ]) THEN 4
                    WHEN UPPER("Name") LIKE ANY (ARRAY[
                        '%AMICACINA%', '%AMOXICILINA%', '%AMPICILINA%', '%AZITROMICINA%', '%BENZILPENICILINA%',
                        '%CEFA%', '%CEFEP%', '%CEFT%', '%CEFTR%', '%CIPROFLOXACINO%', '%CLARITROMICINA%',
                        '%CLINDAMICINA%', '%COLISTINA%', '%ERTAPENEM%', '%GENTAMICINA%', '%IMIPENEM%',
                        '%LEVOFLOXACINO%', '%LINEZOLIDA%', '%MEROPENEM%', '%METRONIDAZOL%', '%MOXIFLOXACINO%',
                        '%OXACILINA%', '%PIPERACILINA%', '%POLIMIXINA%', '%TAZOBACTAM%', '%TIGECICLINA%',
                        '%VANCOMICINA%'
                    ]) THEN 1
                    WHEN UPPER("Name") LIKE ANY (ARRAY[
                        '%ABAIXADOR%', '%AGULHA%', '%ALGODAO%', '%APARELHO P/ TRICOTOMIA%', '%ATADURA%', '%AVENTAL%',
                        '%BOLSA%', '%CAMPO%', '%CANULA%', '%CATETER%', '%COLETOR%', '%COMPRESSA%', '%CONECTOR%',
                        '%CURATIVO%', '%DISPOSITIVO%', '%DRENO%', '%ELETRODO%', '%EQUIPO%', '%ESPARADRAPO%',
                        '%EXTENSOR%', '%FIO %', '%FIXADOR%', '%FRALDA%', '%GAZE%', '%LANCETA%', '%LAMINA%',
                        '%LUVA%', '%MANTA%', '%MASCARA%', '%PAPEL%', '%SACO%', '%SCALP%', '%SERINGA%', '%SONDA%',
                        '%TESTE RAPIDO%', '%TIRA TESTE%', '%TORNEIRA%', '%TRANSDUTOR%', '%TUBO%'
                    ]) THEN 5
                    ELSE 0
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "items");
        }
    }
}
