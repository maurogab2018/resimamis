using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class TareaEsUnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "esUnica",
                table: "TAREA",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
UPDATE ""TAREA"" SET ""esUnica"" = TRUE
WHERE LOWER(""nombre"") LIKE '%abrazo%kangaroo%' OR LOWER(""nombre"") LIKE '%abrazo kangaroo%';
");

            migrationBuilder.AlterColumn<string>(
                name: "observacion",
                table: "MOVIMIENTOSTOCK",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "esEntrada",
                table: "MOVIMIENTOSTOCK",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "esUnica",
                table: "TAREA");

            migrationBuilder.AlterColumn<string>(
                name: "observacion",
                table: "MOVIMIENTOSTOCK",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "esEntrada",
                table: "MOVIMIENTOSTOCK",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
