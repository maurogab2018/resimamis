-- =============================================================================
-- Datos de prueba para PostgreSQL (Resimamis)
-- Ejecutar DESPUÉS de aplicar migraciones EF.
-- Pensado para una BD vacía (o truncar antes; ver bloque comentado abajo).
--
-- NegAsignacion usa idEstado = 1 en ASIGNACION: el primer ESTADO insertado
-- debe ser válido; aquí es "Creada" (ámbito Voluntarias) → idEstado = 1 si
-- la secuencia arranca en 1 y ESTADO estaba vacío.
-- =============================================================================

/*
-- SOLO DESARROLLO: vaciar tablas y reiniciar secuencias (CUIDADO en producción)
TRUNCATE TABLE
  "DETALLEASIGNACION",
  "ASIGNACION",
  "ASISTENCIA",
  "VOLUNTARIAHORARIO",
  "USUARIO",
  "MOVIMIENTOSTOCK",
  "BEBE",
  "VOLUNTARIA",
  "ESTADO",
  "MADRE",
  "HORARIO",
  "DIA",
  "TAREA",
  "INSUMO",
  "PROVEEDOR",
  "SALA",
  "LOCALIDAD",
  "ROL",
  "AMBITO"
RESTART IDENTITY CASCADE;
*/

BEGIN;

SET client_encoding = 'UTF8';

-- ---------------------------------------------------------------------------
-- AMBITO
-- ---------------------------------------------------------------------------
INSERT INTO "AMBITO" ("nombre", "descripcion") VALUES
  ('Voluntarias', 'Ámbito de estados de voluntarias'),
  ('Bebes', 'Ámbito de estados de bebés');

-- ---------------------------------------------------------------------------
-- ESTADO (primera fila = idEstado 1 si la tabla estaba vacía)
-- ---------------------------------------------------------------------------
INSERT INTO "ESTADO" ("nombre", "descripcion", "idAmbito")
SELECT 'Creada', 'Voluntaria recién registrada', a."idAmbito" FROM "AMBITO" a WHERE a."nombre" = 'Voluntarias' LIMIT 1;

INSERT INTO "ESTADO" ("nombre", "descripcion", "idAmbito")
SELECT v.n, v.d, a."idAmbito"
FROM "AMBITO" a
CROSS JOIN (VALUES
  ('Inactiva', 'No disponible'),
  ('Asignada', 'Con asignación activa'),
  ('Carpeta médica', 'Gestión de documentación'),
  ('Licencia', 'Ausencia temporal'),
  ('Activa', 'Disponible para tareas'),
  ('Abrazando', 'Abrazando a un bebé'),
  ('Ayudando', 'En tarea de apoyo'),
  ('Disponible', 'Lista para asignar')
) AS v(n, d)
WHERE a."nombre" = 'Voluntarias';

INSERT INTO "ESTADO" ("nombre", "descripcion", "idAmbito")
SELECT v.n, v.d, a."idAmbito"
FROM "AMBITO" a
CROSS JOIN (VALUES
  ('Sin abrazar', 'En espera de abrazo'),
  ('Asignado', 'Asignado a voluntaria'),
  ('Abrazado', 'En abrazo')
) AS v(n, d)
WHERE a."nombre" = 'Bebes';

-- ---------------------------------------------------------------------------
-- ROL
-- ---------------------------------------------------------------------------
INSERT INTO "ROL" ("Nombre", "Descripcion") VALUES
  ('Voluntaria', 'Abrazo y acompañamiento'),
  ('Coordinadora', 'Coordinación de turnos'),
  ('Administrativa', 'Gestión administrativa');

-- ---------------------------------------------------------------------------
-- PROVEEDOR e INSUMO
-- ---------------------------------------------------------------------------
INSERT INTO "PROVEEDOR" ("nombre", "descripcion") VALUES
  ('Distribuidora Sur', 'Insumos médicos generales'),
  ('NeoCare SRL', 'Productos neonatología');

INSERT INTO "INSUMO" ("nombre", "descripcion", "stockMaximo", "stockMinimo", "stockActual") VALUES
  ('Pañales P', 'Pañales talla P', 500, 50, 200),
  ('Toallitas húmedas', 'Paquete 80 u', 300, 30, 120),
  ('Leche fórmula', 'Lata 400g', 100, 10, 45);

