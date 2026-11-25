using Crown___Sovereign.Models;

namespace Crown___Sovereign.Models;

public sealed record RoomFloorGroup(string Title, int Total, IReadOnlyList<HabitacionModel> Rooms);
public sealed class FiltroHabitacionesOperativas
{
    public string Busqueda { get; set; } = "";
    public string FiltroRapido { get; set; } = "Todas";
    public string Tipo { get; set; } = "Todos los tipos";
    public string Ocupacion { get; set; } = "Todas";
    public string Limpieza { get; set; } = "Todos los estados";
    public string Mantenimiento { get; set; } = "Todos los estados";
    public string Llegada { get; set; } = "Cualquiera";
    public string Salida { get; set; } = "Cualquiera";
    public int? CapacidadMinima { get; set; }
    public int? Piso { get; set; }
    public IReadOnlyList<string> Comodidades { get; set; } = [];
    public string Orden { get; set; } = "Número de habitación";
}
public sealed class HabitacionOperacionModel
{
    public HabitacionModel Habitacion { get; init; } = new();
    public string Clasificacion { get; init; } = "Por preparar";
    public bool Ocupada { get; init; }
    public bool Bloqueada { get; init; }
    public bool RequiereRevisionOperativa { get; init; }
    public string EstadoLimpieza { get; init; } = "Sin verificar";
    public string EstadoMantenimiento { get; init; } = "Operativa";
    public IReadOnlyList<HuespedModel> Huespedes { get; init; } = [];
    public ReservaModel? ReservaActual { get; init; }
    public EstanciaModel? EstanciaActual { get; init; }
    public ReservaModel? ProximaReserva { get; init; }
    public TareaModel? TareaActual { get; init; }
    public InspeccionModel? UltimaInspeccion { get; init; }
    public IReadOnlyList<IncidenciaModel> Incidencias { get; init; } = [];
    public bool LlegadaHoy => ProximaReserva?.Llegada.Date == DateTime.Today;
    public bool SalidaHoy => EstanciaActual?.CheckOut.Date == DateTime.Today || (Ocupada && ReservaActual?.Salida.Date == DateTime.Today);
    public bool CambioMismoDia => SalidaHoy && ProximaReserva?.Llegada.Date == DateTime.Today;
    public string HuespedPrincipal => Huespedes.FirstOrDefault()?.Nombre ?? ProximaReserva?.Huesped ?? "Sin huésped asignado";
    public string ReservaTexto => ReservaActual is not null ? $"Reserva #{ReservaActual.Id}" : ProximaReserva is not null ? $"Reserva #{ProximaReserva.Id}" : "";
    public string ProximaFecha => ProximaReserva is null ? "Sin próxima reserva" : $"Próxima llegada: {ProximaReserva.Llegada:dd MMM}";
    public string AvisoUrgente => Bloqueada && Ocupada ? "Reubicación requerida" : RequiereRevisionOperativa ? "Revisión urgente pendiente" : CambioMismoDia ? "Salida y llegada hoy" : "";
    public string Tooltip => $"Habitación {Habitacion.Numero} · {Habitacion.Tipo}\nOcupación: {(Ocupada ? "Ocupada" : "Desocupada")}\nLimpieza: {EstadoLimpieza}\nMantenimiento: {EstadoMantenimiento}\n{(Huespedes.Count > 0 ? $"Huésped: {string.Join(", ", Huespedes.Select(item => item.Nombre))}" : "")}";
}
public sealed record ResumenOperativoHabitaciones(int Listas, int Ocupadas, int PorPreparar, int Bloqueadas, int LlegadasHoy, int SalidasHoy, int Urgentes, int BloqueadasConHuesped, int InspeccionesPendientes);
public sealed record ResumenPisoHabitaciones(int Piso, int Total, int Listas, int Ocupadas, int PorPreparar, int Bloqueadas);
public sealed record CambioHabitacionModel(int Id, int ReservaId, int EstanciaId, int HabitacionOrigenId, int HabitacionDestinoId, IReadOnlyList<int> Huespedes, DateTime Fecha, string Motivo, string Observaciones);
public sealed record AlertaHabitacionItem(string Titulo, string Detalle, string Filtro, int? HabitacionId = null);
public sealed record OpcionPisoHabitaciones(int? Piso, string Nombre, int Total, int Listas, bool Seleccionado);
public sealed class GrupoPisoOperativo
{
    public int Piso { get; init; }
    public string Titulo { get; init; } = "";
    public int Total { get; init; }
    public string Resumen { get; init; } = "";
    public bool VistaDetallada { get; init; }
    public IReadOnlyList<HabitacionOperacionModel> Habitaciones { get; init; } = [];
    public bool Expandido { get; set; } = true;
}
public sealed record GuestDirectoryEntry(int Id, string Initials, string Name, string Document, string Contact, string StayStatus, string Package);
public sealed record ActiveGuestChoice(int Id, string Name, string Document, string Room, int Floor, string Package, string Reservation, string Role)
{
    public int EstanciaId { get; init; } public int ReservaId { get; init; } public DateTime CheckIn { get; init; } public DateTime CheckOut { get; init; }
    public int SolicitudesPendientes { get; init; } public int CitasProximas { get; init; } public bool TienePaquete => Package != "Sin paquete";
}
public sealed record ServiceRequestView(int Id, string Guest, string Room, string Service, DateTime RequestedAt, DateTime? DesiredDate, string Status, bool RequiresAppointment)
{
    public int GuestId { get; init; } public int StayId { get; init; } public int ReservationId { get; init; } public string Category { get; init; } = "";
    public string ScopeText { get; init; } = ""; public string Responsible { get; init; } = "Por asignar"; public string Description { get; init; } = "";
    public string Coverage { get; init; } = "Cobertura por confirmar"; public decimal EstimatedCharge { get; init; }
    public DateTime? ConfirmedDate { get; init; } public DateTime? CompletedDate { get; init; } public string Outcome { get; init; } = "";
}
public sealed record ServiceBenefitEvaluation(int GuestId,int StayId,int ReservationId,int ServiceId,string CoverageType,string PackageNames,int Allowance,int Consumed,int Remaining,bool Shared,bool RequiresAppointment,int DurationMinutes,decimal EstimatedCharge,string Label,string Detail,bool Available)
{
    public string Status => CoverageType=="Ilimitado"?"Incluido · ilimitado":CoverageType=="No incluido"?"Servicio adicional":Remaining>0?$"Incluido · {Remaining} de {Allowance} usos restantes":"Beneficio agotado";
}
public sealed record ReceptionServiceMetrics(int PendingRequests,int ImmediateRequests,int AppointmentsToday,int ConfirmationsNeeded,int InProgress);
public sealed record ServiceProgressStep(string Title,string Detail,string State,DateTime? Date);
