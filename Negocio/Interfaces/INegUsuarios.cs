using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegUsuarios
{
    bool EsCoordinadoraPorDni(int dni);
    void ValidarCoordinadora(int dniSolicitante);
    RespuestaLogin Loguear(RequestLogin usuario);
    bool RegistrarUsuario(int dniSolicitante, USUARIO usuario);
    List<UsuarioListado> ListarUsuarios(int dniSolicitante);
    List<VOLUNTARIA> ListarVoluntariasSinUsuario(int dniSolicitante);
    object ConsultarUsuarioPorId(int idUsuario, int dniSolicitante);
    bool CambiarContrasena(int dniSolicitante, RequestCambiarContrasena datos);
    bool ModificarUsuario(int dniSolicitante, int idUsuario, USUARIO datos);
    bool EliminarUsuario(int dniSolicitante, int idUsuario);
}
