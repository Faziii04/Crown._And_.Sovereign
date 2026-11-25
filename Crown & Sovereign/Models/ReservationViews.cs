namespace Crown___Sovereign.Models;

public sealed class ReservationAvailabilityLine
{
    public string RoomType { get; init; } = "";
    public bool IsTotal { get; init; }
    public List<ReservationAvailabilityCell> Days { get; init; } = [];
}

public sealed record ReservationAvailabilityCell(DateTime Date, string DayLabel, int Available, int Total, string Level);

public sealed class ReservationTimelineRoom
{
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = "";
    public string RoomType { get; init; } = "";
    public string FloorLabel { get; init; } = "";
    public List<ReservationTimelineCell> Days { get; init; } = [];
    public List<ReservationBarModel> Bars { get; init; } = [];
    public double CalendarWidth { get; init; }
}

public sealed record ReservationTimelineCell(int RoomId, DateTime Date, string DayLabel, string Guest, string Status, string Detail, int? ReservationId, bool IsFirstNight);

public sealed record ReservationDirectoryRow(int Id, string Guest, string Room, DateTime Arrival, DateTime Departure, string Channel, string Package, string Status);

public sealed class ReservationSearchCriteria
{
    public DateTime Llegada { get; set; } = DateTime.Today.AddDays(1);
    public DateTime Salida { get; set; } = DateTime.Today.AddDays(2);
    public int Personas { get; set; } = 1;
    public int Habitaciones { get; set; } = 1;
    public string? TipoHabitacion { get; set; }
    public IReadOnlyList<string> Comodidades { get; set; } = [];
}

public sealed record HabitacionDisponibleModel(int Id, string Numero, int Piso, string Tipo, int Capacidad, string Comodidades, decimal TarifaNoche, decimal TotalEstancia, string EstadoLimpieza);
public sealed record TipoDisponibilidadModel(string Tipo, int CapacidadMaxima, int Coincidentes, decimal TarifaDesde, decimal TotalEstimado, IReadOnlyList<string> Comodidades, IReadOnlyList<HabitacionDisponibleModel> Habitaciones);
public sealed record ResumenReservasModel(int Llegadas, int CheckInsPendientes, int Salidas, int CheckOutsPendientes, int DisponiblesEstaNoche, int TotalHabitaciones, int EsperaActiva, int CoincidenciasEspera);
public sealed record GrupoReservaResumenModel(GrupoReservaModel Grupo, DateTime Llegada, DateTime Salida, int Huespedes, int HabitacionesRequeridas, int HabitacionesAsignadas, int ReservasRegistradas, int LlegadasRegistradas, string Estado);
public sealed record GrupoHuespedAsignacionModel(int Id, string Nombre, string Documento, string Habitacion, int ReservaId, bool Principal);
public sealed record CoincidenciaEsperaModel(ListaEsperaModel Solicitud, string Huesped, string Telefono, string Correo, int HabitacionesCoincidentes, IReadOnlyList<HabitacionDisponibleModel> Habitaciones);
public sealed record ReservationBarModel(int ReservaId, string Huesped, string Estado, DateTime Llegada, DateTime Salida, double Left, double Width, string Tooltip, bool EsGrupo);
public sealed record ReservationInboxRow(int Id, string Guest, string Room, string RoomType, int Floor, DateTime Arrival, DateTime Departure, int Nights, int Guests, string Channel, string Package, string Status, string PrimaryAction, string? DisabledReason, bool IsGroup, decimal EstimatedTotal);
public sealed class ReservationListCriteria
{
    public string FiltroRapido { get; set; } = "Todas";
    public string Busqueda { get; set; } = "";
    public string Estado { get; set; } = "Todos los estados";
    public DateTime? LlegadaDesde { get; set; }
    public DateTime? LlegadaHasta { get; set; }
    public DateTime? SalidaDesde { get; set; }
    public DateTime? SalidaHasta { get; set; }
    public string Canal { get; set; } = "Todos los canales";
    public string TipoHabitacion { get; set; } = "Todos los tipos";
    public int? Piso { get; set; }
    public bool SoloGrupos { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}
public sealed record ReservationInboxPage(IReadOnlyList<ReservationInboxRow> Rows, int Total, int Page, int PageSize);
public sealed record ReservationGuestAssignment(HuespedModel Huesped, HabitacionModel? Habitacion, bool Principal);

/// <summary>Estado temporal del asistente; no se persiste hasta confirmar la reserva.</summary>
public sealed class BorradorReserva
{
    public DateTime Llegada { get; set; } = DateTime.Today.AddDays(1);
    public DateTime Salida { get; set; } = DateTime.Today.AddDays(2);
    public int Personas { get; set; } = 1;
    public int HabitacionesRequeridas { get; set; } = 1;
    public string Canal { get; set; } = "Directa";
    public string? TipoPreferido { get; set; }
    public HashSet<string> Comodidades { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<int> HabitacionesSeleccionadas { get; } = [];
    public int? HuespedPrincipalId { get; set; }
    public List<int> HuespedesAcompanantes { get; } = [];
    public int? PaqueteId { get; set; }
    public int? PromocionId { get; set; }
    public List<int> ServiciosAdicionales { get; } = [];
    public string SolicitudesEspeciales { get; set; } = "";
    public int? GrupoId { get; set; }
    public int? ListaEsperaId { get; set; }
    public int? ReservaOrigenId { get; set; }
    public int PasoInicial { get; set; }
    public bool ModoEdicion => ReservaOrigenId.HasValue;
}
