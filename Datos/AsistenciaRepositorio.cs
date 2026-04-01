using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos
{
    public class AsistenciaRepositorio
    {
        private readonly ApplicationDbContext db;
        public AsistenciaRepositorio()
        {
            db= new ApplicationDbContext();
        }

        public bool registrarAsistencia(ASISTENCIA asistencia)
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var yaExisteAsistencia = db.ASISTENCIA.FirstOrDefault(a =>a.FechaHoraIngreso!=null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.IdVoluntaria == asistencia.IdVoluntaria);
            if (yaExisteAsistencia != null)
                return false;
            var nuevaAsistencia = new ASISTENCIA()
            {
                IdVoluntaria = asistencia.IdVoluntaria,
                FechaHoraIngreso = asistencia.FechaHoraIngreso,
            };
            db.ASISTENCIA.Add(nuevaAsistencia);
            db.SaveChangesAsync();
            return true;
        }

        public bool consultarAsistencia(int idVoluntaria)
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asistenciaHoy = db.ASISTENCIA.FirstOrDefault(a => a.FechaHoraIngreso!=null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.IdVoluntaria==idVoluntaria);
            if (asistenciaHoy == null)
                return false;
            else
                return true;
        }

        public bool registrarAsistenciaSalida(int idVoluntaria)
        {
            var fechaHoy = NegConversorFecha.ObtenerFechaArgentina();
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var asistenciaHoy = db.ASISTENCIA.FirstOrDefault(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia && a.IdVoluntaria == idVoluntaria && a.FechaHoraSalida==null);
            if (asistenciaHoy == null)
                throw new Exception("No existe un registro de asistencia para hoy o ya fue registrado");
            asistenciaHoy.FechaHoraSalida = fechaHoy;
            db.SaveChangesAsync();
            return true;
        }

        public List<ASISTENCIA> consultarAsistenciasFechahoy()
        {
            var (inicioDia, finDia) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var listaAsistencias = db.ASISTENCIA.Where(a => a.FechaHoraIngreso != null && a.FechaHoraIngreso >= inicioDia && a.FechaHoraIngreso < finDia).Select(v=> new ASISTENCIA()
            {
                FechaHoraIngreso = v.FechaHoraIngreso,
                FechaHoraSalida=v.FechaHoraSalida!=null ? v.FechaHoraSalida :v.FechaHoraSalida,
                IdVoluntaria = v.IdVoluntaria,
                IdHorario= v.IdHorario,
                Voluntaria = v.Voluntaria,
            }).ToList();
            if(listaAsistencias.Count==0)
                throw new Exception("No existes asistencias para la fecha");
            return listaAsistencias;
        }

        public List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria)
        {
            var listaAsistencias = db.ASISTENCIA.Where(a => a.IdVoluntaria==idVoluntaria).ToList();
            if (listaAsistencias.Count == 0)
                throw new Exception("No existes asistencias para esa voluntaria");
            return listaAsistencias;
        }

    }
}
