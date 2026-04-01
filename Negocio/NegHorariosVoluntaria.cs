using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class NegHorariosVoluntaria
    {
        public readonly HorarioRepositorio horarioRepositorio;
        public NegHorariosVoluntaria()
        {
            horarioRepositorio = new HorarioRepositorio();
        }

        public bool registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria)
        {
            return horarioRepositorio.registrarHoraraioVoluntaria(horarioVoluntaria);
        }
        public List<DIA> obtenerDias()
        {
            return horarioRepositorio.obtenerDias();
        }

    }
}
