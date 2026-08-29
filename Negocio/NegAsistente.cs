using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegAsistente : INegAsistente
    {
        private const int MaxVueltasHerramientas = 8;
        private const int MaxHistorial = 20;
        private const int MaxPregunta = 2000;

        private static readonly string[] EjemplosPregunta =
        [
            "¿Cómo está el día de hoy con los abrazos y las asistencias?",
            "¿Qué bebés no recibieron abrazo hoy?",
            "¿Qué insumos están bajo el mínimo?",
            "¿Quién fichó asistencia hoy?",
            "¿Quién tiene más fichajes de asistencia este mes?",
            "¿Quién hizo más abrazos en los últimos 30 días?",
            "¿Qué abrazos hizo la voluntaria María?",
            "¿Cuál es la duración promedio de los abrazos?",
            "Buscá al bebé Luca y decime sus abrazos",
            "¿Cómo está el peso de los bebés al egreso?",
            "Mostrame bebés disponibles y voluntarias libres, y generá las asignaciones",
            "Dame los datos de las visitas de hoy",
            "Qué asignaciones de abrazo hay hoy"
        ];

        private static readonly JsonSerializerOptions JsonDatos = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IConfiguration configuration;
        private readonly INegUsuarios negUsuarios;
        private readonly INegDashboard negDashboard;
        private readonly INegBebes negBebes;
        private readonly INegInsumos negInsumos;
        private readonly INegAsistencia negAsistencia;
        private readonly INegVoluntaria negVoluntaria;
        private readonly INegAsignacion negAsignacion;
        private readonly INegVisitas negVisitas;
        private readonly OpenAiChatCompletions openAi;
        private int? dniSolicitanteActual;

        public NegAsistente(
            IConfiguration configuration,
            INegUsuarios negUsuarios,
            INegDashboard negDashboard,
            INegBebes negBebes,
            INegInsumos negInsumos,
            INegAsistencia negAsistencia,
            INegVoluntaria negVoluntaria,
            INegAsignacion negAsignacion,
            INegVisitas negVisitas,
            IHttpClientFactory httpClientFactory)
        {
            this.configuration = configuration;
            this.negUsuarios = negUsuarios;
            this.negDashboard = negDashboard;
            this.negBebes = negBebes;
            this.negInsumos = negInsumos;
            this.negAsistencia = negAsistencia;
            this.negVoluntaria = negVoluntaria;
            this.negAsignacion = negAsignacion;
            this.negVisitas = negVisitas;
            openAi = new OpenAiChatCompletions(httpClientFactory);
        }

        public AsistenteEstadoRespuesta ObtenerEstado()
        {
            var (enabled, model, key) = LeerConfig();
            return new AsistenteEstadoRespuesta
            {
                Habilitado = enabled && !string.IsNullOrWhiteSpace(key),
                Proveedor = "OpenAI",
                Modelo = model,
                QuePuedeConsultar = EjemplosPregunta
            };
        }

        public async Task<AsistentePreguntaRespuesta> Preguntar(int dniSolicitante, AsistentePreguntaRequest request)
        {
            negUsuarios.ValidarCoordinadora(dniSolicitante);
            dniSolicitanteActual = dniSolicitante;

            var pregunta = request?.Pregunta?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(pregunta))
                throw new ApplicationException("Debe enviar una pregunta.");
            if (pregunta.Length > MaxPregunta)
                throw new ApplicationException($"La pregunta no puede superar {MaxPregunta} caracteres.");

            var (enabled, model, apiKey) = LeerConfig();
            if (!enabled)
                throw new ApplicationException("El asistente está deshabilitado. Active Asistente:Enabled.");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ApplicationException("Falta la clave de OpenAI. Configure la variable apiKey (Render), OPENAI_API_KEY o Asistente:ApiKey.");

            var mensajes = new List<OpenAiMessage>
            {
                new() { Role = "system", Content = PromptSistema() }
            };
            AgregarHistorial(mensajes, request?.Historial);
            mensajes.Add(new OpenAiMessage { Role = "user", Content = pregunta });

            var usadas = new List<string>();
            for (var i = 0; i < MaxVueltasHerramientas; i++)
            {
                var completion = await openAi.CompletarAsync(
                    apiKey,
                    new OpenAiChatRequest
                    {
                        Model = model,
                        Messages = mensajes,
                        Tools = DefinirHerramientas(),
                        MaxTokens = 2200,
                        Temperature = 0.35
                    },
                    CancellationToken.None);

                var mensaje = completion.Choices[0].Message;
                var toolCalls = mensaje.ToolCalls;
                if (toolCalls == null || toolCalls.Count == 0)
                {
                    var texto = mensaje.Content?.Trim();
                    if (string.IsNullOrWhiteSpace(texto))
                        throw new ApplicationException("El asistente no generó una respuesta.");
                    return new AsistentePreguntaRespuesta
                    {
                        Respuesta = texto,
                        HerramientasUsadas = usadas.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    };
                }

                mensajes.Add(new OpenAiMessage
                {
                    Role = "assistant",
                    Content = mensaje.Content,
                    ToolCalls = toolCalls
                });

                foreach (var call in toolCalls)
                {
                    var nombre = call.Function?.Name ?? "";
                    usadas.Add(nombre);
                    var resultado = EjecutarHerramienta(nombre, call.Function?.Arguments);
                    mensajes.Add(new OpenAiMessage
                    {
                        Role = "tool",
                        ToolCallId = call.Id,
                        Content = resultado
                    });
                }
            }

            var cierre = await openAi.CompletarAsync(
                apiKey,
                new OpenAiChatRequest
                {
                    Model = model,
                    Messages = mensajes,
                    MaxTokens = 2200,
                    Temperature = 0.35
                },
                CancellationToken.None);
            var textoCierre = cierre.Choices[0].Message.Content?.Trim();
            if (string.IsNullOrWhiteSpace(textoCierre))
                throw new ApplicationException("El asistente no pudo completar la investigación. Reformulá la pregunta.");
            return new AsistentePreguntaRespuesta
            {
                Respuesta = textoCierre,
                HerramientasUsadas = usadas.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        private (bool Enabled, string Model, string? ApiKey) LeerConfig()
        {
            var enabled = configuration.GetValue("Asistente:Enabled", false);
            var model = configuration["Asistente:Model"];
            if (string.IsNullOrWhiteSpace(model))
                model = "gpt-4o-mini";
            var apiKey = configuration["Asistente:ApiKey"]
                ?? configuration["apiKey"]
                ?? Environment.GetEnvironmentVariable("apiKey")
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = null;
            return (enabled, model.Trim(), apiKey?.Trim());
        }

        private static void AgregarHistorial(List<OpenAiMessage> mensajes, List<AsistenteMensaje>? historial)
        {
            if (historial == null || historial.Count == 0)
                return;

            foreach (var item in historial.TakeLast(MaxHistorial))
            {
                var rol = (item.Rol ?? "").Trim().ToLowerInvariant();
                if (rol is not ("user" or "assistant" or "usuario" or "asistente"))
                    continue;
                if (string.IsNullOrWhiteSpace(item.Contenido))
                    continue;
                mensajes.Add(new OpenAiMessage
                {
                    Role = rol is "usuario" or "user" ? "user" : "assistant",
                    Content = item.Contenido.Trim()
                });
            }
        }

        private string PromptSistema()
        {
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return $"""
            Sos el asistente de Resimamis para coordinadoras de un programa de abrazos en neonatología.
            Respondé siempre en español, claro y útil.

            Contexto de fecha (obligatorio):
            - Hoy en calendario Argentina es {hoy:yyyy-MM-dd}. Usá SIEMPRE esa fecha para "hoy".
            - Nunca uses la fecha UTC del servidor si difiere. Si una tool *_hoy ya trae la fecha, citá esa.

            Cómo hablar (más completo, sin inventar):
            La coordinadora pregunta en castellano cotidiano. Vos elegís las herramientas. Nunca le pidas nombres de funciones ni endpoints.
            No respondas con una sola oración corta si hay datos: armá una respuesta de 1 párrafo introductorio + viñetas con números +, si aplica, una lectura corta (qué destaca o qué falta hoy).
            Indicá el período o la fecha que usaste (hoy / últimos 30 días / últimos 365 días).
            Cuando haya listas (bebés sin abrazo, ranking, stock, visitas), mostrá al menos los primeros ítems con nombre y número; no digas solo "hay N".
            Al final, ofrecé 1 o 2 seguimientos concretos (ej. cobertura, abrazos de una voluntaria, stock, un bebé, visitas).
            Si pregunta qué podés hacer, listá ejemplos de consulta.
            Preguntas de números, nombres o rankings: usá herramientas. No inventes cantidades, ids ni nombres.
            Si no hay herramienta para ese dato, decilo y ofrecé qué sí podés consultar.

            Routing de "hoy" (no improvises fechas en tools de período):
            - Visitas de hoy → visitas_hoy
            - Asistencias / quién fichó hoy → asistencias_hoy
            - Asignaciones / abrazos creados hoy (lista) → asignaciones_hoy
            - Snapshot general del día → coordinacion_hoy (+ cobertura_hoy si preguntan cobertura)
            - Bebés disponibles / voluntarias libres → bebes_disponibles_abrazo / voluntarias_libres
            - Visitas de un rango (semana, mes) → visitas_periodo con yyyy-MM-dd Argentina

            Investigá antes de responder:
            - Podés usar varias herramientas en la misma pregunta.
            - Si una herramienta da lista vacía, NO cierres en 0 de inmediato: ampliá el rango (rankings: últimos 365 días) o buscá por nombre y reintentá.
            - Si el ranking de abrazos viene vacío, llamá de nuevo ranking_abrazos_voluntarias SIN fechas (usa 365 días).
            - Recién si el segundo intento también está vacío, decí 0 según el criterio de ESA herramienta.
            - No mezcles conceptos: un ranking vacío de abrazos no es ranking de asistencias.
            Si falta un dato (id de bebé o voluntaria), buscá por nombre con buscar_bebes / buscar_voluntarias.
            Fechas en tools de período: yyyy-MM-dd o "hoy"/"ayer" (Argentina). Rankings sin fechas = 365 días; otros períodos sin fechas = 30 días.
            Pesos y ganancias están en gramos. Duraciones de abrazo están en minutos.

            Única acción de escritura permitida:
            - Generar asignaciones de abrazo del día (parea bebés disponibles con voluntarias libres).
            Flujo obligatorio:
            1) Llamá bebes_disponibles_abrazo y voluntarias_libres.
            2) Mostrá a la coordinadora cuántos y quiénes hay (nombres).
            3) Pedí confirmación explícita (“¿Confirmás que genere las asignaciones?”).
            4) Solo si ella confirma, llamá generar_asignaciones_abrazos con confirmar=true.
            Nunca generes sin confirmación. No hagas altas, bajas ni otras escrituras.

            Vocabulario (no mezclar):
            - Asistencia / fichaje: ingreso y salida de la voluntaria. Tools: asistencias_hoy, ranking_asistencias.
            - Abrazo / asignación: vínculo voluntaria-bebé. Un abrazo finalizado NO es una asistencia.
            - Visita: familiar que visita al bebé. No es abrazo ni asistencia.
            - ranking_abrazos_voluntarias cuenta SOLO abrazos finalizados.
            - Para "qué abrazos hizo tal voluntaria": buscar_voluntarias y después abrazos_voluntaria.
            """;
        }

        private static List<OpenAiTool> DefinirHerramientas() =>
        [
            Tool("coordinacion_hoy", "Snapshot operativo de hoy: bebés, abrazos, cantidad de voluntarias que ficharon asistencia hoy y visitas. No es un ranking."),
            Tool("cobertura_hoy", "Porcentaje de bebés activos con abrazo finalizado hoy y lista de quienes no recibieron abrazo."),
            Tool("bebes_por_estado", "Cantidad de bebés activos por estado (Sin abrazar, Asignado, Abrazado)."),
            Tool("bebes_por_sala", "Bebés activos por sala y promedio de permanencia en NEO."),
            Tool("insumos_bajo_stock", "Insumos con stock actual menor o igual al mínimo."),
            Tool("asistencias_hoy", "Lista de fichajes de asistencia de HOY: quién ingresó/salió. No son abrazos."),
            Tool("visitas_hoy", "Visitas familiares de HOY (calendario Argentina): total, bebés visitados y detalle (visitante, familiar, bebé, hora). Usá esta para 'visitas de hoy'."),
            Tool("asignaciones_hoy", "Lista de asignaciones/abrazos del día HOY (bebe, voluntaria, estado, sala). Para detalle del día, no uses solo el contador de coordinacion_hoy."),
            ToolPeriodo("resumen_periodo", "KPIs del período: asignaciones, abrazos finalizados, visitas, promedios."),
            ToolPeriodo("asignaciones_por_dia", "Cantidad de asignaciones y abrazos por día en un rango (NO detalle de hoy: para lista de hoy usá asignaciones_hoy)."),
            ToolPeriodo("visitas_periodo", "Estadísticas de visitas en un rango (NO para solo hoy: para hoy usá visitas_hoy). Total, por día y por familiar."),
            ToolPeriodoOpcional("evolucion_peso", "Evolución de peso ingreso vs egreso (gramos): promedio, mínima y máxima ganancia."),
            ToolPeriodoOpcional("ranking_abrazos_voluntarias", "Ranking de voluntarias por ABRAZOS FINALIZADOS (no fichajes). Sin fechas: últimos 365 días. Parámetro extra: top (1-20)."),
            ToolPeriodoOpcional("ranking_asistencias", "Ranking de voluntarias por FICHAJES de asistencia (ingresos al hospital, no abrazos). Sin fechas: últimos 365 días. Parámetro extra: top (1-20)."),
            ToolPeriodoOpcional("duracion_abrazos", "Duración de abrazos finalizados: promedio, mínimo y máximo en minutos."),
            Tool("bebes_rango_edades", "Distribución de bebés activos por rango de edad en días."),
            Tool("bebes_permanencia", "Tiempo de permanencia en NEO de bebés activos (días desde ingreso)."),
            Tool("bebes_disponibles_abrazo", "Lista de bebés disponibles para abrazo HOY (sin abrazo iniciado). Solo lectura."),
            Tool("voluntarias_libres", "Lista de voluntarias libres HOY para asignar abrazo. Solo lectura."),
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "generar_asignaciones_abrazos",
                    Description = "ÚNICA escritura: genera asignaciones del día emparejando bebés disponibles con voluntarias libres (misma lógica que POST /api/Asignacion/generar). Requiere confirmar=true después de mostrar listas y pedir OK a la coordinadora.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            confirmar = new
                            {
                                type = "boolean",
                                description = "true solo si la coordinadora confirmó explícitamente generar las asignaciones."
                            }
                        },
                        required = new[] { "confirmar" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "buscar_bebes",
                    Description = "Busca bebés activos por nombre, apellido o id. Devolvés id para otras herramientas.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            texto = new { type = "string", description = "Nombre, apellido o id numérico del bebé." }
                        },
                        required = new[] { "texto" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "abrazos_bebe",
                    Description = "Abrazos de un bebé: hoy (si hoy=true) o historial. Fechas opcionales para historial.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id_bebe = new { type = "integer", description = "Id del bebé." },
                            hoy = new { type = "boolean", description = "true para abrazos de hoy." },
                            fecha_desde = new { type = "string", description = "yyyy-MM-dd" },
                            fecha_hasta = new { type = "string", description = "yyyy-MM-dd" }
                        },
                        required = new[] { "id_bebe" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "buscar_voluntarias",
                    Description = "Busca voluntarias por nombre, apellido o id. Devolvés id para abrazos_voluntaria.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            texto = new { type = "string", description = "Nombre, apellido o id numérico de la voluntaria." }
                        },
                        required = new[] { "texto" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "abrazos_voluntaria",
                    Description = "Abrazos que hizo una voluntaria: hoy (si hoy=true) o historial. Fechas opcionales para historial.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id_voluntaria = new { type = "integer", description = "Id de la voluntaria." },
                            hoy = new { type = "boolean", description = "true para abrazos de hoy." },
                            fecha_desde = new { type = "string", description = "yyyy-MM-dd" },
                            fecha_hasta = new { type = "string", description = "yyyy-MM-dd" }
                        },
                        required = new[] { "id_voluntaria" }
                    }
                }
            }
        ];

        private static OpenAiTool Tool(string name, string description) =>
            new()
            {
                Function = new OpenAiFunction
                {
                    Name = name,
                    Description = description,
                    Parameters = new { type = "object", properties = new { } }
                }
            };

        private static OpenAiTool ToolPeriodo(string name, string description) =>
            new()
            {
                Function = new OpenAiFunction
                {
                    Name = name,
                    Description = description,
                    Parameters = SchemaPeriodo()
                }
            };

        private static OpenAiTool ToolPeriodoOpcional(string name, string description) =>
            ToolPeriodo(name, description);

        private static object SchemaPeriodo() =>
            new
            {
                type = "object",
                properties = new
                {
                    fecha_desde = new { type = "string", description = "Inicio yyyy-MM-dd o 'hoy'/'ayer' (Argentina). Si falta, últimos 30 días (rankings: 365)." },
                    fecha_hasta = new { type = "string", description = "Fin yyyy-MM-dd o 'hoy'/'ayer' (Argentina)." },
                    top = new { type = "integer", description = "Solo ranking: cantidad de voluntarias (default 10)." }
                }
            };

        private string EjecutarHerramienta(string nombre, string? argumentosJson)
        {
            try
            {
                using var args = string.IsNullOrWhiteSpace(argumentosJson)
                    ? JsonDocument.Parse("{}")
                    : JsonDocument.Parse(argumentosJson);

                return nombre switch
                {
                    "coordinacion_hoy" => Json(ResumirCoordinacionHoy()),
                    "cobertura_hoy" => Json(negDashboard.ObtenerCoberturaHoy()),
                    "bebes_por_estado" => Json(negDashboard.ObtenerBebesPorEstado()),
                    "bebes_por_sala" => Json(negDashboard.ObtenerBebesPorSala()),
                    "insumos_bajo_stock" => Json(negInsumos.obtenerInsumosBajoStockMinimo()),
                    "asistencias_hoy" => Json(ResumirAsistenciasHoy()),
                    "visitas_hoy" => Json(ResumirVisitasHoy()),
                    "asignaciones_hoy" => Json(ResumirAsignacionesHoy()),
                    "resumen_periodo" => Json(negDashboard.ObtenerResumen(Desde(args), Hasta(args))),
                    "asignaciones_por_dia" => Json(negDashboard.ObtenerAsignacionesPorDia(Desde(args), Hasta(args))),
                    "visitas_periodo" => Json(ResumirVisitasPeriodo(Desde(args), Hasta(args))),
                    "evolucion_peso" => Json(ResumirPeso(negDashboard.ObtenerEvolucionPesoBebes(DesdeOpcional(args), HastaOpcional(args)))),
                    "ranking_abrazos_voluntarias" => Json(ResumirRankingAbrazos(Desde(args, 364), Hasta(args, 364), Top(args))),
                    "ranking_voluntarias" => Json(ResumirRankingAbrazos(Desde(args, 364), Hasta(args, 364), Top(args))),
                    "ranking_asistencias" => Json(ResumirRankingAsistencias(Desde(args, 364), Hasta(args, 364), Top(args))),
                    "duracion_abrazos" => Json(negDashboard.ObtenerDuracionAbrazos(DesdeOpcional(args), HastaOpcional(args))),
                    "bebes_rango_edades" => Json(negDashboard.ObtenerRangoEdadesBebes()),
                    "bebes_permanencia" => Json(negDashboard.ObtenerPermanenciaBebes()),
                    "bebes_disponibles_abrazo" => Json(ResumirBebesDisponiblesAbrazo()),
                    "voluntarias_libres" => Json(ResumirVoluntariasLibres()),
                    "generar_asignaciones_abrazos" => Json(GenerarAsignacionesAbrazos(args)),
                    "buscar_bebes" => Json(BuscarBebes(LeerString(args, "texto"))),
                    "abrazos_bebe" => Json(AbrazosBebe(args)),
                    "buscar_voluntarias" => Json(BuscarVoluntarias(LeerString(args, "texto"))),
                    "abrazos_voluntaria" => Json(AbrazosVoluntaria(args)),
                    _ => Json(new { error = $"Herramienta desconocida: {nombre}" })
                };
            }
            catch (Exception ex) when (ex is ApplicationException or NotFoundException or ConflictException or JsonException)
            {
                return Json(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error interno al consultar datos: " + ex.Message });
            }
        }

        private object AbrazosBebe(JsonDocument args)
        {
            var idBebe = LeerInt(args, "id_bebe") ?? LeerInt(args, "idBebe");
            if (idBebe is null or <= 0)
                return new { error = "Falta id_bebe." };

            var hoy = LeerBool(args, "hoy") == true;
            if (hoy)
                return negDashboard.ObtenerAbrazosBebeHoy(idBebe.Value);

            var desde = DesdeOpcional(args);
            var hasta = HastaOpcional(args);
            return negDashboard.ObtenerAbrazosBebeHistorial(idBebe.Value, desde, hasta);
        }

        private object AbrazosVoluntaria(JsonDocument args)
        {
            var idVoluntaria = LeerInt(args, "id_voluntaria") ?? LeerInt(args, "idVoluntaria");
            if (idVoluntaria is null or <= 0)
                return new { error = "Falta id_voluntaria." };

            var hoy = LeerBool(args, "hoy") == true;
            if (hoy)
                return negDashboard.ObtenerAbrazosVoluntariaHoy(idVoluntaria.Value);

            var desde = DesdeOpcional(args);
            var hasta = HastaOpcional(args);
            return negDashboard.ObtenerAbrazosVoluntariaHistorial(idVoluntaria.Value, desde, hasta);
        }

        private List<object> BuscarVoluntarias(string? texto)
        {
            texto = texto?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(texto))
                return new List<object> { new { error = "Indique nombre, apellido o id." } };

            if (int.TryParse(texto, out var id) && id > 0)
            {
                try
                {
                    var una = negVoluntaria.consultarVoluntaria(id);
                    return new List<object> { MapearVoluntariaBusqueda(una) };
                }
                catch (NotFoundException)
                {
                    return new List<object> { new { error = "No hay voluntaria con ese id." } };
                }
            }

            var q = texto.ToLowerInvariant();
            return negVoluntaria.listarVoluntarias()
                .Where(v =>
                    (v.Nombre ?? "").ToLowerInvariant().Contains(q)
                    || (v.Apellido ?? "").ToLowerInvariant().Contains(q)
                    || $"{v.Nombre} {v.Apellido}".ToLowerInvariant().Contains(q))
                .Take(15)
                .Select(MapearVoluntariaBusqueda)
                .ToList();
        }

        private static object MapearVoluntariaBusqueda(VOLUNTARIA v) =>
            new
            {
                idVoluntaria = v.IdVoluntaria,
                nombre = v.Nombre,
                apellido = v.Apellido,
                estado = v.Estado?.nombre,
                rol = v.rol
            };

        private List<object> BuscarBebes(string? texto)
        {
            texto = texto?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(texto))
                return new List<object> { new { error = "Indique nombre, apellido o id." } };

            if (int.TryParse(texto, out var id) && id > 0)
            {
                try
                {
                    var uno = negBebes.consultarBebe(id);
                    return new List<object> { MapearBebeBusqueda(uno) };
                }
                catch (NotFoundException)
                {
                    return new List<object> { new { error = "No hay bebé con ese id." } };
                }
            }

            var q = texto.ToLowerInvariant();
            return negBebes.listarBebes()
                .Where(b =>
                    (b.nombre ?? "").ToLowerInvariant().Contains(q)
                    || (b.apellido ?? "").ToLowerInvariant().Contains(q)
                    || $"{b.nombre} {b.apellido}".ToLowerInvariant().Contains(q))
                .Take(15)
                .Select(MapearBebeBusqueda)
                .ToList();
        }

        private static object MapearBebeBusqueda(BEBE b) =>
            new
            {
                idBebe = b.ID,
                nombre = b.nombre,
                apellido = b.apellido,
                sala = b.Sala?.Nombre,
                estado = b.Estado?.nombre,
                fechaIngresoNeo = b.FechaIngresoNEO,
                fechaSalida = b.FechaSalida
            };

        private object ResumirBebesDisponiblesAbrazo()
        {
            var lista = negBebes.listarBebesAbrazar() ?? new List<BEBE>();
            var items = lista.Select(b => new
            {
                idBebe = b.ID,
                nombre = b.nombre,
                apellido = b.apellido,
                sala = b.Sala?.Nombre,
                estado = b.Estado?.nombre
            }).ToList();

            return new
            {
                criterio = "bebes_disponibles_abrazo_hoy",
                aclaracion = "Bebés elegibles para generar asignación de abrazo hoy. Solo lectura.",
                total = items.Count,
                bebes = items
            };
        }

        private object ResumirVoluntariasLibres()
        {
            var lista = negVoluntaria.listarVoluntariasLibres1() ?? new List<VOLUNTARIA>();
            var items = lista.Select(v => new
            {
                idVoluntaria = v.IdVoluntaria,
                nombre = v.Nombre,
                apellido = v.Apellido,
                estado = v.Estado?.nombre,
                rol = v.rol
            }).ToList();

            return new
            {
                criterio = "voluntarias_libres_hoy",
                aclaracion = "Voluntarias libres hoy para asignar abrazo. Solo lectura.",
                total = items.Count,
                voluntarias = items
            };
        }

        private object GenerarAsignacionesAbrazos(JsonDocument args)
        {
            if (LeerBool(args, "confirmar") != true)
            {
                var bebes = ResumirBebesDisponiblesAbrazo();
                var vols = ResumirVoluntariasLibres();
                return new
                {
                    generada = false,
                    error = "Falta confirmación. Mostrá las listas a la coordinadora y pedí OK. Luego llamá de nuevo con confirmar=true.",
                    preview = new { bebes, voluntarias = vols }
                };
            }

            var creadas = negAsignacion.generarAsiganaciones() ?? new List<RespuestaAsignaciones>();
            return new
            {
                generada = true,
                aclaracion = "Asignaciones creadas (estado Creada). Misma lógica que POST /api/Asignacion/generar.",
                total = creadas.Count,
                asignaciones = creadas.Select(a => new
                {
                    a.idAsignacion,
                    a.idBebe,
                    a.nombreBebe,
                    a.idVoluntaria,
                    a.nombreVoluntaria,
                    a.nombreSala,
                    a.estadoAsignacion,
                    a.fechaHoraAsignacion
                }).ToList()
            };
        }

        private object ResumirCoordinacionHoy()
        {
            var data = negDashboard.ObtenerCoordinacionHoy();
            return new
            {
                aclaracion = $"Snapshot del día {data.Fecha:yyyy-MM-dd} (calendario Argentina). Para detalle de visitas usá visitas_hoy; para lista de asignaciones usá asignaciones_hoy.",
                data.Fecha,
                data.BebesActivos,
                data.BebesDisponiblesAbrazo,
                data.BebesAsignados,
                data.AbrazosHoy,
                data.VoluntariasConAsistenciaHoy,
                data.AbrazosColgados,
                data.VisitasHoy
            };
        }

        private object ResumirAsignacionesHoy()
        {
            if (dniSolicitanteActual is null)
                return new { error = "No se pudo identificar a la coordinadora." };

            var lista = negAsignacion.listarAsignacionesHoy(dniSolicitanteActual.Value)
                ?? new List<RespuestaAsignaciones>();
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);

            return new
            {
                criterio = "asignaciones_hoy_argentina",
                aclaracion = "Asignaciones/abrazos del día calendario Argentina.",
                fecha = hoy,
                total = lista.Count,
                finalizados = lista.Count(a =>
                    string.Equals(a.estadoAsignacion, "Finalizado", StringComparison.OrdinalIgnoreCase)),
                enCurso = lista.Count(a =>
                    a.fechaHoraInicio.HasValue && !a.fechaHoraFin.HasValue),
                asignaciones = lista.Select(a => new
                {
                    a.idAsignacion,
                    a.idBebe,
                    a.nombreBebe,
                    a.idVoluntaria,
                    a.nombreVoluntaria,
                    a.nombreSala,
                    a.estadoAsignacion,
                    a.fechaHoraAsignacion,
                    a.fechaHoraInicio,
                    a.fechaHoraFin
                }).ToList()
            };
        }

        private object ResumirVisitasHoy()
        {
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            var (inicioUtc, finUtc) = NegConversorFecha.RangoDiaHoyArgentinaEnUtc();
            var lista = (negVisitas.listarVisitas() ?? new List<VisitaListado>())
                .Where(v => v.activa
                            && v.fechaHoraVisita >= inicioUtc
                            && v.fechaHoraVisita < finUtc)
                .OrderByDescending(v => v.fechaHoraVisita)
                .ToList();

            return new
            {
                criterio = "visitas_hoy_argentina",
                aclaracion = "Visitas del día calendario Argentina. No uses UTC para decidir 'hoy'.",
                fecha = hoy,
                totalVisitas = lista.Count,
                bebesVisitados = lista.Select(v => v.idBebe).Distinct().Count(),
                visitas = lista.Select(v => new
                {
                    v.idVisita,
                    v.idBebe,
                    nombreBebe = $"{v.nombreBebe} {v.apellidoBebe}".Trim(),
                    v.nombreVisitante,
                    v.familiar,
                    v.fechaHoraVisita,
                    v.observacion
                }).ToList()
            };
        }

        private object ResumirVisitasPeriodo(DateTime desde, DateTime hasta)
        {
            var stats = negDashboard.ObtenerEstadisticasVisitas(desde, hasta);
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            var esSoloHoy = DateOnly.FromDateTime(desde.Date) == hoy
                            && DateOnly.FromDateTime(hasta.Date) == hoy;

            object? pistaHoy = null;
            if (stats.TotalVisitas == 0 && !esSoloHoy)
            {
                // Si el rango quedó vacío por fecha UTC mal puesta, avisamos si hoy AR sí tiene datos.
                var hoyStats = negDashboard.ObtenerEstadisticasVisitas(
                    hoy.ToDateTime(TimeOnly.MinValue),
                    hoy.ToDateTime(TimeOnly.MinValue));
                if (hoyStats.TotalVisitas > 0)
                {
                    pistaHoy = new
                    {
                        mensaje = $"El rango pedido dio 0, pero hoy Argentina ({hoy:yyyy-MM-dd}) tiene {hoyStats.TotalVisitas} visita(s). Usá visitas_hoy.",
                        hoyStats.TotalVisitas,
                        hoyStats.BebesVisitados
                    };
                }
            }

            return new
            {
                aclaracion = esSoloHoy
                    ? "Rango de un solo día (hoy Argentina). Preferí visitas_hoy para el detalle."
                    : "Estadísticas agregadas del período. Para el detalle de HOY usá visitas_hoy.",
                stats.FechaInicio,
                stats.FechaFin,
                stats.TotalVisitas,
                stats.BebesVisitados,
                stats.PorDia,
                stats.PorFamiliar,
                pistaHoy
            };
        }

        private object ResumirAsistenciasHoy()
        {
            var lista = negAsistencia.consultarAsistenciasFechahoy() ?? new List<ASISTENCIA>();
            var items = lista.Select(a => new
            {
                idVoluntaria = a.IdVoluntaria,
                nombre = a.Voluntaria?.Nombre,
                apellido = a.Voluntaria?.Apellido,
                fechaHoraIngreso = a.FechaHoraIngreso,
                fechaHoraSalida = a.FechaHoraSalida,
                estado = a.Estado?.nombre
            }).ToList();

            return new
            {
                criterio = "fichajes_asistencia_hoy",
                aclaracion = "Fichajes de ingreso/salida de voluntarias. No son abrazos.",
                total = items.Count,
                voluntarias = items
            };
        }

        private object ResumirRankingAbrazos(DateTime desde, DateTime hasta, int top)
        {
            var ranking = negDashboard.ObtenerRankingVoluntariasAbrazos(desde, hasta, top);
            var amplio = false;
            if (ranking.Ranking.Count == 0)
            {
                var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
                var amplioDesde = hoy.AddDays(-364);
                if (desde.Date > amplioDesde.Date || hasta.Date < hoy.Date)
                {
                    ranking = negDashboard.ObtenerRankingVoluntariasAbrazos(amplioDesde, hoy, top);
                    amplio = true;
                }
            }

            return new
            {
                criterio = "abrazos_finalizados",
                aclaracion = amplio
                    ? "El rango pedido no tenía abrazos finalizados; se amplió a los últimos 365 días."
                    : "Ranking por abrazos finalizados (fechaHoraFin en el período). No es ranking de asistencias/fichajes.",
                ranking.FechaInicio,
                ranking.FechaFin,
                ranking.Top,
                rangoAmpliado = amplio,
                totalVoluntarias = ranking.Ranking.Count,
                ranking.Ranking
            };
        }

        private object ResumirRankingAsistencias(DateTime desde, DateTime hasta, int top)
        {
            var reporte = negAsistencia.ReporteAsistenciaPorPeriodo(desde, hasta);
            var ranking = (reporte.Registros ?? new List<ReporteAsistenciaPeriodoItem>())
                .GroupBy(r => new { r.IdVoluntaria, r.NombreVoluntaria, r.ApellidoVoluntaria })
                .Select(g => new
                {
                    idVoluntaria = g.Key.IdVoluntaria,
                    nombre = $"{g.Key.NombreVoluntaria} {g.Key.ApellidoVoluntaria}".Trim(),
                    cantidadAsistencias = g.Count()
                })
                .OrderByDescending(x => x.cantidadAsistencias)
                .ThenBy(x => x.nombre)
                .Take(top)
                .Select((x, i) => new
                {
                    posicion = i + 1,
                    x.idVoluntaria,
                    x.nombre,
                    x.cantidadAsistencias
                })
                .ToList();

            return new
            {
                criterio = "fichajes_asistencia",
                aclaracion = "Ranking por cantidad de fichajes de asistencia (ingresos). No son abrazos.",
                fechaInicio = reporte.FechaInicio,
                fechaFin = reporte.FechaFin,
                totalFichajes = reporte.TotalRegistros,
                top,
                ranking
            };
        }

        private static object ResumirPeso(EvolucionPesoBebesRespuesta r) =>
            new
            {
                r.FechaInicio,
                r.FechaFin,
                r.TotalBebes,
                r.BebesConComparacionCompleta,
                r.BebesConGanancia,
                r.BebesConPerdida,
                r.BebesSinCambio,
                promedioGananciaGramos = r.PromedioGanancia ?? r.PromedioDiferencia,
                gananciaMinimaGramos = r.GananciaMinima,
                gananciaMaximaGramos = r.GananciaMaxima,
                r.PromedioPesoIngreso,
                r.PromedioPesoEgreso,
                muestra = r.Bebes.Take(20).Select(b => new
                {
                    b.IdBebe,
                    b.Nombre,
                    b.Apellido,
                    b.PesoIngresoNeo,
                    b.PesoEgreso,
                    b.DiferenciaIngresoEgreso,
                    b.FechaIngresoNeo,
                    b.FechaSalida
                })
            };

        private static DateTime Desde(JsonDocument args, int diasDefault = 29) => ResolverRango(args, diasDefault).Inicio;
        private static DateTime Hasta(JsonDocument args, int diasDefault = 29) => ResolverRango(args, diasDefault).Fin;

        private static DateTime? DesdeOpcional(JsonDocument args)
        {
            var raw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var rawFin = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            if (string.IsNullOrWhiteSpace(raw) && string.IsNullOrWhiteSpace(rawFin))
                return null;
            return ResolverRango(args).Inicio;
        }

        private static DateTime? HastaOpcional(JsonDocument args)
        {
            var raw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var rawFin = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            if (string.IsNullOrWhiteSpace(raw) && string.IsNullOrWhiteSpace(rawFin))
                return null;
            return ResolverRango(args).Fin;
        }

        private static (DateTime Inicio, DateTime Fin) ResolverRango(JsonDocument args, int diasDefault = 29)
        {
            var desdeRaw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var hastaRaw = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            var hoyAr = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            var hoy = hoyAr.ToDateTime(TimeOnly.MinValue);
            if (string.IsNullOrWhiteSpace(desdeRaw) && string.IsNullOrWhiteSpace(hastaRaw))
                return (hoy.AddDays(-diasDefault), hoy);

            var inicio = string.IsNullOrWhiteSpace(desdeRaw)
                ? hoy.AddDays(-diasDefault)
                : ParseFechaFlexible(desdeRaw, hoy);
            var fin = string.IsNullOrWhiteSpace(hastaRaw)
                ? hoy
                : ParseFechaFlexible(hastaRaw, hoy);
            if (fin < inicio)
                (inicio, fin) = (fin, inicio);

            // Si el modelo mandó el día UTC (adelantado respecto de Argentina), corregir a hoy AR.
            var diaUtc = DateOnly.FromDateTime(DateTime.UtcNow);
            if (DateOnly.FromDateTime(inicio.Date) == DateOnly.FromDateTime(fin.Date))
            {
                var diaPedido = DateOnly.FromDateTime(inicio.Date);
                if (diaPedido == diaUtc && diaUtc != hoyAr)
                {
                    inicio = hoy;
                    fin = hoy;
                }
            }

            return (inicio, fin);
        }

        private static DateTime ParseFechaFlexible(string raw, DateTime hoyAr)
        {
            var t = raw.Trim().ToLowerInvariant();
            if (t is "hoy" or "today" or "ahora")
                return hoyAr.Date;
            if (t is "ayer" or "yesterday")
                return hoyAr.Date.AddDays(-1);
            return NegConversorFecha.ParseFechaCalendarioReporte(raw);
        }

        private static int Top(JsonDocument args)
        {
            var top = LeerInt(args, "top") ?? 10;
            if (top < 1) top = 1;
            if (top > 20) top = 20;
            return top;
        }

        private static string? LeerString(JsonDocument args, string name)
        {
            if (!args.RootElement.TryGetProperty(name, out var el))
                return null;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        }

        private static int? LeerInt(JsonDocument args, string name)
        {
            if (!args.RootElement.TryGetProperty(name, out var el))
                return null;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
                return n;
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out n))
                return n;
            return null;
        }

        private static bool? LeerBool(JsonDocument args, string name)
        {
            if (!args.RootElement.TryGetProperty(name, out var el))
                return null;
            if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return el.GetBoolean();
            if (el.ValueKind == JsonValueKind.String
                && bool.TryParse(el.GetString(), out var b))
                return b;
            return null;
        }

        private static string Json(object data) => JsonSerializer.Serialize(data, JsonDatos);
    }
}
