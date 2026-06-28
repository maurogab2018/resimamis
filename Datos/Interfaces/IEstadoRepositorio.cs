namespace ResimamisBackend.Datos.Interfaces;

public interface IEstadoRepositorio
{
    int ObtenerIdEstadoEliminado(string nombreAmbito);
    int ObtenerIdEstadoPorNombreYAmbito(string nombreEstado, string nombreAmbito);
    int ObtenerIdEstadoPorNombresYAmbito(string nombreAmbito, params string[] nombresEstado);
    int ObtenerIdVoluntariaDisponible();
    int ObtenerIdVoluntariaAbrazando();
    int ObtenerIdVoluntariaEnTarea();
    int ObtenerIdBebeSinAbrazar();
    int ObtenerIdBebeAbrazado();
    int ObtenerIdEstadoAsignacionIniciado();
    int ObtenerIdEstadoAsignacionFinalizado();
}
