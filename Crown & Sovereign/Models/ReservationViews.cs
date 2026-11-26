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
    public int CantidadPaquete { get; set; } = 1;
    public decimal? PrecioPaqueteAcordado { get; set; }
    public int? PromocionId { get; set; }
    public List<int> ServiciosAdicionales { get; } = [];
    public List<ReservaRecursoBorrador> RecursosAdicionales { get; } = [];
    public List<ServicioAdquiridoBorrador> ServiciosAdquiridos { get; } = [];
    public string SolicitudesEspeciales { get; set; } = "";
    public int? GrupoId { get; set; }
    public int? ListaEsperaId { get; set; }
    public int? ReservaOrigenId { get; set; }
    public int? ReservaConfirmadaId { get; set; }
    public int PasoInicial { get; set; }
    public bool ModoEdicion => ReservaOrigenId.HasValue;
}

public sealed class ReservaRecursoBorrador
{
    public int RecursoId { get; set; } public int Cantidad { get; set; } public decimal? PrecioUnitarioAcordado { get; set; }
    public string? TipoCobroAplicado { get; set; } public string Estado { get; set; } = "PENDIENTE"; public string Observaciones { get; set; } = "";
    public int? CargoId { get; set; }
}

public sealed class ServicioAdquiridoBorrador
{
    public int ServicioId { get; set; } public int? HuespedId { get; set; } public int? ReservaHabitacionId { get; set; }
    public int Cantidad { get; set; } public decimal? PrecioUnitarioAcordado { get; set; } public string Estado { get; set; } = "ADQUIRIDO";
    public int? CargoId { get; set; } public string Observaciones { get; set; } = "";
}

public sealed record ResumenEconomicoReserva(decimal Alojamiento, decimal Paquetes, decimal Recursos, decimal Servicios, decimal Descuentos, decimal Impuestos, decimal Total, bool PreciosPendientes = false);

public sealed class RecursoAdicionalOpcion : System.ComponentModel.INotifyPropertyChanged
{
    private int _cantidad;
    private string _observaciones = "";
    public decimal? PrecioUnitarioAcordado { get; set; }
    public string? TipoCobroAplicado { get; set; } = "POR_ESTANCIA";
    public required RecursoAdicionalModel Recurso { get; init; }
    public string Nombre => Recurso.Nombre; public string Tipo => Recurso.Tipo; public string Descripcion => Recurso.Descripcion;
    public decimal? Precio => PrecioUnitarioAcordado; public string Modalidad => Precio is null || TipoCobroAplicado is null ? "Condición por verificar" : Precio == 0 ? "Sin costo" : TipoCobroAplicado == "POR_NOCHE" ? "Por noche" : "Por estancia";
    public string PrecioTexto => Precio is null || TipoCobroAplicado is null ? "Precio/modalidad acordados sin verificar" : Precio == 0 ? "Sin costo" : $"Bs {Precio:N2} / {(TipoCobroAplicado == "POR_NOCHE" ? "noche" : "estancia")}";
    public string DisponibilidadTexto { get; set; } = "Sin control de stock limitado";
    public string DisponibilidadDetalle { get; set; } = "";
    public int CantidadMaxima { get; set; } = 999;
    public bool RequierePreparacion => Recurso.RequierePreparacion;
    public string Imagen => Recurso.Imagen ?? "";
    public bool TieneImagen => !string.IsNullOrWhiteSpace(Imagen);
    public int Cantidad { get => _cantidad; set { var next = Math.Max(0, value); if (_cantidad == next) return; _cantidad = next; PropertyChanged?.Invoke(this, new(nameof(Cantidad))); PropertyChanged?.Invoke(this, new(nameof(Subtotal))); PropertyChanged?.Invoke(this, new(nameof(Seleccionado))); } }
    public bool Seleccionado => Cantidad > 0;
    public int Noches { get; set; } = 1;
    public decimal Subtotal => (Precio ?? 0m) * Cantidad * (TipoCobroAplicado == "POR_NOCHE" ? Noches : TipoCobroAplicado == "POR_ESTANCIA" ? 1 : 0);
    public string SubtotalTexto => $"Subtotal: Bs {Subtotal:N2}";
    public string Observaciones { get => _observaciones; set { _observaciones = value; PropertyChanged?.Invoke(this, new(nameof(Observaciones))); } }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ServicioAdicionalOpcion : System.ComponentModel.INotifyPropertyChanged
{
    private int _cantidad;
    public required ServicioModel Servicio { get; init; }
    public string Nombre => Servicio.Nombre; public string Categoria { get; set; } = "";
    public string Descripcion => Servicio.Descripcion; public decimal? PrecioUnitarioAcordado { get; set; }
    public decimal Precio => PrecioUnitarioAcordado ?? 0m;
    public string PrecioTexto => PrecioUnitarioAcordado is decimal price ? $"Bs {price:N2}" : "Precio acordado sin verificar";
    public bool RequiereCita => Servicio.RequiereCita; public int DuracionMinutos => Servicio.DuracionMinutos; public string Proveedor => Servicio.Proveedor;
    public string Cobertura { get; set; } = "No incluido"; public int Incluidos { get; set; }
    public bool IncluidoIlimitado { get; set; }
    public bool HabilitarCompra => !IncluidoIlimitado && PrecioUnitarioAcordado is decimal price && price >= 0;
    public string CoberturaTexto => PrecioUnitarioAcordado is null ? "Precio acordado sin verificar · requiere revisión" : IncluidoIlimitado ? "Incluido sin límite · no requiere compra" : Incluidos > 0 ? $"Incluye {Incluidos} · unidades adicionales a Bs {Precio:N2}" : $"Adicional · Bs {Precio:N2} por unidad";
    public int Cantidad { get => _cantidad; set { var next = Math.Max(0, value); if (_cantidad == next) return; _cantidad = next; PropertyChanged?.Invoke(this, new(nameof(Cantidad))); PropertyChanged?.Invoke(this, new(nameof(Subtotal))); } }
    public int? HuespedId { get; set; }
    public string Observaciones { get; set; } = "";
    public decimal Subtotal => Cantidad * Precio;
    public string SubtotalTexto => Cantidad > 0 ? $"Subtotal adicional: Bs {Subtotal:N2}" : "Sin compra adicional";
    public string CitaTexto => RequiereCita ? "Requiere cita · se programa durante la estancia" : "No requiere cita";
    public System.Collections.Generic.IReadOnlyList<HuespedReservaChoice> Destinatarios { get; set; } = [];
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public sealed record HuespedReservaChoice(int? Id, string Nombre);
