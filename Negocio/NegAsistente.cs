using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.Negocio
{
    public class NegAsistente : INegAsistente
    {
        private const int MaxVueltasHerramientas = 10;
        private const int MaxHistorial = 20;
        private const int MaxPregunta = 2000;
        private const int MaxItemsLista = 40;

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
            "Qué asignaciones de abrazo hay hoy",
            "Dame la ficha completa del bebé Luca",
            "¿Cuál es el contacto y el horario de la voluntaria Ana?",
            "Listame los bebés de una sala",
            "¿Cómo está el stock de todos los insumos?",
            "¿Hubo movimientos de stock esta semana?",
            "¿Qué asistencias tuvo María este mes?",
            "¿Quién visitó a un bebé?",
            "Buscá a la mamá González",
            "Compará esta semana con la anterior",
            "¿Cómo funciona el ciclo de un abrazo?",
            "Estadísticas de edades y localidades de las mamás"
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
        private readonly INegMadres negMadres;
        private readonly INegSalas negSalas;
        private readonly INegTareas negTareas;
        private readonly INegProveedores negProveedores;
        private readonly INegGenericos negGenericos;
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
            INegMadres negMadres,
            INegSalas negSalas,
            INegTareas negTareas,
            INegProveedores negProveedores,
            INegGenericos negGenericos,
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
            this.negMadres = negMadres;
            this.negSalas = negSalas;
            this.negTareas = negTareas;
            this.negProveedores = negProveedores;
            this.negGenericos = negGenericos;
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

        public async Task<AsistentePreguntaRespuesta> Preguntar(
            int dniSolicitante,
            AsistentePreguntaRequest request,
            CancellationToken cancellationToken = default)
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
                        MaxTokens = 2500,
                        Temperature = 0.35
                    },
                    cancellationToken);

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
                    MaxTokens = 2500,
                    Temperature = 0.35
                },
                    cancellationToken);
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
                var contenido = item.Contenido.Trim();
                if (contenido.Length > MaxPregunta)
                    contenido = contenido[..MaxPregunta];
                mensajes.Add(new OpenAiMessage
                {
                    Role = rol is "usuario" or "user" ? "user" : "assistant",
                    Content = contenido
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
            Indicá el período o la fecha que usaste (hoy / esta semana / últimos 30 días / últimos 365 días).
            Cuando haya listas (bebés sin abrazo, ranking, stock, visitas, fichas), mostrá al menos los primeros ítems con nombre y número; no digas solo "hay N".
            Al final, ofrecé 1 o 2 seguimientos concretos (ej. cobertura, ficha de un bebé, horario de una voluntaria, stock, visitas).
            Si pregunta qué podés hacer, listá ejemplos de consulta (operación del día, fichas, stock, rankings, comparativos, cómo funciona un flujo).
            Preguntas de números, nombres, fichas o rankings: usá herramientas. No inventes cantidades, ids, mails, teléfonos ni nombres.
            Si no hay herramienta para ese dato, decilo y ofrecé qué sí podés consultar. Para "cómo funciona X" usá guia_operativa.

            Routing de "hoy" (no improvises fechas en tools de período):
            - Visitas de hoy → visitas_hoy
            - Asistencias / quién fichó hoy → asistencias_hoy
            - Asignaciones / abrazos creados hoy (lista) → asignaciones_hoy
            - Abrazos en curso / no finalizados de hoy → abrazos_en_curso
            - Snapshot general del día → coordinacion_hoy (+ cobertura_hoy si preguntan cobertura)
            - Bebés disponibles / voluntarias libres → bebes_disponibles_abrazo / voluntarias_libres
            - Visitas de un rango (semana, mes) → visitas_periodo con periodo="esta_semana" o fechas yyyy-MM-dd Argentina

            Routing de fichas y listados:
            - Ficha / datos completos de un bebé → buscar_bebes y después ficha_bebe
            - Contacto, mail, DNI u horarios de una voluntaria → buscar_voluntarias y después ficha_voluntaria
            - Listado de bebés activos (por sala o estado) → listar_bebes
            - Listado de voluntarias (por estado) → listar_voluntarias
            - Stock de TODOS los insumos o uno por nombre → stock_insumos (bajo mínimo sigue siendo insumos_bajo_stock)
            - Movimientos de depósito → movimientos_stock
            - Asistencias de un período o de una voluntaria → asistencias_periodo (id_voluntaria opcional)
            - Visitas de un bebé concreto → visitas_bebe
            - Mamá / madre → buscar_madres y si hace falta ficha_madre
            - Salas, tareas, proveedores, estados → catalogos
            - Edades/localidades de mamás o consumo de insumos → estadisticas_extra
            - Esta semana vs la anterior → comparar_periodos
            - Cómo funciona un flujo (abrazo, asistencia, stock, estados) → guia_operativa

            Investigá antes de responder:
            - Podés usar varias herramientas en la misma pregunta.
            - Si una herramienta da lista vacía, NO cierres en 0 de inmediato: ampliá el rango (rankings: últimos 365 días) o buscá por nombre y reintentá.
            - Si el ranking de abrazos viene vacío, llamá de nuevo ranking_abrazos_voluntarias SIN fechas (usa 365 días).
            - Recién si el segundo intento también está vacío, decí 0 según el criterio de ESA herramienta.
            - No mezcles conceptos: un ranking vacío de abrazos no es ranking de asistencias.
            Si falta un dato (id de bebé, voluntaria o madre), buscá por nombre con buscar_bebes / buscar_voluntarias / buscar_madres.
            Fechas en tools de período: yyyy-MM-dd, "hoy"/"ayer", o periodo="esta_semana"|"semana_pasada"|"este_mes"|"mes_pasado"|"ultimos_7"|"ultimos_30" (Argentina). Rankings sin fechas = 365 días; otros períodos sin fechas = 30 días.
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
            - Asistencia / fichaje: ingreso y salida de la voluntaria. Tools: asistencias_hoy, asistencias_periodo, ranking_asistencias.
            - Abrazo / asignación: vínculo voluntaria-bebé. Un abrazo finalizado NO es una asistencia.
            - Visita: familiar que visita al bebé. No es abrazo ni asistencia.
            - ranking_abrazos_voluntarias cuenta SOLO abrazos finalizados.
            - Para "qué abrazos hizo tal voluntaria": buscar_voluntarias y después abrazos_voluntaria.
            - Para "datos / ficha / peso / diagnóstico de un bebé": ficha_bebe (no alcanza con buscar_bebes).
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
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "ficha_bebe",
                    Description = "Ficha completa de un bebé: pesos en gramos, diagnósticos, sala, madre, fechas de ingreso/egreso. Si no tenés id, usá buscar_bebes primero.",
                    Parameters = SchemaId("id_bebe", "Id del bebé.")
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "ficha_voluntaria",
                    Description = "Ficha de una voluntaria: contacto, estado, rol y horarios. Si no tenés id, usá buscar_voluntarias primero.",
                    Parameters = SchemaId("id_voluntaria", "Id de la voluntaria.")
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "listar_bebes",
                    Description = "Lista bebés ACTIVOS (sin egreso). Filtros opcionales por estado (Sin abrazar, Asignado, Abrazado), sala o texto.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            filtro_estado = new { type = "string", description = "Estado del bebé, ej. Sin abrazar / Asignado / Abrazado." },
                            filtro_sala = new { type = "string", description = "Nombre o parte del nombre de la sala." },
                            texto = new { type = "string", description = "Nombre o apellido opcional." }
                        }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "listar_voluntarias",
                    Description = "Lista voluntarias vigentes. Filtro opcional por estado (Disponible, Asignada, Abrazando, Activa) o texto.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            filtro_estado = new { type = "string", description = "Estado de la voluntaria." },
                            texto = new { type = "string", description = "Nombre o apellido opcional." }
                        }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "stock_insumos",
                    Description = "Stock de insumos (todos o filtrados por nombre). Incluye actual/mínimo/máximo y si está bajo mínimo. Para SOLO bajo mínimo preferí insumos_bajo_stock.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            texto = new { type = "string", description = "Nombre o parte del nombre del insumo." }
                        }
                    }
                }
            },
            ToolPeriodo("movimientos_stock", "Movimientos de depósito (entradas/salidas) en un rango. Sin fechas: últimos 30 días."),
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "asistencias_periodo",
                    Description = "Fichajes de asistencia en un rango (NO de hoy suelto: para hoy usá asistencias_hoy). Opcional: una voluntaria. Sin fechas: últimos 30 días.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            fecha_desde = new { type = "string", description = "Inicio yyyy-MM-dd o alias (esta_semana, este_mes, ultimos_7)." },
                            fecha_hasta = new { type = "string", description = "Fin yyyy-MM-dd." },
                            periodo = new { type = "string", description = "Alias de rango: hoy, esta_semana, semana_pasada, este_mes, mes_pasado, ultimos_7, ultimos_30." },
                            id_voluntaria = new { type = "integer", description = "Si viene, solo fichajes de esa voluntaria." },
                            top = new { type = "integer", description = "Máximo de filas a listar (1-40)." }
                        }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "visitas_bebe",
                    Description = "Visitas familiares de un bebé (historial). Si no tenés id, usá buscar_bebes.",
                    Parameters = SchemaId("id_bebe", "Id del bebé.")
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "buscar_madres",
                    Description = "Busca mamás por nombre, apellido, DNI o id. Devolvés id para ficha_madre.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            texto = new { type = "string", description = "Nombre, apellido, DNI o id de la madre." }
                        },
                        required = new[] { "texto" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "ficha_madre",
                    Description = "Ficha de una mamá: contacto, localidad, hijos y bebés asociados. Si no tenés id, usá buscar_madres.",
                    Parameters = SchemaId("id_madre", "Id de la madre.")
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "catalogos",
                    Description = "Catálogos: salas, tareas asignables, proveedores, estados de bebés o de voluntarias.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            tipo = new
                            {
                                type = "string",
                                description = "salas | tareas | proveedores | estados_bebes | estados_voluntarias"
                            }
                        },
                        required = new[] { "tipo" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "estadisticas_extra",
                    Description = "Estadísticas que no están en el dashboard de abrazos: edades y localidades de mamás, o consumo de insumos.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            tipo = new { type = "string", description = "madres | insumos" }
                        },
                        required = new[] { "tipo" }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "abrazos_en_curso",
                    Description = "Abrazos de HOY ya iniciados y todavía sin finalizar. Para el recuento global de colgados usá también coordinacion_hoy.",
                    Parameters = new { type = "object", properties = new { } }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "asignaciones_hoy_voluntaria",
                    Description = "Asignaciones/abrazos de HOY de una voluntaria. Si no tenés id, usá buscar_voluntarias.",
                    Parameters = SchemaId("id_voluntaria", "Id de la voluntaria.")
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "comparar_periodos",
                    Description = "Compara KPIs (asignaciones, abrazos, visitas) entre dos períodos. Preferí un preset.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            preset = new
                            {
                                type = "string",
                                description = "esta_semana_vs_anterior | este_mes_vs_anterior | ultimos_7_vs_anteriores_7"
                            }
                        }
                    }
                }
            },
            new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = "guia_operativa",
                    Description = "Explica cómo funciona el programa o el sistema (ciclo de abrazo, asistencia, visitas, stock, estados). Usala para 'cómo se hace / qué significa'.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            tema = new
                            {
                                type = "string",
                                description = "general | abrazos | asistencias | visitas | insumos | estados | asignaciones | cobertura"
                            }
                        }
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
                    periodo = new { type = "string", description = "Alias: hoy, ayer, esta_semana, semana_pasada, este_mes, mes_pasado, ultimos_7, ultimos_30." },
                    top = new { type = "integer", description = "Solo ranking: cantidad de voluntarias (default 10)." }
                }
            };

        private static object SchemaId(string name, string description) =>
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    [name] = new { type = "integer", description }
                },
                required = new[] { name }
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
                    "ficha_bebe" => Json(FichaBebe(args)),
                    "ficha_voluntaria" => Json(FichaVoluntaria(args)),
                    "listar_bebes" => Json(ListarBebes(args)),
                    "listar_voluntarias" => Json(ListarVoluntarias(args)),
                    "stock_insumos" => Json(StockInsumos(LeerString(args, "texto"))),
                    "movimientos_stock" => Json(MovimientosStock(Desde(args), Hasta(args))),
                    "asistencias_periodo" => Json(AsistenciasPeriodo(args)),
                    "visitas_bebe" => Json(VisitasBebe(args)),
                    "buscar_madres" => Json(BuscarMadres(LeerString(args, "texto"))),
                    "ficha_madre" => Json(FichaMadre(args)),
                    "catalogos" => Json(Catalogos(LeerString(args, "tipo"))),
                    "estadisticas_extra" => Json(EstadisticasExtra(LeerString(args, "tipo"))),
                    "abrazos_en_curso" => Json(AbrazosEnCursoHoy()),
                    "asignaciones_hoy_voluntaria" => Json(AsignacionesHoyVoluntaria(args)),
                    "comparar_periodos" => Json(CompararPeriodos(args)),
                    "guia_operativa" => Json(GuiaOperativa(LeerString(args, "tema"))),
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
                    var porDni = (negVoluntaria.listarVoluntarias() ?? new List<VOLUNTARIA>())
                        .Where(v => v.Dni == id)
                        .Take(15)
                        .Select(MapearVoluntariaBusqueda)
                        .Cast<object>()
                        .ToList();
                    if (porDni.Count > 0)
                        return porDni;
                    return new List<object> { new { error = "No hay voluntaria con ese id o DNI." } };
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
                    var porDni = (negBebes.listarBebes() ?? new List<BEBE>())
                        .Where(b => b.Dni == id)
                        .Take(15)
                        .Select(MapearBebeBusqueda)
                        .Cast<object>()
                        .ToList();
                    if (porDni.Count > 0)
                        return porDni;
                    return new List<object> { new { error = "No hay bebé con ese id o DNI." } };
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
                sexo = b.Sexo,
                sala = b.Sala?.Nombre,
                estado = b.Estado?.nombre,
                fechaIngresoNeo = b.FechaIngresoNEO,
                fechaSalida = b.FechaSalida
            };

        private object FichaBebe(JsonDocument args)
        {
            var idBebe = LeerInt(args, "id_bebe") ?? LeerInt(args, "idBebe");
            if (idBebe is null or <= 0)
                return new { error = "Falta id_bebe. Buscá primero con buscar_bebes." };

            var b = negBebes.consultarBebe(idBebe.Value);
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            int? edadDias = b.FechaNacimiento.HasValue
                ? Math.Max(0, hoy.DayNumber - DateOnly.FromDateTime(b.FechaNacimiento.Value.Date).DayNumber)
                : null;
            int? permanenciaDias = b.FechaIngresoNEO.HasValue
                ? NegConversorFecha.DiasDesdeFechaCalendarioHastaHoyArgentina(b.FechaIngresoNEO.Value)
                : null;

            return new
            {
                criterio = "ficha_bebe",
                aclaracion = "Pesos en gramos. Buscar_bebes no alcanza: esta es la ficha completa.",
                idBebe = b.ID,
                b.Dni,
                nombre = b.nombre,
                apellido = b.apellido,
                sexo = b.Sexo,
                fechaNacimiento = b.FechaNacimiento,
                edadDias,
                lugarNacimiento = b.LugarNacimiento,
                fechaIngresoNeo = b.FechaIngresoNEO,
                permanenciaDias,
                fechaSalida = b.FechaSalida,
                pesoNacimientoGramos = b.PesoNacimiento,
                pesoIngresoNeoGramos = b.PesoIngresoNEO,
                pesoDiaAbrazosGramos = b.PesoDiaAbrazos,
                pesoAltaGramos = b.PesoAlta,
                diagnosticoIngreso = b.DiagnosticoIngreso,
                diagnosticoEgreso = b.DiagnosticoEgreso,
                sala = b.Sala?.Nombre,
                estado = b.Estado?.nombre,
                localidad = b.LocalidadDetalle?.nombre,
                madre = b.Madre == null
                    ? null
                    : new
                    {
                        idMadre = b.Madre.IdMadre,
                        nombre = b.Madre.Nombre,
                        apellido = b.Madre.Apellido,
                        celular = b.Madre.Celular
                    }
            };
        }

        private object FichaVoluntaria(JsonDocument args)
        {
            var id = LeerInt(args, "id_voluntaria") ?? LeerInt(args, "idVoluntaria");
            if (id is null or <= 0)
                return new { error = "Falta id_voluntaria. Buscá primero con buscar_voluntarias." };

            var d = negVoluntaria.consultarVoluntariaDetalle(id.Value);
            return new
            {
                criterio = "ficha_voluntaria",
                d.IdVoluntaria,
                d.Dni,
                d.Nombre,
                d.Apellido,
                d.Mail,
                d.Celular,
                d.FechaInicio,
                d.FechaFin,
                estado = negVoluntaria.consultarVoluntaria(id.Value).Estado?.nombre,
                rol = d.Rol,
                horarios = d.Horarios.Select(h => new
                {
                    h.Dia,
                    h.Turno,
                    horaIngreso = h.HoraIngreso.ToString(@"hh\:mm"),
                    horaSalida = h.HoraSalida.ToString(@"hh\:mm"),
                    h.Activa
                }).ToList()
            };
        }

        private object ListarBebes(JsonDocument args)
        {
            var estado = (LeerString(args, "filtro_estado") ?? LeerString(args, "filtroEstado") ?? "").Trim();
            var sala = (LeerString(args, "filtro_sala") ?? LeerString(args, "filtroSala") ?? "").Trim();
            var texto = (LeerString(args, "texto") ?? "").Trim();

            IEnumerable<BEBE> q = negBebes.listarBebes() ?? new List<BEBE>();
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var e = estado.ToLowerInvariant();
                q = q.Where(b => (b.Estado?.nombre ?? "").ToLowerInvariant().Contains(e));
            }
            if (!string.IsNullOrWhiteSpace(sala))
            {
                var s = sala.ToLowerInvariant();
                q = q.Where(b => (b.Sala?.Nombre ?? "").ToLowerInvariant().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.ToLowerInvariant();
                q = q.Where(b =>
                    (b.nombre ?? "").ToLowerInvariant().Contains(t)
                    || (b.apellido ?? "").ToLowerInvariant().Contains(t)
                    || $"{b.nombre} {b.apellido}".ToLowerInvariant().Contains(t));
            }

            var items = q
                .OrderBy(b => b.apellido)
                .ThenBy(b => b.nombre)
                .Take(MaxItemsLista)
                .Select(MapearBebeBusqueda)
                .ToList();

            return new
            {
                criterio = "bebes_activos",
                aclaracion = "Bebés sin fecha de salida y no eliminados. Máximo 40.",
                filtroEstado = string.IsNullOrWhiteSpace(estado) ? null : estado,
                filtroSala = string.IsNullOrWhiteSpace(sala) ? null : sala,
                total = items.Count,
                bebes = items
            };
        }

        private object ListarVoluntarias(JsonDocument args)
        {
            var estado = (LeerString(args, "filtro_estado") ?? LeerString(args, "filtroEstado") ?? "").Trim();
            var texto = (LeerString(args, "texto") ?? "").Trim();

            IEnumerable<VOLUNTARIA> q = negVoluntaria.listarVoluntarias() ?? new List<VOLUNTARIA>();
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var e = estado.ToLowerInvariant();
                q = q.Where(v => (v.Estado?.nombre ?? "").ToLowerInvariant().Contains(e));
            }
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.ToLowerInvariant();
                q = q.Where(v =>
                    (v.Nombre ?? "").ToLowerInvariant().Contains(t)
                    || (v.Apellido ?? "").ToLowerInvariant().Contains(t)
                    || $"{v.Nombre} {v.Apellido}".ToLowerInvariant().Contains(t));
            }

            var items = q
                .OrderBy(v => v.Apellido)
                .ThenBy(v => v.Nombre)
                .Take(MaxItemsLista)
                .Select(MapearVoluntariaBusqueda)
                .ToList();

            return new
            {
                criterio = "voluntarias_vigentes",
                aclaracion = "Voluntarias no eliminadas. Máximo 40.",
                filtroEstado = string.IsNullOrWhiteSpace(estado) ? null : estado,
                total = items.Count,
                voluntarias = items
            };
        }

        private object StockInsumos(string? texto)
        {
            texto = texto?.Trim() ?? "";
            IEnumerable<INSUMO> q = negInsumos.obtenerInsumos() ?? new List<INSUMO>();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.ToLowerInvariant();
                q = q.Where(i =>
                    (i.nombre ?? "").ToLowerInvariant().Contains(t)
                    || (i.descripcion ?? "").ToLowerInvariant().Contains(t));
            }

            var items = q
                .OrderBy(i => i.nombre)
                .Take(MaxItemsLista)
                .Select(i => new
                {
                    i.idInsumo,
                    i.nombre,
                    i.descripcion,
                    i.stockActual,
                    i.stockMinimo,
                    i.stockMaximo,
                    bajoMinimo = i.stockActual <= i.stockMinimo,
                    estado = i.Estado?.nombre
                })
                .ToList();

            return new
            {
                criterio = "stock_insumos",
                aclaracion = "Stock operativo. Para el recorte de solo bajo mínimo usá insumos_bajo_stock.",
                filtro = string.IsNullOrWhiteSpace(texto) ? null : texto,
                total = items.Count,
                bajoMinimo = items.Count(i => i.bajoMinimo),
                insumos = items
            };
        }

        private object MovimientosStock(DateTime desde, DateTime hasta)
        {
            var lista = negInsumos.obtenerMovimientos(new RequestMovimiento
            {
                fechaDesde = desde.Date,
                fechaHasta = hasta.Date.AddDays(1).AddTicks(-1)
            }) ?? new List<ConsultaMovimiento>();

            var items = lista
                .OrderByDescending(m => m.fechaMovimiento)
                .Take(MaxItemsLista)
                .Select(m => new
                {
                    m.idMovimiento,
                    m.nombreInsumo,
                    m.nombreMovimiento,
                    m.esEntrada,
                    m.cantidad,
                    m.fechaMovimiento,
                    m.nombreVoluntaria,
                    nombreBebe = $"{m.nombreBebe} {m.apellidoBebe}".Trim(),
                    m.nombreProveedor,
                    m.observacion
                })
                .ToList();

            return new
            {
                criterio = "movimientos_stock",
                fechaInicio = DateOnly.FromDateTime(desde.Date),
                fechaFin = DateOnly.FromDateTime(hasta.Date),
                total = lista.Count,
                muestra = items
            };
        }

        private object AsistenciasPeriodo(JsonDocument args)
        {
            var desde = Desde(args);
            var hasta = Hasta(args);
            var idVol = LeerInt(args, "id_voluntaria") ?? LeerInt(args, "idVoluntaria");
            var top = TopLista(args);

            var reporte = negAsistencia.ReporteAsistenciaPorPeriodo(desde, hasta);
            var registros = reporte.Registros ?? new List<ReporteAsistenciaPeriodoItem>();
            if (idVol is > 0)
                registros = registros.Where(r => r.IdVoluntaria == idVol).ToList();

            var porVoluntaria = registros
                .GroupBy(r => new { r.IdVoluntaria, r.NombreVoluntaria, r.ApellidoVoluntaria })
                .Select(g => new
                {
                    idVoluntaria = g.Key.IdVoluntaria,
                    nombre = $"{g.Key.NombreVoluntaria} {g.Key.ApellidoVoluntaria}".Trim(),
                    cantidad = g.Count()
                })
                .OrderByDescending(x => x.cantidad)
                .ThenBy(x => x.nombre)
                .Take(15)
                .ToList();

            return new
            {
                criterio = "fichajes_asistencia_periodo",
                aclaracion = "Fichajes de ingreso/salida. No son abrazos. Para HOY preferí asistencias_hoy.",
                reporte.FechaInicio,
                reporte.FechaFin,
                idVoluntariaFiltro = idVol,
                totalFichajes = registros.Count,
                voluntariasDistintas = registros.Select(r => r.IdVoluntaria).Distinct().Count(),
                porVoluntaria,
                muestra = registros
                    .OrderByDescending(r => r.FechaHoraIngreso)
                    .Take(top)
                    .Select(r => new
                    {
                        r.IdAsistencia,
                        r.IdVoluntaria,
                        nombre = $"{r.NombreVoluntaria} {r.ApellidoVoluntaria}".Trim(),
                        r.FechaHoraIngreso,
                        r.FechaHoraSalida,
                        r.DuracionMinutos,
                        r.EstadoAsistencia
                    })
                    .ToList()
            };
        }

        private object VisitasBebe(JsonDocument args)
        {
            var idBebe = LeerInt(args, "id_bebe") ?? LeerInt(args, "idBebe");
            if (idBebe is null or <= 0)
                return new { error = "Falta id_bebe. Buscá primero con buscar_bebes." };

            var bebe = negBebes.consultarBebe(idBebe.Value);
            var lista = (negVisitas.listarVisitasPorBebe(idBebe.Value) ?? new List<VisitaListado>())
                .Where(v => v.activa)
                .OrderByDescending(v => v.fechaHoraVisita)
                .ToList();

            return new
            {
                criterio = "visitas_de_un_bebe",
                idBebe = bebe.ID,
                nombreBebe = $"{bebe.nombre} {bebe.apellido}".Trim(),
                total = lista.Count,
                visitas = lista.Take(MaxItemsLista).Select(v => new
                {
                    v.idVisita,
                    v.nombreVisitante,
                    v.familiar,
                    v.fechaHoraVisita,
                    v.observacion
                }).ToList()
            };
        }

        private List<object> BuscarMadres(string? texto)
        {
            texto = texto?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(texto))
                return new List<object> { new { error = "Indique nombre, apellido, DNI o id." } };

            if (int.TryParse(texto, out var id) && id > 0)
            {
                try
                {
                    var una = negMadres.consultarMadre(id);
                    return new List<object> { MapearMadreBusqueda(una) };
                }
                catch (NotFoundException)
                {
                    var porDni = (negMadres.listarMadres() ?? new List<MADRE>())
                        .Where(m => m.Dni == id)
                        .Take(15)
                        .Select(MapearMadreBusqueda)
                        .Cast<object>()
                        .ToList();
                    if (porDni.Count > 0)
                        return porDni;
                    return new List<object> { new { error = "No hay madre con ese id o DNI." } };
                }
            }

            var q = texto.ToLowerInvariant();
            return (negMadres.listarMadres() ?? new List<MADRE>())
                .Where(m =>
                    (m.Nombre ?? "").ToLowerInvariant().Contains(q)
                    || (m.Apellido ?? "").ToLowerInvariant().Contains(q)
                    || $"{m.Nombre} {m.Apellido}".ToLowerInvariant().Contains(q))
                .Take(15)
                .Select(MapearMadreBusqueda)
                .Cast<object>()
                .ToList();
        }

        private static object MapearMadreBusqueda(MADRE m) =>
            new
            {
                idMadre = m.IdMadre,
                nombre = m.Nombre,
                apellido = m.Apellido,
                dni = m.Dni,
                localidad = m.LocalidadDetalle?.nombre,
                celular = m.Celular,
                cantidadHijos = m.CantidadHijos
            };

        private object FichaMadre(JsonDocument args)
        {
            var id = LeerInt(args, "id_madre") ?? LeerInt(args, "idMadre");
            if (id is null or <= 0)
                return new { error = "Falta id_madre. Buscá primero con buscar_madres." };

            var m = negMadres.consultarMadre(id.Value);
            var estadoCivil = (negGenericos.obtenerEstadosCiviles() ?? new List<EstadoCivilItem>())
                .FirstOrDefault(e => e.id == m.EstadoCivil)?.nombre;

            return new
            {
                criterio = "ficha_madre",
                idMadre = m.IdMadre,
                m.Nombre,
                m.Apellido,
                m.Dni,
                fechaNacimiento = m.FechaNacimiento,
                localidad = m.LocalidadDetalle?.nombre,
                estadoCivil,
                m.CantidadHijos,
                m.Celular,
                motivoAbrazo = m.MotivoAbrazo,
                estado = m.EstadoDetalle?.nombre,
                bebes = (m.Bebe ?? new List<BEBE>()).Select(b => new
                {
                    idBebe = b.ID,
                    nombre = b.nombre,
                    apellido = b.apellido,
                    sala = b.Sala?.Nombre,
                    estado = b.Estado?.nombre
                }).ToList()
            };
        }

        private object Catalogos(string? tipo)
        {
            tipo = (tipo ?? "").Trim().ToLowerInvariant();
            return tipo switch
            {
                "salas" => new
                {
                    tipo = "salas",
                    items = (negSalas.listarSalas() ?? new List<SALA>()).Select(s => new
                    {
                        s.IdSala,
                        s.Nombre,
                        s.Activa
                    }).ToList()
                },
                "tareas" => new
                {
                    tipo = "tareas",
                    aclaracion = "Tareas de catálogo vigentes. Las disponibles para asignar son las activas.",
                    items = (negTareas.listarTareas() ?? new List<TAREA>()).Select(t => new
                    {
                        t.idTarea,
                        t.nombre,
                        vigente = t.Estado,
                        t.esUnica
                    }).ToList()
                },
                "proveedores" => new
                {
                    tipo = "proveedores",
                    items = (negProveedores.listarProveedores() ?? new List<PROVEEDOR>()).Select(p => new
                    {
                        p.idProveedor,
                        p.nombre,
                        p.descripcion,
                        p.Activa
                    }).ToList()
                },
                "estados_bebes" => new
                {
                    tipo = "estados_bebes",
                    items = (negBebes.listarEstadosBebes() ?? new List<ESTADO>()).Select(e => new
                    {
                        e.idEstado,
                        e.nombre,
                        e.descripcion
                    }).ToList()
                },
                "estados_voluntarias" => new
                {
                    tipo = "estados_voluntarias",
                    items = (negVoluntaria.devolverEstadosVoluntarias() ?? new List<ESTADO>()).Select(e => new
                    {
                        e.idEstado,
                        e.nombre,
                        e.descripcion
                    }).ToList()
                },
                _ => new { error = "tipo debe ser: salas, tareas, proveedores, estados_bebes o estados_voluntarias." }
            };
        }

        private object EstadisticasExtra(string? tipo)
        {
            tipo = (tipo ?? "").Trim().ToLowerInvariant();
            if (tipo is "madres" or "mama" or "mamás" or "mamas")
            {
                var edades = negMadres.devolverEstadisticasEdadesMadres() ?? new List<EstadisticaEdadesMadres>();
                var localidades = negMadres.devolverEstadisticasLocalidades() ?? new List<EstadisticaLocalidades>();
                return new
                {
                    tipo = "madres",
                    totalMadresEdad = edades.Sum(e => e.CantidadMadres),
                    edades = edades.OrderBy(e => e.Edad).ToList(),
                    localidades = localidades.OrderByDescending(l => l.CantidadMadres).ToList()
                };
            }

            if (tipo is "insumos" or "consumo" or "stock")
            {
                return new
                {
                    tipo = "insumos",
                    aclaracion = "Consumo/uso agregado de insumos (estadística del sistema).",
                    items = negInsumos.obtenerEstadisticaInsumo() ?? new List<EstadisticaInsumo>()
                };
            }

            return new { error = "tipo debe ser: madres o insumos." };
        }

        private object AbrazosEnCursoHoy()
        {
            if (dniSolicitanteActual is null)
                return new { error = "No se pudo identificar a la coordinadora." };

            var lista = negAsignacion.listarAsignacionesHoy(dniSolicitanteActual.Value)
                ?? new List<RespuestaAsignaciones>();
            var enCurso = lista
                .Where(a => a.fechaHoraInicio.HasValue && !a.fechaHoraFin.HasValue)
                .Select(MapearAsignacionResumen)
                .ToList();
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);

            return new
            {
                criterio = "abrazos_en_curso_hoy",
                aclaracion = "Asignaciones de HOY con inicio y sin fin. El recuento de colgados de coordinacion_hoy también incluye días anteriores.",
                fecha = hoy,
                total = enCurso.Count,
                asignaciones = enCurso
            };
        }

        private object AsignacionesHoyVoluntaria(JsonDocument args)
        {
            var id = LeerInt(args, "id_voluntaria") ?? LeerInt(args, "idVoluntaria");
            if (id is null or <= 0)
                return new { error = "Falta id_voluntaria. Buscá primero con buscar_voluntarias." };

            var lista = negAsignacion.listarAsignacionesHoyVoluntaria(id.Value)
                ?? new List<RespuestaAsignaciones>();
            var hoy = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            return new
            {
                criterio = "asignaciones_hoy_de_una_voluntaria",
                fecha = hoy,
                idVoluntaria = id.Value,
                total = lista.Count,
                asignaciones = lista.Select(MapearAsignacionResumen).ToList()
            };
        }

        private static object MapearAsignacionResumen(RespuestaAsignaciones a) =>
            new
            {
                a.idAsignacion,
                a.idBebe,
                a.nombreBebe,
                a.idTarea,
                a.nombreTarea,
                a.idVoluntaria,
                a.nombreVoluntaria,
                a.nombreSala,
                a.estadoAsignacion,
                a.fechaHoraAsignacion,
                a.fechaHoraInicio,
                a.fechaHoraFin,
                a.comentario
            };

        private object CompararPeriodos(JsonDocument args)
        {
            var preset = (LeerString(args, "preset") ?? "esta_semana_vs_anterior").Trim().ToLowerInvariant();
            var hoyAr = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
            DateTime aDesde, aHasta, bDesde, bHasta;
            string etiquetaA;
            string etiquetaB;

            switch (preset.Replace(' ', '_'))
            {
                case "este_mes_vs_anterior":
                    aDesde = new DateTime(hoyAr.Year, hoyAr.Month, 1);
                    aHasta = hoyAr.Date;
                    bHasta = aDesde.AddDays(-1);
                    bDesde = new DateTime(bHasta.Year, bHasta.Month, 1);
                    etiquetaA = "este mes (hasta hoy)";
                    etiquetaB = "mes anterior (completo)";
                    break;
                case "ultimos_7_vs_anteriores_7":
                    aDesde = hoyAr.Date.AddDays(-6);
                    aHasta = hoyAr.Date;
                    bHasta = aDesde.AddDays(-1);
                    bDesde = bHasta.AddDays(-6);
                    etiquetaA = "últimos 7 días";
                    etiquetaB = "7 días previos";
                    break;
                default:
                    var inicioSemana = InicioSemanaLunes(hoyAr.Date);
                    aDesde = inicioSemana;
                    aHasta = hoyAr.Date;
                    bHasta = inicioSemana.AddDays(-1);
                    bDesde = InicioSemanaLunes(bHasta);
                    etiquetaA = "esta semana (lun-hoy)";
                    etiquetaB = "semana anterior";
                    preset = "esta_semana_vs_anterior";
                    break;
            }

            var actual = negDashboard.ObtenerResumen(aDesde, aHasta);
            var previo = negDashboard.ObtenerResumen(bDesde, bHasta);
            return new
            {
                criterio = "comparar_periodos",
                preset,
                actual = MapearResumenComparado(etiquetaA, actual),
                previo = MapearResumenComparado(etiquetaB, previo)
            };
        }

        private static object MapearResumenComparado(string etiqueta, DashboardResumenRespuesta r) =>
            new
            {
                etiqueta,
                r.FechaInicio,
                r.FechaFin,
                r.AsignacionesEnPeriodo,
                r.AbrazosFinalizadosEnPeriodo,
                r.VisitasEnPeriodo,
                r.BebesActivos,
                r.BebesDisponiblesAbrazo,
                r.PromedioDuracionAbrazoMinutos,
                r.PromedioPermanenciaDias
            };

        private static DateTime InicioSemanaLunes(DateTime dia)
        {
            var diff = ((int)dia.DayOfWeek + 6) % 7;
            return dia.Date.AddDays(-diff);
        }

        private static object GuiaOperativa(string? tema)
        {
            tema = (tema ?? "general").Trim().ToLowerInvariant();
            var textos = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["general"] = new
                {
                    resumen = "Resimamis coordina abrazos de voluntarias a bebés en neonatología, más fichajes de asistencia, visitas de familiares e insumos.",
                    quePuedeConsultar = "Día de hoy, cobertura, rankings, fichas de bebé/voluntaria/mamá, stock, visitas, comparativos y cómo funciona cada flujo.",
                    unicaEscritura = "Solo generar asignaciones de abrazo del día, con confirmación explícita de la coordinadora."
                },
                ["abrazos"] = new
                {
                    ciclo = "Crear asignación (bebé Asignado, voluntaria Asignada) → iniciar abrazo (bebé Abrazado, voluntaria Abrazando) → finalizar (bebé Sin abrazar, voluntaria Disponible). La asignación queda Creada; el ciclo se marca con fechaHoraInicio y fechaHoraFin.",
                    generarAuto = "generar empareja bebés disponibles hoy con voluntarias libres (con asistencia hoy). El asistente solo lo hace si la coordinadora confirma.",
                    colgados = "Abrazo colgado: iniciado y sin finalizar. coordinacion_hoy cuenta todos; abrazos_en_curso lista los de hoy."
                },
                ["asistencias"] = new
                {
                    queEs = "Fichaje de ingreso y salida de la voluntaria al hospital. No es un abrazo.",
                    hoy = "asistencias_hoy lista quién fichó hoy. ranking_asistencias ordena por cantidad de fichajes."
                },
                ["visitas"] = new
                {
                    queEs = "Familiar que visita al bebé. No es abrazo ni asistencia.",
                    hoy = "visitas_hoy para el día. visitas_bebe para el historial de un bebé. visitas_periodo para un rango."
                },
                ["insumos"] = new
                {
                    stock = "stock_insumos muestra actual/mínimo/máximo. insumos_bajo_stock solo los que están en o bajo el mínimo.",
                    movimientos = "movimientos_stock lista entradas y salidas del depósito en un período."
                },
                ["estados"] = new
                {
                    bebes = "Sin abrazar (libre para abrazo) → Asignado (tiene asignación creada) → Abrazado (abrazo iniciado). Al finalizar vuelve a Sin abrazar. Eliminado es baja lógica.",
                    voluntarias = "Disponible/Activa (libre, con asistencia) → Asignada → Abrazando (abrazo con bebé). Tareas de catálogo pueden pasar a Ayudando.",
                    asignaciones = "Creada al generar. Eliminado es baja lógica. Iniciar/finalizar NO cambian el estado de la asignación, solo las horas."
                },
                ["asignaciones"] = new
                {
                    hoy = "asignaciones_hoy lista las del día (bebé, voluntaria, sala, estado, inicio/fin).",
                    porVoluntaria = "asignaciones_hoy_voluntaria o abrazos_voluntaria para el historial."
                },
                ["cobertura"] = new
                {
                    queEs = "Porcentaje de bebés activos con al menos un abrazo FINALIZADO hoy, más la lista de quienes todavía no lo recibieron.",
                    tool = "cobertura_hoy"
                }
            };

            if (textos.TryGetValue(tema, out var elegido))
                return new { tema, contenido = elegido, extra = textos["general"] };

            return new { tema = "general", contenido = textos["general"], temasDisponibles = textos.Keys };
        }

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
            var periodo = LeerString(args, "periodo");
            var raw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var rawFin = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            if (string.IsNullOrWhiteSpace(periodo) && string.IsNullOrWhiteSpace(raw) && string.IsNullOrWhiteSpace(rawFin))
                return null;
            return ResolverRango(args).Inicio;
        }

        private static DateTime? HastaOpcional(JsonDocument args)
        {
            var periodo = LeerString(args, "periodo");
            var raw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var rawFin = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            if (string.IsNullOrWhiteSpace(periodo) && string.IsNullOrWhiteSpace(raw) && string.IsNullOrWhiteSpace(rawFin))
                return null;
            return ResolverRango(args).Fin;
        }

        private static (DateTime Inicio, DateTime Fin) ResolverRango(JsonDocument args, int diasDefault = 29)
        {
            var desdeRaw = LeerString(args, "fecha_desde") ?? LeerString(args, "fechaDesde");
            var hastaRaw = LeerString(args, "fecha_hasta") ?? LeerString(args, "fechaHasta");
            var periodoRaw = LeerString(args, "periodo");
            var hoyAr = NegConversorFecha.FechaCalendarioArgentina(DateTime.UtcNow);
            var hoy = hoyAr.ToDateTime(TimeOnly.MinValue);

            if (TryRangoNombrado(periodoRaw, hoy, out var porPeriodo))
                return porPeriodo;
            if (TryRangoNombrado(desdeRaw, hoy, out var porDesde) && string.IsNullOrWhiteSpace(hastaRaw))
                return porDesde;

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

        private static bool TryRangoNombrado(string? raw, DateTime hoyAr, out (DateTime Inicio, DateTime Fin) rango)
        {
            rango = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var t = raw.Trim().ToLowerInvariant()
                .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u')
                .Replace(" ", "_");

            switch (t)
            {
                case "hoy":
                case "today":
                case "ahora":
                    rango = (hoyAr.Date, hoyAr.Date);
                    return true;
                case "ayer":
                case "yesterday":
                    rango = (hoyAr.Date.AddDays(-1), hoyAr.Date.AddDays(-1));
                    return true;
                case "esta_semana":
                case "semana":
                case "semana_actual":
                {
                    var inicio = InicioSemanaLunes(hoyAr.Date);
                    rango = (inicio, hoyAr.Date);
                    return true;
                }
                case "semana_pasada":
                case "la_semana_pasada":
                {
                    var inicioEsta = InicioSemanaLunes(hoyAr.Date);
                    rango = (inicioEsta.AddDays(-7), inicioEsta.AddDays(-1));
                    return true;
                }
                case "este_mes":
                case "mes":
                case "mes_actual":
                    rango = (new DateTime(hoyAr.Year, hoyAr.Month, 1), hoyAr.Date);
                    return true;
                case "mes_pasado":
                case "el_mes_pasado":
                {
                    var inicioMes = new DateTime(hoyAr.Year, hoyAr.Month, 1);
                    var finAnt = inicioMes.AddDays(-1);
                    rango = (new DateTime(finAnt.Year, finAnt.Month, 1), finAnt);
                    return true;
                }
                case "ultimos_7":
                case "ultimos_7_dias":
                case "ultima_semana":
                    rango = (hoyAr.Date.AddDays(-6), hoyAr.Date);
                    return true;
                case "ultimos_15":
                case "ultimos_15_dias":
                    rango = (hoyAr.Date.AddDays(-14), hoyAr.Date);
                    return true;
                case "ultimos_30":
                case "ultimos_30_dias":
                case "ultimo_mes":
                    rango = (hoyAr.Date.AddDays(-29), hoyAr.Date);
                    return true;
                default:
                    return false;
            }
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

        private static int TopLista(JsonDocument args)
        {
            var top = LeerInt(args, "top") ?? MaxItemsLista;
            if (top < 1) top = 1;
            if (top > MaxItemsLista) top = MaxItemsLista;
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
