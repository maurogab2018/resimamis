using ResimamisBackend.Datos;

namespace ResimamisBackend.Negocio.MaquinasEstado
{
    public class Abrazado : EstadoBebe
    {
        public Abrazado() : base("Abrazado", "El bebé se encuentra en estado abrazado")
        {
        }

        public override void Abrazar(BEBE bebe)
        {
            //buscar estado con ambito bebe y asignarlo al bebe
            //bebe.
        }

        public override void SinAbrazar(BEBE bebe)
        {
            Console.WriteLine("El bebé sigue feliz incluso sin ser abrazado.");
        }
        public override void SiendoAbrazado(BEBE bebe)
        {
            Console.WriteLine("El bebé sigue feliz incluso sin ser abrazado.");
        }
    }
}
