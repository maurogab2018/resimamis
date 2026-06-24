-- =============================================================================
-- Localidades de Córdoba (260 municipios oficiales, provincia idProvincia = 14)
-- Fuente: municipios con carta orgánica / ley de creación (Wikipedia Córdoba AR)
--
-- Ya existentes en tu BD (no se duplican):
--   1 Córdoba Capital | 2 Villa María | 3 Río Cuarto
--
-- Idempotente: INSERT solo si no existe el mismo nombre + idProvincia.
-- idLocalidad es identity: los nuevos registros continúan desde 4 en adelante.
-- =============================================================================

BEGIN;

SET client_encoding = 'UTF8';

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Achiras', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Achiras' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Adelia María', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Adelia María' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Agua de Oro', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Agua de Oro' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alcira Gigena', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alcira Gigena' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alejandro Roca', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alejandro Roca' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alejo Ledesma', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alejo Ledesma' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alicia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alicia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Almafuerte', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Almafuerte' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alpa Corral', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alpa Corral' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alta Gracia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alta Gracia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Alto Alegre', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Alto Alegre' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Altos de Chipión', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Altos de Chipión' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Anisacate', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Anisacate' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Arias', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Arias' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Arroyito', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Arroyito' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Arroyo Algodón', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Arroyo Algodón' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Arroyo Cabral', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Arroyo Cabral' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ausonia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ausonia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ballesteros', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ballesteros' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ballesteros Sud', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ballesteros Sud' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Balnearia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Balnearia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Bell Ville', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Bell Ville' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Bengolea', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Bengolea' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Benjamín Gould', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Benjamín Gould' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Berrotarán', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Berrotarán' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Bialet Massé', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Bialet Massé' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Bouwer', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Bouwer' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Brinkmann', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Brinkmann' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Buchardo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Buchardo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Bulnes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Bulnes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Calchín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Calchín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Calchín Oeste', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Calchín Oeste' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Camilo Aldao', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Camilo Aldao' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Canals', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Canals' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cañada de Luque', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cañada de Luque' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Capilla del Carmen', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Capilla del Carmen' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Capilla del Monte', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Capilla del Monte' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Capitán General Bernardo O''Higgins', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Capitán General Bernardo O''Higgins' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Carnerillo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Carnerillo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Carrilobo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Carrilobo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cavanagh', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cavanagh' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Chaján', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Chaján' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Charras', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Charras' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Chazón', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Chazón' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Chilibroste', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Chilibroste' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cintra', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cintra' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colazo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colazo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Almada', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Almada' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Bismarck', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Bismarck' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Caroya', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Caroya' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Italiana', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Italiana' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Marina', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Marina' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Prosperidad', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Prosperidad' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia San Bartolomé', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia San Bartolomé' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Tirolesa', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Tirolesa' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Colonia Vignaud', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Colonia Vignaud' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Córdoba Capital', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Córdoba Capital' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Coronel Baigorria', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Coronel Baigorria' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Coronel Moldes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Coronel Moldes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Corral de Bustos-Ifflinger', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Corral de Bustos-Ifflinger' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Corralito', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Corralito' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cosquín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cosquín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Costa Sacate', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Costa Sacate' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cruz Alta', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cruz Alta' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Cruz del Eje', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Cruz del Eje' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Dalmacio Vélez(Dalmacio Vélez Sársfield)', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Dalmacio Vélez(Dalmacio Vélez Sársfield)' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Deán Funes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Deán Funes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Del Campillo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Del Campillo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Despeñaderos', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Despeñaderos' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Devoto', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Devoto' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'El Arañado', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'El Arañado' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'El Brete', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'El Brete' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'El Fortín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'El Fortín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'El Tío', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'El Tío' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Elena', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Elena' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Embalse', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Embalse' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Estación General Paz', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Estación General Paz' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Estación Juárez Celman', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Estación Juárez Celman' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Etruria', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Etruria' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Freyre', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Freyre' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'General Baldissera', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'General Baldissera' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'General Cabrera', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'General Cabrera' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'General Deheza', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'General Deheza' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'General Levalle', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'General Levalle' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'General Roca', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'General Roca' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Guatimozín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Guatimozín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Hernando', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Hernando' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Huanchilla', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Huanchilla' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Huerta Grande', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Huerta Grande' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Huinca Renancó', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Huinca Renancó' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Idiazábal', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Idiazábal' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Inriville', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Inriville' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Isla Verde', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Isla Verde' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Italó', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Italó' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'James Craik', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'James Craik' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Jesús María', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Jesús María' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Jovita', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Jovita' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Justiniano Posse', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Justiniano Posse' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Calera', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Calera' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Carlota', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Carlota' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Cautiva', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Cautiva' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Cesira', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Cesira' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Cruz', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Cruz' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Cumbre', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Cumbre' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Falda', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Falda' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Francia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Francia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Granja', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Granja' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Laguna', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Laguna' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Palestina', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Palestina' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Paquita', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Paquita' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Para', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Para' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Paz', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Paz' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Playosa', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Playosa' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Puerta', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Puerta' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'La Tordilla', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'La Tordilla' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Laborde', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Laborde' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Laboulaye', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Laboulaye' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Laguna Larga', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Laguna Larga' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Acequias', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Acequias' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Arrias', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Arrias' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Higueras', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Higueras' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Junturas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Junturas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Peñas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Peñas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Perdices', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Perdices' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Tapias', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Tapias' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Varas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Varas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Varillas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Varillas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Las Vertientes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Las Vertientes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Leones', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Leones' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Cerrillos', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Cerrillos' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Cisnes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Cisnes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Cocos', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Cocos' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Cóndores', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Cóndores' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Surgentes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Surgentes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Los Zorros', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Los Zorros' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Lozada', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Lozada' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Luca', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Luca' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Lucio Victorio Mansilla', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Lucio Victorio Mansilla' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Luque', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Luque' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Malagueño', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Malagueño' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Malvinas Argentinas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Malvinas Argentinas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Manfredi', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Manfredi' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Marcos Juárez', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Marcos Juárez' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Marull', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Marull' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Matorrales', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Matorrales' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Mattaldi', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Mattaldi' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Melo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Melo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Mendiolaza', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Mendiolaza' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Mi Granja', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Mi Granja' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Mina Clavero', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Mina Clavero' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Miramar', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Miramar' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte Buey', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte Buey' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte Cristo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte Cristo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte de los Gauchos', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte de los Gauchos' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte Leña', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte Leña' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte Maíz', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte Maíz' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Monte Ralo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Monte Ralo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Morrison', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Morrison' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Morteros', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Morteros' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Noetinger', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Noetinger' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Nono', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Nono' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Obispo Trejo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Obispo Trejo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Olaeta', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Olaeta' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Oliva', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Oliva' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Oncativo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Oncativo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ordóñez', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ordóñez' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pampayasta Sud', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pampayasta Sud' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pascanas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pascanas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pasco', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pasco' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pilar', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pilar' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Piquillín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Piquillín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Porteña', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Porteña' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pozo del Molle', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pozo del Molle' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Pueblo Italiano', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Pueblo Italiano' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Quebracho Herrado', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Quebracho Herrado' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Quilino', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Quilino' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Reducción', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Reducción' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río Ceballos', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río Ceballos' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río Cuarto', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río Cuarto' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río de los Sauces', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río de los Sauces' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río Primero', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río Primero' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río Segundo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río Segundo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Río Tercero', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Río Tercero' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Rosales', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Rosales' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Sacanta', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Sacanta' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Saira', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Saira' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Saldán', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Saldán' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Salsacate', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Salsacate' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Salsipuedes', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Salsipuedes' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Sampacho', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Sampacho' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Agustín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Agustín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Antonio de Arredondo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Antonio de Arredondo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Antonio de Litín', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Antonio de Litín' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Basilio', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Basilio' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Carlos Minas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Carlos Minas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Esteban', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Esteban' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Francisco', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Francisco' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Francisco del Chañar', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Francisco del Chañar' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Javier y Yacanto', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Javier y Yacanto' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San José', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San José' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San José de la Dormida', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San José de la Dormida' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San José de las Salinas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San José de las Salinas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Marcos Sierras', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Marcos Sierras' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Marcos Sud', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Marcos Sud' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Pedro', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Pedro' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'San Pedro Norte', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'San Pedro Norte' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Santa Catalina(Holmberg)', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Santa Catalina(Holmberg)' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Santa Eufemia', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Santa Eufemia' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Santa María de Punilla', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Santa María de Punilla' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Santa Rosa de Calamuchita', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Santa Rosa de Calamuchita' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Santiago Temple', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Santiago Temple' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Sarmiento', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Sarmiento' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Saturnino María Laspiur', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Saturnino María Laspiur' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Sebastián Elcano', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Sebastián Elcano' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Seeber', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Seeber' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Serrano', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Serrano' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Serrezuela', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Serrezuela' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Silvio Pellico', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Silvio Pellico' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Sinsacate', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Sinsacate' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Tancacha', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Tancacha' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Tanti', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Tanti' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ticino', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ticino' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Tío Pujio', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Tío Pujio' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Toledo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Toledo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Tosquita', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Tosquita' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Tránsito', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Tránsito' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Ucacha', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Ucacha' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Unquillo', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Unquillo' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Valle Hermoso', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Valle Hermoso' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Viamonte', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Viamonte' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Vicuña Mackenna', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Vicuña Mackenna' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Allende', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Allende' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Ascasubi', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Ascasubi' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Carlos Paz', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Carlos Paz' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Concepción del Tío', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Concepción del Tío' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Cura Brochero', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Cura Brochero' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa de las Rosas', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa de las Rosas' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa de María', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa de María' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa de Soto', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa de Soto' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa del Dique', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa del Dique' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa del Rosario', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa del Rosario' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa del Totoral', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa del Totoral' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Dolores', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Dolores' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Fontana', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Fontana' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa General Belgrano', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa General Belgrano' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Giardino', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Giardino' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Huidobro', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Huidobro' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa María', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa María' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Nueva', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Nueva' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Parque Santa Ana', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Parque Santa Ana' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Río Icho Cruz(Villa Icho Cruz)Municipio por ley n.º 9974 sanc. el 5 de julio de 2011.', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Río Icho Cruz(Villa Icho Cruz)Municipio por ley n.º 9974 sanc. el 5 de julio de 2011.' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Rossi', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Rossi' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Rumipal', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Rumipal' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Santa Cruz del Lago', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Santa Cruz del Lago' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Santa Rosa(Santa Rosa de Río Primero)', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Santa Rosa(Santa Rosa de Río Primero)' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Sarmiento', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Sarmiento' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Tulumba', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Tulumba' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Valeria', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Valeria' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Villa Yacanto(Yacanto de Calamuchita)', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Villa Yacanto(Yacanto de Calamuchita)' AND "idProvincia" = 14
);

INSERT INTO "LOCALIDAD" ("nombre", "idProvincia")
SELECT 'Wenceslao Escalante', 14
WHERE NOT EXISTS (
  SELECT 1 FROM "LOCALIDAD" WHERE "nombre" = 'Wenceslao Escalante' AND "idProvincia" = 14
);

COMMIT;

-- Verificación:
-- SELECT COUNT(*) FROM "LOCALIDAD" WHERE "idProvincia" = 14;  -- esperado: 260

