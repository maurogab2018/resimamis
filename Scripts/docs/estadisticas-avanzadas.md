# Estadísticas avanzadas — guía para el front

Endpoints nuevos bajo `api/Estadisticas`, de solo lectura, con el mismo token de siempre. No reemplazan a las
estadísticas de `api/Dashboard`, que siguen igual.

- Respuesta: `{ "data": { ... } }`, como el resto de la API.
- Fechas de entrada: `yyyy-MM-dd` o `dd/MM/yyyy`. Sin fechas: últimos 90 días (cumplimiento: 30).
- Fechas con hora de salida: UTC (convertir a hora local en el front). Fechas sin hora: calendario Argentina.
- "Abrazo real": de bebé, iniciado, finalizado y no cerrado automáticamente.
- Errores de parámetros: 400 con `message` (rango invertido, `dias` fuera de rango).

| # | Endpoint | Qué responde | Gráfico sugerido |
|---|---|---|---|
| 1 | `GET /api/Estadisticas/impacto?fechaDesde&fechaHasta` | Bebés egresados agrupados por abrazos recibidos (`tramos`: 0-5, 6-15, 16+) con ganancia de peso por día y días de internación. `bebes` trae el detalle. Fechas = rango de egreso (opcional) | Barras por tramo: ganancia g/día y días de internación |
| 2 | `GET /api/Estadisticas/primer-abrazo` | Días desde el ingreso a NEO hasta el primer abrazo: `promedioDias`, `medianaDias`, `activosSinPrimerAbrazo`, `porMesDeIngreso`, `bebes` | Tarjetas (promedio, mediana, sin abrazo) + línea por mes de ingreso |
| 3 | `GET /api/Estadisticas/equidad-bebes` | Abrazos por semana de internación de cada bebé activo; `porDebajoDelPromedio` = menos de la mitad del promedio. Ordenado de menos a más | Barras horizontales, resaltando `porDebajoDelPromedio` |
| 4 | `GET /api/Estadisticas/cumplimiento-horario?fechaDesde&fechaHasta` | Por voluntaria con horario: días previstos, días que vino, días extra y `porcentajeCumplimiento`. De menor a mayor | Tabla con barra de porcentaje |
| 5 | `GET /api/Estadisticas/proyeccion-insumos?dias=30` | Por insumo: consumo diario (salidas de los últimos `dias`, de 7 a 365), `diasHastaMinimo`, `diasHastaAgotarse` y `estado` (Bajo mínimo / Crítico ≤7 d / Atención ≤15 d / OK / Sin consumo). Los urgentes primero | Tabla con semáforo por `estado` |
| 6 | `GET /api/Estadisticas/mapa-calor?fechaDesde&fechaHasta` | `celdas` con `idDia` (1 = lunes … 7 = domingo), `hora` (0-23, Argentina), `abrazos` y `asistencias`. Solo las celdas con actividad | Mapa de calor día × hora (con selector abrazos / asistencias) |
| 7 | `GET /api/Estadisticas/tasa-completados?fechaDesde&fechaHasta` | Asignaciones de abrazo: completadas, cerradas automáticamente, nunca iniciadas, en curso y `porcentajeCompletados`; `porVoluntaria` y `porDiaSemana` | Dona del total + barras por voluntaria y por día |
| 8 | `GET /api/Estadisticas/bebes-sin-visitas?dias=7` | Bebés activos sin visita familiar hace al menos `dias` (1 a 365). `nuncaVisitado` si nunca tuvo (cuenta desde el ingreso) | Lista de alerta |
| 9 | `GET /api/Estadisticas/rendimiento-voluntarias?fechaDesde&fechaHasta` | Por voluntaria activa: horas en el hospital, abrazos, minutos abrazando, `abrazosPorHora` y `porcentajeTiempoAbrazando` (null con menos de 1 hora) | Tabla o barras de abrazos por hora |
| 10 | `GET /api/Estadisticas/duracion-por-grupo?fechaDesde&fechaHasta` | Duración promedio y mediana de los abrazos `porSala` y `porPeso` (menos de 1,5 kg / 1,5 a 2,5 kg / 2,5 kg o más / sin peso) | Barras agrupadas promedio vs mediana |

## Notas sobre los datos
- Los pesos están cargados a veces en kg (1.85) y a veces en gramos (2550): el back los normaliza a gramos
  (un valor menor a 20 se toma como kg).
- Parte de los datos de prueba iniciales tiene la hora guardada sin zona horaria, por eso el mapa de calor muestra
  actividad a las 5 de la mañana. Con datos cargados desde el sistema se ve bien.
- Impacto (1) muestra una relación entre abrazos y evolución, no prueba causa; conviene mostrar la aclaración que
  devuelve el endpoint.
