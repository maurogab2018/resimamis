namespace ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Datos;

public interface IGenericosRepositorio
{
    List<LOCALIDAD> obtenerLocalidades();
    bool existeLocalidad(int id);
}
