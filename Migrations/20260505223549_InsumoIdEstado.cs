using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class InsumoIdEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'INSUMO' AND column_name = 'idEstado') THEN
    ALTER TABLE ""INSUMO"" ADD COLUMN ""idEstado"" INTEGER NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_INSUMO_idEstado"" ON ""INSUMO"" (""idEstado"");");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint c
    INNER JOIN pg_class rel ON rel.oid = c.conrelid
    WHERE rel.relname = 'INSUMO'
      AND c.contype = 'f'
      AND (c.conname = 'FK_INSUMO_ESTADO_idEstado' OR c.conname = 'fk_insumo_estado')
  ) THEN
    ALTER TABLE ""INSUMO""
      ADD CONSTRAINT ""FK_INSUMO_ESTADO_idEstado""
      FOREIGN KEY (""idEstado"") REFERENCES ""ESTADO"" (""idEstado"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
INSERT INTO ""AMBITO"" (""nombre"", ""descripcion"")
SELECT 'Insumos', 'Ámbito de estados de insumos'
WHERE NOT EXISTS (SELECT 1 FROM ""AMBITO"" WHERE ""nombre"" = 'Insumos');

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Activo', 'Insumo vigente', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Insumos'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Insumos' AND e.""nombre"" = 'Activo'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Insumos'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Insumos' AND e.""nombre"" = 'Eliminado'
);

UPDATE ""INSUMO"" i
SET ""idEstado"" = (
  SELECT e.""idEstado"" FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" a ON e.""idAmbito"" = a.""idAmbito""
  WHERE a.""nombre"" = 'Insumos' AND e.""nombre"" = 'Activo'
  LIMIT 1
)
WHERE i.""idEstado"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""INSUMO"" DROP CONSTRAINT IF EXISTS ""FK_INSUMO_ESTADO_idEstado"";
ALTER TABLE ""INSUMO"" DROP CONSTRAINT IF EXISTS ""fk_insumo_estado"";
DROP INDEX IF EXISTS ""IX_INSUMO_idEstado"";
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'INSUMO' AND column_name = 'idEstado') THEN
    ALTER TABLE ""INSUMO"" DROP COLUMN ""idEstado"";
  END IF;
END $$;
");
        }
    }
}
