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
        public List<RespuestaAsignaciones> generarAsiganaciones() {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var bebesAbrazar = bebeRepositorio.obtenerBebesAbrazar();
                    var voluntariasActivas = voluntariaRepositorio.obtenerVoluntariasLibres();
                    var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                    var (diaInicio, diaFin) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
                    var asignaciones = new List<ASIGNACION>();
                    if (bebesAbrazar.Count == voluntariasActivas.Count)
                    {
                        for (int i = 0; i < bebesAbrazar.Count; i++)
                        {
                            var asignacion = new ASIGNACION();
                            asignacion.idVoluntaria = voluntariasActivas[i].IdVoluntaria;
                            asignacion.idBebe = bebesAbrazar[i].ID;
                            asignacion.fechaHoraAsignacion = fechaHoy;
                            asignacion.idEstado = 1;
                            bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                            voluntariaRepositorio.asignarVoluntaria(voluntariasActivas[i].IdVoluntaria);
                            db.ASIGNACION.Add(asignacion);
                            db.SaveChanges();
                            asignaciones.Add(asignacion);
                        }
                    }
                    if (bebesAbrazar.Count > voluntariasActivas.Count)
                    {
                        for (int i = 0; i < bebesAbrazar.Count; i++)
                        {
                            int minAsignacionesHoy = voluntariasActivas
                                    .Where(voluntaria => voluntaria.Asignaciones != null)
                                    .Select(voluntaria => voluntaria.Asignaciones
                                        .Count(asignacion => asignacion.fechaHoraAsignacion >= diaInicio && asignacion.fechaHoraAsignacion < diaFin))
                                    .DefaultIfEmpty(0) // Si no hay elementos, establece el valor predeterminado a 0
                                    .Min();


                            // Filtrar voluntarias que tienen la cantidad mínima de asignaciones en el día actual
                            var voluntariaMenosAsignacionesHoy = voluntariasActivas
                                .Where(voluntaria => voluntaria.Asignaciones != null && voluntaria.Asignaciones
                                    .Count(asignacion => asignacion.fechaHoraAsignacion >= diaInicio && asignacion.fechaHoraAsignacion < diaFin) == minAsignacionesHoy)
                                .ToList();

                            if (voluntariaMenosAsignacionesHoy.Count == 1)
                            {
                                var asignacion = new ASIGNACION();
                                asignacion.idVoluntaria = voluntariaMenosAsignacionesHoy[0].IdVoluntaria;
                                asignacion.idBebe = bebesAbrazar[i].ID;
                                asignacion.fechaHoraAsignacion = fechaHoy;
                                asignacion.idEstado = 1;
                                voluntariaMenosAsignacionesHoy[0].Asignaciones.Add(asignacion);
                                bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                                voluntariaRepositorio.asignarVoluntaria(voluntariaMenosAsignacionesHoy[0].IdVoluntaria);
                                db.ASIGNACION.Add(asignacion);
                                db.SaveChanges();
                                asignacion.bebe = bebesAbrazar[i];
                                asignacion.voluntaria = voluntariaMenosAsignacionesHoy[0];
                                asignaciones.Add(asignacion);
                            }
                            else
                            {
                                DateTime fechaMesAnterior = fechaHoy.AddMonths(-1);
                                int minAsignacionesMesPasado = voluntariaMenosAsignacionesHoy
                                    .Select(voluntaria => voluntaria.Asignaciones
                                        .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy))
                                    .DefaultIfEmpty(0) // Establece el valor predeterminado a 0 si no hay elementos
                                    .Min();

                                // Filtrar voluntarias que tienen la cantidad mínima de asignaciones en el día actual
                                var voluntariaMenosAsignacionesMes = voluntariaMenosAsignacionesHoy
                                    .Where(voluntaria => voluntaria.Asignaciones != null && voluntaria.Asignaciones
                                        .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy) == minAsignacionesMesPasado)
                                    .FirstOrDefault();
                                if (voluntariaMenosAsignacionesMes != null)
                                {
                                    var asignacion = new ASIGNACION();
                                    asignacion.idVoluntaria = voluntariaMenosAsignacionesMes.IdVoluntaria;
                                    asignacion.idBebe = bebesAbrazar[i].ID;
                                    asignacion.fechaHoraAsignacion = fechaHoy;
                                    asignacion.idEstado = 1;
                                    var actualizaVoluntariasLibres = voluntariasActivas.Single(v => v.IdVoluntaria == voluntariaMenosAsignacionesMes.IdVoluntaria);
                                    actualizaVoluntariasLibres.Asignaciones.Add(asignacion);
                                    bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                                    voluntariaRepositorio.asignarVoluntaria(voluntariaMenosAsignacionesMes.IdVoluntaria);
                                    db.ASIGNACION.Add(asignacion);
                                    db.SaveChanges();
                                    asignacion.bebe = bebesAbrazar[i];
                                    asignacion.voluntaria = voluntariaMenosAsignacionesMes;
                                    asignaciones.Add(asignacion);
                                }
                                else
                                {
                                    var asignacion = new ASIGNACION();
                                    asignacion.idVoluntaria = voluntariasActivas[0].IdVoluntaria;
                                    asignacion.idBebe = bebesAbrazar[i].ID;
                                    asignacion.fechaHoraAsignacion = fechaHoy;
                                    asignacion.idEstado = 1;
                                    var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == voluntariasActivas[0].IdVoluntaria);
                                    voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                                    bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                                    voluntariaRepositorio.asignarVoluntaria(voluntariasActivas[0].IdVoluntaria);
                                    db.ASIGNACION.Add(asignacion);
                                    db.SaveChanges();
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
                            //revisar logica asignacion, ordenar y luego traer el primero
                            DateTime fechaMesAnterior = fechaHoy.AddMonths(-1);
                            int minAsignacionesMesPasado = voluntariasActivas
                                .Where(voluntaria => voluntaria.Asignaciones != null)
                                .Select(voluntaria => voluntaria.Asignaciones
                                    .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy))
                                .DefaultIfEmpty(0) // Establece el valor predeterminado a 0 si no hay elementos
                                .Min();

                            var voluntariaMenosAsignacionesMes = voluntariasActivas
                                .Where(voluntaria => voluntaria.Asignaciones
                                    .Count(asignacion => asignacion.fechaHoraAsignacion >= fechaMesAnterior && asignacion.fechaHoraAsignacion < fechaHoy) == minAsignacionesMesPasado)
                                .FirstOrDefault();
                            if (voluntariaMenosAsignacionesMes != null)
                            {
                                var asignacion = new ASIGNACION();
                                asignacion.idVoluntaria = voluntariaMenosAsignacionesMes.IdVoluntaria;
                                asignacion.idBebe = bebesAbrazar[i].ID;
                                asignacion.fechaHoraAsignacion = fechaHoy;
                                asignacion.idEstado = 1;
                                var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == voluntariaMenosAsignacionesMes.IdVoluntaria);
                                voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                                bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                                voluntariaRepositorio.asignarVoluntaria(voluntariaMenosAsignacionesMes.IdVoluntaria);
                                db.ASIGNACION.Add(asignacion);
                                db.SaveChanges();
                                asignacion.bebe = bebesAbrazar[i];
                                asignacion.voluntaria = voluntariaMenosAsignacionesMes;
                                asignaciones.Add(asignacion);
                            }
                            else
                            {
                                var asignacion = new ASIGNACION();
                                asignacion.idVoluntaria = voluntariasActivas[0].IdVoluntaria;
                                asignacion.idBebe = bebesAbrazar[i].ID;
                                asignacion.fechaHoraAsignacion = fechaHoy;
                                asignacion.idEstado = 1;
                                var actualizaVoluntariaAsignada = voluntariasActivas.Single(v => v.IdVoluntaria == voluntariasActivas[0].IdVoluntaria);
                                voluntariasActivas.Remove(actualizaVoluntariaAsignada);
                                bebeRepositorio.asignarBebe(bebesAbrazar[i]);
                                voluntariaRepositorio.asignarVoluntaria(voluntariasActivas[0].IdVoluntaria);
                                db.ASIGNACION.Add(asignacion);
                                db.SaveChanges();
                                asignacion.bebe = bebesAbrazar[i];
                                asignacion.voluntaria = voluntariasActivas[0];
                                asignaciones.Add(asignacion);
                            }
                        }
                    }
                    // Si todo va bien, confirma la transacción

                    var asignacionesConDatos = db.ASIGNACION
                        .Include(a => a.bebe)
                        .Include(a => a.voluntaria)
                        .Where(a => asignaciones.Select(x => x.idAsignacion).Contains(a.idAsignacion))
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
                    // Si ocurre algún error, realiza un rollback para deshacer los cambios
                    transaction.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }
            
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