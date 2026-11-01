using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;

namespace ResimamisBackend.Negocio.Interfaces;

public interface INegRegistroActividad
{
    /// <summary>Anota un alta o baja con el usuario logueado como responsable. Nunca tira: la operación principal ya se hizo.</summary>
    void Registrar(string tipoMovimiento, string tipoRegistro, int idRegistro, string descripcion);
    /// <summary>Igual que <see cref="Registrar"/> para varios registros, en un solo guardado.</summary>
    void RegistrarVarios(string tipoMovimiento, string tipoRegistro, IEnumerable<(int IdRegistro, string Descripcion)> registros);
    List<REGISTROACTIVIDAD> ListarPorRangoUtc(DateTime inicioUtc, DateTime finUtcExclusivo);
    HistorialBajasRespuesta ListarHistorialBajas(string? tipoRegistro);
    CierresAutomaticosRespuesta ListarCierresAutomaticos(DateTime fechaInicio, DateTime fechaFin);
}
