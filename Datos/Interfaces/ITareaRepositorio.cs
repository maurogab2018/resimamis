using ResimamisBackend.Datos;

namespace ResimamisBackend.Datos.Interfaces;

public interface ITareaRepositorio
{
    List<TAREA> listarTareas();
    List<TAREA> listarTareasActivas();
    List<TAREA> listarTareasDisponiblesParaAsignar();
    bool tareaUnicaOcupadaHoy(int idTarea);
    TAREA? obtenerPorId(int idTarea);
    TAREA obtenerParaModificar(int idTarea);
    bool registrarTarea(TAREA tarea);
    bool modificarTarea(TAREA datos, TAREA existente);
    bool eliminarTareaLogica(int idTarea);
    bool existeOtraTareaConNombre(string nombre, int? exceptIdTarea = null);
}
