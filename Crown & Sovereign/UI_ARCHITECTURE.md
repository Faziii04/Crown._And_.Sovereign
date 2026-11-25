# Arquitectura de interfaz

## Shell y navegación

```text
App.xaml
└── MainWindow
    ├── Shared/Navigation/SidebarControl
    ├── Shared/Navigation/TopBarControl
    └── ContentControl
        └── ModuleViewFactory → un principal UserControl por opción permitida
```

`MainWindow` no genera páginas ni contiene contenido de negocio. La fábrica hace un mapeo explícito desde nombre de módulo y rol. Si falta una ruta, lanza una excepción; no recurre a una página genérica.

## Árbol funcional

```text
Views/UserControls/
├── Shared/
│   ├── Cards/ (HabitacionCardControl, ServicioCardControl)
│   ├── Navigation/ (SidebarControl, TopBarControl)
│   └── Selectors/ (SelectorHuespedActivoControl)
├── Recepcion/
│   ├── Dashboard/
│   ├── Reservas/
│   │   ├── Disponibilidad/ (búsqueda, matriz por noche, resultados y selector de habitaciones)
│   │   ├── Calendario/ (barras continuas por habitación y navegación de fechas)
│   │   ├── Listado/ (bandeja paginada y acciones operativas)
│   │   ├── Grupos/ (directorio, asignación de habitaciones y huéspedes)
│   │   ├── ListaEspera/ (coincidencias de estancia y registro de contacto)
│   │   ├── Wizard/ (cinco pasos separados para estancia, habitaciones, huéspedes, adicionales y revisión)
│   │   ├── Detalle/ (resumen, habitaciones y huéspedes de la reserva)
│   │   └── Shared/ (métricas operativas)
│   ├── Habitaciones/
│   │   ├── HabitacionesControl (shell de cinco regiones y coordinador)
│   │   ├── Resumen/ (métricas operativas y alertas)
│   │   ├── Filtros/ (búsqueda, filtros rápidos y avanzados)
│   │   ├── Tablero/ (navegación por piso, agrupación, tarjetas compactas/detalladas y vista rápida)
│   │   └── Detalle/ (resumen, disponibilidad, preparación, incidencias, estados y comodidades)
│   ├── Huespedes/ (HuespedesControl → Perfil, Historial)
│   ├── Estancias/ (EstanciasControl → Detalle → Resumen, Folio, Servicios, Consumos, Llaves, Actividad)
│   ├── Caja/ (CajaControl → CajaActual, Pagos, Facturas, Historial)
│   └── Servicios/ (ServiciosRecepcionControl → SolicitarServicio, SolicitudesHuesped)
├── Operaciones/
│   ├── Dashboard/
│   ├── AtencionServicios/ (AtencionServiciosControl → Beneficios, ServiciosDisponibles, Resumen e Historial)
│   ├── AgendaServicios/ (AgendaServiciosControl → AgendaHoy, CalendarioCitas, Solicitudes)
│   ├── CatalogoServicios/
│   ├── Consumos/ (ConsumosControl → CatalogoConsumos, CarritoConsumos)
│   ├── Inventario/ (InventarioControl → CatalogoProductos, Existencias, Movimientos)
│   ├── Limpieza/ (LimpiezaControl → Tablero, ProgresoPisos)
│   ├── Mantenimiento/ (MantenimientoControl → Incidencias, Ordenes, HabitacionesBloqueadas)
│   └── ObjetosOlvidados/
├── Administracion/
│   ├── Dashboard/
│   ├── Empleados/
│   ├── UsuariosRoles/ (UsuariosRolesControl → Usuarios, Roles)
│   ├── Configuracion/
│   ├── Habitaciones/ (reutiliza el tablero compartido de habitaciones)
│   └── Sistema/ (ModulosParametros, Auditoria)
└── Gerencia/
    ├── Dashboard/
    ├── Comercial/ (TarifasTemporadas → Tarifas, Temporadas; PaquetesPromociones → Paquetes, Promociones; Convenios)
    ├── Finanzas/ (FinanzasControl → Resumen, Movimientos, Facturacion)
    └── Supervision/ (OperacionesControl → resúmenes de habitaciones, limpieza y mantenimiento; Reportes; PoliticasHotel)
```

Las ventanas se organizan en `Views/Dialogs/{Recepcion,Operaciones,Administracion,Gerencia,Shared}`. No se utiliza un diálogo de formulario genérico.

## Separación de responsabilidades

