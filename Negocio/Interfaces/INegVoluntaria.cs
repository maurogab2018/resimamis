using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegVoluntaria
{
    List<VOLUNTARIA> listarVoluntarias();
    VoluntariaDetalle registrarVoluntaria(VOLUNTARIA voluntaria, List<HorarioVoluntaria>? horarios = null);
    bool eliminarVoluntaria(int id);
    VOLUNTARIA consultarVoluntaria(int id);
    VoluntariaDetalle consultarVoluntariaDetalle(int id);
    bool modificarVoluntaria(VOLUNTARIA voluntaria, int id);
    List<VOLUNTARIA> listarVoluntariasLibres();
    List<VOLUNTARIA> listarVoluntariasLibres1();
    List<ESTADO> devolverEstadosVoluntarias();
}
