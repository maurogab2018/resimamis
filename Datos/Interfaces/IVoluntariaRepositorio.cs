using ResimamisBackend.Datos;

namespace ResimamisBackend.Datos.Interfaces;

public interface IVoluntariaRepositorio
{
    List<VOLUNTARIA> listarVoluntarias();
    bool registrarVoluntaria(VOLUNTARIA Voluntaria);
    bool cambioEstadoVoluntaria(VOLUNTARIA Voluntaria);
    VOLUNTARIA consultarVoluntaria(int Dni);
    List<VOLUNTARIA> consultarVoluntarias(List<int> idVoluntarias);
    bool eliminarVoluntaria(int dni);
    bool modificarVoluntaria(VOLUNTARIA voluntaria, VOLUNTARIA voluntariaModificar);
    List<VOLUNTARIA> obtenerVoluntariasLibres();
    List<VOLUNTARIA> obtenerVoluntariasLibres1();
    VOLUNTARIA? asignarVoluntaria(int idVoluntaria);
    List<ESTADO> devolverEstadosVoluntarias();
    List<VOLUNTARIA> listarVoluntariasSinUsuario();
    bool existeOtraVoluntariaConDni(int dni, int? exceptIdVoluntaria = null);
    bool existeOtraVoluntariaConMail(string mail, int? exceptIdVoluntaria = null);
}
