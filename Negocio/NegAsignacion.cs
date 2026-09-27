using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegAsignacion : INegAsignacion
    {
        private readonly IBebeRepositorio bebeRepositorio;
        private readonly IVoluntariaRepositorio voluntariaRepositorio;
        private readonly IAsignacionRepositorio asignacionRepositorio;
        private readonly IEstadoRepositorio estadoRepositorio;
        private readonly INegTareas negTareas;
        private readonly INegUsuarios negUsuarios;
        private readonly ApplicationDbContext db;

        public NegAsignacion(
            IBebeRepositorio bebeRepositorio,
            IVoluntariaRepositorio voluntariaRepositorio,
            IAsignacionRepositorio asignacionRepositorio,
            IEstadoRepositorio estadoRepositorio,
            INegTareas negTareas,
            INegUsuarios negUsuarios,
            ApplicationDbContext db)
        {
            this.bebeRepositorio = bebeRepositorio;
            this.voluntariaRepositorio = voluntariaRepositorio;
            this.asignacionRepositorio = asignacionRepositorio;
            this.estadoRepositorio = estadoRepositorio;
            this.negTareas = negTareas;
            this.negUsuarios = negUsuarios;
            this.db = db;
        }

        private void AsegurarAsignacionNoEliminada(ASIGNACION asignacion)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            if (asignacion.idEstado == idElim)
                throw new ApplicationException("La asignación fue eliminada.");
        }

        private static string NombreCompletoVoluntaria(VOLUNTARIA? voluntaria) =>
            voluntaria == null ? string.Empty : $"{voluntaria.Nombre} {voluntaria.Apellido}".Trim();

        private static string? NombreCompletoBebe(BEBE? bebe) =>
            bebe == null ? null : $"{bebe.nombre} {bebe.apellido}".Trim();

        private static string? NombreSalaBebe(BEBE? bebe) =>
            bebe?.Sala?.Nombre ?? bebe?.NombreSala;

        private List<DetalleAsignacionResumido> ObtenerDetallesResumidos(int idAsignacion) =>
            db.DETALLEASIGNACION
                .Where(d => d.idAsignacion == idAsignacion)
                .Select(d => new DetalleAsignacionResumido
                {
                    cantidad = d.cantidad,
                    idInsumo = d.idInsumo,
                    nombreInsumo = d.nombreInsumo ?? string.Empty,
                    fechaEntrega = d.fechaEntrega
                })
                .ToList();

        private RespuestaAsignaciones MapearRespuestaAsignacion(ASIGNACION a) =>
            new()
            {
                idAsignacion = a.idAsignacion,
                idTarea = a.idTarea,
                idBebe = a.idBebe,
                idVoluntaria = a.idVoluntaria,
                nombreBebe = NombreCompletoBebe(a.bebe),
                nombreTarea = a.tarea?.nombre,
                nombreVoluntaria = NombreCompletoVoluntaria(a.voluntaria),
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin = a.fechaHoraFin,
                fechaHoraInicio = a.fechaHoraInicio,
                estadoAsignacion = a.estado?.nombre ?? a.idEstado.ToString(),
                comentario = a.comentario,
                sala = a.bebe?.IdSala,
                nombreSala = NombreSalaBebe(a.bebe),
                detalles = ObtenerDetallesResumidos(a.idAsignacion)
            };


        public List<RespuestaAsignaciones> generarAsiganacionTareasPorId(RequestAsignacionTareas requestAsignacion)
        {
            if (requestAsignacion.idVoluntarias == null || requestAsignacion.idVoluntarias.Count == 0)
                throw new ApplicationException("Debe indicar al menos una voluntaria.");
            if (requestAsignacion.idTareas == null || requestAsignacion.idTareas.Count == 0)
                throw new ApplicationException("Debe indicar al menos una tarea.");
            if (requestAsignacion.idVoluntarias.Any(id => id <= 0))
                throw new ApplicationException("Hay voluntarias con id inválido.");
            if (requestAsignacion.idTareas.Any(id => id <= 0))
                throw new ApplicationException("Hay tareas con id inválido.");

            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            var idsTareas = requestAsignacion.idTareas.Distinct().ToList();
            var idsVoluntarias = requestAsignacion.idVoluntarias.Distinct().ToList();

            var tareas = db.TAREA.Where(t => idsTareas.Contains(t.idTarea)).ToList();
            var voluntarias = voluntariaRepositorio.consultarVoluntarias(idsVoluntarias);

            var tareasFaltantes = idsTareas.Where(id => tareas.All(t => t.idTarea != id)).ToList();
            if (tareasFaltantes.Count > 0)
                throw new ApplicationException("Tarea(s) inexistentes: " + string.Join(", ", tareasFaltantes) + ".");

            var voluntariasFaltantes = idsVoluntarias.Where(id => voluntarias.All(v => v.IdVoluntaria != id)).ToList();
            if (voluntariasFaltantes.Count > 0)
                throw new ApplicationException("Voluntaria(s) inexistentes: " + string.Join(", ", voluntariasFaltantes) + ".");

            if (tareas.Count == 0)
                throw new ApplicationException("No se encontraron tareas válidas.");

            if (voluntarias.Count == 0)
                throw new ApplicationException("No se encontraron voluntarias válidas.");

            var idEstadoAsignacionCreada = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creada", "Asignaciones");
            var idEstadoAsignacionEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

            var asignacionesHoyPorVoluntaria = db.ASIGNACION
                .Where(a => requestAsignacion.idVoluntarias.Contains(a.idVoluntaria)
                    && a.fechaHoraAsignacion >= diaInicio && a.fechaHoraAsignacion < diaFin
                    && a.idEstado != idEstadoAsignacionEliminado)
                .GroupBy(a => a.idVoluntaria)
                .ToDictionary(g => g.Key, g => g.Count());

            var voluntariasConAsignaciones = voluntarias.Select(v => new VoluntariaConAsignaciones()
            {
                Voluntaria = v,
                CantidadAsignacionesHoy = asignacionesHoyPorVoluntaria.GetValueOrDefault(v.IdVoluntaria, 0)
            }).ToList();

            // Para desempatar cuando hay igual cantidad de asignaciones
            var random = new Random();

            // Resultado
            var respuestas = new List<RespuestaAsignaciones>();

            foreach (var tarea in tareas)
            {
                negTareas.ValidarTareaDisponibleParaAsignar(tarea.idTarea);

                // Obtenemos las voluntarias con menor cantidad de asignaciones hoy
                var minAsignaciones = voluntariasConAsignaciones.Min(v => v.CantidadAsignacionesHoy);
                var candidatas = voluntariasConAsignaciones
                    .Where(v => v.CantidadAsignacionesHoy == minAsignaciones)
                    .OrderBy(v => random.Next()) // random entre las que menos tienen
                    .ToList();

                var seleccionada = candidatas.First().Voluntaria;

                var asignacion = new ASIGNACION
                {
                    idVoluntaria = seleccionada.IdVoluntaria,
                    idTarea = tarea.idTarea,
                    fechaHoraAsignacion = fechaHoy,
                    idEstado = idEstadoAsignacionCreada
                };

                voluntariaRepositorio.asignarVoluntaria(seleccionada.IdVoluntaria);
                db.ASIGNACION.Add(asignacion);
                db.SaveChanges();

                respuestas.Add(new RespuestaAsignaciones
                {
                    idAsignacion = asignacion.idAsignacion,
                    idVoluntaria = seleccionada.IdVoluntaria,
                    nombreVoluntaria = $"{seleccionada.Nombre} {seleccionada.Apellido}",
                    fechaHoraAsignacion = fechaHoy,
                    estadoAsignacion = "Creada"
                });

                // Actualizamos las asignaciones de la voluntaria seleccionada
                var voluntariaAsignada = voluntariasConAsignaciones.First(v => v.Voluntaria.IdVoluntaria == seleccionada.IdVoluntaria);
                voluntariaAsignada.CantidadAsignacionesHoy++;
            }

            return respuestas;

        }

        /// <summary>
        /// Genera asignaciones de abrazo con bebés y voluntarias elegidas.
        /// <paramref name="requestAsignacion.idTareas"/> son ids de bebé (BEBE.ID), no de la tabla TAREA.
        /// </summary>
        public List<RespuestaAsignaciones> generarAsignacionesSeleccion(RequestAsignacionTareas requestAsignacion)
        {
            if (requestAsignacion.idVoluntarias == null || requestAsignacion.idVoluntarias.Count == 0)
                throw new ApplicationException("Debe indicar al menos una voluntaria.");
            if (requestAsignacion.idTareas == null || requestAsignacion.idTareas.Count == 0)
                throw new ApplicationException("Debe indicar al menos un bebé.");
            if (requestAsignacion.idVoluntarias.Any(id => id <= 0))
                throw new ApplicationException("Hay voluntarias con id inválido.");
            if (requestAsignacion.idTareas.Any(id => id <= 0))
                throw new ApplicationException("Hay bebés con id inválido.");

            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            var bebesAbrazar = CargarBebesPorIdsParaGenerar(requestAsignacion.idTareas);
            var voluntariasActivas = CargarVoluntariasPorIdsParaGenerar(
                requestAsignacion.idVoluntarias,
                diaInicio,
                diaFin);

            return EjecutarGeneracionAsignacionesAbrazos(bebesAbrazar, voluntariasActivas);
        }

        /// <summary>
        /// Asigna un bebé a una voluntaria. <paramref name="requestAsignacion.idTarea"/> es BEBE.ID.
        /// </summary>
        public RespuestaAsignaciones generarAsiganacionTarea(RequestAsignacionTarea requestAsignacion)
        {
            if (requestAsignacion.idVoluntaria <= 0)
                throw new ApplicationException("Debe indicar una voluntaria válida.");
            if (requestAsignacion.idTarea <= 0)
                throw new ApplicationException("Debe indicar un bebé válido.");

            var resultados = generarAsignacionesSeleccion(new RequestAsignacionTareas
            {
                idVoluntarias = new List<int> { requestAsignacion.idVoluntaria },
                idTareas = new List<int> { requestAsignacion.idTarea }
            });

            return resultados.First();
        }

        /// <summary>
        /// Asigna una tarea del catálogo (tabla TAREA) a una voluntaria.
        /// </summary>
        public RespuestaAsignaciones generarAsignacionTareaCatalogo(RequestAsignacionTarea requestAsignacion)
        {
            var voluntaria = voluntariaRepositorio.consultarVoluntaria(requestAsignacion.idVoluntaria);
            if (voluntaria == null)
                throw new NotFoundException("Voluntaria no encontrada");
            var tarea = db.TAREA.FirstOrDefault(t => t.idTarea == requestAsignacion.idTarea);
            if (tarea == null)
                throw new NotFoundException("Tarea no encontrada");
            negTareas.ValidarTareaDisponibleParaAsignar(tarea.idTarea);
            try
            {
                var idEstadoAsignacionCreada = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creada", "Asignaciones");
                var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                var asignacion = new ASIGNACION
                {
                    idVoluntaria = voluntaria.IdVoluntaria,
                    fechaHoraAsignacion = fechaHoy,
                    idEstado = idEstadoAsignacionCreada,
                    idTarea = tarea.idTarea
                };
                voluntariaRepositorio.asignarVoluntaria(voluntaria.IdVoluntaria);
                db.ASIGNACION.Add(asignacion);
                db.SaveChanges();

                return new RespuestaAsignaciones
                {
                    idAsignacion = asignacion.idAsignacion,
                    idVoluntaria = voluntaria.IdVoluntaria,
                    nombreVoluntaria = $"{voluntaria.Nombre} {voluntaria.Apellido}",
                    fechaHoraAsignacion = fechaHoy,
                    estadoAsignacion = "Creada",
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }
        public List<RespuestaAsignaciones> generarAsiganaciones()
        {
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            var bebesAbrazar = CargarBebesAbrazarParaGenerar();
            if (bebesAbrazar.Count == 0)
                throw new ApplicationException("No hay bebes para abrazar para el día de hoy");

            var voluntariasActivas = CargarVoluntariasLibresParaGenerar(diaInicio, diaFin);
            if (voluntariasActivas.Count == 0)
                throw new ApplicationException("No hay voluntarias disponibles para el día de hoy");

            return EjecutarGeneracionAsignacionesAbrazos(bebesAbrazar, voluntariasActivas);
        }

        /// <summary>
        /// Menos abrazos hoy, después menos en el mes, después apellido, nombre e id.
        /// </summary>
        private static VOLUNTARIA ElegirVoluntariaParaAbrazo(
            IReadOnlyList<VOLUNTARIA> candidatas,
            Dictionary<int, int> hoy,
            Dictionary<int, int> mes)
        {
            var mejor = candidatas[0];
            var mejorHoy = hoy.GetValueOrDefault(mejor.IdVoluntaria);
            var mejorMes = mes.GetValueOrDefault(mejor.IdVoluntaria);

            for (var i = 1; i < candidatas.Count; i++)
            {
                var actual = candidatas[i];
                var actualHoy = hoy.GetValueOrDefault(actual.IdVoluntaria);
                var actualMes = mes.GetValueOrDefault(actual.IdVoluntaria);
                if (EsMejorCandidata(actual, actualHoy, actualMes, mejor, mejorHoy, mejorMes))
                {
                    mejor = actual;
                    mejorHoy = actualHoy;
                    mejorMes = actualMes;
                }
            }

            return mejor;
        }

        /// <summary>Menos abrazos en el mes; empate por apellido, nombre e id. Una sola por voluntaria.</summary>
        private static VOLUNTARIA ElegirVoluntariaPorMes(
            IReadOnlyList<VOLUNTARIA> candidatas,
            Dictionary<int, int> mes)
        {
            var mejor = candidatas[0];
            var mejorMes = mes.GetValueOrDefault(mejor.IdVoluntaria);

            for (var i = 1; i < candidatas.Count; i++)
            {
                var actual = candidatas[i];
                var actualMes = mes.GetValueOrDefault(actual.IdVoluntaria);
                if (actualMes < mejorMes
                    || (actualMes == mejorMes && CompararNombre(actual, mejor) < 0))
                {
                    mejor = actual;
                    mejorMes = actualMes;
                }
            }

            return mejor;
        }

        private static bool EsMejorCandidata(
            VOLUNTARIA actual, int actualHoy, int actualMes,
            VOLUNTARIA mejor, int mejorHoy, int mejorMes)
        {
            if (actualHoy != mejorHoy)
                return actualHoy < mejorHoy;
            if (actualMes != mejorMes)
                return actualMes < mejorMes;
            return CompararNombre(actual, mejor) < 0;
        }

        private static int CompararNombre(VOLUNTARIA a, VOLUNTARIA b)
        {
            var porApellido = string.Compare(a.Apellido, b.Apellido, StringComparison.OrdinalIgnoreCase);
            if (porApellido != 0)
                return porApellido;
            var porNombre = string.Compare(a.Nombre, b.Nombre, StringComparison.OrdinalIgnoreCase);
            if (porNombre != 0)
                return porNombre;
            return a.IdVoluntaria.CompareTo(b.IdVoluntaria);
        }

        /// <summary>Un solo viaje: abrazos (con bebé), no tareas. Hoy y último mes.</summary>
        private (Dictionary<int, int> Hoy, Dictionary<int, int> Mes) ContarAbrazosPorVoluntaria(
            List<int> idsVoluntarias,
            DateTime diaInicio,
            DateTime diaFin,
            DateTime mesDesde,
            DateTime mesHastaExclusivo,
            int idEstadoAsignacionEliminado)
        {
            var hoy = idsVoluntarias.ToDictionary(id => id, _ => 0);
            var mes = idsVoluntarias.ToDictionary(id => id, _ => 0);
            if (idsVoluntarias.Count == 0)
                return (hoy, mes);

            var filas = db.ASIGNACION
                .AsNoTracking()
                .Where(a => idsVoluntarias.Contains(a.idVoluntaria)
                            && a.idBebe != null
                            && a.idEstado != idEstadoAsignacionEliminado
                            && a.fechaHoraAsignacion >= mesDesde
                            && a.fechaHoraAsignacion < diaFin)
                .GroupBy(a => a.idVoluntaria)
                .Select(g => new
                {
                    Id = g.Key,
                    Hoy = g.Count(a => a.fechaHoraAsignacion >= diaInicio && a.fechaHoraAsignacion < diaFin),
                    Mes = g.Count(a => a.fechaHoraAsignacion >= mesDesde && a.fechaHoraAsignacion < mesHastaExclusivo)
                })
                .ToList();

            foreach (var fila in filas)
            {
                hoy[fila.Id] = fila.Hoy;
                mes[fila.Id] = fila.Mes;
            }

            return (hoy, mes);
        }

        private List<RespuestaAsignaciones> EjecutarGeneracionAsignacionesAbrazos(
            List<BEBE> bebesAbrazar,
            List<VOLUNTARIA> voluntariasActivas)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                var fechaMesAnterior = fechaHoy.AddMonths(-1);
                var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

                var estBebeRow = db.ESTADO.AsNoTracking()
                    .FirstOrDefault(e => e.ambito.nombre == "Bebes" && e.nombre == "Asignado");
                var estVolRow = db.ESTADO.AsNoTracking()
                    .FirstOrDefault(e => e.ambito.nombre == "Voluntarias" && e.nombre == "Asignada");
                if (estBebeRow == null || estVolRow == null)
                    throw new ApplicationException("Estado asignado inexistente (bebé o voluntaria).");

                var idEstadoBebeAsignado = estBebeRow.idEstado;
                var idEstadoVolAsignada = estVolRow.idEstado;

                var idEstadoAsignacionCreada = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creada", "Asignaciones");
                var idEstadoAsignacionEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

                var idsBebes = bebesAbrazar.Select(b => b.ID).ToList();
                var idsVoluntarias = voluntariasActivas.Select(v => v.IdVoluntaria).ToList();

                var bebes = db.BEBE
                    .Include(b => b.Sala)
                    .Where(b => idsBebes.Contains(b.ID))
                    .ToList()
                    .OrderBy(b => idsBebes.IndexOf(b.ID))
                    .ToList();

                var voluntarias = db.VOLUNTARIA
                    .Where(v => idsVoluntarias.Contains(v.IdVoluntaria))
                    .ToList();

                var (asignacionesHoyPorVol, asignacionesMesPorVol) = ContarAbrazosPorVoluntaria(
                    idsVoluntarias, diaInicio, diaFin, fechaMesAnterior, fechaHoy, idEstadoAsignacionEliminado);

                var asignaciones = new List<ASIGNACION>(bebes.Count);

                void RegistrarAsignacion(BEBE bebe, VOLUNTARIA voluntaria)
                {
                    bebe.IdEstado = idEstadoBebeAsignado;
                    voluntaria.IdEstado = idEstadoVolAsignada;

                    var asignacion = new ASIGNACION
                    {
                        idVoluntaria = voluntaria.IdVoluntaria,
                        idBebe = bebe.ID,
                        fechaHoraAsignacion = fechaHoy,
                        idEstado = idEstadoAsignacionCreada,
                        bebe = bebe,
                        voluntaria = voluntaria
                    };

                    db.ASIGNACION.Add(asignacion);
                    asignaciones.Add(asignacion);

                    asignacionesHoyPorVol[voluntaria.IdVoluntaria] =
                        asignacionesHoyPorVol.GetValueOrDefault(voluntaria.IdVoluntaria, 0) + 1;
                    asignacionesMesPorVol[voluntaria.IdVoluntaria] =
                        asignacionesMesPorVol.GetValueOrDefault(voluntaria.IdVoluntaria, 0) + 1;
                }

                if (bebes.Count >= voluntarias.Count)
                {
                    foreach (var bebe in bebes)
                    {
                        RegistrarAsignacion(
                            bebe,
                            ElegirVoluntariaParaAbrazo(voluntarias, asignacionesHoyPorVol, asignacionesMesPorVol));
                    }
                }
                else
                {
                    var pool = voluntarias.ToList();
                    foreach (var bebe in bebes)
                    {
                        var voluntaria = ElegirVoluntariaPorMes(pool, asignacionesMesPorVol);
                        RegistrarAsignacion(bebe, voluntaria);
                        pool.Remove(voluntaria);
                    }
                }

                db.SaveChanges();

                var asignacionesRespuesta = asignaciones.Select(a => new RespuestaAsignaciones()
                {
                    idAsignacion = a.idAsignacion,
                    idBebe = a.idBebe,
                    idVoluntaria = a.idVoluntaria,
                    nombreBebe = NombreCompletoBebe(a.bebe) ?? "Desconocido",
                    nombreVoluntaria = a.voluntaria != null ? (a.voluntaria.Nombre + " " + a.voluntaria.Apellido) : "Desconocido",
                    fechaHoraAsignacion = a.fechaHoraAsignacion,
                    fechaHoraFin = a.fechaHoraFin,
                    fechaHoraInicio = a.fechaHoraInicio,
                    estadoAsignacion = "Creada",
                    sala = a.bebe?.IdSala,
                    nombreSala = NombreSalaBebe(a.bebe)
                }).ToList();

                transaction.Commit();
                return asignacionesRespuesta;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ApplicationException(ex.Message);
            }
        }

        /// <summary>Misma lógica que BebeRepositorio.obtenerBebesAbrazar.</summary>
        private List<BEBE> CargarBebesAbrazarParaGenerar()
        {
            return bebeRepositorio.obtenerBebesAbrazar();
        }

        private List<BEBE> CargarBebesPorIdsParaGenerar(List<int> idsBebes)
        {
            var ids = idsBebes.Distinct().ToList();
            var elegibles = bebeRepositorio.obtenerBebesAbrazarPorIds(ids)
                .ToDictionary(b => b.ID);

            var faltantes = ids.Where(id => !elegibles.ContainsKey(id)).ToList();
            if (faltantes.Count > 0)
                throw new ApplicationException(
                    $"Bebé(s) no disponibles para abrazo o inexistentes: {string.Join(", ", faltantes)}.");

            return ids.Select(id => elegibles[id]).ToList();
        }

        private List<VOLUNTARIA> CargarVoluntariasPorIdsParaGenerar(
            List<int> idsVoluntarias,
            DateTime inicioDia,
            DateTime finDia)
        {
            var ids = idsVoluntarias.Distinct().ToList();
            var libres = CargarVoluntariasLibresParaGenerar(inicioDia, finDia)
                .Where(v => ids.Contains(v.IdVoluntaria))
                .ToDictionary(v => v.IdVoluntaria);

            var faltantes = ids.Where(id => !libres.ContainsKey(id)).ToList();
            if (faltantes.Count > 0)
                throw new ApplicationException(
                    $"Voluntaria(s) no disponibles o inexistentes: {string.Join(", ", faltantes)}.");

            return ids.Select(id => libres[id]).ToList();
        }

        /// <summary>Voluntarias con entrada de hoy sin salida y estado operativo.</summary>
        private List<VOLUNTARIA> CargarVoluntariasLibresParaGenerar(DateTime inicioDia, DateTime finDia)
        {
            return db.VOLUNTARIA
                .AsNoTracking()
                .Where(v => v.Asistencias != null
                            && v.Asistencias.Any(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.FechaHoraSalida == null)
                            && v.Estado.nombre != "Inactiva"
                            && v.Estado.nombre != "Licencia"
                            && v.Estado.nombre != "Carpeta médica"
                            && v.Estado.nombre != "Creada")
                .ToList();
        }
        
        public bool registrarInicioAsignacionAbrazo(int idAsignacion)
        {
            var asignacion = asignacionRepositorio.consultarAsignacion(idAsignacion);
            AsegurarAsignacionNoEliminada(asignacion);
            if (asignacion.fechaHoraInicio != null)
                throw new ConflictException("Abrazo ya inicializado");

            // Una voluntaria no puede estar abrazando a dos bebés al mismo tiempo.
            var yaTieneAbrazoEnCurso = asignacionRepositorio
                .listarAsignacionesHoyVoluntaria(asignacion.idVoluntaria)
                .Any(a => a.fechaHoraInicio != null && a.fechaHoraFin == null);
            if (yaTieneAbrazoEnCurso)
                throw new ConflictException("La voluntaria ya tiene un abrazo en curso. Debe finalizarlo antes de iniciar otro.");

            var idEstadoVoluntaria = asignacion.idBebe.HasValue
                ? estadoRepositorio.ObtenerIdVoluntariaAbrazando()
                : estadoRepositorio.ObtenerIdVoluntariaEnTarea();

            var voluntariaAsignacion = voluntariaRepositorio.consultarVoluntaria(asignacion.idVoluntaria);
            voluntariaAsignacion.IdEstado = idEstadoVoluntaria;
            voluntariaRepositorio.cambioEstadoVoluntaria(voluntariaAsignacion);

            if (asignacion.idBebe.HasValue)
            {
                var idEstadoBebeAbrazado = estadoRepositorio.ObtenerIdBebeAbrazado();
                var bebeAbrazado = bebeRepositorio.consultarBebe(asignacion.idBebe.Value);
                bebeAbrazado.IdEstado = idEstadoBebeAbrazado;
                bebeRepositorio.cambioEstadoBebe(bebeAbrazado, idEstadoBebeAbrazado);
            }

            asignacion.fechaHoraInicio = NegConversorFecha.ObtenerFechaArgentina();
            asignacion.idEstado = estadoRepositorio.ObtenerIdEstadoAsignacionIniciado();
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }

        public bool registrarFinAsignacionAbrazo(int idAsignacion, string? comentario)
        {
            var asignacion = asignacionRepositorio.consultarAsignacion(idAsignacion);
            AsegurarAsignacionNoEliminada(asignacion);
            if (asignacion.fechaHoraInicio == null)
                throw new ApplicationException("Abrazo nunca fue inicializado");

            if (asignacion.fechaHoraFin != null)
                throw new ConflictException("Abrazo ya fue finalizado");

            var idVolDisponible = estadoRepositorio.ObtenerIdVoluntariaDisponible();

            var voluntariaAsignacion = voluntariaRepositorio.consultarVoluntaria(asignacion.idVoluntaria);
            voluntariaAsignacion.IdEstado = idVolDisponible;
            voluntariaRepositorio.cambioEstadoVoluntaria(voluntariaAsignacion);

            if (asignacion.idBebe.HasValue)
            {
                var idBebeSinAbrazar = estadoRepositorio.ObtenerIdBebeSinAbrazar();
                var bebeAbrazado = bebeRepositorio.consultarBebe(asignacion.idBebe.Value);
                bebeAbrazado.IdEstado = idBebeSinAbrazar;
                bebeRepositorio.cambioEstadoBebe(bebeAbrazado, idBebeSinAbrazar);
            }

            asignacion.fechaHoraFin = NegConversorFecha.ObtenerFechaArgentina();
            asignacion.comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario;
            asignacion.idEstado = estadoRepositorio.ObtenerIdEstadoAsignacionFinalizado();
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }

        /// <summary>
        /// Cierra asignaciones con bebé donde el abrazo se inició antes del día calendario actual en Argentina
        /// y nunca se finalizó: bebé a Sin abrazar, voluntaria a Disponible/Activa, asignación Finalizado con fechaHoraFin.
        /// </summary>
        /// <returns>Cantidad de asignaciones actualizadas.</returns>
        public int ResetearAbrazosBebeColgadosAntesDeHoy()
        {
            var (inicioHoyUtc, _) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idElimAsig = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            var idEstadoFinalizado = estadoRepositorio.ObtenerIdEstadoAsignacionFinalizado();
            var idVolDisponible = estadoRepositorio.ObtenerIdVoluntariaDisponible();
            var idBebeSinAbrazar = estadoRepositorio.ObtenerIdBebeSinAbrazar();

            var asignaciones = db.ASIGNACION
                .Where(a =>
                    a.idBebe != null
                    && a.fechaHoraInicio != null
                    && a.fechaHoraFin == null
                    && a.fechaHoraInicio < inicioHoyUtc
                    && a.idEstado != idElimAsig)
                .ToList();

            if (asignaciones.Count == 0)
                return 0;

            const string comentarioAuto = AbrazoAtipico.MarcaCierreAutomatico
                + ": abrazo iniciado en día anterior sin finalizar.";
            var ahora = NegConversorFecha.ObtenerFechaArgentina();

            using var tx = db.Database.BeginTransaction();
            try
            {
                foreach (var asignacion in asignaciones)
                {
                    var vol = db.VOLUNTARIA.FirstOrDefault(v => v.IdVoluntaria == asignacion.idVoluntaria);
                    if (vol != null)
                        vol.IdEstado = idVolDisponible;

                    if (asignacion.idBebe.HasValue)
                    {
                        var bebe = db.BEBE.FirstOrDefault(b => b.ID == asignacion.idBebe.Value);
                        if (bebe != null)
                            bebe.IdEstado = idBebeSinAbrazar;
                    }

                    asignacion.fechaHoraFin = ahora;
                    asignacion.idEstado = idEstadoFinalizado;
                    asignacion.comentario = string.IsNullOrWhiteSpace(asignacion.comentario)
                        ? comentarioAuto
                        : (asignacion.comentario.Contains(AbrazoAtipico.MarcaCierreAutomatico, StringComparison.Ordinal)
                            ? asignacion.comentario
                            : $"{comentarioAuto} | {asignacion.comentario}");
                }

                db.SaveChanges();
                tx.Commit();
                return asignaciones.Count;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>Actualiza tarea, bebé, voluntaria, comentario y fechas de inicio/fin de una asignación existente.</summary>
        public bool eliminarAsignacion(int idAsignacion)
        {
            return asignacionRepositorio.eliminarAsignacionLogica(idAsignacion);
        }

        public bool modificarAsignacion(int idAsignacion, RespuestaAsignaciones datos)
        {
            var existentePrevio = asignacionRepositorio.consultarAsignacion(idAsignacion);
            AsegurarAsignacionNoEliminada(existentePrevio);

            if (datos.idVoluntaria <= 0)
                throw new ApplicationException("Voluntaria inválida");

            voluntariaRepositorio.consultarVoluntaria(datos.idVoluntaria);

            if (datos.idTarea.HasValue && datos.idTarea.Value > 0)
            {
                var tarea = db.TAREA.FirstOrDefault(t => t.idTarea == datos.idTarea.Value);
                if (tarea == null)
                    throw new NotFoundException("Tarea no encontrada");
            }

            if (datos.idBebe.HasValue && datos.idBebe.Value > 0)
                bebeRepositorio.consultarBebe(datos.idBebe.Value);

            var fechaInicio = datos.fechaHoraInicio ?? existentePrevio.fechaHoraInicio;
            var fechaFin = datos.fechaHoraFin ?? existentePrevio.fechaHoraFin;
            if (fechaFin != null && fechaInicio == null)
                throw new ApplicationException("No se puede finalizar un abrazo que nunca fue iniciado.");

            var idIniciado = estadoRepositorio.ObtenerIdEstadoAsignacionIniciado();
            var idFinalizado = estadoRepositorio.ObtenerIdEstadoAsignacionFinalizado();

            var idEstado = existentePrevio.idEstado;
            if (datos.fechaHoraFin != null)
                idEstado = idFinalizado;
            else if (datos.fechaHoraInicio != null)
                idEstado = idIniciado;

            var pasoAFinalizado = idEstado == idFinalizado && existentePrevio.idEstado != idFinalizado;
            var pasoAIniciado = idEstado == idIniciado && existentePrevio.idEstado != idIniciado;

            var patch = new ASIGNACION
            {
                idTarea = datos.idTarea ?? existentePrevio.idTarea,
                idBebe = datos.idBebe ?? existentePrevio.idBebe,
                idVoluntaria = datos.idVoluntaria,
                comentario = datos.comentario ?? existentePrevio.comentario,
                fechaHoraInicio = fechaInicio,
                fechaHoraFin = fechaFin,
                idEstado = idEstado,
            };
            asignacionRepositorio.modificarAsignacion(patch, existentePrevio);

            if (pasoAFinalizado)
            {
                var idVolDisponible = estadoRepositorio.ObtenerIdVoluntariaDisponible();
                var voluntaria = voluntariaRepositorio.consultarVoluntaria(datos.idVoluntaria);
                voluntaria.IdEstado = idVolDisponible;
                voluntariaRepositorio.cambioEstadoVoluntaria(voluntaria);

                var idBebe = datos.idBebe ?? existentePrevio.idBebe;
                if (idBebe.HasValue)
                {
                    var idBebeSinAbrazar = estadoRepositorio.ObtenerIdBebeSinAbrazar();
                    var bebe = bebeRepositorio.consultarBebe(idBebe.Value);
                    bebe.IdEstado = idBebeSinAbrazar;
                    bebeRepositorio.cambioEstadoBebe(bebe, idBebeSinAbrazar);
                }
            }
            else if (pasoAIniciado)
            {
                var idEstadoVoluntaria = datos.idBebe.HasValue || existentePrevio.idBebe.HasValue
                    ? estadoRepositorio.ObtenerIdVoluntariaAbrazando()
                    : estadoRepositorio.ObtenerIdVoluntariaEnTarea();
                var voluntaria = voluntariaRepositorio.consultarVoluntaria(datos.idVoluntaria);
                voluntaria.IdEstado = idEstadoVoluntaria;
                voluntariaRepositorio.cambioEstadoVoluntaria(voluntaria);

                var idBebe = datos.idBebe ?? existentePrevio.idBebe;
                if (idBebe.HasValue)
                {
                    var idBebeAbrazado = estadoRepositorio.ObtenerIdBebeAbrazado();
                    var bebe = bebeRepositorio.consultarBebe(idBebe.Value);
                    bebe.IdEstado = idBebeAbrazado;
                    bebeRepositorio.cambioEstadoBebe(bebe, idBebeAbrazado);
                }
            }

            return true;
        }

        public RespuestaAsignaciones consultarAsignacionPorId(int idAsignacion)
        {
            var a = db.ASIGNACION
                .Include(x => x.voluntaria)
                .Include(x => x.bebe!)
                    .ThenInclude(b => b.Sala)
                .Include(x => x.tarea)
                .Include(x => x.estado)
                .FirstOrDefault(x => x.idAsignacion == idAsignacion);
            if (a == null)
                throw new NotFoundException("Asignación con ese id inexistente");

            AsegurarAsignacionNoEliminada(a);

            return MapearRespuestaAsignacion(a);
        }

        /// <summary>Historial de abrazos (asignaciones con bebé) para un bebé.</summary>
        public List<RespuestaAsignaciones> listarAbrazosHistoricos(int idBebe)
        {
            bebeRepositorio.consultarBebe(idBebe);
            return asignacionRepositorio.listarAbrazosHistoricosPorBebe(idBebe)
                .Select(MapearRespuestaAsignacion)
                .ToList();
        }


        public List<RespuestaAsignaciones>? listarAsignacionesHoy(int dniSolicitante)
        {
            negUsuarios.ValidarCoordinadora(dniSolicitante);

            var asignacionesHoy = asignacionRepositorio.listarAsignacionesHoy()
                .Select(MapearRespuestaAsignacion)
                .ToList();

            asignacionRepositorio.devolverDuracionesAbrazos();

            if (asignacionesHoy.Count == 0)
                return new List<RespuestaAsignaciones>();
            return asignacionesHoy;
        }

        public EstadisticaDuracionesAbrazos devolverDuracionesAbrazos()
        {
            return asignacionRepositorio.devolverDuracionesAbrazos();
        }
        public List<RespuestaAsignaciones> listarAsignacionesHoyVoluntaria(int idVoluntaria)
        {
            return asignacionRepositorio.listarAsignacionesHoyVoluntaria(idVoluntaria)
                .Select(MapearRespuestaAsignacion)
                .ToList();
        }

        public bool registrarDetalleAsignacion(List<RequestDetalleAsignacion> request)
        {
            if (request == null || request.Count == 0)
                throw new ApplicationException("Debe indicar al menos un detalle de insumos.");

            foreach (var r in request)
            {
                if (!r.cantidadInsumo.HasValue || r.cantidadInsumo.Value <= 0)
                    throw new ApplicationException("Cada detalle debe tener cantidadInsumo mayor a 0.");
                if (!r.idAsignacion.HasValue || r.idAsignacion.Value <= 0)
                    throw new ApplicationException("Cada detalle debe indicar idAsignacion.");
                if (!r.idInsumo.HasValue || r.idInsumo.Value <= 0)
                    throw new ApplicationException("Cada detalle debe indicar idInsumo.");

                var asignacion = asignacionRepositorio.consultarAsignacion(r.idAsignacion.Value);
                AsegurarAsignacionNoEliminada(asignacion);
            }

            return asignacionRepositorio.registrarDetalleAsignacion(request);
        }

        //public List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones(string fechaInicio,string fechaFin)
        //{
        //    return asignacionRepositorio.devolverEstadisticaCantidadAsignaciones(fechaInicio,fechaFin);
        //}

        public List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones()
        {
            return asignacionRepositorio.devolverEstadisticaCantidadAsignaciones1();
        }
    }


        
}