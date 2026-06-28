using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegSalas
{
    List<SALA> listarSalas();
    List<SALA> listarSalasActivas();
    SALA consultarSala(int idSala);
    bool registrarSala(SALA sala);
    bool modificarSala(int idSala, SALA sala);
    bool eliminarSala(int idSala);
    void ValidarSalaActivaParaBebe(int? idSala);
}
