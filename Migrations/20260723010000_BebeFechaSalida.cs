using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class BebeFechaSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'BEBE'
      AND column_name = 'FechaSalida') THEN
    ALTER TABLE ""BEBE"" ADD COLUMN ""FechaSalida"" timestamp without time zone NULL;
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
    WHERE table_schema = 'public' AND table_name = 'BEBE'
      AND column_name = 'FechaSalida') THEN
    ALTER TABLE ""BEBE"" DROP COLUMN ""FechaSalida"";
  END IF;
END $$;
");
        }
    }
}
