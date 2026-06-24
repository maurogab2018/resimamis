using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BebeController : ControllerBase
    {
        public readonly NegBebes neg_Bebes;

        private static readonly JsonSerializerOptions BebeJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private static readonly HashSet<string> PropsIgnorarEnBody = new(StringComparer.OrdinalIgnoreCase)
        {
            "madre", "sala", "estado", "localidad", "asignaciones", "visitas", "nombreSala"
        };

        public BebeController()
        {
            neg_Bebes = new NegBebes();
        }

        private static BEBE LeerBebeDesdeBody(JsonElement body, bool esAlta)
        {
            var payload = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("data", out var data)
                ? data
                : body;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var prop in payload.EnumerateObject())
                {
                    if (PropsIgnorarEnBody.Contains(prop.Name))
                        continue;

                    if (esAlta && prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Null)
                            continue;
                        if (prop.Value.ValueKind == JsonValueKind.Number
                            && prop.Value.TryGetInt32(out var id)
                            && id <= 0)
                            continue;
                    }

                    if (esAlta && prop.Name.Equals("idEstado", StringComparison.OrdinalIgnoreCase))
                        continue;

                    prop.WriteTo(writer);
                }

                if (!TienePropiedadConValor(payload, "idMadre")
                    && payload.TryGetProperty("madre", out var madre)
                    && madre.ValueKind == JsonValueKind.Object
                    && madre.TryGetProperty("idMadre", out var idMadre)
                    && idMadre.ValueKind == JsonValueKind.Number)
                {
                    writer.WriteNumber("idMadre", idMadre.GetInt32());
                }

                if (!TienePropiedadConValor(payload, "idSala")
                    && payload.TryGetProperty("sala", out var sala)
                    && sala.ValueKind == JsonValueKind.Object
                    && sala.TryGetProperty("idSala", out var idSala)
                    && idSala.ValueKind == JsonValueKind.Number)
                {
                    writer.WriteNumber("idSala", idSala.GetInt32());
                }

                if (!TienePropiedadConValor(payload, "idLocalidad")
                    && payload.TryGetProperty("localidad", out var localidad)
                    && localidad.ValueKind == JsonValueKind.Object)
                {
                    if (localidad.TryGetProperty("idLocalidad", out var idLocalidad)
                        && idLocalidad.ValueKind == JsonValueKind.Number)
                    {
                        writer.WriteNumber("idLocalidad", idLocalidad.GetInt32());
                    }
                    else if (localidad.TryGetProperty("id", out var idLoc)
                             && idLoc.ValueKind == JsonValueKind.Number)
                    {
                        writer.WriteNumber("idLocalidad", idLoc.GetInt32());
                    }
                }

                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            return JsonSerializer.Deserialize<BEBE>(json, BebeJsonOptions)
                ?? throw new ApplicationException("Bebé inválido.");
        }

        private static bool TienePropiedadConValor(JsonElement obj, string name) =>
            obj.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null;
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var lista = neg_Bebes.listarBebes();
                return ApiResults.Success(lista);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("listarSalas")]
        public IActionResult ListarSalas()
        {
            try
            {
                var lista = neg_Bebes.listarSalas();
                return ApiResults.Success(lista);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpGet("id/{Dni}")]
        public IActionResult Get(int Dni)
        {
            try
            {
                var bebeDni = neg_Bebes.consultarBebe(Dni);
                return ApiResults.Success(bebeDni);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        /// <summary>Bebés disponibles para abrazar hoy (estado Sin abrazar; sin abrazo ya iniciado en el día).</summary>
        [HttpGet("disponibles-abrazo")]
        [HttpGet("abrazar")]
        public IActionResult GetBebesDisponiblesParaAbrazar()
        {
            try
            {
                var listado = neg_Bebes.listarBebesAbrazar();
                return ApiResults.Success(listado);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost]
        public IActionResult post([FromBody] JsonElement body)
        {
            try
            {
                var bebe = LeerBebeDesdeBody(body, esAlta: true);
                var respuesta = neg_Bebes.registrarBebe(bebe);
                return ApiResults.Success(respuesta);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }


        [HttpPut]
        public IActionResult Put([FromBody] JsonElement body)
        {
            try
            {
                var bebe = LeerBebeDesdeBody(body, esAlta: false);
                var respuesta = neg_Bebes.modificarBebe(bebe);
                return ApiResults.Success(respuesta);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(int idBebe)
        {
            try
            {
                var ok = neg_Bebes.eliminarBebe(idBebe);
                return ApiResults.Success(ok);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }
        }

    }
}
