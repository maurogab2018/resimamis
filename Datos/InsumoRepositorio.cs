using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class InsumoRepositorio : IInsumoRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly IVoluntariaRepositorio voluntariaRepositorio;
        private readonly IEstadoRepositorio estadoRepositorio;

        public InsumoRepositorio(ApplicationDbContext db, IVoluntariaRepositorio voluntariaRepositorio, IEstadoRepositorio estadoRepositorio)
        {
            this.db = db;
            this.voluntariaRepositorio = voluntariaRepositorio;
            this.estadoRepositorio = estadoRepositorio;
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

        public List<ConsultaMovimiento> obtenerMovimientos(RequestMovimiento? movimientoFiltro)
        {
            var query = db.MOVIMIENTOSTOCK.AsNoTracking().AsQueryable();
            if (movimientoFiltro?.fechaDesde != null)
                query = query.Where(m => m.fechaMovimiento >= movimientoFiltro.fechaDesde);
            if (movimientoFiltro?.fechaHasta != null)
                query = query.Where(m => m.fechaMovimiento <= movimientoFiltro.fechaHasta);

            var movimientos = query
                .OrderByDescending(m => m.fechaMovimiento)
                .ThenByDescending(m => m.idMovimiento)
                .ToList();

            return movimientos.Select(MapConsultaMovimiento).ToList();
        }

        private ConsultaMovimiento MapConsultaMovimiento(MOVIMIENTOSTOCK m)
        {
            var insumo = db.INSUMO.AsNoTracking().FirstOrDefault(i => i.idInsumo == m.idInsumo);
            var bebe = m.idBebe.HasValue
                ? db.BEBE.AsNoTracking().FirstOrDefault(b => b.ID == m.idBebe.Value)
                : null;
            var voluntaria = m.idVoluntaria.HasValue
                ? db.VOLUNTARIA.AsNoTracking().FirstOrDefault(v => v.IdVoluntaria == m.idVoluntaria.Value)
                : null;
            var proveedor = m.idProveedor.HasValue
                ? db.PROVEEDOR.AsNoTracking().FirstOrDefault(p => p.idProveedor == m.idProveedor.Value)
                : null;

            return new ConsultaMovimiento
            {
                idMovimiento = m.idMovimiento,
                idInsumo = m.idInsumo,
                nombreInsumo = insumo?.nombre ?? string.Empty,
                idBebe = m.idBebe,
                nombreBebe = bebe?.nombre,
                apellidoBebe = bebe?.apellido,
                idVoluntaria = m.idVoluntaria,
                nombreVoluntaria = voluntaria == null
                    ? string.Empty
                    : $"{voluntaria.Nombre} {voluntaria.Apellido}".Trim(),
                fechaMovimiento = m.fechaMovimiento,
                observacion = m.observacion ?? string.Empty,
                cantidad = m.cantidad,
                esEntrada = m.esEntrada ?? string.Empty,
                idProveedor = m.idProveedor,
                nombreProveedor = proveedor?.nombre ?? string.Empty,
                nombreMovimiento = m.esEntrada == "S" || m.esEntrada == "s"
                    ? "Entrada de insumos"
                    : "Salida de insumos"
            };
        }

        public DetalleMovimiento obtenerMovimientoPorId(int idMovimiento)
        {
            var movimiento = db.MOVIMIENTOSTOCK
                .AsNoTracking()
                .FirstOrDefault(m => m.idMovimiento == idMovimiento);
            if (movimiento == null)
                throw new NotFoundException("Movimiento no encontrado con ese id.");

            var insumo = db.INSUMO.AsNoTracking().FirstOrDefault(i => i.idInsumo == movimiento.idInsumo);
            var bebe = movimiento.idBebe.HasValue
                ? db.BEBE.AsNoTracking().FirstOrDefault(b => b.ID == movimiento.idBebe.Value)
                : null;
            var voluntaria = movimiento.idVoluntaria.HasValue
                ? db.VOLUNTARIA.AsNoTracking().FirstOrDefault(v => v.IdVoluntaria == movimiento.idVoluntaria.Value)
                : null;
            var proveedor = movimiento.idProveedor.HasValue
                ? db.PROVEEDOR.AsNoTracking().FirstOrDefault(p => p.idProveedor == movimiento.idProveedor.Value)
                : null;

            return new DetalleMovimiento
            {
                IdMovimiento = movimiento.idMovimiento,
                IdInsumo = movimiento.idInsumo,
                NombreInsumo = insumo?.nombre ?? string.Empty,
                IdBebe = movimiento.idBebe,
                NombreBebe = bebe?.nombre,
                ApellidoBebe = bebe?.apellido,
                IdVoluntaria = movimiento.idVoluntaria,
                NombreVoluntaria = voluntaria == null
                    ? null
                    : $"{voluntaria.Nombre} {voluntaria.Apellido}".Trim(),
                FechaMovimiento = movimiento.fechaMovimiento,
                Observacion = movimiento.observacion,
                Cantidad = movimiento.cantidad,
                EsEntrada = movimiento.esEntrada,
                IdProveedor = movimiento.idProveedor,
                NombreProveedor = proveedor?.nombre,
                NombreMovimiento = movimiento.esEntrada == "S" || movimiento.esEntrada == "s"
                    ? "Entrada de insumos"
                    : "Salida de insumos"
            };
        }

        public INSUMO consultarInsumo(int idInsumo)
        {
            var insumo = db.INSUMO
                .Include(i => i.Estado!)
                .ThenInclude(e => e!.ambito)
                .FirstOrDefault(i => i.idInsumo == idInsumo);
            if (insumo == null)
                throw new NotFoundException("Insumo no encontrado con ese id.");
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
                throw new NotFoundException("Insumo no encontrado con ese id.");
            if (insumo.Estado != null && insumo.Estado.ambito?.nombre == "Insumos" && insumo.Estado.nombre == "Eliminado")
                throw new ApplicationException("No se puede modificar un insumo dado de baja.");
            return insumo;
        }

        public INSUMO registrarInsumo(INSUMO insumo)
        {
            var nombreNormalizado = insumo.nombre.Trim();
            var duplicado = QueryInsumosVisiblesAmbitoInsumos()
                .Any(i => i.nombre.ToLower() == nombreNormalizado.ToLower());
            if (duplicado)
                throw new ApplicationException("Ya existe un insumo activo con ese nombre.");

            if (!insumo.idEstado.HasValue)
                insumo.idEstado = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Activo", "Insumos");

            insumo.nombre = nombreNormalizado;
            insumo.descripcion = string.IsNullOrWhiteSpace(insumo.descripcion)
                ? string.Empty
                : insumo.descripcion.Trim();
            db.INSUMO.Add(insumo);
            db.SaveChanges();
            return insumo;
        }

        public bool modificarInsumo(INSUMO parcial, INSUMO existente)
        {
            existente.nombre = parcial.nombre;
            existente.descripcion = string.IsNullOrWhiteSpace(parcial.descripcion)
                ? string.Empty
                : parcial.descripcion.Trim();
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
                throw new NotFoundException("Insumo no encontrado con ese id.");
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

        public List<INSUMO> obtenerInsumosBajoStockMinimo()
        {
            return QueryInsumosVisiblesAmbitoInsumos()
                .AsNoTracking()
                .Where(i => i.stockActual <= i.stockMinimo)
                .OrderBy(i => i.nombre)
                .ToList();
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
