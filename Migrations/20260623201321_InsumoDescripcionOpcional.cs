using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class InsumoDescripcionOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'INSUMO'
      AND column_name = 'descripcion' AND is_nullable = 'NO') THEN
    ALTER TABLE ""INSUMO"" ALTER COLUMN ""descripcion"" DROP NOT NULL;
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""INSUMO"" SET ""descripcion"" = '' WHERE ""descripcion"" IS NULL;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'INSUMO'
      AND column_name = 'descripcion' AND is_nullable = 'YES') THEN
    ALTER TABLE ""INSUMO"" ALTER COLUMN ""descripcion"" SET NOT NULL;
  END IF;
END $$;
");
        }
    }
}
