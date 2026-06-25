using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class SalaActiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'SALA'
      AND column_name = 'Activa') THEN
    ALTER TABLE ""SALA"" ADD COLUMN ""Activa"" BOOLEAN NOT NULL DEFAULT TRUE;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"UPDATE ""SALA"" SET ""Activa"" = TRUE WHERE ""Activa"" IS NOT TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'SALA'
      AND column_name = 'Activa') THEN
    ALTER TABLE ""SALA"" DROP COLUMN ""Activa"";
  END IF;
END $$;
");
        }
    }
}