- El control principal selecciona pestañas y coordina el contexto de sección.
- Los controles hijos presentan y filtran su sección y elevan eventos hacia su propietario.
- Las reglas de permisos y cambios de estado se ejecutan en Services.
- `SampleData` mantiene una sola instancia de huéspedes, habitaciones, reservas, estancias, caja, servicios, inventario, tareas, incidencias y colecciones de administración/gerencia.
- Los controles de supervisión de Gerencia presentan resúmenes de datos operativos compartidos en modo de solo lectura.
- Los recursos de color y estilos comunes viven en `Resources/Colors.xaml` y `Resources/Controls.xaml`.

## Espacio de Reservas

- `ReservasControl` coordina las cinco vistas del módulo y conserva el contexto al entrar y salir del detalle.
- `HabitacionDisponibilidadService` es la regla compartida para búsqueda, matriz nocturna, selector de habitaciones y lista de espera. La disponibilidad se comprueba por habitación física durante el intervalo completo de la estancia.
- `ReservaService`, `EstanciaService`, `GrupoReservaService` y `ListaEsperaService` gestionan creación, confirmación, check-in/out, asignaciones y registro de contactos sobre `SampleData`.
- El contacto con huéspedes es un registro de demostración; la aplicación no envía mensajes ni correo.
- La nueva interfaz no modifica el esquema remoto ni agrega persistencia. Los cambios se reinician al cerrar el proceso.

## Operaciones de habitaciones

- `HabitacionesControl` conserva la ruta administrativa de alta/edición en su propio panel y carga el centro operativo para Recepción. Las tarjetas compartidas no exponen edición de infraestructura al rol de recepción.
- `HabitacionService` proyecta ocupación, limpieza y mantenimiento como condiciones independientes y deriva la clasificación visible con prioridad para habitaciones bloqueadas.
- El tablero usa `ResumenHabitacionesControl`, `AlertasHabitacionesControl`, `FiltrosHabitacionesControl`, `NavegacionPisosControl`, `TableroHabitacionesControl` y controles especializados de detalle. Las tarjetas compactas y detalladas comparten los mismos datos y acciones.
- Las solicitudes de limpieza, reportes de incidencia, check-in/out, disponibilidad y cambios de habitación reutilizan los Services existentes y las colecciones de `SampleData`. Las solicitudes de limpieza avisan al área de servicios; los reportes y las reubicaciones quedan en memoria y auditoría demo.
- La recepción puede marcar una incidencia que requiere evaluación preventiva; solo el rol de Mantenimiento puede aplicar o retirar el bloqueo formal. Una habitación ocupada y bloqueada se clasifica como bloqueada y muestra la alerta de reubicación.
- El cambio de habitación conserva la estancia y el folio, reasigna huéspedes, cambia la habitación enlazada a la reserva y revoca/reemite las llaves demo asociadas al número de habitación.
- Los tiempos exactos de llegada/salida intradía, historial de reubicaciones, metadatos ampliados de tareas e incidencias y algunas relaciones entre llaves y habitaciones son estado temporal del prototipo; no se agregaron migraciones ni se modificó el esquema PostgreSQL.
- Verificación de esta implementación: compilación WPF en .NET 10 con cero advertencias y cero errores, además de verificaciones de servicios para clasificación, filtros, check-in/out, limpieza, inspección, incidencias, permisos de bloqueo y reubicación. El control se abrió en una ventana de vista previa, pero el escritorio quedó cubierto por una advertencia de seguridad de Windows; no se da por validada la composición visual final ni se interactuó con el acceso.

## Conteo de estructura

- 100 archivos XAML de `UserControl` en total, incluyendo controles principales, hijos y compartidos.
- 29 ventanas XAML, incluyendo acceso y diálogos específicos por flujo.
- 31 opciones de navegación protegidas entre los cuatro roles; las 27 rutas no iniciales están registradas explícitamente, y `Inicio` se resuelve según el rol.
- Renderer genérico eliminado: `WorkspaceControl.xaml`, `WorkspaceControl.xaml.cs`, `WorkspaceViewModel.cs`.
- Diálogo genérico eliminado: `FormDialog.xaml` y su code-behind.

## Compilación

```powershell
dotnet build "Crown & Sovereign.slnx" --no-restore
```

La compilación local se verificó en .NET 10 WPF. Se revisó visualmente el shell de Reservas y el asistente de creación en la cuenta demo de recepción; no se capturó una validación visual de todas las rutas.
