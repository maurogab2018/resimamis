-- =============================================================================
-- Deja bebés alineados con obtenerBebesAbrazar() / GET api/Bebe/disponibles-abrazo:
--
--   • IdEstado = "Sin abrazar" (ámbito Bebes), no "Eliminado"
--   • Sin ASIGNACION hoy que bloquee el listado:
--       fechaHoraAsignacion en [inicio día AR, fin día AR)
--       Y fechaHoraInicio en ese mismo rango (abrazo ya iniciado hoy)
--
-- Paso 1 cierra esas asignaciones (fechaHoraFin). Paso 2 pone bebés en Sin abrazar.
-- Rango del día = calendario Argentina (misma idea que NegConversorFecha).
-- =============================================================================

WITH bounds AS (
  SELECT
    (date_trunc('day', (now() AT TIME ZONE 'America/Argentina/Buenos_Aires'))
      AT TIME ZONE 'America/Argentina/Buenos_Aires') AS inicio_dia_utc,
    (date_trunc('day', (now() AT TIME ZONE 'America/Argentina/Buenos_Aires'))
      AT TIME ZONE 'America/Argentina/Buenos_Aires') + interval '1 day' AS fin_dia_utc
),
estados AS (
  SELECT
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Sin abrazar'
     LIMIT 1) AS id_sin_abrazar,
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Eliminado'
     LIMIT 1) AS id_elim_bebe,
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Asignaciones' AND e."nombre" = 'Eliminado'
     LIMIT 1) AS id_elim_asig
),
-- Paso 1: quitar bloqueo del .Any() (abrazo iniciado hoy sin finalizar)
cerradas AS (
  UPDATE "ASIGNACION" a
  SET
    "fechaHoraFin" = NOW(),
    "comentario" = CASE
      WHEN a."comentario" IS NULL OR btrim(a."comentario") = '' THEN
        'Cierre automático (script): abrazo iniciado hoy sin finalizar.'
      WHEN a."comentario" LIKE '%Cierre automático (script)%' THEN a."comentario"
      ELSE 'Cierre automático (script): abrazo iniciado hoy sin finalizar. | ' || a."comentario"
    END
  FROM bounds b, estados e
  WHERE a."idBebe" IS NOT NULL
    AND a."fechaHoraFin" IS NULL
    AND a."fechaHoraInicio" IS NOT NULL
    AND a."fechaHoraAsignacion" >= b.inicio_dia_utc
    AND a."fechaHoraAsignacion" < b.fin_dia_utc
    AND a."fechaHoraInicio" >= b.inicio_dia_utc
    AND a."fechaHoraInicio" < b.fin_dia_utc
    AND (e.id_elim_asig IS NULL OR a."idEstado" IS NULL OR a."idEstado" <> e.id_elim_asig)
  RETURNING a."idBebe"
)
-- Paso 2: estado operativo del bebé para el listado
UPDATE "BEBE" be
SET "IdEstado" = e.id_sin_abrazar
FROM estados e
WHERE e.id_sin_abrazar IS NOT NULL
  AND (e.id_elim_bebe IS NULL OR be."IdEstado" IS NULL OR be."IdEstado" <> e.id_elim_bebe);

-- Verificación (misma lógica que el backend):
-- WITH bounds AS ( ... mismo bounds ... ),
-- estados AS ( ... ),
-- id_elim_asig AS (SELECT id_elim_asig FROM estados)
-- SELECT b."ID", b."nombre", est."nombre" AS estado_bebe
-- FROM "BEBE" b
-- INNER JOIN "ESTADO" est ON est."idEstado" = b."IdEstado"
-- INNER JOIN "AMBITO" amb ON amb."idAmbito" = est."idAmbito"
-- CROSS JOIN bounds bnd
-- CROSS JOIN estados e
-- WHERE est."nombre" = 'Sin abrazar' AND amb."nombre" = 'Bebes'
--   AND (e.id_elim_bebe IS NULL OR b."IdEstado" <> e.id_elim_bebe)
--   AND NOT EXISTS (
--     SELECT 1 FROM "ASIGNACION" a
--     WHERE a."idBebe" = b."ID"
--       AND a."fechaHoraAsignacion" >= bnd.inicio_dia_utc AND a."fechaHoraAsignacion" < bnd.fin_dia_utc
--       AND a."fechaHoraInicio" IS NOT NULL
--       AND a."fechaHoraInicio" >= bnd.inicio_dia_utc AND a."fechaHoraInicio" < bnd.fin_dia_utc
--       AND (e.id_elim_asig IS NULL OR a."idEstado" IS NULL OR a."idEstado" <> e.id_elim_asig)
--   )
-- ORDER BY b."nombre";