-- ---------------------------------------------------------------------------
-- TAREA
-- ---------------------------------------------------------------------------
INSERT INTO "TAREA" ("nombre", "Estado") VALUES
  ('Abrazo kangaroo', TRUE),
  ('Apoyo en sala', TRUE),
  ('Entrega de insumos', TRUE);

-- ---------------------------------------------------------------------------
-- LOCALIDAD y SALA
-- ---------------------------------------------------------------------------
INSERT INTO "LOCALIDAD" ("nombre", "idProvincia") VALUES
  ('Córdoba Capital', 14),
  ('Villa María', 14),
  ('Río Cuarto', 14);

INSERT INTO "SALA" ("Nombre") VALUES
  ('NEO A'),
  ('NEO B'),
  ('UCIN 1');

-- ---------------------------------------------------------------------------
-- DIA y HORARIO (interval = TimeSpan en Npgsql)
-- ---------------------------------------------------------------------------
INSERT INTO "DIA" ("Descripcion") VALUES
  ('Lunes'), ('Martes'), ('Miércoles'), ('Jueves'), ('Viernes');

INSERT INTO "HORARIO" ("IdDia", "Turno", "HoraIngreso", "HoraSalida") VALUES
  (1, 'Mañana', INTERVAL '8 hours', INTERVAL '12 hours'),
  (1, 'Tarde', INTERVAL '14 hours', INTERVAL '18 hours'),
  (2, 'Mañana', INTERVAL '8 hours', INTERVAL '12 hours'),
  (3, 'Mañana', INTERVAL '8 hours', INTERVAL '12 hours'),
  (4, 'Tarde', INTERVAL '14 hours', INTERVAL '18 hours'),
  (5, 'Mañana', INTERVAL '9 hours', INTERVAL '13 hours');

-- ---------------------------------------------------------------------------
-- MADRE (5)
-- ---------------------------------------------------------------------------
INSERT INTO "MADRE" ("Nombre", "FechaNacimiento", "Apellido", "Dni", "Localidad", "EstadoCivil", "CantidadHijos", "Estado", "MotivoAbrazo", "Celular") VALUES
  ('María', TIMESTAMPTZ '1990-05-10 00:00:00+00', 'González', 28111222, 1, 1, 2, TRUE, 'Prematuridad', 3516001001),
  ('Laura', TIMESTAMPTZ '1992-08-20 00:00:00+00', 'Pérez', 35222333, 1, 1, 1, TRUE, 'NEO prolongada', 3516001002),
  ('Ana', TIMESTAMPTZ '1988-01-15 00:00:00+00', 'Fernández', 30133444, 2, 2, 3, TRUE, 'Apoyo emocional', 3516001003),
  ('Carla', TIMESTAMPTZ '1995-11-30 00:00:00+00', 'López', 38444555, 3, 1, 1, TRUE, 'Primera madre', 3516001004),
  ('Sofía', TIMESTAMPTZ '1991-03-22 00:00:00+00', 'Ruiz', 32455666, 1, 1, 2, TRUE, 'Gemelos', 3516001005);

-- ---------------------------------------------------------------------------
-- BEBE (5) — IdMadre 1..5, salas 1..3, estados Bebes por nombre
-- ---------------------------------------------------------------------------
INSERT INTO "BEBE" ("Dni", "nombre", "apellido", "Sexo", "FechaNacimiento", "LugarNacimiento", "FechaIngresoNEO",
  "PesoNacimiento", "PesoIngresoNEO", "PesoDiaAbrazos", "PesoAlta", "DiagnosticoIngreso", "DiagnosticoEgreso",
  "IdSala", "IdMadre", "IdEstado")
