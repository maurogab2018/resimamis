-- =============================================================================
-- Horarios completos: Lunes a Viernes × Mañana, Tarde, Noche, Jornada completa
-- PostgreSQL / Npgsql (HoraIngreso y HoraSalida = interval / TimeSpan)
--
-- Idempotente: no duplica si ya existe el mismo IdDia + Turno.
-- No borra horarios existentes (VOLUNTARIAHORARIO puede referenciarlos).
-- =============================================================================

BEGIN;

SET client_encoding = 'UTF8';

-- Días laborables (por si la tabla DIA está vacía o incompleta)
INSERT INTO "DIA" ("IdDia", "Descripcion")
VALUES
  (1, 'Lunes'),
  (2, 'Martes'),
  (3, 'Miércoles'),
  (4, 'Jueves'),
  (5, 'Viernes')
ON CONFLICT ("IdDia") DO UPDATE
SET "Descripcion" = EXCLUDED."Descripcion";

-- Turnos por día
--   Mañana           08:00 - 12:00
--   Tarde            14:00 - 18:00
--   Noche            18:00 - 22:00
--   Jornada completa 08:00 - 18:00

INSERT INTO "HORARIO" ("IdDia", "Turno", "HoraIngreso", "HoraSalida")
SELECT v."IdDia", v."Turno", v."HoraIngreso", v."HoraSalida"
FROM (VALUES
  (1, 'Mañana',           INTERVAL '8 hours',  INTERVAL '12 hours'),
  (1, 'Tarde',            INTERVAL '14 hours', INTERVAL '18 hours'),
  (1, 'Noche',            INTERVAL '18 hours', INTERVAL '22 hours'),
  (1, 'Jornada completa', INTERVAL '8 hours',  INTERVAL '18 hours'),

  (2, 'Mañana',           INTERVAL '8 hours',  INTERVAL '12 hours'),
  (2, 'Tarde',            INTERVAL '14 hours', INTERVAL '18 hours'),
  (2, 'Noche',            INTERVAL '18 hours', INTERVAL '22 hours'),
  (2, 'Jornada completa', INTERVAL '8 hours',  INTERVAL '18 hours'),

  (3, 'Mañana',           INTERVAL '8 hours',  INTERVAL '12 hours'),
  (3, 'Tarde',            INTERVAL '14 hours', INTERVAL '18 hours'),
  (3, 'Noche',            INTERVAL '18 hours', INTERVAL '22 hours'),
  (3, 'Jornada completa', INTERVAL '8 hours',  INTERVAL '18 hours'),

  (4, 'Mañana',           INTERVAL '8 hours',  INTERVAL '12 hours'),
  (4, 'Tarde',            INTERVAL '14 hours', INTERVAL '18 hours'),
  (4, 'Noche',            INTERVAL '18 hours', INTERVAL '22 hours'),
  (4, 'Jornada completa', INTERVAL '8 hours',  INTERVAL '18 hours'),

  (5, 'Mañana',           INTERVAL '8 hours',  INTERVAL '12 hours'),
  (5, 'Tarde',            INTERVAL '14 hours', INTERVAL '18 hours'),
  (5, 'Noche',            INTERVAL '18 hours', INTERVAL '22 hours'),
  (5, 'Jornada completa', INTERVAL '8 hours',  INTERVAL '18 hours')
) AS v("IdDia", "Turno", "HoraIngreso", "HoraSalida")
WHERE NOT EXISTS (
  SELECT 1
  FROM "HORARIO" h
  WHERE h."IdDia" = v."IdDia"
    AND h."Turno" = v."Turno"
);

COMMIT;

-- Verificación
SELECT h."IdHorario", h."IdDia", d."Descripcion" AS dia, h."Turno",
       h."HoraIngreso", h."HoraSalida"
FROM "HORARIO" h
JOIN "DIA" d ON d."IdDia" = h."IdDia"
WHERE h."IdDia" BETWEEN 1 AND 5
ORDER BY h."IdDia", h."HoraIngreso", h."Turno";
