using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio
{
    public class RespuestaLogin
    {
        public string Resultado { get; set; }

        public string Token { get; set; }

        public VOLUNTARIA Voluntaria { get; set; }
    }
}
