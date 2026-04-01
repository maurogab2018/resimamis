using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio.MaquinasEstado
{
    public abstract class EstadoBebe
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public EstadoBebe(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public abstract void Abrazar(BEBE bebe);
        public abstract void SinAbrazar(BEBE bebe);
        public abstract void SiendoAbrazado(BEBE bebe);
    }

}
