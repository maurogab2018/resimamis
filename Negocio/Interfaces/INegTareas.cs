using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegTareas
{
    List<TAREA> listarTareas();
    List<TAREA> listarTareasDisponiblesParaAsignar();
    TAREA? consultarTarea(int idTarea);
    bool registrarTarea(TAREA tarea);
    bool modificarTarea(int idTarea, TAREA tarea);
    bool eliminarTarea(int idTarea);
    void ValidarTareaDisponibleParaAsignar(int idTarea);
}
