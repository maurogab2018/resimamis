-- =============================================================================
-- Voluntarias “libres” para listados / generación de abrazos (según el backend).
--
-- 1) VOLUNTARIA: pasa a estado "Activa" (ámbito Voluntarias).
--    No modifica voluntarias dadas de baja ("Eliminado").
--
-- 2) OPCIONAL — Cerrar asistencias abiertas (entrada sin salida). Útil en test
--    si quedaron filas colgadas y bloquean lógica o duplican ingresos.
--
-- 3) OPCIONAL — Insertar asistencia "Creada" de hoy para voluntarias Activas que
--    no tengan ninguna asistencia abierta. Sin esto, obtenerVoluntariasLibres /
--    generar pueden no ver a nadie: exigen ingreso hoy y salida NULL.
--
-- Ejecutar en PostgreSQL (Render, psql, etc.). Revisá cada bloque antes de correr.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) OBLIGATORIO (recomendado): IdEstado = Activa
-- -----------------------------------------------------------------------------
WITH ids AS (
  SELECT
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Voluntarias' AND e."nombre" = 'Activa'
     LIMIT 1) AS id_activa,
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Voluntarias' AND e."nombre" = 'Eliminado'
     LIMIT 1) AS id_elim_vol
)
UPDATE "VOLUNTARIA" v
SET "IdEstado" = ids.id_activa
FROM ids
WHERE ids.id_activa IS NOT NULL
  AND (ids.id_elim_vol IS NULL OR v."IdEstado" IS DISTINCT FROM ids.id_elim_vol);


-- -----------------------------------------------------------------------------
-- 2) OPCIONAL — Cerrar todas las asistencias sin salida (descomentar si aplica).
--     En producción usá con cuidado (afecta a todas las voluntarias).
-- -----------------------------------------------------------------------------
/*
UPDATE "ASISTENCIA" a
SET "FechaHoraSalida" = NOW()
WHERE a."FechaHoraSalida" IS NULL
  AND a."FechaHoraIngreso" IS NOT NULL;
*/


-- -----------------------------------------------------------------------------
-- 3) OPCIONAL — Ingreso de asistencia hoy para Activas sin asistencia abierta.
--     Descomentá después del (1) y, si hace falta, del (2).
-- -----------------------------------------------------------------------------
/*
WITH ids AS (
  SELECT
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Voluntarias' AND e."nombre" = 'Activa'
     LIMIT 1) AS id_activa,
    (SELECT e."idEstado"
     FROM "ESTADO" e
     INNER JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito"
     WHERE amb."nombre" = 'Asistencias' AND e."nombre" = 'Creada'
     LIMIT 1) AS id_asist_creada
)
INSERT INTO "ASISTENCIA" ("IdVoluntaria", "IdHorario", "FechaHoraIngreso", "FechaHoraSalida", "idEstado")
SELECT v."IdVoluntaria", NULL, NOW(), NULL, ids.id_asist_creada
FROM "VOLUNTARIA" v
CROSS JOIN ids
WHERE ids.id_activa IS NOT NULL
  AND ids.id_asist_creada IS NOT NULL
  AND v."IdEstado" = ids.id_activa
  AND NOT EXISTS (
    SELECT 1
    FROM "ASISTENCIA" a
    WHERE a."IdVoluntaria" = v."IdVoluntaria"
      AND a."FechaHoraSalida" IS NULL
  );
*/
