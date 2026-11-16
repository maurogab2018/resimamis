using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class AsignacionRepositorio : IAsignacionRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IInsumoRepositorio insumoRepositorio;
        private readonly IEstadoRepositorio estadoRepositorio;

        public AsignacionRepositorio(ApplicationDbContext db, IInsumoRepositorio insumoRepositorio, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.insumoRepositorio = insumoRepositorio;
            this.estadoRepositorio = estadoRepositorio;
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
                throw new NotFoundException("Asignación no encontrada con ese id.");
            return asignacion;
        }

        public bool modificarAsignacion(ASIGNACION datos, ASIGNACION existente)
        {
            existente.idTarea = datos.idTarea;
            existente.idBebe = datos.idBebe;
            existente.idVoluntaria = datos.idVoluntaria;
            existente.comentario = datos.comentario;
            existente.fechaHoraInicio = datos.fechaHoraInicio;
            existente.fechaHoraFin = datos.fechaHoraFin;
            existente.idEstado = datos.idEstado;
            db.SaveChanges();
            return true;
        }

        public bool eliminarAsignacionLogica(int idAsignacion)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            var asignacion = consultarAsignacion(idAsignacion);
            if (asignacion.idEstado == idElim)
                throw new ApplicationException("La asignación ya está eliminada.");
            asignacion.idEstado = idElim;
            db.SaveChanges();
            return true;
        }

        public List<ASIGNACION> listarAsignacionesHoy()
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asignaciones= db.ASIGNACION
                .Include(a => a.bebe!).ThenInclude(b => b.Sala)
                .Include(a => a.voluntaria)
                .Include(a => a.tarea)
                .Include(a => a.estado)
                .Where(a => a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia && a.idEstado != idElim).Select(a=> new ASIGNACION()
            {
                idAsignacion=a.idAsignacion,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin=a.fechaHoraFin,
                fechaHoraInicio=a.fechaHoraInicio,
                voluntaria=a.voluntaria,
                bebe=a.bebe,
                tarea=a.tarea,
                idBebe=a.idBebe,
                idTarea=a.idTarea,
                idVoluntaria=a.idVoluntaria,  
                estado=a.estado,
                idEstado =a.idEstado,
                comentario = a.comentario,
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
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asignaciones = db.ASIGNACION
                .Include(a => a.bebe!).ThenInclude(b => b.Sala)
                .Include(a => a.voluntaria)
                .Include(a => a.tarea)
                .Include(a => a.estado)
                .Where(a => a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia && a.idVoluntaria==idVoluntaria && a.idEstado != idElim).Select(a => new ASIGNACION()
            {
                idAsignacion = a.idAsignacion,
                fechaHoraAsignacion = a.fechaHoraAsignacion,
                fechaHoraFin = a.fechaHoraFin,
                fechaHoraInicio = a.fechaHoraInicio,
                voluntaria = a.voluntaria,
                bebe = a.bebe,
                tarea = a.tarea,
                idBebe = a.idBebe,
                idTarea = a.idTarea,
                idVoluntaria = a.idVoluntaria,
                estado = a.estado,
                idEstado = a.idEstado,
                comentario = a.comentario,
            }).ToList();

            return asignaciones;
        }

        /// <summary>Asignaciones de abrazo de un bebé (idBebe), excluye eliminadas, más recientes primero.</summary>
        public List<ASIGNACION> listarAbrazosHistoricosPorBebe(int idBebe)
        {
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            return db.ASIGNACION
                .AsNoTracking()
                .Include(a => a.bebe!).ThenInclude(b => b.Sala)
                .Include(a => a.voluntaria)
                .Include(a => a.tarea)
                .Include(a => a.estado)
                .Where(a => a.idBebe == idBebe && a.idEstado != idElim)
                .OrderByDescending(a => a.fechaHoraAsignacion)
                .ThenByDescending(a => a.idAsignacion)
                .ToList();
        }

        public void registrarCambioaAsignacion()
        {
            db.SaveChanges();
        }

        public bool registrarDetalleAsignacion(List<RequestDetalleAsignacion> request)
        {
            // Todo o nada: si una fila no tenía stock, las anteriores ya habían descontado stock
            // (registrarMovimientoStock guarda) y su detalle se perdía. El lock es el mismo de los
            // movimientos de stock, para que dos pedidos a la vez no entreguen más de lo que hay.
            using var tx = db.Database.BeginTransaction();
            BloqueosDb.Tomar(db, BloqueosDb.Stock);
            request.ForEach(r =>
            {
                if (!r.cantidadInsumo.HasValue || r.cantidadInsumo.Value <= 0)
                    return;

                var asignacion = consultarAsignacion(r.idAsignacion.Value);
                var insumo = insumoRepositorio.consultarInsumo(r.idInsumo.Value);
                if (insumo.stockActual < r.cantidadInsumo.Value)
                    throw new ApplicationException("No hay stock disponible para esa cantidad de " + insumo.nombre);

                var movimiento = new MOVIMIENTOSTOCK
                {
                    idInsumo = insumo.idInsumo,
                    idBebe = asignacion.idBebe,
                    idVoluntaria = asignacion.idVoluntaria,
                    cantidad = r.cantidadInsumo.Value,
                    esEntrada = "N",
                    observacion = $"Salida por asignación #{asignacion.idAsignacion}"
                };
                insumoRepositorio.registrarMovimientoStock(movimiento);

                var detalleAsignacion = new DETALLEASIGNACION()
                {
                    idAsignacion = asignacion.idAsignacion,
                    idInsumo = insumo.idInsumo,
                    cantidad = r.cantidadInsumo.Value,
                    fechaEntrega = NegConversorFecha.ObtenerFechaArgentina()
                };

                detalleAsignacion.nombreInsumo = insumo.nombre;
                db.DETALLEASIGNACION.Add(detalleAsignacion);
            });

            db.SaveChanges();
            tx.Commit();

            return true;
        }

        public EstadisticaDuracionesAbrazos devolverDuracionesAbrazos()
        {
            var estadisticaResultado= new EstadisticaDuracionesAbrazos();
            // Solo las columnas necesarias de los abrazos terminados (antes traía toda la tabla con
            // tracking), sin eliminadas ni duraciones negativas. Sin datos devuelve lista vacía en
            // vez de "No hay asignaciones" (400).
            var idElim = estadoRepositorio.ObtenerIdEstadoEliminado("Asignaciones");
            var resultados = db.ASIGNACION.AsNoTracking()
                .Where(a => a.fechaHoraInicio != null
                            && a.fechaHoraFin != null
                            && a.fechaHoraFin >= a.fechaHoraInicio
                            && !a.cierreAutomatico
                            && a.idEstado != idElim)
                .Select(a => new { Inicio = a.fechaHoraInicio!.Value, Fin = a.fechaHoraFin!.Value })
                .ToList()
                .Select(a => new DuracionAbrazos()
                {
                    Minutos = (int)(a.Fin - a.Inicio).TotalMinutes,
                    Segundos = (int)(a.Fin - a.Inicio).TotalSeconds % 60
                })
                .ToList();
            estadisticaResultado.listadoDuracionesAbrazos = resultados;

            // El promedio va sobre los abrazos finalizados: dividir por el total de
            // asignaciones lo diluía con las que nunca se iniciaron.
            estadisticaResultado.promedioDuracionAbrazos = resultados.Count > 0
                ? resultados.Average(r => r.Minutos + (r.Segundos / 60.0))
                : 0;
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
                    Mes = g.Key.Mes,
                    Anio = g.Key.Año,
                    CantidadAsignaciones = g.Count()
                })
                .OrderBy(r => r.Anio)
                .ThenBy(r => r.Mes)
                .ToList();

            return cantidadAsignacionesPorMes;
        }
    }
}
