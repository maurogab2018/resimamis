using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class BebeRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly MadreRepositorio madreRepositorio;
        private readonly EstadoRepositorio estadoRepositorio;

        public BebeRepositorio()
        {
            db = new ApplicationDbContext();
            madreRepositorio = new MadreRepositorio();
            estadoRepositorio = new EstadoRepositorio();
        }

        private int? IdEstadoEliminadoBebes()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Bebes")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();
        }

        private int? IdEstadoEliminadoAsignaciones()
        {
            return db.ESTADO
                .AsNoTracking()
                .Include(e => e.ambito)
                .Where(e => e.nombre == "Eliminado" && e.ambito.nombre == "Asignaciones")
                .Select(e => (int?)e.idEstado)
                .FirstOrDefault();
        }

        public List<BEBE> listarBebes()
        {
            var idEl = IdEstadoEliminadoBebes();
            var q = db.BEBE
                .AsNoTracking()
                .Include(b => b.Sala)
                .Include(b => b.Madre)
                .Include(b => b.LocalidadDetalle)
                .Include(b => b.Estado!).ThenInclude(e => e!.ambito)
                .AsQueryable();
            if (idEl != null)
                q = q.Where(b => b.IdEstado != idEl);
            return q.ToList();
        }

        public List<SALA> listarSalas()
        {
            return db.SALA.ToList();
        }
        public bool registrarBebe(BEBE bebe) {
            if (bebe.Dni.HasValue)
            {
                var existeBebe = db.BEBE.FirstOrDefault(b => b.Dni == bebe.Dni);
                if (existeBebe != null)
                    throw new ApplicationException("Bebe existente con ese Dni");
            }
            bebe.IdEstado = estadoRepositorio.ObtenerIdEstadoPorNombreYAmbito("Sin abrazar", "Bebes");
            db.BEBE.Add(bebe);
            db.SaveChanges();
            return true;
        }
        public BEBE consultarBebe(int id)
        {
            var bebe = db.BEBE
                .Include(b => b.Sala)
                .Include(b => b.LocalidadDetalle)
                .Include(b => b.Estado!).ThenInclude(e => e!.ambito)
                .Include(b => b.Madre)
                .FirstOrDefault(b => b.ID == id);
            if (bebe == null)
                throw new NotFoundException("Bebé no encontrado con ese Id.");
            return bebe;
        }

        public bool modificarBebe(BEBE bebe ,BEBE bebeModificar)
        {
            var madreBebe = madreRepositorio.consultarMadre(bebe.IdMadre!.Value);
            bebeModificar.nombre = bebe.nombre;
            bebeModificar.apellido = bebe.apellido;
            bebeModificar.Sexo = bebe.Sexo;
            bebeModificar.LugarNacimiento = bebe.LugarNacimiento;
            bebeModificar.FechaNacimiento = bebe.FechaNacimiento;
            bebeModificar.FechaIngresoNEO = bebe.FechaIngresoNEO;
            bebeModificar.PesoNacimiento = bebe.PesoNacimiento;
            bebeModificar.PesoAlta = bebe.PesoAlta;
            bebeModificar.PesoIngresoNEO = bebe.PesoIngresoNEO;
            bebeModificar.PesoDiaAbrazos = bebe.PesoDiaAbrazos;
            bebeModificar.DiagnosticoEgreso = bebe.DiagnosticoEgreso;
            bebeModificar.DiagnosticoIngreso = bebe.DiagnosticoIngreso;
            bebeModificar.IdSala = bebe.IdSala;
            bebeModificar.IdLocalidad = bebe.IdLocalidad;
            bebeModificar.IdMadre = madreBebe.IdMadre;
            db.SaveChanges();
            return true;
        }

        public bool eliminarBebeLogico(int idBebe)
        {
            var bebe = db.BEBE.FirstOrDefault(b => b.ID == idBebe);
            if (bebe == null)
                throw new NotFoundException("Bebé no encontrado con ese Id.");

            var idEliminado = estadoRepositorio.ObtenerIdEstadoEliminado("Bebes");
            bebe.IdEstado = idEliminado;
            db.SaveChanges();
            return true;
        }

        public List<BEBE> obtenerBebesAbrazar()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            var idElimBebe = IdEstadoEliminadoBebes();
            var idElimAsig = IdEstadoEliminadoAsignaciones();
            return db.BEBE
                .AsNoTracking()
                .Include(b => b.Estado!)
                .ThenInclude(e => e!.ambito)
                .Include(b => b.Sala)
                .Include(b => b.Madre)
                .Include(b => b.LocalidadDetalle)
                .Where(v => v.Estado != null
                            && (idElimBebe == null || v.IdEstado != idElimBebe)
                            && v.Estado.ambito.nombre == "Bebes"
                            && v.Estado.nombre == "Sin abrazar"
                            && !v.Asignaciones.Any(a =>
                                a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia
                                && a.fechaHoraInicio != null
                                && a.fechaHoraInicio >= inicioDia && a.fechaHoraInicio < finDia
                                && (idElimAsig == null || a.idEstado == null || a.idEstado != idElimAsig)))
                .OrderBy(b => b.nombre)
                .ToList();
        }
        public bool cambioEstadoBebe(BEBE bebe, int idEstado )
        {
            bebe.IdEstado=idEstado;
            db.SaveChanges();
            return true;
        }

        public void asignarBebe(BEBE bebe)
        {
            if (bebe != null)
            {
                var estadoAsignado = db.ESTADO.FirstOrDefault(e => e.ambito.nombre == "Bebes" && e.nombre == "Asignado");
                if (estadoAsignado == null)
                    throw new ApplicationException("Estado asignado inexistente");

                if (bebe.IdEstado != estadoAsignado.idEstado)
                {
                    bebe.IdEstado = estadoAsignado.idEstado;
                    db.SaveChanges();
                }
            }

        }
    }
}
