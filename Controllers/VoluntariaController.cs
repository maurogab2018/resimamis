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
    public class VoluntariaController : ControllerBase
    {
        public readonly NegVoluntaria negVoluntaria;

        private static readonly JsonSerializerOptions VoluntariaJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private static readonly HashSet<string> PropsIgnorarEnBody = new(StringComparer.OrdinalIgnoreCase)
        {
            "horarios", "rol", "estado", "estadoDetalle", "rolInfo", "asignaciones", "asistencias"
        };

        public VoluntariaController()
        {
            negVoluntaria = new NegVoluntaria();
        }

        private static (VOLUNTARIA voluntaria, List<HorarioVoluntaria>? horarios) LeerVoluntariaDesdeBody(
            JsonElement body, bool esAlta)
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

                    if (esAlta && prop.Name.Equals("idVoluntaria", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (esAlta && prop.Name.Equals("idEstado", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (esAlta && prop.Name.Equals("idRol", StringComparison.OrdinalIgnoreCase))
                        continue;

                    prop.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            var voluntaria = JsonSerializer.Deserialize<VOLUNTARIA>(json, VoluntariaJsonOptions)
                ?? throw new ApplicationException("Voluntaria inválida.");

            return (voluntaria, LeerHorariosDesdeBody(payload));
        }

        private static List<HorarioVoluntaria>? LeerHorariosDesdeBody(JsonElement payload)
        {
            if (!payload.TryGetProperty("horarios", out var horariosEl)
                || horariosEl.ValueKind != JsonValueKind.Array)
                return null;

            var list = new List<HorarioVoluntaria>();
            foreach (var item in horariosEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var h = new HorarioVoluntaria();

                if (item.TryGetProperty("idDia", out var idDia) && idDia.ValueKind == JsonValueKind.Number)
                    h.IdDia = idDia.GetInt32();

                if (item.TryGetProperty("idHorario", out var idHorario) && idHorario.ValueKind == JsonValueKind.Number)
                    h.IdHorario = idHorario.GetInt32();

                if (item.TryGetProperty("turno", out var turno) && turno.ValueKind == JsonValueKind.String)
                    h.Turno = turno.GetString() ?? string.Empty;

                if (h.IdDia > 0 || h.IdHorario > 0 || !string.IsNullOrWhiteSpace(h.Turno))
                    list.Add(h);
            }

            return list.Count > 0 ? list : null;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var listadoVoluntaria = negVoluntaria.listarVoluntarias();
                return ApiResults.Success(listadoVoluntaria);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }

        [HttpGet("libres")]
        public IActionResult GetLibres()
        {
            try
            {
                var listadoVoluntariasLibres = negVoluntaria.listarVoluntariasLibres1();
                return ApiResults.Success(listadoVoluntariasLibres);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }
        [HttpGet("estados")]
        public IActionResult GetEstados()
        {
            try
            {
                var listadoEstadosVoluntarias = negVoluntaria.devolverEstadosVoluntarias();
                return ApiResults.Success(listadoEstadosVoluntarias.Select(e=> new {idEstado=e.idEstado,nombre=e.nombre,descripcion=e.descripcion,idAmbito=e.idAmbito}).ToList());
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }

        [HttpGet("id/{Id}")]
        public IActionResult GetVoluntaria(int Id)
        {
            try
            {
                var voluntariaDni = negVoluntaria.consultarVoluntariaDetalle(Id);
                return ApiResults.Success(voluntariaDni);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }

        [HttpPut("id/{Id}")]
        public IActionResult Put(VOLUNTARIA voluntaria, int Id)
        {
            try
            {
                var resultado = negVoluntaria.modificarVoluntaria(voluntaria,Id);
                return ApiResults.Success(resultado);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }


        [HttpPost("delete")]
        public IActionResult Delete(int idVoluntaria)
        {
            try
            {
                var registroVoluntaria = negVoluntaria.eliminarVoluntaria(idVoluntaria);
                return ApiResults.Success(registroVoluntaria);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }

        [HttpPost]
        public IActionResult Post([FromBody] JsonElement body)
        {
            try
            {
                var (voluntaria, horarios) = LeerVoluntariaDesdeBody(body, esAlta: true);
                var registroVoluntaria = negVoluntaria.registrarVoluntaria(voluntaria, horarios);
                return ApiResults.Success(registroVoluntaria);
            }
            catch (NotFoundException ex)
            {
                return ApiResults.NotFound(ex.Message);
            }
            catch (ApplicationException exa)
            {
                return ApiResults.BadRequest(exa.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError();
            }

        }



    }
}
