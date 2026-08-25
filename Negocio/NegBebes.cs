using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;
using System.Text.RegularExpressions;

namespace ResimamisBackend.Negocio
{
    public class NegBebes : INegBebes
    {
        private readonly IBebeRepositorio repositorioBebe;
        private readonly IMadreRepositorio madreRepositorio;
        private readonly INegSalas negSalas;
        private readonly IGenericosRepositorio genericosRepositorio;

        public NegBebes(
            IBebeRepositorio repositorioBebe,
            IMadreRepositorio madreRepositorio,
            INegSalas negSalas,
            IGenericosRepositorio genericosRepositorio)
        {
            this.repositorioBebe = repositorioBebe;
            this.madreRepositorio = madreRepositorio;
            this.negSalas = negSalas;
            this.genericosRepositorio = genericosRepositorio;
        }

        /// <summary>Reglas alineadas con modificar/registrar: nombre, apellido, sexo, dni opcional, fecha nacimiento.</summary>
        public void ValidarCamposBebe(BEBE bebe, ResultadoValidacion resultado, string prefijo = "")
        {
            if (bebe == null)
            {
                resultado.Errores.Add(prefijo + "Bebé inválido.");
                return;
            }

            bebe.nombre = ValidacionTextoPersona.Normalizar(bebe.nombre) ?? bebe.nombre;
            bebe.apellido = ValidacionTextoPersona.Normalizar(bebe.apellido) ?? bebe.apellido;

            if (string.IsNullOrWhiteSpace(bebe.nombre))
                resultado.Errores.Add(prefijo + "Nombre es obligatorio.");
            else
            {
                if (!ValidacionTextoPersona.EsNombreApellidoValido(bebe.nombre))
                    resultado.Errores.Add(prefijo + "Nombre solo permite letras, espacios y tildes.");
                else if (bebe.nombre.Trim().Length > 50)
                    resultado.Errores.Add(prefijo + "Nombre no permite más de 50 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(bebe.apellido))
                resultado.Errores.Add(prefijo + "Apellido es obligatorio.");
            else
            {
                if (!ValidacionTextoPersona.EsNombreApellidoValido(bebe.apellido))
                    resultado.Errores.Add(prefijo + "Apellido solo permite letras, espacios y tildes.");
                else if (bebe.apellido.Trim().Length > 50)
                    resultado.Errores.Add(prefijo + "Apellido no permite más de 50 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(bebe.Sexo))
                resultado.Errores.Add(prefijo + "Sexo es obligatorio.");
            else if (!Regex.IsMatch(bebe.Sexo.Trim(), @"^(M|F|O|m|f|o)$"))
                resultado.Errores.Add(prefijo + "Sexo debe ser M, F u O.");

            if (bebe.Dni.HasValue && bebe.Dni.Value > 0)
            {
                if (!Regex.IsMatch(bebe.Dni.Value.ToString(), @"^\d{7,8}$"))
                    resultado.Errores.Add(prefijo + "Dni tiene que tener entre 7 y 8 dígitos.");
            }

            if (bebe.FechaNacimiento == null || bebe.FechaNacimiento == default)
                resultado.Errores.Add(prefijo + "FechaNacimiento es obligatorio.");
            else if (bebe.FechaNacimiento.Value.Date > DateTime.UtcNow.Date)
                resultado.Errores.Add(prefijo + "FechaNacimiento no puede ser futura.");

            if (bebe.FechaIngresoNEO.HasValue
                && bebe.FechaNacimiento.HasValue
                && bebe.FechaIngresoNEO.Value.Date < bebe.FechaNacimiento.Value.Date)
            {
                resultado.Errores.Add(prefijo + "FechaIngresoNEO no puede ser anterior a FechaNacimiento.");
            }

            if (bebe.FechaSalida.HasValue
                && bebe.FechaIngresoNEO.HasValue
                && bebe.FechaSalida.Value.Date < bebe.FechaIngresoNEO.Value.Date)
            {
                resultado.Errores.Add(prefijo + "FechaSalida no puede ser anterior a FechaIngresoNEO.");
            }

            ValidarPesoOpcional(bebe.PesoNacimiento, prefijo + "PesoNacimiento", resultado);
            ValidarPesoOpcional(bebe.PesoIngresoNEO, prefijo + "PesoIngresoNEO", resultado);
            ValidarPesoOpcional(bebe.PesoDiaAbrazos, prefijo + "PesoDiaAbrazos", resultado);
            ValidarPesoOpcional(bebe.PesoAlta, prefijo + "PesoAlta", resultado);

            if (!string.IsNullOrWhiteSpace(bebe.LugarNacimiento) && bebe.LugarNacimiento.Trim().Length > 100)
                resultado.Errores.Add(prefijo + "LugarNacimiento no permite más de 100 caracteres.");
            if (!string.IsNullOrWhiteSpace(bebe.DiagnosticoIngreso) && bebe.DiagnosticoIngreso.Trim().Length > 500)
                resultado.Errores.Add(prefijo + "DiagnosticoIngreso no permite más de 500 caracteres.");
            if (!string.IsNullOrWhiteSpace(bebe.DiagnosticoEgreso) && bebe.DiagnosticoEgreso.Trim().Length > 500)
                resultado.Errores.Add(prefijo + "DiagnosticoEgreso no permite más de 500 caracteres.");

            if (bebe.IdMadre.HasValue && bebe.IdMadre.Value <= 0)
                bebe.IdMadre = null;

            if (bebe.IdLocalidad.HasValue)
            {
                if (bebe.IdLocalidad.Value <= 0)
                    bebe.IdLocalidad = null;
                else if (!genericosRepositorio.existeLocalidad(bebe.IdLocalidad.Value))
                    resultado.Errores.Add(prefijo + "Localidad no existente con ese ID.");
            }

            if (bebe.IdSala.HasValue)
            {
                if (bebe.IdSala.Value <= 0)
                    bebe.IdSala = null;
                else
                {
                    try
                    {
                        negSalas.ValidarSalaActivaParaBebe(bebe.IdSala);
                    }
                    catch (NotFoundException)
                    {
                        resultado.Errores.Add(prefijo + "Sala no existente con ese ID.");
                    }
                    catch (ApplicationException ex)
                    {
                        resultado.Errores.Add(prefijo + ex.Message);
                    }
                }
            }
        }

        private static void ValidarPesoOpcional(decimal? peso, string campo, ResultadoValidacion resultado)
        {
            if (peso.HasValue && peso.Value < 0)
                resultado.Errores.Add(campo + " no puede ser negativo.");
        }

        private void AsegurarMadreSiIndica(int? idMadre)
        {
            if (!idMadre.HasValue || idMadre.Value <= 0)
                return;
            madreRepositorio.consultarMadre(idMadre.Value);
        }

        public List<BEBE> listarBebes()
        {
            return repositorioBebe.listarBebes();
        }

        public List<SALA> listarSalas()
        {
            return negSalas.listarSalasActivas();
        }

        public bool registrarBebe(BEBE bebe)
        {
            var resultado = new ResultadoValidacion();
            ValidarCamposBebe(bebe, resultado);
            if (!resultado.Exito)
                throw new ApplicationException(string.Join(" ", resultado.Errores));

            AsegurarMadreSiIndica(bebe.IdMadre);
            return repositorioBebe.registrarBebe(bebe);
        }

        public BEBE consultarBebe(int id)
        {
            return repositorioBebe.consultarBebe(id);
        }

        public bool modificarBebe(BEBE bebe)
        {
            var resultado = new ResultadoValidacion();
            ValidarCamposBebe(bebe, resultado);
            if (!resultado.Exito)
                throw new ApplicationException(string.Join(" ", resultado.Errores));

            var bebeModificar = repositorioBebe.consultarBebe(bebe.ID);

            // Si el front no manda idMadre, conservar el actual.
            if (!bebe.IdMadre.HasValue || bebe.IdMadre.Value <= 0)
                bebe.IdMadre = bebeModificar.IdMadre;
            else
                AsegurarMadreSiIndica(bebe.IdMadre);

            if (bebe.Dni.HasValue && bebe.Dni.Value > 0
                && repositorioBebe.existeOtroBebeConDni(bebe.Dni.Value, bebe.ID))
                throw new ConflictException("Ya existe otro bebé con ese Dni.");

            return repositorioBebe.modificarBebe(bebe, bebeModificar);
        }

        public bool eliminarBebe(int idBebe)
        {
            return repositorioBebe.eliminarBebeLogico(idBebe);
        }

        public List<BEBE> listarBebesAbrazar()
        {
            return repositorioBebe.obtenerBebesAbrazar();
        }

        public List<ESTADO> listarEstadosBebes()
        {
            return repositorioBebe.listarEstadosBebes();
        }

        public BEBE actualizarEstadoBebe(int idBebe, int idEstado)
        {
            if (idEstado <= 0)
                throw new ApplicationException("idEstado inválido.");

            var estadosValidos = repositorioBebe.listarEstadosBebes()
                .Select(e => e.idEstado)
                .ToHashSet();
            if (!estadosValidos.Contains(idEstado))
                throw new ApplicationException(
                    "Estado inválido para bebés. Use GET api/Bebe/estados. La baja lógica se hace con fechaSalida o POST delete.");

            var bebe = repositorioBebe.consultarBebe(idBebe);
            if (bebe.FechaSalida.HasValue)
                throw new ApplicationException(
                    "El bebé ya tiene fecha de salida; no se puede cambiar el estado operativo. Quedó dado de baja.");

            repositorioBebe.cambioEstadoBebe(bebe, idEstado);
            return repositorioBebe.consultarBebe(idBebe);
        }
    }
}
