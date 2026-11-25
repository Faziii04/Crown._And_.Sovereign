# Crown & Sovereign — PMS WPF

Prototipo de escritorio para administración hotelera. La interfaz está construida con WPF nativo y XAML. Los servicios usan una única fuente de muestra en memoria; no hay base de datos ni integraciones externas.

## Ejecutar

Requisitos: Windows y .NET SDK 10.

```powershell
dotnet restore "Crown & Sovereign.slnx"
dotnet run --project "Crown & Sovereign/Crown & Sovereign.csproj"
```

## Acceso de demostración

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `demo123` | Administrador |
| `recepcion` | `demo123` | Recepcionista |
| `servicios` | `demo123` | Encargado de servicios |
| `gerente` | `demo123` | Gerente |

## Organización

- `MainWindow` proporciona shell, barra superior, menú lateral y carga explícita de módulos mediante `Services/ModuleViewFactory.cs`.
- Cada opción permitida carga un principal `UserControl`; las áreas complejas coordinan controles hijos por pestaña o sección.
- Ventanas de creación/edición se mantienen en `Views/Dialogs`, organizadas por área.
- `Services/HotelServices.cs` y `Services/ManagementServices.cs` mantienen reglas y cambios de estado.
- `Views/UserControls/Recepcion/Reservas/` organiza disponibilidad, calendario, bandeja, grupos, lista de espera, asistente y detalle en controles funcionales separados.
- `Views/UserControls/Recepcion/Habitaciones/` organiza el centro de operaciones por resumen, alertas, filtros, tablero por piso y detalle; los diálogos de limpieza, incidencias y reubicación están en `Views/Dialogs/Recepcion/Habitaciones/`.
- `Services/ReservationWorkspaceServices.cs` contiene disponibilidad continua, estimaciones de tarifa y lógica compartida para grupos y lista de espera.
- `Data/SampleData.cs` es la única colección compartida de demostración.
- `Resources` contiene paleta y estilos WPF compartidos.
- `UI_ARCHITECTURE.md` documenta el árbol de vistas, responsabilidades y conteos.

## Cobertura de navegación

Hay 31 opciones de navegación entre los cuatro roles: administrador, recepcionista, encargado de servicios y gerente. Todas tienen una ruta explícita en la fábrica. El proyecto ya no contiene el renderer genérico `WorkspaceControl` ni el diálogo genérico de formulario.

## Límites de esta fase

- Los cambios viven solo durante el proceso y se reinician con los datos demo al cerrar la aplicación.
- No se ha conectado PostgreSQL, Npgsql ni otro almacén remoto.
- La autenticación demo compara contraseñas en texto plano; las cuentas son ilustrativas y no aptas para producción.
- Los contactos de lista de espera solo se registran en memoria; no se envían mensajes. La facturación fiscal, pagos externos, correo, adjuntos y algunos flujos administrativos requieren implementación adicional.
- La disponibilidad de habitaciones, sus estados operativos, solicitudes, incidencias y reubicaciones usan datos temporales en memoria; no se agregaron migraciones ni persistencia para esta fase.
- Verificación de Habitaciones: `dotnet build "Crown & Sovereign.slnx" --no-restore` finalizó con cero advertencias y cero errores. El ejecutable mostró la ventana de acceso, pero esta sesión no validó la interfaz de habitaciones después de iniciar sesión.
