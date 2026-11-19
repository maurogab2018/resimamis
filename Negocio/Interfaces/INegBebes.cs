using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegBebes
{
    void ValidarCamposBebe(BEBE bebe, ResultadoValidacion resultado, string prefijo = "");
    List<BEBE> listarBebes();
    List<SALA> listarSalas();
    bool registrarBebe(BEBE bebe);
    BEBE consultarBebe(int id);
    bool modificarBebe(BEBE bebe);
    bool eliminarBebe(int idBebe);
    List<BEBE> listarBebesAbrazar();
    List<ESTADO> listarEstadosBebes();
    BEBE actualizarEstadoBebe(int idBebe, int idEstado);
    /// <summary>Mueve el bebé a otra sala activa (panel de salas). No toca ningún otro dato.</summary>
    bool cambiarSala(int idBebe, int idSala);
}
