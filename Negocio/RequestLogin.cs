using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class RequestLogin
    {
        public int Dni { get; set; }
        public string Contrasena { get; set; }
    }
}
