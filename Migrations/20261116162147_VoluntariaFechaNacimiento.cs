using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class VoluntariaFechaNacimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente, igual que ProveedorActiva/SalaActiva: si la columna ya existe no falla el arranque.
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'VOLUNTARIA'
      AND column_name = 'FechaNacimiento') THEN
    ALTER TABLE ""VOLUNTARIA"" ADD COLUMN ""FechaNacimiento"" timestamp without time zone NULL;
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "VOLUNTARIA");
        }
    }
}
