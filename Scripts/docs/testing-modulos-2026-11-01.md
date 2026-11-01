# Testing por módulos — 2026-11-01

Ciclo completo contra Render (`resimamis.onrender.com`) desde el front (`resimamis-tesis.vercel.app`),
con sesión de coordinadora. Datos de prueba con apellido "QA" (madre DNI 39990010, voluntaria DNI 39990020,
"Insumo Test QA"), dados de baja al final.

Resultado: ✅ ok · ❌ falló (bug corregido en el commit indicado) · ⚠️ observación para el front

## M1 Madres
| Caso | Resultado |
|---|---|
| Alta de madre con un bebé | ✅ |
| Buscar madre por DNI (`GET /Madre/dni/{dni}`) | ✅ |
| Perfil de madre (`GET /Madre/id/{id}`) | ✅ |
| Editar madre enviando la ficha completa (como el front) | ✅ |
| Editar madre enviando solo el campo cambiado | ⚠️ 400 "Error de validación": el PUT exige la ficha completa |
| Alta con datos inválidos (DNI corto, fecha futura, sin bebé) | ✅ rechazada con mensaje |
| Listado de madres | ✅ |

## M2 Bebés
| Caso | Resultado |
|---|---|
| Alta de bebé asociado a madre existente (hermano) | ✅ |
| Ficha del bebé | ✅ |
| Editar bebé (peso del día de abrazo, diagnóstico) | ✅ |
| Alta inválida (sin nombre, sexo inválido, fecha futura) | ✅ rechazada |
| Bebé nuevo aparece como disponible para abrazo | ✅ |
| Estados de bebé | ✅ |

## Visitas
| Caso | Resultado |
|---|---|
| Registrar visita | ✅ |
| Ver visitas del bebé | ✅ |
| Editar visita | ✅ |
| Estadística de visitas filtrada por bebé | ✅ |
| Visita inválida (sin visitante, fecha futura) | ✅ rechazada |

## M3 Voluntarias
| Caso | Resultado |
|---|---|
| Alta con horario (lunes mañana) | ✅ |
| Detalle con horarios | ✅ |
| Editar voluntaria | ✅ |
| DNI repetido | ✅ 409 |
| Horarios y estados | ✅ |
| Voluntaria nueva ("Creada") asignable después de marcar entrada | ❌ → corregido en `79747a2`; verificado: Creada → Disponible al entrar y se le puede asignar |

## M4 Usuarios
| Caso | Resultado |
|---|---|
| Listado de usuarios (sin contraseñas expuestas) | ✅ |
| Voluntarias sin usuario | ✅ |
| Detalle de usuario | ✅ |
| Login con clave incorrecta | ✅ rechazado |
| Crear usuario con contraseña y editarlo | Pendiente: hacerlo a mano (Coordinación → Usuarios). Claude no crea cuentas con clave en producción. |

## M5 Insumos
| Caso | Resultado |
|---|---|
| Alta de insumo / nombre repetido rechazado | ✅ |
| Editar insumo | ✅ |
| Movimiento de entrada (suma) y de salida (resta) | ✅ |
| Salida mayor al stock / entrada que supera el máximo | ✅ rechazadas |
| Aparece en "bajo stock mínimo" | ✅ |
| Consulta y detalle de movimientos | ✅ |
| Estadística de insumos y proveedores | ✅ |

## M6 Asistencia
| Caso | Resultado |
|---|---|
| Registrar entrada / segunda entrada sin salida rechazada | ✅ |
| Asistencias de hoy, histórico, reporte por período | ✅ |
| Registrar salida y nueva entrada | ✅ |
| Con entrada registrada figura como libre | ✅ |

## M7 Asignaciones
| Caso | Resultado |
|---|---|
| Asignación individual bebé ↔ voluntaria | ✅ |
| Tarea de catálogo | ✅ |
| Editar asignación con PUT parcial | ❌ → corregido en `79747a2` |
| Eliminar asignación | ✅ |
| Eliminar una asignación abierta libera bebé y voluntaria | ❌ → corregido en `ea6335b`; verificado borrando las 31 automáticas: 31 bebés disponibles otra vez, ninguna voluntaria trabada |
| Generar asignaciones automáticas (31 bebés, 23 voluntarias) | ✅ |
| Generar sin bebés disponibles | ✅ 400 con mensaje |

