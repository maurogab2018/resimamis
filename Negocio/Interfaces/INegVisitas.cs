using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegVisitas
{
    List<VisitaListado> listarVisitas();
    List<VisitaListado> listarVisitasPorBebe(int idBebe);
    VisitaListado consultarVisita(int idVisita);
    VISITA registrarVisita(VISITA visita);
    bool modificarVisita(int idVisita, VISITA visita);
    bool eliminarVisita(int idVisita);
}
