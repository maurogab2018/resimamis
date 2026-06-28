using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Datos.Interfaces;

public interface IHorarioRepositorio
{
    bool registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria);
    bool reemplazarHorarios(int idVoluntaria, List<HorarioVoluntaria> nuevos);
    bool eliminarHorarioVoluntariaLogico(int idHorarioVoluntaria);
    HORARIO consultarHorario(int id);
    List<DIA> obtenerDias();
    List<HorarioVoluntariaRespuesta> obtenerHorariosPorVoluntaria(int idVoluntaria);
}
