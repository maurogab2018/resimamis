using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class InsumoRepositorio
    {
        private readonly ApplicationDbContext db;
        private VoluntariaRepositorio voluntariaRepositorio;
        public InsumoRepositorio()
        {
            voluntariaRepositorio = new VoluntariaRepositorio();
            db = new ApplicationDbContext();
        }


        public List<INSUMO> obtenerInsumos()
        {
            return db.INSUMO.ToList();
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
            var insumo = db.INSUMO.FirstOrDefault(i => i.idInsumo == idInsumo);
            if (insumo == null)
                throw new ApplicationException("Insumo con ese id inexistente");
            return insumo;
        }

        public bool actualizarStock(INSUMO insumo,int cantidad)
        {
            insumo.stockActual = insumo.stockActual - cantidad;
            db.SaveChangesAsync();
            return true;
        }
        public bool registrarMovimientoStock(MOVIMIENTOSTOCK movimiento)
        {
            var insumo = consultarInsumo(movimiento.idInsumo);
            if (movimiento.esEntrada=="S" || movimiento.esEntrada == "s")
            { 
                if(insumo.stockActual + movimiento.cantidad!.Value > insumo.stockMaximo)
                {
                    throw new ApplicationException("El ingreso de este insumo supera el stock maximo de: " + insumo.stockMaximo);
                }
                insumo.stockActual = insumo.stockActual + movimiento.cantidad.Value;
            }
            else
            {
                if(insumo.stockActual < movimiento.cantidad)
                {
                    throw new ApplicationException("La cantidad de salida de este insumo supera el stock disponible de: " + insumo.stockActual);
                }
                insumo.stockActual = insumo.stockActual - movimiento.cantidad!.Value;
            }
            movimiento.fechaMovimiento = NegConversorFecha.ObtenerFechaArgentina();
            db.MOVIMIENTOSTOCK.Add(movimiento);
            db.SaveChangesAsync();
            return true;
        }

        public List<EstadisticaInsumo> devolverEstadisticas()
        {
            var resultado = db.DETALLEASIGNACION
                .GroupBy(detalle => detalle.insumo.nombre)
                .Select(grupo => new EstadisticaInsumo() { nombreInsumo = grupo.Key, cantidad = grupo.Sum(detalle => detalle.cantidad) })
                .ToList();
            return resultado;
        }
    }
}
