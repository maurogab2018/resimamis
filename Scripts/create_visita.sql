-- Tabla VISITA: registro de visitas familiares a bebés en NEO.
-- Ejecutar en PostgreSQL si no se aplican migraciones EF automáticamente.

CREATE TABLE IF NOT EXISTS "VISITA" (
    "idVisita" SERIAL PRIMARY KEY,
    "idBebe" INTEGER NOT NULL,
    "nombreVisitante" TEXT NOT NULL,
    "familiar" TEXT NOT NULL,
    "fechaHoraVisita" TIMESTAMPTZ NOT NULL,
    "observacion" TEXT NULL,
    "documentoVisitante" INTEGER NULL,
    "telefonoVisitante" BIGINT NULL,
    "Activa" BOOLEAN NOT NULL DEFAULT TRUE,
    "fechaRegistro" TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT "FK_VISITA_BEBE_idBebe"
        FOREIGN KEY ("idBebe") REFERENCES "BEBE" ("ID")
        ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_VISITA_idBebe" ON "VISITA" ("idBebe");
CREATE INDEX IF NOT EXISTS "IX_VISITA_fechaHoraVisita" ON "VISITA" ("fechaHoraVisita");
CREATE INDEX IF NOT EXISTS "IX_VISITA_Activa" ON "VISITA" ("Activa");

COMMENT ON TABLE "VISITA" IS 'Visitas familiares registradas por bebé';
COMMENT ON COLUMN "VISITA"."familiar" IS 'Vínculo con el bebé (Madre, Padre, Abuela, etc.)';
COMMENT ON COLUMN "VISITA"."Activa" IS 'Baja lógica: FALSE = eliminada';
