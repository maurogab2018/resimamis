using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class AsignacionCierreAutomatico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente como el resto de las migraciones posteriores a Visita.
            // El backfill marca los cierres automáticos que hasta ahora solo se
            // reconocían por el texto del comentario.
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'ASIGNACION'
      AND column_name = 'cierreAutomatico') THEN
    ALTER TABLE ""ASIGNACION"" ADD COLUMN ""cierreAutomatico"" boolean NOT NULL DEFAULT false;

    UPDATE ""ASIGNACION""
       SET ""cierreAutomatico"" = true
     WHERE ""comentario"" LIKE '%Cierre automático%';
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
    WHERE table_schema = 'public' AND table_name = 'ASIGNACION'
      AND column_name = 'cierreAutomatico') THEN
    ALTER TABLE ""ASIGNACION"" DROP COLUMN ""cierreAutomatico"";
  END IF;
END $$;
");
        }
    }
}
