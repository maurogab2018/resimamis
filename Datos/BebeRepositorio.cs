using Microsoft.EntityFrameworkCore;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class BebeRepositorio
    {
        private readonly ApplicationDbContext db;
        private readonly MadreRepositorio madreRepositorio;
        public BebeRepositorio()
        {
            db = new ApplicationDbContext();
            madreRepositorio = new MadreRepositorio();
        }

        public List<BEBE> listarBebes() { 

            return db.BEBE.Include(b => b.Sala).ToList();
        }

        public List<SALA> listarSalas()
        {
            return db.SALA.ToList();
        }
        public bool registrarBebe(BEBE bebe) {
            var existeBebe = db.BEBE.FirstOrDefault(b => b.Dni == bebe.Dni);
            if(existeBebe!=null)
                throw new ApplicationException("Bebe existente con ese Dni");
            db.BEBE.Add(bebe);
            db.SaveChanges();
            return true;
        }
        public BEBE consultarBebe(int id)
        {
            var bebe = db.BEBE.FirstOrDefault(b => b.ID == id);
            if (bebe == null)
                throw new ApplicationException("Bebe no existente con ese Id");
            return bebe;
        }

        public bool modificarBebe(BEBE bebe ,BEBE bebeModificar)
        {
            var madreBebe = madreRepositorio.consultarMadre(bebe.IdMadre!.Value);
            bebeModificar.nombre = bebe.nombre;
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
            bebeModificar.IdMadre = madreBebe.IdMadre;
            db.SaveChangesAsync();
            return true;
        }

        public List<BEBE> obtenerBebesAbrazar()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();

            var bebesAbrazar = db.BEBE
                .Where(v => v.Estado != null
                            && v.Estado.ambito.nombre == "Bebes"
                            && v.Estado.nombre == "Sin abrazar"
                            && v.Estado.nombre != "Asignado"
                            && !v.Asignaciones.Any(a =>
                                a.fechaHoraAsignacion >= inicioDia && a.fechaHoraAsignacion < finDia
                                && a.fechaHoraInicio != null
                                && a.fechaHoraInicio >= inicioDia && a.fechaHoraInicio < finDia))
                .ToList();
            if(bebesAbrazar.Count==0)
                throw new ApplicationException("No hay bebes para abrazar para el día de hoy");
            return bebesAbrazar;
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