VALUES
  (50111222, 'Lucas', 'González', 'M', TIMESTAMPTZ '2026-02-01 12:00:00+00', 'Hospital NEO', TIMESTAMPTZ '2026-02-05 08:00:00+00',
    1.85, 1.90, NULL, NULL, 'Prematuridad leve', NULL, 1, 1,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito" WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Sin abrazar' LIMIT 1)),
  (50111223, 'Emma', 'Pérez', 'F', TIMESTAMPTZ '2026-02-02 12:00:00+00', 'Hospital NEO', TIMESTAMPTZ '2026-02-06 08:00:00+00',
    1.78, 1.82, NULL, NULL, 'Prematuridad moderada', NULL, 2, 2,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito" WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Sin abrazar' LIMIT 1)),
  (50111224, 'Mateo', 'Fernández', 'M', TIMESTAMPTZ '2026-02-03 12:00:00+00', 'Hospital NEO', TIMESTAMPTZ '2026-02-07 08:00:00+00',
    2.10, 2.05, 2.08, NULL, 'Control evolutivo', NULL, 3, 3,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito" WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Asignado' LIMIT 1)),
  (50111225, 'Valentina', 'López', 'F', TIMESTAMPTZ '2026-02-04 12:00:00+00', 'Hospital NEO', TIMESTAMPTZ '2026-02-08 08:00:00+00',
    1.92, 1.95, 2.00, NULL, 'NEO estándar', NULL, 1, 4,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito" WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Abrazado' LIMIT 1)),
  (50111226, 'Benjamín', 'Ruiz', 'M', TIMESTAMPTZ '2026-02-05 12:00:00+00', 'Hospital NEO', TIMESTAMPTZ '2026-02-09 08:00:00+00',
    1.70, 1.75, 1.80, NULL, 'Prematuridad leve', NULL, 2, 5,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" amb ON amb."idAmbito" = e."idAmbito" WHERE amb."nombre" = 'Bebes' AND e."nombre" = 'Abrazado' LIMIT 1));

-- ---------------------------------------------------------------------------
-- VOLUNTARIA (10) — Dni únicos; mezcla de estados Voluntarias y roles
-- ---------------------------------------------------------------------------
INSERT INTO "VOLUNTARIA" ("Dni", "Nombre", "Apellido", "Mail", "Celular", "FechaInicio", "FechaFin", "IdEstado", "IdRol") VALUES
  (30111111, 'Paula', 'Martínez', 'paula.m@test.seed', 3516111001, TIMESTAMPTZ '2025-01-10 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Activa' LIMIT 1), 1),
  (30111112, 'Julia', 'Sánchez', 'julia.s@test.seed', 3516111002, TIMESTAMPTZ '2025-02-15 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Activa' LIMIT 1), 1),
  (30111113, 'Micaela', 'Torres', 'micaela.t@test.seed', 3516111003, TIMESTAMPTZ '2025-03-01 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Abrazando' LIMIT 1), 1),
  (30111114, 'Florencia', 'Díaz', 'florencia.d@test.seed', 3516111004, TIMESTAMPTZ '2024-11-20 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Disponible' LIMIT 1), 1),
  (30111115, 'Natalia', 'Romero', 'natalia.r@test.seed', 3516111005, TIMESTAMPTZ '2025-04-05 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Inactiva' LIMIT 1), 1),
  (30111116, 'Verónica', 'Castro', 'veronica.c@test.seed', 3516111006, TIMESTAMPTZ '2025-05-12 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Asignada' LIMIT 1), 1),
  (30111117, 'Gabriela', 'Morales', 'gabriela.m@test.seed', 3516111007, TIMESTAMPTZ '2025-06-01 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Ayudando' LIMIT 1), 2),
  (30111118, 'Andrea', 'Rojas', 'andrea.r@test.seed', 3516111008, TIMESTAMPTZ '2024-09-01 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Creada' LIMIT 1), 1),
  (30111119, 'Lucía', 'Vega', 'lucia.v@test.seed', 3516111009, TIMESTAMPTZ '2025-07-20 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Licencia' LIMIT 1), 1),
  (30111120, 'Rocío', 'Mendoza', 'rocio.m@test.seed', 3516111010, TIMESTAMPTZ '2025-08-10 00:00:00+00', NULL,
    (SELECT e."idEstado" FROM "ESTADO" e JOIN "AMBITO" a ON a."idAmbito" = e."idAmbito" WHERE a."nombre" = 'Voluntarias' AND e."nombre" = 'Carpeta médica' LIMIT 1), 3);

