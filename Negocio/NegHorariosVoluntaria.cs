using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio
{
    public class NegHorariosVoluntaria
    {
        public readonly HorarioRepositorio horarioRepositorio;
        public NegHorariosVoluntaria()
        {
            horarioRepositorio = new HorarioRepositorio();
        }

        public List<HorarioVoluntariaRespuesta> registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria)
        {
            if (horarioVoluntaria == null || horarioVoluntaria.Count == 0)
                throw new ApplicationException("Debe indicar al menos un horario.");

            horarioRepositorio.registrarHoraraioVoluntaria(horarioVoluntaria);
            return horarioVoluntaria
                .Select(h => h.IdVoluntaria)
                .Distinct()
                .SelectMany(id => horarioRepositorio.obtenerHorariosPorVoluntaria(id))
                .OrderBy(h => h.IdVoluntaria)
                .ThenBy(h => h.IdDia)
                .ThenBy(h => h.Turno)
                .ToList();
        }
        public List<DIA> obtenerDias()
        {
            return horarioRepositorio.obtenerDias();
        }

        public List<HorarioVoluntariaRespuesta> obtenerHorariosPorVoluntaria(int idVoluntaria)
        {
            if (idVoluntaria <= 0)
                throw new ApplicationException("Id de voluntaria inválido.");
            return horarioRepositorio.obtenerHorariosPorVoluntaria(idVoluntaria);
        }

    }
}