## M8 Abrazo completo
| Caso | Resultado |
|---|---|
| Iniciar abrazo (bebé Abrazado, voluntaria Abrazando) / iniciar dos veces | ✅ / ✅ 409 |
| Cargar insumo usado en el abrazo (descuenta stock) / más que el stock | ✅ / ✅ rechazado |
| Finalizar con comentario / finalizar dos veces | ✅ / ✅ 409 |
| Consultar: finalizado, comentario y detalle de insumo | ✅ |
| Bebé vuelve a Sin abrazar y voluntaria a Disponible | ✅ |
| Dashboard del bebé y panel del día cuentan el abrazo | ✅ |

## M9 Estadísticas
| Caso | Resultado |
|---|---|
| 24 endpoints de estadísticas y reportes | ✅ todos con datos |
| Pantalla Estadísticas (11 secciones) | ✅ carga |

## M10 Panel de coordinación
| Caso | Resultado |
|---|---|
| Pestañas Asignación, Asistencia, Usuarios, Bajas, Proveedores, Salas | ✅ cargan con datos |
| Botón "Cerrar abrazos sin finalizar" con confirmación | ✅ |
| Texto del diálogo "de días anteriores" | ⚠️ desactualizado: ahora también cierra los en curso de hoy |
| Mensaje tras cerrar: "Operación de asignación ejecutada." | ⚠️ no dice cuántos cerró (el back devuelve la cantidad) |

## Asistente (sesiones anteriores del mismo día)
Más de 50 preguntas, flujo de generar asignaciones con confirmación y control del lado del servidor. Ver commits
`6999146` a `4d869cc`. ⚠️ El front dice "Solo lectura: no modifica nada", pero el asistente puede generar asignaciones.

## Limpieza
Al final se dieron de baja (baja lógica) la madre, los 2 bebés, la visita, las 2 voluntarias y el insumo de
prueba, y se eliminaron las asignaciones de prueba. La base quedó con 29 bebés disponibles y 24 voluntarias libres.
Los registros "QA" siguen visibles en el historial de bajas y en el reporte del día.

## Resumen
Unos 90 casos. Se encontraron y corrigieron 3 bugs del back (`79747a2`, `ea6335b`); el resto pasó.

## Segunda tanda
| Caso | Resultado |
|---|---|
| Proveedores: alta, editar, baja (deja de figurar en activos) | ✅ |
| Proveedor con nombre repetido | ⚠️ rechazado con 400 (en voluntarias y salas el mismo caso da 409) |
| Salas: alta, editar, baja; no deja dar de baja una sala con bebés | ✅ |
| Tareas: alta, editar, aparece en disponibles, baja | ✅ |
| Horarios: reemplazar, quitar uno, día inexistente rechazado | ✅ |
| Bebé: cambiar estado, estado inválido rechazado | ✅ |
| Bebé: egreso por fecha de salida → baja + historial "Egreso del NEO" con responsable | ✅ |
| Bebé egresado no permite cambiar estado | ✅ |
| Seguridad: 10 endpoints sin token o con token falso → 401 | ✅ |
| Texto tipo inyección SQL, nombre de 500 caracteres, HTML en el nombre, DNI no numérico | ✅ rechazados |
| Apellido con apóstrofo (D'Angelo) | ❌ → corregido: se aceptan apóstrofo y guion |
| Dos "Generar" a la vez (doble clic) | ❌ creaba 58 asignaciones para 29 bebés → corregido con lock en Postgres |

## Pendientes del front
1. Texto "Solo lectura" del asistente.
2. Texto del diálogo de "Cerrar abrazos sin finalizar" y mostrar la cantidad cerrada.
3. Crear y editar un usuario con contraseña (prueba manual).
