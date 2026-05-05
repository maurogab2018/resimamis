-- =============================================================================
-- Bebés disponibles para abrazo y para generación de asignaciones (abrazos).
-- Pone "IdEstado" en el estado "Sin abrazar" (ámbito Bebes en ESTADO).
-- No toca bebés en estado "Eliminado" (baja lógica).
--
-- Ejecutar contra la misma base que usa Render (psql, pgAdmin, DBeaver).
--
-- Nota: NegAsignacion / obtenerBebesAbrazar también excluyen bebés que tengan
-- hoy una ASIGNACION con fechaHoraInicio dentro del día (abrazo ya iniciado).
-- Si tras este UPDATE aún no aparecen, revisá ASIGNACION para ese bebé/fecha.
-- =============================================================================

WITH ids AS (
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
     LIMIT 1) AS id_eliminado
)
UPDATE "BEBE" b
SET "IdEstado" = ids.id_sin_abrazar
FROM ids
WHERE ids.id_sin_abrazar IS NOT NULL
  AND (
    ids.id_eliminado IS NULL
    OR b."IdEstado" IS NULL
    OR b."IdEstado" <> ids.id_eliminado
  );

-- Verificación rápida (opcional):
-- SELECT b."ID", b."nombre", e."nombre" AS estado_bebe
-- FROM "BEBE" b
-- LEFT JOIN "ESTADO" e ON e."idEstado" = b."IdEstado"
-- LEFT JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
-- ORDER BY b."ID";
