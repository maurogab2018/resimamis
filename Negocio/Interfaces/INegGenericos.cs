using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegGenericos
{
    List<LOCALIDAD> obtenerLocalidades();
    List<EstadoCivilItem> obtenerEstadosCiviles();
}
