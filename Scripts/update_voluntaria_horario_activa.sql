-- =============================================================================
-- VOLUNTARIAHORARIO.Activa: baja lógica de disponibilidad por voluntaria.
-- Idempotente. Seguro si la columna ya fue agregada a mano en Render.
-- =============================================================================

DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'VOLUNTARIAHORARIO'
      AND column_name = 'Activa') THEN
    ALTER TABLE "VOLUNTARIAHORARIO" ADD COLUMN "Activa" BOOLEAN NOT NULL DEFAULT TRUE;
  END IF;
END $$;

UPDATE "VOLUNTARIAHORARIO" SET "Activa" = TRUE WHERE "Activa" IS NOT TRUE;
