-- =============================================================================
-- Roles de voluntaria: solo Voluntaria (2) y Coordinadora (3).
-- Coordinadora reemplaza al rol legacy Administrativa / Admin / Administrador.
--
-- Ejecutar en PostgreSQL (Render, psql, etc.).
-- Idempotente: se puede correr más de una vez.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) Asegurar filas base en ROL (si faltan)
-- -----------------------------------------------------------------------------
INSERT INTO "ROL" ("Nombre", "Descripcion")
SELECT 'Voluntaria', 'Abrazo y acompañamiento'
WHERE NOT EXISTS (
  SELECT 1 FROM "ROL" WHERE LOWER("Nombre") = 'voluntaria'
);

INSERT INTO "ROL" ("Nombre", "Descripcion")
SELECT 'Coordinadora', 'Coordinación y gestión administrativa'
WHERE NOT EXISTS (
  SELECT 1 FROM "ROL" WHERE LOWER("Nombre") = 'coordinadora'
);

-- -----------------------------------------------------------------------------
-- 2) Renombrar rol legacy Administrativa -> Coordinadora (si no hay Coordinadora)
-- -----------------------------------------------------------------------------
UPDATE "ROL"
SET
  "Nombre" = 'Coordinadora',
  "Descripcion" = 'Coordinación y gestión administrativa'
WHERE LOWER("Nombre") IN ('administrativa', 'administrador', 'admin')
  AND NOT EXISTS (
    SELECT 1 FROM "ROL" r2 WHERE LOWER(r2."Nombre") = 'coordinadora'
  );

-- -----------------------------------------------------------------------------
-- 3) Pasar voluntarias del rol legacy a Coordinadora
-- -----------------------------------------------------------------------------
WITH ids AS (
  SELECT
    (SELECT "IdRol" FROM "ROL" WHERE LOWER("Nombre") = 'coordinadora' ORDER BY "IdRol" LIMIT 1) AS id_coordinadora
),
legacy_admin AS (
  SELECT "IdRol"
  FROM "ROL"
  WHERE LOWER("Nombre") IN ('administrativa', 'administrador', 'admin')
)
UPDATE "VOLUNTARIA" v
SET "IdRol" = ids.id_coordinadora
FROM ids
WHERE ids.id_coordinadora IS NOT NULL
  AND v."IdRol" IN (SELECT "IdRol" FROM legacy_admin)
  AND v."IdRol" IS DISTINCT FROM ids.id_coordinadora;

-- -----------------------------------------------------------------------------
-- 4) Forzar IdRol = 3 cuando en prod Coordinadora es id 3 y quedó mal asignado
--    (solo si existe Coordinadora con IdRol = 3)
-- -----------------------------------------------------------------------------
UPDATE "VOLUNTARIA" v
SET "IdRol" = 3
WHERE EXISTS (SELECT 1 FROM "ROL" WHERE "IdRol" = 3 AND LOWER("Nombre") = 'coordinadora')
  AND v."IdRol" IN (
    SELECT r."IdRol"
    FROM "ROL" r
    WHERE LOWER(r."Nombre") IN ('administrativa', 'administrador', 'admin')
  );

-- -----------------------------------------------------------------------------
-- 5) OPCIONAL — Eliminar filas ROL legacy si ya no hay voluntarias referenciándolas
--    Descomentar si querés limpiar la tabla ROL.
-- -----------------------------------------------------------------------------
/*
DELETE FROM "ROL" r
WHERE LOWER(r."Nombre") IN ('administrativa', 'administrador', 'admin')
  AND NOT EXISTS (SELECT 1 FROM "VOLUNTARIA" v WHERE v."IdRol" = r."IdRol");
*/

-- -----------------------------------------------------------------------------
-- Verificación
-- -----------------------------------------------------------------------------
SELECT "IdRol", "Nombre", "Descripcion" FROM "ROL" ORDER BY "IdRol";

SELECT v."IdVoluntaria", v."Nombre", v."Apellido", v."IdRol", r."Nombre" AS rol
FROM "VOLUNTARIA" v
LEFT JOIN "ROL" r ON r."IdRol" = v."IdRol"
ORDER BY v."IdVoluntaria";
