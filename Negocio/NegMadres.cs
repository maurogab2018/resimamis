using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegMadres
    {
        public readonly MadreRepositorio repositorioMadre;
        public NegMadres()
        {
            repositorioMadre = new MadreRepositorio();
        }

        public List<MADRE> listarMadres()
        {
            return repositorioMadre.listarMadres();
        }

        public List<EstadisticaLocalidades> devolverEstadisticasLocalidades()
        {
            return repositorioMadre.devolverEstadisticasLocalidades();
        }

        public ResultadoValidacion registrarMadre(MADRE madre)
        {
            var resultado = new ResultadoValidacion();
            ValidarCamposMadre(madre, resultado);
            ValidarBebesAlta(madre, resultado);

            // Si hay errores, no seguimos
            if (!resultado.Exito)
                return resultado;

            // Si pasa validaciones, registrar
            bool registrado = repositorioMadre.registrarMadre(madre);

            if (!registrado)
                resultado.Errores.Add("Error al registrar la madre.");

            return resultado;
        }

        public MADRE consultarMadre(int id)
        {
            return repositorioMadre.consultarMadre(id);
        }

        /// <summary>
        /// Misma regla de fusión que <see cref="MadreRepositorio.modificarMadre"/> para validar el estado resultante.
        /// </summary>
        private static bool PropiedadMadreIgnorar(string name) =>
            name is nameof(MADRE.Bebe) or nameof(MADRE.EstadoDetalle);

        private static MADRE CombinarParcheMadre(MADRE existente, MADRE parcial)
        {
            var combinada = new MADRE();
            foreach (var prop in typeof(MADRE).GetProperties())
            {
                if (PropiedadMadreIgnorar(prop.Name) || !prop.CanWrite)
                    continue;
                prop.SetValue(combinada, prop.GetValue(existente));
            }

            foreach (var prop in typeof(MADRE).GetProperties())
            {
                if (PropiedadMadreIgnorar(prop.Name) || !prop.CanWrite)
                    continue;
                var nuevoValor = prop.GetValue(parcial);
                var valorActual = prop.GetValue(existente);
                if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                    prop.SetValue(combinada, nuevoValor);
            }

            return combinada;
        }

        private static bool PropiedadBebeIgnorar(string name) =>
            name is nameof(BEBE.Madre) or nameof(BEBE.Sala) or nameof(BEBE.Estado)
                or nameof(BEBE.Asignaciones) or nameof(BEBE.NombreSala);

        private static BEBE CombinarParcheBebe(BEBE existente, BEBE parcial)
        {
            var combinada = new BEBE();
            foreach (var prop in typeof(BEBE).GetProperties())
            {
                if (PropiedadBebeIgnorar(prop.Name) || !prop.CanWrite)
                    continue;
                prop.SetValue(combinada, prop.GetValue(existente));
            }

            foreach (var prop in typeof(BEBE).GetProperties())
            {
                if (PropiedadBebeIgnorar(prop.Name) || !prop.CanWrite)
                    continue;
                var nuevoValor = prop.GetValue(parcial);
                var valorActual = prop.GetValue(existente);
                if (nuevoValor != null && !nuevoValor.Equals(valorActual))
                    prop.SetValue(combinada, nuevoValor);
            }

            return combinada;
        }

        private static void ValidarBebesAlta(MADRE madre, ResultadoValidacion resultado)
        {
            if (madre.Bebe == null || madre.Bebe.Count == 0)
                return;
            for (var i = 0; i < madre.Bebe.Count; i++)
                NegBebes.ValidarCamposBebe(madre.Bebe[i], resultado, $"Bebé (índice {i + 1}): ");
        }

        private static void ValidarBebesEnModificacion(MADRE parcial, MADRE existente, ResultadoValidacion resultado)
        {
            if (parcial.Bebe == null || parcial.Bebe.Count == 0)
                return;
            var existentes = existente.Bebe ?? new List<BEBE>();
            for (var i = 0; i < parcial.Bebe.Count; i++)
            {
                var bebeParcial = parcial.Bebe[i];
                BEBE aValidar;
                if (bebeParcial.ID > 0)
                {
                    var ex = existentes.FirstOrDefault(b => b.ID == bebeParcial.ID);
                    aValidar = ex == null ? bebeParcial : CombinarParcheBebe(ex, bebeParcial);
                }
                else
                    aValidar = bebeParcial;

                NegBebes.ValidarCamposBebe(aValidar, resultado, $"Bebé (índice {i + 1}): ");
            }
        }

        private static void ValidarCamposMadre(MADRE madre, ResultadoValidacion resultado)
        {
            if (string.IsNullOrWhiteSpace(madre.Nombre))
                resultado.Errores.Add("Nombre es obligatorio.");
            else
            {
                if (!Regex.IsMatch(madre.Nombre, @"^[a-zA-Z]+$"))
                    resultado.Errores.Add("Nombre no permite caracteres especiales.");
                if (madre.Nombre.Length > 15)
                    resultado.Errores.Add("Nombre no permite más de 15 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(madre.Apellido))
                resultado.Errores.Add("Apellido es obligatorio.");
            else
            {
                if (!Regex.IsMatch(madre.Apellido, @"^[a-zA-Z]+$"))
                    resultado.Errores.Add("Apellido no permite caracteres especiales.");
                if (madre.Apellido.Length > 20)
                    resultado.Errores.Add("Apellido no permite más de 20 caracteres.");
            }

            if (madre.FechaNacimiento == default)
                resultado.Errores.Add("FechaNacimiento es obligatorio.");

            if (madre.Dni == 0)
                resultado.Errores.Add("Dni es obligatorio.");
            else if (!Regex.IsMatch(madre.Dni.ToString(), @"^\d{7,8}$"))
                resultado.Errores.Add("Dni tiene que tener entre 7 y 8 dígitos.");

            if (madre.Localidad <= 0)
                resultado.Errores.Add("Localidad es obligatorio.");
            else if (!ExisteLocalidadStatic(madre.Localidad))
                resultado.Errores.Add("Localidad no existente con ese ID.");

            if (madre.EstadoCivil <= 0)
                resultado.Errores.Add("EstadoCivil es obligatorio.");
            else if (!ExisteEstadoCivilStatic(madre.EstadoCivil))
                resultado.Errores.Add("EstadoCivil no existente con ese ID.");

            if (madre.CantidadHijos <= 0)
                resultado.Errores.Add("CantidadHijos es obligatorio.");
            else if (madre.CantidadHijos > 10)
                resultado.Errores.Add("CantidadHijos tiene que ser un número entre 1 y 10.");

            if (string.IsNullOrWhiteSpace(madre.MotivoAbrazo))
                resultado.Errores.Add("MotivoAbrazo es obligatorio.");
            else if (madre.MotivoAbrazo.Length > 100)
                resultado.Errores.Add("MotivoAbrazo no permite más de 100 caracteres.");

            var celularStr = madre.Celular.ToString();
            if (madre.Celular == 0)
                resultado.Errores.Add("Celular es obligatorio.");
            else if (!Regex.IsMatch(celularStr, @"^\d{10,13}$"))
                resultado.Errores.Add("Celular tiene que tener entre 10 y 13 dígitos.");
        }

        private static bool ExisteLocalidadStatic(int id) => id >= 1 && id <= 100;

        private static bool ExisteEstadoCivilStatic(int id) => id >= 1 && id <= 5;

        public ResultadoValidacion modificarMadre(MADRE madre, int Id, out MADRE? madreActualizada)
        {
            madreActualizada = null;
            var resultado = new ResultadoValidacion();
            var madreModificar = repositorioMadre.consultarMadre(Id);
            var aValidar = CombinarParcheMadre(madreModificar, madre);
            ValidarCamposMadre(aValidar, resultado);
            ValidarBebesEnModificacion(madre, madreModificar, resultado);

            if (!resultado.Exito)
                return resultado;

            madreActualizada = repositorioMadre.modificarMadre(madre, madreModificar);
            return resultado;
        }

        public bool eliminarMadre(int idMadre)
        {
            return repositorioMadre.eliminarMadreLogico(idMadre);
        }

        public List<EstadisticaEdadesMadres> devolverEstadisticasEdadesMadres()
        {
            return repositorioMadre.devolverEstadisticasEdadesMadres();
        }
    }
}
