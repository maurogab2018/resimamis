using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Datos.Interfaces;

public interface IUsuarioRepositorio
{
    USUARIO? ObtenerPorId(int idUsuario, bool asNoTracking = false);
    USUARIO? ObtenerPorDni(int dni, bool asNoTracking = false);
    bool ExisteOtroUsuarioConDni(int dni, int exceptIdUsuario);
    bool ExisteUsuarioOperativoConDni(int dni, int? exceptIdUsuario = null);
    bool VoluntariaTieneUsuarioActivo(int idVoluntaria, int? exceptIdUsuario = null);
    List<UsuarioListado> ListarUsuariosOperativos();
    void GuardarCambios();
    bool EliminarLogico(int idUsuario);
}
