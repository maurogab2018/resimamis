using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class SeedEstadosAsignaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO ""AMBITO"" (""nombre"", ""descripcion"")
SELECT 'Asignaciones', 'Ámbito de estados de asignaciones'
WHERE NOT EXISTS (SELECT 1 FROM ""AMBITO"" WHERE ""nombre"" = 'Asignaciones');

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Creada', 'Asignación registrada', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Asignaciones'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Asignaciones' AND e.""nombre"" = 'Creada'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Asignaciones'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Asignaciones' AND e.""nombre"" = 'Eliminado'
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""ESTADO""
WHERE ""idAmbito"" IN (SELECT ""idAmbito"" FROM ""AMBITO"" WHERE ""nombre"" = 'Asignaciones');

DELETE FROM ""AMBITO"" WHERE ""nombre"" = 'Asignaciones';
");
        }
    }
}
