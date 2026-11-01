using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IMadreRepositorio
{
    List<MADRE> listarMadres();
    bool registrarMadre(MADRE madre);
    MADRE consultarMadre(int Dni);
    MADRE? consultarMadrePorDni(int dni);
    bool estaDadaDeBaja(MADRE madre);
    MADRE modificarMadre(MADRE madre, MADRE madreModificar);
    List<EstadisticaLocalidades> devolverEstadisticasLocalidades();
    List<EstadisticaEdadesMadres> devolverEstadisticasEdadesMadres();
    bool eliminarMadreLogico(int idMadre);
}
