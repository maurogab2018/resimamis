using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class AsistenciaIdEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'ASISTENCIA' AND column_name = 'idEstado') THEN
    ALTER TABLE ""ASISTENCIA"" ADD COLUMN ""idEstado"" INTEGER NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_ASISTENCIA_idEstado"" ON ""ASISTENCIA"" (""idEstado"");");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint c
    INNER JOIN pg_class rel ON rel.oid = c.conrelid
    WHERE rel.relname = 'ASISTENCIA'
      AND c.contype = 'f'
      AND (c.conname = 'FK_ASISTENCIA_ESTADO_idEstado' OR c.conname = 'fk_asistenciaa_estado')
  ) THEN
    ALTER TABLE ""ASISTENCIA""
      ADD CONSTRAINT ""FK_ASISTENCIA_ESTADO_idEstado""
      FOREIGN KEY (""idEstado"") REFERENCES ""ESTADO"" (""idEstado"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
INSERT INTO ""AMBITO"" (""nombre"", ""descripcion"")
SELECT 'Asistencias', 'Ámbito de estados de asistencias'
WHERE NOT EXISTS (SELECT 1 FROM ""AMBITO"" WHERE ""nombre"" = 'Asistencias');

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Creada', 'Asistencia registrada', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Asistencias'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Asistencias' AND e.""nombre"" = 'Creada'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Asistencias'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Asistencias' AND e.""nombre"" = 'Eliminado'
);

UPDATE ""ASISTENCIA"" ast
SET ""idEstado"" = (
  SELECT e.""idEstado"" FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Asistencias' AND e.""nombre"" = 'Creada'
  LIMIT 1
)
WHERE ast.""idEstado"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""ASISTENCIA"" DROP CONSTRAINT IF EXISTS ""FK_ASISTENCIA_ESTADO_idEstado"";
ALTER TABLE ""ASISTENCIA"" DROP CONSTRAINT IF EXISTS ""fk_asistenciaa_estado"";
DROP INDEX IF EXISTS ""IX_ASISTENCIA_idEstado"";
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'ASISTENCIA' AND column_name = 'idEstado') THEN
    ALTER TABLE ""ASISTENCIA"" DROP COLUMN ""idEstado"";
  END IF;
END $$;
");
        }
    }
}
