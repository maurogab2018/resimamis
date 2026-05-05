using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class MadreIdEstadoEliminadoEstados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO ""AMBITO"" (""nombre"", ""descripcion"")
SELECT 'Madres', 'Ámbito de estados de madres'
WHERE NOT EXISTS (SELECT 1 FROM ""AMBITO"" WHERE ""nombre"" = 'Madres');

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Voluntarias'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Voluntarias' AND e.""nombre"" = 'Eliminado'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Bebes'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Bebes' AND e.""nombre"" = 'Eliminado'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Madres'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Madres' AND e.""nombre"" = 'Eliminado'
);
");

            migrationBuilder.AddColumn<int>(
                name: "IdEstado",
                table: "MADRE",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MADRE_IdEstado",
                table: "MADRE",
                column: "IdEstado");

            migrationBuilder.AddForeignKey(
                name: "FK_MADRE_ESTADO_IdEstado",
                table: "MADRE",
                column: "IdEstado",
                principalTable: "ESTADO",
                principalColumn: "idEstado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MADRE_ESTADO_IdEstado",
                table: "MADRE");

            migrationBuilder.DropIndex(
                name: "IX_MADRE_IdEstado",
                table: "MADRE");

            migrationBuilder.DropColumn(
                name: "IdEstado",
                table: "MADRE");

            // No eliminamos filas AMBITO/ESTADO creadas (podrían referenciarse).
        }
    }
}
