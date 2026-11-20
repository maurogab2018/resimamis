using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResimamisBackend.Datos;

namespace ResimamisBackend.Tests.Infra;

/// <summary>
/// Crea un <see cref="ApplicationDbContext"/> respaldado por el proveedor InMemory de EF Core.
/// Sirve para los <c>Neg*</c> que reciben el <c>db</c> directo. Dos detalles:
/// - cada contexto usa un nombre de base único, así los tests no comparten estado;
/// - InMemory no soporta transacciones; se ignora el <see cref="InMemoryEventId.TransactionIgnoredWarning"/>
///   para que <c>BloqueosDb.Ejecutar</c> (que abre una transacción) no tire al correr.
/// </summary>
public static class TestDb
{
    public static ApplicationDbContext Crear()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
