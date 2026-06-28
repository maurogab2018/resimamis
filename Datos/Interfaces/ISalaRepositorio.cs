namespace ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Datos;

public interface ISalaRepositorio
{
    List<SALA> listarSalas();
    List<SALA> listarSalasActivas();
    SALA? obtenerPorId(int idSala);
    SALA obtenerParaModificar(int idSala);
    bool registrarSala(SALA sala);
    bool modificarSala(SALA datos, SALA existente);
    bool eliminarSalaLogica(int idSala);
    bool existeOtraSalaConNombre(string nombre, int? exceptIdSala = null);
    bool salaActiva(int idSala);
    bool tieneBebesActivosAsignados(int idSala);
}
