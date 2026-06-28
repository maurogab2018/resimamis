using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegMadres
{
    List<MADRE> listarMadres();
    List<EstadisticaLocalidades> devolverEstadisticasLocalidades();
    ResultadoValidacion registrarMadre(MADRE madre);
    MADRE consultarMadre(int id);
    ResultadoValidacion modificarMadre(MADRE madre, int Id, out MADRE? madreActualizada);
    bool eliminarMadre(int idMadre);
    List<EstadisticaEdadesMadres> devolverEstadisticasEdadesMadres();
}