-- ---------------------------------------------------------------------------
-- ASIGNACION (muestras; idEstado = 1 como en NegAsignacion)
-- ---------------------------------------------------------------------------
INSERT INTO "ASIGNACION" ("fechaHoraInicio", "fechaHoraFin", "fechaHoraAsignacion", "idBebe", "idTarea", "idEstado", "idVoluntaria", "comentario")
VALUES
  (TIMESTAMPTZ '2026-03-31 09:00:00+00', NULL, TIMESTAMPTZ '2026-03-31 08:30:00+00', 3, 1, 1, 3, 'Abrazo programado'),
  (NULL, NULL, TIMESTAMPTZ '2026-03-31 07:00:00+00', 1, 2, 1, 1, 'Apoyo sala'),
  (TIMESTAMPTZ '2026-03-30 10:00:00+00', TIMESTAMPTZ '2026-03-30 11:00:00+00', TIMESTAMPTZ '2026-03-30 09:45:00+00', 4, 1, 1, 4, 'Turno completado');

-- ---------------------------------------------------------------------------
-- DETALLEASIGNACION
-- ---------------------------------------------------------------------------
INSERT INTO "DETALLEASIGNACION" ("cantidad", "fechaRetiro", "fechaEntrega", "nombreInsumo", "idAsignacion", "idInsumo") VALUES
  (2, NULL, TIMESTAMPTZ '2026-03-31 08:35:00+00', 'Pañales P', 1, 1),
  (1, TIMESTAMPTZ '2026-03-30 10:00:00+00', TIMESTAMPTZ '2026-03-30 10:30:00+00', 'Toallitas húmedas', 3, 2);

-- ---------------------------------------------------------------------------
-- ASISTENCIA
-- ---------------------------------------------------------------------------
INSERT INTO "ASISTENCIA" ("IdVoluntaria", "IdHorario", "FechaHoraIngreso", "FechaHoraSalida") VALUES
  (1, 1, TIMESTAMPTZ '2026-03-31 07:55:00+00', NULL),
  (2, 2, TIMESTAMPTZ '2026-03-31 13:50:00+00', NULL),
  (3, 1, TIMESTAMPTZ '2026-03-30 07:50:00+00', TIMESTAMPTZ '2026-03-30 12:05:00+00');

-- ---------------------------------------------------------------------------
-- VOLUNTARIAHORARIO
-- ---------------------------------------------------------------------------
INSERT INTO "VOLUNTARIAHORARIO" ("IdHorario", "IdVoluntaria") VALUES
  (1, 1), (2, 1), (1, 2), (3, 3), (4, 4), (1, 5);

-- ---------------------------------------------------------------------------
-- USUARIO (misma Dni que voluntaria para pruebas; contraseña en claro solo dev)
-- ---------------------------------------------------------------------------
INSERT INTO "USUARIO" ("Dni", "Contrasena", "FechaCreacion", "IdVoluntaria") VALUES
  (30111111, 'CambiarEnProd123', TIMESTAMPTZ '2025-01-10 12:00:00+00', 1),
  (30111112, 'CambiarEnProd123', TIMESTAMPTZ '2025-02-15 12:00:00+00', 2),
  (30111113, 'CambiarEnProd123', TIMESTAMPTZ '2025-03-01 12:00:00+00', 3);

-- ---------------------------------------------------------------------------
-- MOVIMIENTOSTOCK
-- ---------------------------------------------------------------------------
INSERT INTO "MOVIMIENTOSTOCK" ("idInsumo", "idBebe", "idVoluntaria", "fechaMovimiento", "observacion", "cantidad", "esEntrada", "idProveedor") VALUES
  (1, 1, NULL, TIMESTAMPTZ '2026-03-29 10:00:00+00', 'Ingreso lote marzo', 50, 'S', 1),
  (2, NULL, 1, TIMESTAMPTZ '2026-03-31 09:00:00+00', 'Retiro para sala', 5, 'N', NULL),
  (3, 2, NULL, TIMESTAMPTZ '2026-03-28 15:00:00+00', 'Donación', 12, 'S', 2);

COMMIT;
