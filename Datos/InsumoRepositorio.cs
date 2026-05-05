using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class InsumoRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly VoluntariaRepositorio voluntariaRepositorio;
        private readonly EstadoRepositorio estadoRepositorio;

        public InsumoRepositorio()
        {
            voluntariaRepositorio = new VoluntariaRepositorio();
            db = new ApplicationDbContext();
            estadoRepositorio = new EstadoRepositorio();
        }

        /// <summary>Insumo con estado en ámbito Insumos y que no esté en estado Eliminado.</summary>
        internal static bool EsInsumoVisibleLista(INSUMO i) =>
            i.idEstado != null
            && i.Estado != null
            && i.Estado.ambito != null
            && i.Estado.ambito.nombre == "Insumos"
            && i.Estado.nombre != "Eliminado";

        private IQueryable<INSUMO> QueryInsumosVisiblesAmbitoInsumos()
        {
            return db.INSUMO
                .Include(i => i.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(i =>
                    i.idEstado != null
                    && i.Estado != null
                    && i.Estado!.ambito!.nombre == "Insumos"
                    && i.Estado.nombre != "Eliminado");
        }

        public List<INSUMO> obtenerInsumos()
        {
            return QueryInsumosVisiblesAmbitoInsumos().AsNoTracking().ToList();
        }

        public INSUMO? obtenerInsumoPorIdSinTracking(int idInsumo)
        {
            return QueryInsumosVisiblesAmbitoInsumos().AsNoTracking().FirstOrDefault(i => i.idInsumo == idInsumo);
        }

        public List<PROVEEDOR> obtenerProveedores()
        {
            return db.PROVEEDOR.ToList();
        }

        public List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro)
        {
            var listaMovimientos = db.MOVIMIENTOSTOCK
                .Where(m =>
            (movimientoFiltro == null ||
             ((movimientoFiltro.fechaDesde == null || m.fechaMovimiento >= movimientoFiltro.fechaDesde) &&
              (movimientoFiltro.fechaHasta == null || m.fechaMovimiento <= movimientoFiltro.fechaHasta))))
                .Select(m => new ConsultaMovimiento()
                {
                    idMovimiento = m.idMovimiento,
                    idInsumo = m.idInsumo,
                    idBebe = m.idBebe,
                    idVoluntaria = m.idVoluntaria,
                    fechaMovimiento = m.fechaMovimiento,
                    observacion = m.observacion,
                    cantidad = m.cantidad,
                    esEntrada = m.esEntrada,
                    idProveedor = m.idProveedor,
                    nombreProveedor = db.PROVEEDOR.FirstOrDefault(p => p.idProveedor == m.idProveedor)!.nombre,
                    nombreVoluntaria = voluntariaRepositorio.consultarVoluntaria(m.idVoluntaria!.Value).Nombre + " " +
                                       voluntariaRepositorio.consultarVoluntaria(m.idVoluntaria!.Value).Apellido,
                    nombreMovimiento = m.esEntrada == "S" ? "Entrada de insumos" : "Salida de insumos"
                }).ToList();

            return listaMovimientos;
        }

        public INSUMO consultarInsumo(int idInsumo)
        {
            var insumo = db.INSUMO
                .Include(i => i.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(i => i.idInsumo == idInsumo);
            if (insumo == null)
                throw new ApplicationException("Insumo con ese id inexistente");
            if (!EsInsumoVisibleLista(insumo))
                throw new ApplicationException("Insumo no disponible o dado de baja.");
            return insumo;
        }

        public INSUMO obtenerInsumoParaModificar(int idInsumo)
        {
            var insumo = db.INSUMO
                .Include(i => i.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(i => i.idInsumo == idInsumo);
            if (insumo == null)
                throw new ApplicationException("Insumo con ese id inexistente");
            if (insumo.Estado != null && insumo.Estado.ambito?.nombre == "Insumos" && insumo.Estado.nombre == "Eliminado")
                throw new ApplicationException("No se puede modificar un insumo dado de baja.");
            return insumo;
        }

        public bool modificarInsumo(INSUMO parcial, INSUMO existente)
        {
            existente.nombre = parcial.nombre;
            existente.descripcion = parcial.descripcion;
            existente.stockMaximo = parcial.stockMaximo;
            existente.stockMinimo = parcial.stockMinimo;
            existente.stockActual = parcial.stockActual;
            if (parcial.idEstado.HasValue)
                existente.idEstado = parcial.idEstado;
            db.SaveChanges();
            return true;
        }

        public bool eliminarInsumoLogico(int idInsumo)
        {
            var insumo = db.INSUMO.FirstOrDefault(i => i.idInsumo == idInsumo);
            if (insumo == null)
                throw new ApplicationException("Insumo con ese id inexistente");
            insumo.idEstado = estadoRepositorio.ObtenerIdEstadoEliminado("Insumos");
            db.SaveChanges();
            return true;
        }

        public bool actualizarStock(INSUMO insumo, int cantidad)
        {
            insumo.stockActual = insumo.stockActual - cantidad;
            db.SaveChanges();
            return true;
        }

        public bool registrarMovimientoStock(MOVIMIENTOSTOCK movimiento)
        {
            var insumo = consultarInsumo(movimiento.idInsumo);
            if (movimiento.esEntrada == "S" || movimiento.esEntrada == "s")
            {
                if (insumo.stockActual + movimiento.cantidad!.Value > insumo.stockMaximo)
                {
                    throw new ApplicationException("El ingreso de este insumo supera el stock maximo de: " + insumo.stockMaximo);
                }
                insumo.stockActual = insumo.stockActual + movimiento.cantidad.Value;
            }
            else
            {
                if (insumo.stockActual < movimiento.cantidad)
                {
                    throw new ApplicationException("La cantidad de salida de este insumo supera el stock disponible de: " + insumo.stockActual);
                }
                insumo.stockActual = insumo.stockActual - movimiento.cantidad!.Value;
            }
            movimiento.fechaMovimiento = NegConversorFecha.ObtenerFechaArgentina();
            db.MOVIMIENTOSTOCK.Add(movimiento);
            db.SaveChanges();
            return true;
        }

        public List<EstadisticaInsumo> devolverEstadisticas()
        {
            var resultado = db.DETALLEASIGNACION
                .Include(d => d.insumo!)
                .ThenInclude(i => i!.Estado!)
                .ThenInclude(e => e!.ambito)
                .Where(d =>
                    d.insumo != null
                    && d.insumo.idEstado != null
                    && d.insumo.Estado != null
                    && d.insumo.Estado.ambito != null
                    && d.insumo.Estado.ambito.nombre == "Insumos"
                    && d.insumo.Estado.nombre != "Eliminado")
                .GroupBy(detalle => detalle.insumo!.nombre)
                .Select(grupo => new EstadisticaInsumo() { nombreInsumo = grupo.Key, cantidad = grupo.Sum(detalle => detalle.cantidad) })
                .ToList();
            return resultado;
        }

        /// <summary>Indica si un idEstado existe y pertenece al ámbito Insumos (cualquier nombre de estado).</summary>
        public bool IdEstadoEsDelAmbitoInsumos(int idEstado)
        {
            return db.ESTADO.AsNoTracking()
                .Include(e => e.ambito)
                .Any(e => e.idEstado == idEstado && e.ambito.nombre == "Insumos");
        }

        /// <summary>Evita usar estado Eliminado en altas/modificaciones vía payload.</summary>
        public bool IdEstadoEsEliminadoInsumos(int idEstado)
        {
            return db.ESTADO.AsNoTracking()
                .Include(e => e.ambito)
                .Any(e =>
                    e.idEstado == idEstado && e.ambito.nombre == "Insumos" && e.nombre == "Eliminado");
        }
    }
}
