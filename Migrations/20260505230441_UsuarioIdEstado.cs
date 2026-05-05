using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioIdEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'USUARIO' AND column_name = 'idEstado') THEN
    ALTER TABLE ""USUARIO"" ADD COLUMN ""idEstado"" INTEGER NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_USUARIO_idEstado"" ON ""USUARIO"" (""idEstado"");");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint c
    INNER JOIN pg_class rel ON rel.oid = c.conrelid
    WHERE rel.relname = 'USUARIO'
      AND c.contype = 'f'
      AND (c.conname = 'FK_USUARIO_ESTADO_idEstado' OR c.conname = 'fk_usuario_estado')
  ) THEN
    ALTER TABLE ""USUARIO""
      ADD CONSTRAINT ""FK_USUARIO_ESTADO_idEstado""
      FOREIGN KEY (""idEstado"") REFERENCES ""ESTADO"" (""idEstado"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
INSERT INTO ""AMBITO"" (""nombre"", ""descripcion"")
SELECT 'Usuarios', 'Ámbito de estados de usuarios'
WHERE NOT EXISTS (SELECT 1 FROM ""AMBITO"" WHERE ""nombre"" = 'Usuarios');

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Creado', 'Usuario registrado', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Usuarios'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Usuarios' AND e.""nombre"" = 'Creado'
);

INSERT INTO ""ESTADO"" (""nombre"", ""descripcion"", ""idAmbito"")
SELECT 'Eliminado', 'Baja lógica', a.""idAmbito""
FROM ""AMBITO"" a
WHERE a.""nombre"" = 'Usuarios'
AND NOT EXISTS (
  SELECT 1 FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Usuarios' AND e.""nombre"" = 'Eliminado'
);

UPDATE ""USUARIO"" u
SET ""idEstado"" = (
  SELECT e.""idEstado"" FROM ""ESTADO"" e
  INNER JOIN ""AMBITO"" am ON e.""idAmbito"" = am.""idAmbito""
  WHERE am.""nombre"" = 'Usuarios' AND e.""nombre"" = 'Creado'
  LIMIT 1
)
WHERE u.""idEstado"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""USUARIO"" DROP CONSTRAINT IF EXISTS ""FK_USUARIO_ESTADO_idEstado"";
ALTER TABLE ""USUARIO"" DROP CONSTRAINT IF EXISTS ""fk_usuario_estado"";
DROP INDEX IF EXISTS ""IX_USUARIO_idEstado"";
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'USUARIO' AND column_name = 'idEstado') THEN
    ALTER TABLE ""USUARIO"" DROP COLUMN ""idEstado"";
  END IF;
END $$;
");
        }
    }
}
