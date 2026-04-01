using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class AsignacionRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly InsumoRepositorio insumoRepositorio;
        public AsignacionRepositorio()
        {
            db = new ApplicationDbContext();
            insumoRepositorio = new InsumoRepositorio();
        }

        public void registrarAsignacion(ASIGNACION asignacion)
        {
            db.ASIGNACION.Add(asignacion);
            db.SaveChanges();
        }
        public ASIGNACION consultarAsignacion(int idAsignacion)
        {
            var asignacion = db.ASIGNACION.FirstOrDefault(a => a.idAsignacion == idAsignacion);
            if (asignacion == null)
                throw new ApplicationException("Asignación con ese id inexistente");
            return asignacion;
        }
        public List<ASIGNACION> listarAsignacionesHoy()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asignaciones= db.ASIGNACION.Where(a => a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia).Select(a=> new ASIGNACION()
            {
                idAsignacion=a.idAsignacion,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin=a.fechaHoraFin,
                fechaHoraInicio=a.fechaHoraInicio,
                voluntaria=a.voluntaria,
                bebe=a.bebe,
                idBebe=a.idBebe,
                idVoluntaria=a.idVoluntaria,  
                estado=a.estado,
                idEstado =a.idEstado,
            }).ToList();

            return asignaciones;
        }

        //public ASIGNACION consultarAsignacionCompleta(int idAsignacion)
        //{
        //    var asignacion = db.ASIGNACION.FirstOrDefault(a => a.idAsignacion == idAsignacion).Select(d=>new ASIGNACION()
        //    {

        //    });
        //    if (asignacion == null)
        //        throw new ApplicationException("Asignación con ese id inexistente");
        //    return asignacion;
        //}

        public List<ASIGNACION> listarAsignacionesHoyVoluntaria(int idVoluntaria)
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asignaciones = db.ASIGNACION.Where(a => a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia && a.idVoluntaria==idVoluntaria).Select(a => new ASIGNACION()
            {
                idAsignacion = a.idAsignacion,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin = a.fechaHoraFin,
                fechaHoraInicio = a.fechaHoraInicio,
                voluntaria = a.voluntaria,
                bebe = a.bebe,
                idBebe = a.idBebe,
                idVoluntaria = a.idVoluntaria,
                estado = a.estado,
                idEstado = a.idEstado,
            }).ToList();

            return asignaciones;
        }
        public void registrarCambioaAsignacion()
        {
            db.SaveChangesAsync();
        }

        public bool registrarDetalleAsignacion(List<RequestDetalleAsignacion> request)
        {
            request.ForEach(r =>
            {
                var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
                var asignacion = consultarAsignacion(r.idAsignacion.Value);
                var insumo = insumoRepositorio.consultarInsumo(r.idInsumo.Value);
                if (insumo.stockActual < r.cantidadInsumo.Value)
                    throw new ApplicationException("No hay stock disponible para esa cantidad de " + insumo.nombre);

                var detalleAsignacion = new DETALLEASIGNACION()
                {
                    idAsignacion = asignacion.idAsignacion,
                    idInsumo = insumo.idInsumo,
                    cantidad = r.cantidadInsumo.Value,
                    fechaEntrega = NegConversorFecha.ObtenerFechaArgentina()
                };

                insumoRepositorio.actualizarStock(insumo, r.cantidadInsumo.Value);
                detalleAsignacion.nombreInsumo = insumo.nombre;
                db.DETALLEASIGNACION.Add(detalleAsignacion);
            });

            db.SaveChanges();

            return true;
        }

        public EstadisticaDuracionesAbrazos devolverDuracionesAbrazos()
        {
            var estadisticaResultado= new EstadisticaDuracionesAbrazos();
            List<ASIGNACION> asignaciones= db.ASIGNACION.ToList(); // Puedes reemplazar esto con la obtención real de datos desde tu base de datos.
            var resultados = asignaciones
                .Where(a => a.fechaHoraInicio != null && a.fechaHoraFin != null)
                .Select(a => new DuracionAbrazos()
                {
                    Minutos = (int)(a.fechaHoraFin.Value - a.fechaHoraInicio.Value).TotalMinutes,
                    Segundos = (int)(a.fechaHoraFin.Value - a.fechaHoraInicio.Value).TotalSeconds % 60
                })
                .ToList();
            estadisticaResultado.listadoDuracionesAbrazos = resultados;

            if (asignaciones.Count > 0)
            {
                double sumaTotalMinutos = 0;
                var asignacionesValidas = asignaciones
                .Where(a => a.fechaHoraInicio != null && a.fechaHoraFin != null);

                foreach (var asignacion in asignacionesValidas)
                {
                    var diferenciaEnMinutos = (asignacion.fechaHoraFin.Value - asignacion.fechaHoraInicio.Value).TotalMinutes;
                    sumaTotalMinutos += diferenciaEnMinutos;
                }
                var promedioEnMinutos = sumaTotalMinutos / asignaciones.Count;
                estadisticaResultado.promedioDuracionAbrazos = promedioEnMinutos;
            }
            else
            {
                throw new ApplicationException("No hay asignaciones");
            }
            return estadisticaResultado;
        }

        //esta queda comenada por el momento
        //public List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones(string FechaInicio,string FechaFin)
        //{
        //    List<ASIGNACION> asignaciones = db.ASIGNACION.ToList();// Reemplaza esto con la obtención real de datos desde tu base de datos.

        //    DateTime fechaInicio = DateTime.Parse(FechaInicio);
        //    DateTime fechaFin = DateTime.Parse(FechaFin);

        //    var cantidadAsignacionesPorDia = asignaciones
        //        .Where(a => a.fechaHoraAsignacion >= fechaInicio && a.fechaHoraAsignacion <= fechaFin)
        //        .GroupBy(a => a.fechaHoraAsignacion.Date)
        //        .Select(g => new EstadsiticaCantidadAsignacion()
        //        {
        //            Fecha = g.Key,
        //            CantidadAsignaciones = g.Count()
        //        })
        //        .OrderBy(r => r.Fecha)
        //        .ToList();
        //    return cantidadAsignacionesPorDia;
        //}
        public List<EstadsiticaCantidadAsignacion> devolverEstadisticaCantidadAsignaciones1()
        {
            List<ASIGNACION> asignaciones = db.ASIGNACION.ToList();// Reemplaza esto con la obtención real de datos desde tu base de datos.

            var cantidadAsignacionesPorMes = asignaciones
                .GroupBy(a => new { Mes = a.fechaHoraAsignacion.Month, Año = a.fechaHoraAsignacion.Year })
                .Select(g => new EstadsiticaCantidadAsignacion()
                {
                    Mes =  g.Key.Mes, // El primer día del mes
                    CantidadAsignaciones = g.Count()
                })
                .OrderBy(r => r.Mes)
                .ToList();

            return cantidadAsignacionesPorMes;
        }
    }
}
