using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class BebeIdLocalidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BEBE"
                ADD COLUMN IF NOT EXISTS "IdLocalidad" integer NULL;

                CREATE INDEX IF NOT EXISTS "IX_BEBE_IdLocalidad" ON "BEBE" ("IdLocalidad");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_BEBE_LOCALIDAD_IdLocalidad'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_BEBE_LOCALIDAD'
                    ) THEN
                        ALTER TABLE "BEBE"
                        ADD CONSTRAINT "FK_BEBE_LOCALIDAD_IdLocalidad"
                        FOREIGN KEY ("IdLocalidad")
                        REFERENCES "LOCALIDAD"("idLocalidad");
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_MADRE_Localidad" ON "MADRE" ("Localidad");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_MADRE_LOCALIDAD_Localidad'
                    ) THEN
                        ALTER TABLE "MADRE"
                        ADD CONSTRAINT "FK_MADRE_LOCALIDAD_Localidad"
                        FOREIGN KEY ("Localidad")
                        REFERENCES "LOCALIDAD"("idLocalidad")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BEBE" DROP CONSTRAINT IF EXISTS "FK_BEBE_LOCALIDAD_IdLocalidad";
                ALTER TABLE "BEBE" DROP CONSTRAINT IF EXISTS "FK_BEBE_LOCALIDAD";
                DROP INDEX IF EXISTS "IX_BEBE_IdLocalidad";
                ALTER TABLE "BEBE" DROP COLUMN IF EXISTS "IdLocalidad";

                ALTER TABLE "MADRE" DROP CONSTRAINT IF EXISTS "FK_MADRE_LOCALIDAD_Localidad";
                DROP INDEX IF EXISTS "IX_MADRE_Localidad";
                """);
        }
    }
}
