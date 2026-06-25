using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    public class NegAsignacion
    {
        private readonly BebeRepositorio bebeRepositorio;
        private readonly VoluntariaRepositorio voluntariaRepositorio;
        private readonly AsignacionRepositorio asignacionRepositorio;
        private readonly EstadoRepositorio estadoRepositorio;
        private readonly NegTareas negTareas;
        private readonly NegUsuarios negUsuarios;
        private readonly ApplicationDbContext db;
        public NegAsignacion()
        {
            bebeRepositorio = new BebeRepositorio();
            voluntariaRepositorio = new VoluntariaRepositorio();
            asignacionRepositorio= new AsignacionRepositorio();
            estadoRepositorio = new EstadoRepositorio();
            negTareas = new NegTareas();
            negUsuarios = new NegUsuarios();
            db = new ApplicationDbContext();
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

            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            // Traemos las tareas y voluntarias desde la base
            var tareas = db.TAREA.Where(t => requestAsignacion.idTareas.Contains(t.idTarea)).ToList();
            var voluntarias = voluntariaRepositorio.consultarVoluntarias(requestAsignacion.idVoluntarias);

            if (tareas == null || tareas.Count == 0)
                throw new ApplicationException("No se encontraron tareas válidas.");

            if (voluntarias == null || voluntarias.Count == 0)
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

            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var fechaMesAnterior = fechaHoy.AddMonths(-1);
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idEstadoAsignacionEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

            var bebesAbrazar = CargarBebesPorIdsParaGenerar(
                requestAsignacion.idTareas,
                diaInicio,
                diaFin,
                idEstadoAsignacionEliminado);
            var voluntariasActivas = CargarVoluntariasPorIdsParaGenerar(
                requestAsignacion.idVoluntarias,
                diaInicio,
                diaFin,
                fechaMesAnterior);

            return EjecutarGeneracionAsignacionesAbrazos(bebesAbrazar, voluntariasActivas);
        }

        public RespuestaAsignaciones generarAsiganacionTarea(RequestAsignacionTarea requestAsignacion)
        {
            var voluntaria = voluntariaRepositorio.consultarVoluntaria(requestAsignacion.idVoluntaria);
            if(voluntaria== null)
                throw new NotFoundException("Voluntaria no encontrada");
            var tarea = db.TAREA.FirstOrDefault(t=>t.idTarea==requestAsignacion.idTarea);
            if (tarea == null)
                throw new NotFoundException("Tarea no encontrada");
            negTareas.ValidarTareaDisponibleParaAsignar(tarea.idTarea);
            try
            {
                var idEstadoAsignacionCreada = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Creada", "Asignaciones");
                var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                var asignacion = new ASIGNACION();
                asignacion.idVoluntaria = voluntaria.IdVoluntaria;
                //asignacion.idBebe = bebesAbrazar[i].ID;
                asignacion.fechaHoraAsignacion = fechaHoy;
                asignacion.idEstado = idEstadoAsignacionCreada;
                asignacion.idTarea = tarea.idTarea;
                voluntariaRepositorio.asignarVoluntaria(voluntaria.IdVoluntaria);
                db.ASIGNACION.Add(asignacion);
                db.SaveChanges();


                var asignacionesRespuesta = new RespuestaAsignaciones()
                {
                    idAsignacion = asignacion.idAsignacion,
                    idVoluntaria = voluntaria.IdVoluntaria,
                    nombreVoluntaria = $"{voluntaria.Nombre} {voluntaria.Apellido}",
                    fechaHoraAsignacion = fechaHoy,
                    estadoAsignacion = "Creada",
                };

                return asignacionesRespuesta;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }


        }
        public List<RespuestaAsignaciones> generarAsiganaciones()
        {
            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var fechaMesAnterior = fechaHoy.AddMonths(-1);
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idEstadoAsignacionEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");

            var bebesAbrazar = CargarBebesAbrazarParaGenerar(diaInicio, diaFin, idEstadoAsignacionEliminado);
            if (bebesAbrazar.Count == 0)
                throw new ApplicationException("No hay bebes para abrazar para el día de hoy");

            var voluntariasActivas = CargarVoluntariasLibresParaGenerar(inicioDia: diaInicio, finDia: diaFin, fechaMesAnterior);
            if (voluntariasActivas.Count == 0)
                throw new ApplicationException("No hay voluntarias disponibles para el día de hoy");

            return EjecutarGeneracionAsignacionesAbrazos(bebesAbrazar, voluntariasActivas);
        }

        private static VOLUNTARIA ElegirVoluntariaConMenorConteo(
            IReadOnlyList<VOLUNTARIA> candidatas,
            Func<VOLUNTARIA, int> conteo)
        {
            var min = candidatas.Min(conteo);
            return candidatas.First(v => conteo(v) == min);
        }

        private Dictionary<int, int> ContarAsignacionesPorVoluntaria(
            IEnumerable<int> idsVoluntarias,
            DateTime desde,
            DateTime hasta,
            int idEstadoAsignacionEliminado)
        {
            var ids = idsVoluntarias.Distinct().ToList();
            var conteos = db.ASIGNACION
                .AsNoTracking()
                .Where(a => ids.Contains(a.idVoluntaria)
                            && a.fechaHoraAsignacion >= desde
                            && a.fechaHoraAsignacion < hasta
                            && a.idEstado != idEstadoAsignacionEliminado)
                .GroupBy(a => a.idVoluntaria)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var id in ids)
                conteos.TryAdd(id, 0);

            return conteos;
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
                    .Where(b => idsBebes.Contains(b.ID))
                    .ToList()
                    .OrderBy(b => idsBebes.IndexOf(b.ID))
                    .ToList();

                var voluntarias = db.VOLUNTARIA
                    .Where(v => idsVoluntarias.Contains(v.IdVoluntaria))
                    .ToList()
                    .OrderBy(v => idsVoluntarias.IndexOf(v.IdVoluntaria))
                    .ToList();

                var asignacionesHoyPorVol = ContarAsignacionesPorVoluntaria(
                    idsVoluntarias, diaInicio, diaFin, idEstadoAsignacionEliminado);
                var asignacionesMesPorVol = ContarAsignacionesPorVoluntaria(
                    idsVoluntarias, fechaMesAnterior, fechaHoy, idEstadoAsignacionEliminado);

                var asignaciones = new List<ASIGNACION>();

                void RegistrarAsignacion(BEBE bebe, VOLUNTARIA voluntaria)
                {
                    bebe.IdEstado = idEstadoBebeAsignado;
                    voluntaria.IdEstado = idEstadoVolAsignada;

                    var asignacion = new ASIGNACION
                    {
                        idVoluntaria = voluntaria.IdVoluntaria,
                        idBebe = bebe.ID,
                        fechaHoraAsignacion = fechaHoy,
                        idEstado = idEstadoAsignacionCreada
                    };

                    db.ASIGNACION.Add(asignacion);
                    asignaciones.Add(asignacion);

                    asignacionesHoyPorVol[voluntaria.IdVoluntaria] =
                        asignacionesHoyPorVol.GetValueOrDefault(voluntaria.IdVoluntaria, 0) + 1;
                    asignacionesMesPorVol[voluntaria.IdVoluntaria] =
                        asignacionesMesPorVol.GetValueOrDefault(voluntaria.IdVoluntaria, 0) + 1;
                }

                if (bebes.Count == voluntarias.Count)
                {
                    for (var i = 0; i < bebes.Count; i++)
                        RegistrarAsignacion(bebes[i], voluntarias[i]);
                }
                else if (bebes.Count > voluntarias.Count)
                {
                    foreach (var bebe in bebes)
                    {
                        var minHoy = voluntarias.Min(v => asignacionesHoyPorVol.GetValueOrDefault(v.IdVoluntaria, 0));
                        var candidatasHoy = voluntarias
                            .Where(v => asignacionesHoyPorVol.GetValueOrDefault(v.IdVoluntaria, 0) == minHoy)
                            .ToList();

                        var voluntaria = candidatasHoy.Count == 1
                            ? candidatasHoy[0]
                            : ElegirVoluntariaConMenorConteo(
                                candidatasHoy,
                                v => asignacionesMesPorVol.GetValueOrDefault(v.IdVoluntaria, 0));

                        RegistrarAsignacion(bebe, voluntaria);
                    }
                }
                else
                {
                    var pool = voluntarias.ToList();
                    foreach (var bebe in bebes)
                    {
                        var voluntaria = ElegirVoluntariaConMenorConteo(
                            pool,
                            v => asignacionesMesPorVol.GetValueOrDefault(v.IdVoluntaria, 0));

                        RegistrarAsignacion(bebe, voluntaria);
                        pool.Remove(voluntaria);
                    }
                }

                db.SaveChanges();

                var idsCreados = asignaciones.Select(a => a.idAsignacion).ToHashSet();
                var asignacionesConDatos = db.ASIGNACION
                    .AsSplitQuery()
                    .Include(a => a.bebe!)
                        .ThenInclude(b => b.Sala)
                    .Include(a => a.voluntaria)
                    .Include(a => a.estado)
                    .Where(a => idsCreados.Contains(a.idAsignacion))
                    .ToList();

                var asignacionesRespuesta = asignacionesConDatos.Select(a => new RespuestaAsignaciones()
                {
                    idAsignacion = a.idAsignacion,
                    idBebe = a.idBebe,
                    idVoluntaria = a.idVoluntaria,
                    nombreBebe = NombreCompletoBebe(a.bebe) ?? "Desconocido",
                    nombreVoluntaria = a.voluntaria != null ? (a.voluntaria.Nombre + " " + a.voluntaria.Apellido) : "Desconocido",
                    fechaHoraAsignacion = a.fechaHoraAsignacion,
                    fechaHoraFin = a.fechaHoraFin,
                    fechaHoraInicio = a.fechaHoraInicio,
                    estadoAsignacion = a.estado?.nombre ?? a.idEstado.ToString(),
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

        /// <summary>Misma lógica que BebeRepositorio.obtenerBebesAbrazar, sobre el DbContext de esta clase (una sola unidad de trabajo).</summary>
        private List<BEBE> CargarBebesAbrazarParaGenerar(DateTime diaInicio, DateTime diaFin, int idEstadoAsignacionEliminado)
        {
            return db.BEBE
                .Where(v => v.Estado != null
                            && v.Estado.ambito.nombre == "Bebes"
                            && v.Estado.nombre == "Sin abrazar"
                            && v.Estado.nombre != "Asignado"
                            && !v.Asignaciones.Any(a =>
                                a.idEstado != idEstadoAsignacionEliminado
                                && a.fechaHoraAsignacion >= diaInicio && a.fechaHoraAsignacion < diaFin
                                && a.fechaHoraInicio != null
                                && a.fechaHoraInicio >= diaInicio && a.fechaHoraInicio < diaFin))
                .ToList();
        }

        private List<BEBE> CargarBebesPorIdsParaGenerar(
            List<int> idsBebes,
            DateTime diaInicio,
            DateTime diaFin,
            int idEstadoAsignacionEliminado)
        {
            var ids = idsBebes.Distinct().ToList();
            var elegibles = CargarBebesAbrazarParaGenerar(diaInicio, diaFin, idEstadoAsignacionEliminado)
                .Where(b => ids.Contains(b.ID))
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
            DateTime finDia,
            DateTime fechaMesAnterior)
        {
            var ids = idsVoluntarias.Distinct().ToList();
            var libres = CargarVoluntariasLibresParaGenerar(inicioDia, finDia, fechaMesAnterior)
                .Where(v => ids.Contains(v.IdVoluntaria))
                .ToDictionary(v => v.IdVoluntaria);

            var faltantes = ids.Where(id => !libres.ContainsKey(id)).ToList();
            if (faltantes.Count > 0)
                throw new ApplicationException(
                    $"Voluntaria(s) no disponibles o inexistentes: {string.Join(", ", faltantes)}.");

            return ids.Select(id => libres[id]).ToList();
        }

        /// <summary>Misma regla que obtenerVoluntariasLibres; Include filtrado de asignaciones (~1 mes) para evitar cargar todo el historial.</summary>
        private List<VOLUNTARIA> CargarVoluntariasLibresParaGenerar(DateTime inicioDia, DateTime finDia, DateTime fechaMesAnterior)
        {
            return db.VOLUNTARIA
                .AsNoTracking()
                .AsSplitQuery()
                .Include(v => v.RolInfo)
                .Include(v => v.Asignaciones.Where(a => a.fechaHoraAsignacion >= fechaMesAnterior))
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
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }

        public bool registrarFinAsignacionAbrazo(int idAsignacion,string comentario)
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
            asignacion.comentario = comentario;
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }

        /// <summary>
        /// Cierra asignaciones con bebé donde el abrazo se inició antes del día calendario actual en Argentina
        /// y nunca se finalizó: bebé a Sin abrazar, voluntaria a Disponible/Activa, asignación con fechaHoraFin.
        /// </summary>
        /// <returns>Cantidad de asignaciones actualizadas.</returns>
        public int ResetearAbrazosBebeColgadosAntesDeHoy()
        {
            var (inicioHoyUtc, _) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var idElimAsig = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
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

            const string comentarioAuto = "Cierre automático: abrazo iniciado en día anterior sin finalizar.";
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
                    asignacion.comentario = string.IsNullOrWhiteSpace(asignacion.comentario)
                        ? comentarioAuto
                        : (asignacion.comentario.Contains("Cierre automático", StringComparison.Ordinal)
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

        /// <summary>Actualiza tarea, bebé, voluntaria y comentario de una asignación existente.</summary>
        public bool eliminarAsignacion(int idAsignacion)
        {
            return asignacionRepositorio.eliminarAsignacionLogica(idAsignacion);
        }

        public bool modificarAsignacion(int idAsignacion, ASIGNACION datos)
        {
            var existentePrevio = asignacionRepositorio.consultarAsignacion(idAsignacion);
            AsegurarAsignacionNoEliminada(existentePrevio);

            if (datos.idVoluntaria <= 0)
                throw new ApplicationException("Voluntaria inválida");

            voluntariaRepositorio.consultarVoluntaria(datos.idVoluntaria);

            if (datos.idTarea.HasValue)
            {
                var tarea = db.TAREA.FirstOrDefault(t => t.idTarea == datos.idTarea.Value);
                if (tarea == null)
                    throw new NotFoundException("Tarea no encontrada");
            }

            if (datos.idBebe.HasValue)
                bebeRepositorio.consultarBebe(datos.idBebe.Value);

            return asignacionRepositorio.modificarAsignacion(datos, existentePrevio);
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