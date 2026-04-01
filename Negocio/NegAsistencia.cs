using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class NegAsistencia
    {
        public readonly AsistenciaRepositorio repositorioAsistencia;
        public readonly VoluntariaRepositorio repositorioVoluntaria;
        public readonly HorarioRepositorio repositorioHorario;
        public NegAsistencia()
        {
            repositorioAsistencia = new AsistenciaRepositorio();
            repositorioVoluntaria = new VoluntariaRepositorio();
            repositorioHorario=new HorarioRepositorio();
        }

        public bool registrarAsistencia(int idVoluntaria )
        {
            var asistencia = new ASISTENCIA();
            var existeVoluntaria = repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
            //var existeHorario = repositorioHorario.consultarHorario(asistencia.IdHorario.Value);
            //asistencia.IdHorario = existeHorario.IdHorario;
            asistencia.IdVoluntaria = existeVoluntaria.IdVoluntaria;
            asistencia.FechaHoraIngreso=NegConversorFecha.ObtenerFechaArgentina();
            asistencia.FechaHoraSalida = null;
            return repositorioAsistencia.registrarAsistencia(asistencia);
        }
        public bool consultarAsistencia(int idVoluntaria)
        {
            return repositorioAsistencia.consultarAsistencia(idVoluntaria);
        }

        public List<ASISTENCIA> consultarAsistenciasVoluntaria(int idVoluntaria)
        {
            return repositorioAsistencia.consultarAsistenciasVoluntaria(idVoluntaria);
        }

        public List<ASISTENCIA> consultarAsistenciasFechahoy()
        {
            return repositorioAsistencia.consultarAsistenciasFechahoy();
        }
        public bool registrarAsistenciaSalida(int idVoluntaria)
        {
            var existeVoluntaria = repositorioVoluntaria.consultarVoluntaria(idVoluntaria);
            return repositorioAsistencia.registrarAsistenciaSalida(idVoluntaria);
        }
    }
}
