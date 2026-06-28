using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegHorariosVoluntaria
{
    List<HorarioVoluntariaRespuesta> registrarHoraraioVoluntaria(List<HorarioVoluntaria> horarioVoluntaria);
    List<HorarioVoluntariaRespuesta> reemplazarHorarios(int idVoluntaria, List<HorarioVoluntaria> horarios);
    List<DIA> obtenerDias();
    List<HorarioVoluntariaRespuesta> obtenerHorariosPorVoluntaria(int idVoluntaria);
    bool eliminarHorarioVoluntaria(int idHorarioVoluntaria);
}
