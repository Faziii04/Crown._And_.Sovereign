namespace Crown___Sovereign.Models;

public sealed record SupervisionIndicador(string Titulo, string Valor, string Contexto, string Seccion);
public sealed record SupervisionAlerta(string Nivel, string Entidad, string Titulo, string Detalle, string Consecuencia, DateTime? Fecha, string Seccion, int RegistroId);
public sealed record SupervisionHabitacionFila(int Id, string Numero, int Piso, string Tipo, string Estado, string Ocupacion, string Limpieza, bool BloqueoActivo, string ProximaLlegada, string ProximaSalida, string Preparacion, bool RequiereAtencion, string NotaAtencion, string HuespedesActuales, string ReservaActual, int? ReservaProximaId);
public sealed record SupervisionIngresoFila(int ReservaId, string Huesped, DateTime Llegada, string Habitaciones, string Preparacion, string Condicion, string Prioridad);
public sealed record SupervisionTareaFila(int Id, int HabitacionId, string Habitacion, int Piso, string Tipo, DateTime Programada, string Responsable, string Prioridad, string Estado, string Atencion, string Observaciones);
public sealed record SupervisionCargaFila(string Responsable, int Asignadas, int Pendientes, int EnCurso, int Completadas);
public sealed record SupervisionInspeccionFila(int Id, int TareaId, string Habitacion, DateTime Fecha, string Resultado, string Observaciones);
public sealed record SupervisionIncidenciaFila(int Id, int HabitacionId, string Habitacion, string Descripcion, string Categoria, string Prioridad, string Estado, bool BloqueoActivo, bool RequiereBloqueoSinRegistro, DateTime Reportada, string Impacto, string Ordenes, string ReservasAfectadas)
{ public string Resumen => $"{Prioridad} · {Estado} · {Impacto}"; }
public sealed record SupervisionOrdenFila(int Id, int IncidenciaId, string Habitacion, string Incidencia, string Responsable, DateTime? Estimada, string Estado, bool Vencida);
public sealed record SupervisionServicioFila(int Id, string Tipo, string Huesped, string Habitacion, string Servicio, DateTime? Fecha, string Responsable, string Proveedor, string Estado, string Detalle, int? SolicitudId, bool Conflicto);
public sealed record SupervisionProductoFila(int Id, string Producto, string Categoria, int Stock, int Minimo, string Estado, string ServiciosAfectados);
public sealed record SupervisionRecursoFila(int Id, int ReservaId, string Recurso, string Huesped, string Habitacion, int Cantidad, string Estado, DateTime Llegada, DateTime Salida, string Disponibilidad, bool DevolucionPendiente);
public sealed record SupervisionConsumoFila(DateTime Fecha, string Producto, string Huesped, int Cantidad, bool Incluido, string Origen);
public sealed record SupervisionSnapshot(DateTime Actualizado, string Periodo, string BasePeriodo,
    IReadOnlyList<SupervisionIndicador> Indicadores, IReadOnlyList<SupervisionAlerta> Alertas,
    IReadOnlyList<SupervisionHabitacionFila> Habitaciones, IReadOnlyList<SupervisionIngresoFila> Ingresos,
    IReadOnlyList<SupervisionTareaFila> Tareas, IReadOnlyList<SupervisionCargaFila> Carga,
    IReadOnlyList<SupervisionInspeccionFila> Inspecciones, IReadOnlyList<SupervisionIncidenciaFila> Incidencias,
    IReadOnlyList<SupervisionOrdenFila> Ordenes, IReadOnlyList<SupervisionServicioFila> Servicios,
    IReadOnlyList<SupervisionProductoFila> Productos, IReadOnlyList<SupervisionRecursoFila> Recursos,
    IReadOnlyList<SupervisionConsumoFila> Consumos, IReadOnlyList<string> Diagnosticos,
    int HabitacionesTotales, int HabitacionesListas, int HabitacionesOcupadas, int HabitacionesPreparacion,
    int HabitacionesBloqueadas, int TareasProgramadas, int TareasCompletadas, int TareasEnCurso, int TareasPendientes);
