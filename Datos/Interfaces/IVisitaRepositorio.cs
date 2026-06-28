using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IVisitaRepositorio
{
    List<VisitaListado> listarVisitas();
    List<VisitaListado> listarVisitasPorBebe(int idBebe);
    VISITA? obtenerPorId(int idVisita);
    VISITA obtenerParaModificar(int idVisita);
    bool registrarVisita(VISITA visita);
    bool modificarVisita(VISITA datos, VISITA existente);
    bool eliminarVisitaLogica(int idVisita);
}
