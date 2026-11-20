using ResimamisBackend.Datos;

namespace ResimamisBackend.Tests.Infra;

public class TestDbSmokeTests
{
    [Fact]
    public void Crear_ConstruyeElModeloYPersiste()
    {
        using var db = TestDb.Crear();
        db.BEBE.Add(new BEBE { ID = 1, nombre = "Test" });
        db.SaveChanges();

        Assert.Single(db.BEBE);
    }
}
