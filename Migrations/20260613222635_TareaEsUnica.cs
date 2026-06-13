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
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'TAREA' AND column_name = 'esUnica') THEN
    ALTER TABLE ""TAREA"" ADD COLUMN ""esUnica"" BOOLEAN NOT NULL DEFAULT FALSE;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
UPDATE ""TAREA"" SET ""esUnica"" = TRUE
WHERE LOWER(""nombre"") LIKE '%abrazo%'
  AND ""esUnica"" IS NOT TRUE;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'MOVIMIENTOSTOCK'
      AND column_name = 'observacion' AND is_nullable = 'NO') THEN
    ALTER TABLE ""MOVIMIENTOSTOCK"" ALTER COLUMN ""observacion"" DROP NOT NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'MOVIMIENTOSTOCK'
      AND column_name = 'esEntrada' AND is_nullable = 'NO') THEN
    ALTER TABLE ""MOVIMIENTOSTOCK"" ALTER COLUMN ""esEntrada"" DROP NOT NULL;
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'TAREA' AND column_name = 'esUnica') THEN
    ALTER TABLE ""TAREA"" DROP COLUMN ""esUnica"";
  END IF;
END $$;
");
        }
    }
}
