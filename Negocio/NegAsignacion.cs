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
        private readonly ApplicationDbContext db;
        public NegAsignacion()
        {
            bebeRepositorio = new BebeRepositorio();
            voluntariaRepositorio = new VoluntariaRepositorio();
            asignacionRepositorio= new AsignacionRepositorio();
            db = new ApplicationDbContext();
        }


        public List<RespuestaAsignaciones> generarAsiganacionTareas(RequestAsignacionTareas requestAsignacion)
        {


            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            // Traemos las tareas y voluntarias desde la base
            var tareas = db.TAREA.Where(t => requestAsignacion.idTareas.Contains(t.idTarea)).ToList();
            var voluntarias = voluntariaRepositorio.consultarVoluntarias(requestAsignacion.idVoluntarias);

            if (tareas == null || tareas.Count == 0)
                throw new ApplicationException("No se encontraron tareas válidas.");

            if (voluntarias == null || voluntarias.Count == 0)
                throw new ApplicationException("No se encontraron voluntarias válidas.");

            // Armamos un diccionario con las asignaciones de hoy para cada voluntaria
            var voluntariasConAsignaciones = voluntarias.Select(v => new VoluntariaConAsignaciones()
            {
                Voluntaria = v,
                CantidadAsignacionesHoy = v.Asignaciones?.Count(a => a.fechaHoraAsignacion >= diaInicio && a.fechaHoraAsignacion < diaFin) ?? 0
            }).ToList();

            // Para desempatar cuando hay igual cantidad de asignaciones
            var random = new Random();

            // Resultado
            var respuestas = new List<RespuestaAsignaciones>();

            foreach (var tarea in tareas)
            {
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
                    idEstado = 1
                };

                voluntariaRepositorio.asignarVoluntaria(seleccionada.IdVoluntaria);
                db.ASIGNACION.Add(asignacion);
                db.SaveChanges();

                respuestas.Add(new RespuestaAsignaciones
                {
                    idAsignacion = asignacion.idAsignacion,
                    idVoluntaria = seleccionada.IdVoluntaria,
                    nombreVoluntaria = seleccionada.Nombre,
                    fechaHoraAsignacion = fechaHoy,
                    estadoAsignacion = asignacion.idEstado.ToString()
                });

                // Actualizamos las asignaciones de la voluntaria seleccionada
                var voluntariaAsignada = voluntariasConAsignaciones.First(v => v.Voluntaria.IdVoluntaria == seleccionada.IdVoluntaria);
                voluntariaAsignada.CantidadAsignacionesHoy++;
            }

            return respuestas;

        }
        public RespuestaAsignaciones generarAsiganacionTarea(RequestAsignacionTarea requestAsignacion)
        {
            var voluntaria = voluntariaRepositorio.consultarVoluntaria(requestAsignacion.idVoluntaria);
            if(voluntaria== null)
                throw new ApplicationException("Voluntaria no encontrada");
            var tarea = db.TAREA.FirstOrDefault(t=>t.idTarea==requestAsignacion.idTarea);
            if (tarea == null)
                throw new ApplicationException("Tarea no encontrada");
            try
            {
                var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                var asignacion = new ASIGNACION();
                asignacion.idVoluntaria = voluntaria.IdVoluntaria;
                //asignacion.idBebe = bebesAbrazar[i].ID;
                asignacion.fechaHoraAsignacion = fechaHoy;
                asignacion.idEstado = 1;
                asignacion.idTarea = tarea.idTarea;
                voluntariaRepositorio.asignarVoluntaria(voluntaria.IdVoluntaria);
                db.ASIGNACION.Add(asignacion);
                db.SaveChanges();


                var asignacionesRespuesta = new RespuestaAsignaciones()
                {
                    idAsignacion = asignacion.idAsignacion,
                    idVoluntaria = voluntaria.IdVoluntaria,
                    nombreVoluntaria =voluntaria.Nombre,
                    fechaHoraAsignacion = fechaHoy,
                    estadoAsignacion =asignacion.idEstado.ToString(),
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

                var bebesAbrazar = CargarBebesAbrazarParaGenerar(diaInicio, diaFin);
                if (bebesAbrazar.Count == 0)
                    throw new ApplicationException("No hay bebes para abrazar para el día de hoy");

                var voluntariasActivas = CargarVoluntariasLibresParaGenerar(inicioDia: diaInicio, finDia: diaFin, fechaMesAnterior);
                if (voluntariasActivas.Count == 0)
                    throw new ApplicationException("No hay voluntarias disponibles para el día de hoy");

                var asignaciones = new List<ASIGNACION>();

                void AsegurarListaAsignaciones(VOLUNTARIA v)
                {
                    v.Asignaciones ??= new List<ASIGNACION>();
                }

                void AplicarEstadosBebeYVoluntaria(BEBE bebe, int idVoluntaria)
                {
                    if (bebe.IdEstado != idEstadoBebeAsignado)
                        bebe.IdEstado = idEstadoBebeAsignado;

                    var vol = db.VOLUNTARIA.Local.FirstOrDefault(x => x.IdVoluntaria == idVoluntaria)
                              ?? db.VOLUNTARIA.Find(idVoluntaria);
                    if (vol == null)
                        throw new ApplicationException("Voluntaria no existente con ese Id");
                    if (vol.IdEstado != idEstadoVolAsignada)
                        vol.IdEstado = idEstadoVolAsignada;
                }

                if (bebesAbrazar.Count == voluntariasActivas.Count)
                {
                    for (int i = 0; i < bebesAbrazar.Count; i++)
                    {
                        var asignacion = new ASIGNACION
                        {
                            idVoluntaria = voluntariasActivas[i].IdVoluntaria,
                            idBebe = bebesAbrazar[i].ID,
                            fechaHoraAsignacion = fechaHoy,
                            idEstado = 1
                        };
                        AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], voluntariasActivas[i].IdVoluntaria);
                        db.ASIGNACION.Add(asignacion);
                        asignaciones.Add(asignacion);
                    }
                }

                if (bebesAbrazar.Count > voluntariasActivas.Count)
                {
                    for (int i = 0; i < bebesAbrazar.Count; i++)
                    {
                        int minAsignacionesHoy = voluntariasActivas
                            .Where(voluntaria => voluntaria.Asignaciones != null)
                            .Select(voluntaria => voluntaria.Asignaciones!
                                .Count(asignacion => asignacion.fechaHoraAsignacion >= diaInicio && asignacion.fechaHoraAsignacion < diaFin))
                            .DefaultIfEmpty(0)
                            .Min();

                        var voluntariaMenosAsignacionesHoy = voluntariasActivas
                            .Where(voluntaria => voluntaria.Asignaciones != null && voluntaria.Asignaciones
                                .Count(asignacion => asignacion.fechaHoraAsignacion >= diaInicio && asignacion.fechaHoraAsignacion < diaFin) == minAsignacionesHoy)
                            .ToList();

                        if (voluntariaMenosAsignacionesHoy.Count == 1)
                        {
                            var vol = voluntariaMenosAsignacionesHoy[0];
                            AsegurarListaAsignaciones(vol);
                            var asignacion = new ASIGNACION
                            {
                                idVoluntaria = vol.IdVoluntaria,
                                idBebe = bebesAbrazar[i].ID,
                                fechaHoraAsignacion = fechaHoy,
                                idEstado = 1
                            };
                            vol.Asignaciones!.Add(asignacion);
                            AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], vol.IdVoluntaria);
                            db.ASIGNACION.Add(asignacion);
                            asignacion.bebe = bebesAbrazar[i];
                            asignacion.voluntaria = vol;
                            asignaciones.Add(asignacion);
                        }
                        else
                        {
                            int minAsignacionesMesPasado = voluntariaMenosAsignacionesHoy
                                .Select(voluntaria => voluntaria.Asignaciones!
                                    .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy))
                                .DefaultIfEmpty(0)
                                .Min();

                            var voluntariaMenosAsignacionesMes = voluntariaMenosAsignacionesHoy
                                .Where(voluntaria => voluntaria.Asignaciones != null && voluntaria.Asignaciones
                                    .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy) == minAsignacionesMesPasado)
                                .FirstOrDefault();

                            if (voluntariaMenosAsignacionesMes != null)
                            {
                                var vol = voluntariaMenosAsignacionesMes;
                                AsegurarListaAsignaciones(vol);
                                var asignacion = new ASIGNACION
                                {
                                    idVoluntaria = vol.IdVoluntaria,
                                    idBebe = bebesAbrazar[i].ID,
                                    fechaHoraAsignacion = fechaHoy,
                                    idEstado = 1
                                };
                                var actualizaVoluntariasLibres = voluntariasActivas.Single(v => v.IdVoluntaria == vol.IdVoluntaria);
                                actualizaVoluntariasLibres.Asignaciones!.Add(asignacion);
                                AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], vol.IdVoluntaria);
                                db.ASIGNACION.Add(asignacion);
                                asignacion.bebe = bebesAbrazar[i];
                                asignacion.voluntaria = vol;
                                asignaciones.Add(asignacion);
                            }
                            else
                            {
                                var asignacion = new ASIGNACION
                                {
                                    idVoluntaria = voluntariasActivas[0].IdVoluntaria,
                                    idBebe = bebesAbrazar[i].ID,
                                    fechaHoraAsignacion = fechaHoy,
                                    idEstado = 1
                                };
                                var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == voluntariasActivas[0].IdVoluntaria);
                                voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                                AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], voluntariasActivas[0].IdVoluntaria);
                                db.ASIGNACION.Add(asignacion);
                                asignacion.bebe = bebesAbrazar[i];
                                asignacion.voluntaria = voluntariasActivas[0];
                                asignaciones.Add(asignacion);
                            }
                        }
                    }
                }

                if (bebesAbrazar.Count < voluntariasActivas.Count)
                {
                    for (int i = 0; i < bebesAbrazar.Count; i++)
                    {
                        int minAsignacionesMesPasado = voluntariasActivas
                            .Where(voluntaria => voluntaria.Asignaciones != null)
                            .Select(voluntaria => voluntaria.Asignaciones!
                                .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy))
                            .DefaultIfEmpty(0)
                            .Min();

                        var voluntariaMenosAsignacionesMes = voluntariasActivas
                            .Where(voluntaria => (voluntaria.Asignaciones ?? new List<ASIGNACION>())
                                .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy) == minAsignacionesMesPasado)
                            .FirstOrDefault();

                        if (voluntariaMenosAsignacionesMes != null)
                        {
                            var vol = voluntariaMenosAsignacionesMes;
                            AsegurarListaAsignaciones(vol);
                            var asignacion = new ASIGNACION
                            {
                                idVoluntaria = vol.IdVoluntaria,
                                idBebe = bebesAbrazar[i].ID,
                                fechaHoraAsignacion = fechaHoy,
                                idEstado = 1
                            };
                            var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == vol.IdVoluntaria);
                            voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                            vol.Asignaciones!.Add(asignacion);
                            AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], vol.IdVoluntaria);
                            db.ASIGNACION.Add(asignacion);
                            asignacion.bebe = bebesAbrazar[i];
                            asignacion.voluntaria = vol;
                            asignaciones.Add(asignacion);
                        }
                        else
                        {
                            var vol0 = voluntariasActivas[0];
                            AsegurarListaAsignaciones(vol0);
                            var asignacion = new ASIGNACION
                            {
                                idVoluntaria = vol0.IdVoluntaria,
                                idBebe = bebesAbrazar[i].ID,
                                fechaHoraAsignacion = fechaHoy,
                                idEstado = 1
                            };
                            var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == vol0.IdVoluntaria);
                            voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                            vol0.Asignaciones!.Add(asignacion);
                            AplicarEstadosBebeYVoluntaria(bebesAbrazar[i], vol0.IdVoluntaria);
                            db.ASIGNACION.Add(asignacion);
                            asignacion.bebe = bebesAbrazar[i];
                            asignacion.voluntaria = vol0;
                            asignaciones.Add(asignacion);
                        }
                    }
                }

                db.SaveChanges();

                var idsCreados = asignaciones.Select(a => a.idAsignacion).ToHashSet();
                var asignacionesConDatos = db.ASIGNACION
                    .AsSplitQuery()
                    .Include(a => a.bebe)
                    .Include(a => a.voluntaria)
                    .Where(a => idsCreados.Contains(a.idAsignacion))
                    .ToList();

                var asignacionesRespuesta = asignacionesConDatos.Select(a => new RespuestaAsignaciones()
                {
                    idAsignacion = a.idAsignacion,
                    idBebe = a.idBebe,
                    idVoluntaria = a.idVoluntaria,
                    nombreBebe = a.bebe != null ? a.bebe.nombre : "Desconocido",
                    nombreVoluntaria = a.voluntaria != null ? (a.voluntaria.Nombre + " " + a.voluntaria.Apellido) : "Desconocido",
                    fechaHoraAsignacion = a.fechaHoraAsignacion,
                    fechaHoraFin = a.fechaHoraFin,
                    fechaHoraInicio = a.fechaHoraInicio,
                    estadoAsignacion = a.idEstado.ToString(),
                    sala = a.bebe != null ? a.bebe.IdSala : 0
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
        private List<BEBE> CargarBebesAbrazarParaGenerar(DateTime diaInicio, DateTime diaFin)
        {
            return db.BEBE
                .Where(v => v.Estado != null
                            && v.Estado.ambito.nombre == "Bebes"
                            && v.Estado.nombre == "Sin abrazar"
                            && v.Estado.nombre != "Asignado"
                            && !v.Asignaciones.Any(a =>
                                a.fechaHoraAsignacion >= diaInicio && a.fechaHoraAsignacion < diaFin
                                && a.fechaHoraInicio != null
                                && a.fechaHoraInicio >= diaInicio && a.fechaHoraInicio < diaFin))
                .ToList();
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
                            && v.Estado.nombre != "Carpeta médica")
                .ToList();
        }
        
        public bool registrarInicioAsignacionAbrazo(int idAsignacion)
        {
            var asignacion = asignacionRepositorio.consultarAsignacion(idAsignacion);
            if (asignacion.fechaHoraInicio != null)
                throw new ApplicationException("Abrazo ya inicializado");

            var nombreEstadoVoluntaria = asignacion.idBebe.HasValue ? "Abrazando" : "Ayudando";

            var estado = db.ESTADO.FirstOrDefault(e => e.nombre == nombreEstadoVoluntaria && e.ambito.nombre == "Voluntarias");
            if (estado == null)
                throw new ApplicationException("Estado no existente");

            var voluntariaAsignacion = voluntariaRepositorio.consultarVoluntaria(asignacion.idVoluntaria);
            voluntariaAsignacion.IdEstado = estado.idEstado;
            voluntariaRepositorio.cambioEstadoVoluntaria(voluntariaAsignacion);

            if (asignacion.idBebe.HasValue)
            {
                var estadoBebe = db.ESTADO.FirstOrDefault(e => e.nombre == "Abrazado" && e.ambito.nombre == "Bebes");
                if (estadoBebe == null)
                    throw new ApplicationException("Estado no existente");

                var bebeAbrazado = bebeRepositorio.consultarBebe(asignacion.idBebe.Value);
                bebeAbrazado.IdEstado = estadoBebe.idEstado;
                bebeRepositorio.cambioEstadoBebe(bebeAbrazado, estadoBebe.idEstado);
            }

            asignacion.fechaHoraInicio = NegConversorFecha.ObtenerFechaArgentina();
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }

        public bool registrarFinAsignacionAbrazo(int idAsignacion,string comentario)
        {
            var asignacion = asignacionRepositorio.consultarAsignacion(idAsignacion);
            if (asignacion.fechaHoraInicio == null)
                throw new ApplicationException("Abrazo nunca fue inicializado");

            if (asignacion.fechaHoraFin != null)
                throw new ApplicationException("Abrazo ya fue finalizado");

            var estado = db.ESTADO.FirstOrDefault(e => e.nombre == "Activa" && e.ambito.nombre == "Voluntarias");
            if (estado == null)
                throw new ApplicationException("Estado no existente");

            var voluntariaAsignacion = voluntariaRepositorio.consultarVoluntaria(asignacion.idVoluntaria);
            voluntariaAsignacion.IdEstado = estado.idEstado;
            voluntariaRepositorio.cambioEstadoVoluntaria(voluntariaAsignacion);

            if (asignacion.idBebe.HasValue)
            {
                // El bebé es seteado a "Sin abrazar" al finalizar el turno
                var estadoBebe = db.ESTADO.FirstOrDefault(e => e.nombre == "Sin abrazar" && e.ambito.nombre == "Bebes");
                if (estadoBebe == null)
                    throw new ApplicationException("Estado no existente");

                var bebeAbrazado = bebeRepositorio.consultarBebe(asignacion.idBebe.Value);
                bebeAbrazado.IdEstado = estadoBebe.idEstado;
                bebeRepositorio.cambioEstadoBebe(bebeAbrazado, estadoBebe.idEstado);
            }

            asignacion.fechaHoraFin = NegConversorFecha.ObtenerFechaArgentina();
            asignacion.comentario = comentario;
            asignacionRepositorio.registrarCambioaAsignacion();

            return true;
        }


        public List<RespuestaAsignaciones>? listarAsignacionesHoy()
        {
            var asignacionesHoy = asignacionRepositorio.listarAsignacionesHoy().Select(a => new RespuestaAsignaciones()
            {
                idAsignacion = a.idAsignacion,
                idBebe = a.idBebe,
                idVoluntaria = a.idVoluntaria,
                nombreBebe = a.bebe.nombre,
                nombreVoluntaria = a.voluntaria.Nombre +" "+a.voluntaria.Apellido,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin = a.fechaHoraFin,
                fechaHoraInicio = a.fechaHoraInicio,
                estadoAsignacion = a.idEstado.ToString(),
                sala = a.bebe.IdSala,
                detalles = db.DETALLEASIGNACION.Where(d=>d.idAsignacion==a.idAsignacion).Select(a => new DetalleAsignacionResumido()
                {
                    cantidad = a.cantidad,
                    idInsumo= a.idInsumo,
                    nombreInsumo= a.nombreInsumo,
                    fechaEntrega=a.fechaEntrega.Value
                }).ToList()
            }).ToList();

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
            var asignacionesHoy = asignacionRepositorio.listarAsignacionesHoyVoluntaria(idVoluntaria).Select(a => new RespuestaAsignaciones()
            {
                idAsignacion = a.idAsignacion,
                idBebe = a.idBebe,
                idVoluntaria = a.idVoluntaria,
                nombreBebe = a.bebe.nombre,
                nombreVoluntaria = a.voluntaria.Nombre + " " + a.voluntaria.Apellido,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin = a.fechaHoraFin,
                fechaHoraInicio = a.fechaHoraInicio,
                estadoAsignacion = a.idEstado.ToString(),
                sala=a.bebe.IdSala,
                detalles = db.DETALLEASIGNACION.Where(d => d.idAsignacion == a.idAsignacion).Select(a => new DetalleAsignacionResumido()
                {
                    cantidad = a.cantidad,
                    idInsumo = a.idInsumo,
                    nombreInsumo = a.nombreInsumo,
                    fechaEntrega = a.fechaEntrega.Value
                }).ToList()
            }).ToList();
            if (asignacionesHoy.Count == 0)
                throw new ApplicationException("No hay asignaciones en el día de hoy");
            return asignacionesHoy;
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