using ResimamisBackend.Datos;

namespace ResimamisBackend.Datos.Interfaces;

public interface IBebeRepositorio
{
    List<BEBE> listarBebes();
    bool registrarBebe(BEBE bebe);
    BEBE consultarBebe(int id);
    bool modificarBebe(BEBE bebe, BEBE bebeModificar);
    bool eliminarBebeLogico(int idBebe);
    List<BEBE> obtenerBebesAbrazar();
    List<BEBE> obtenerBebesAbrazarPorIds(IEnumerable<int> idsBebes);
    bool cambioEstadoBebe(BEBE bebe, int idEstado);
    void asignarBebe(BEBE bebe);
}
