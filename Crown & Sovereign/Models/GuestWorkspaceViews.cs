namespace Crown___Sovereign.Models;

public sealed class FiltroDirectorioHuespedes
{
    public string Busqueda { get; set; } = "";
    public string Estado { get; set; } = "Todos";
    public string Estancias { get; set; } = "Todas";
    public bool? Proximas { get; set; }
    public bool? Recurrentes { get; set; }
    public bool? SolicitudesPendientes { get; set; }
    public bool ContactoIncompleto { get; set; }
    public bool ConsentimientoRevision { get; set; }
    public int? Piso { get; set; }
    public string? Habitacion { get; set; }
    public string Orden { get; set; } = "Nombre A–Z";
}

public sealed record HuespedDirectorioFila(int Id, string Iniciales, string Nombre, string Documento, string Contexto, string Estado, string Color, DateTime? ProximaLlegada, DateTime? UltimaEstancia, int EstanciasCompletadas, bool TieneEstancia, bool TieneProximaReserva, bool EsRecurrente);
public sealed record ResumenHuespedesModel(int Registrados, int EnHotel, int ProximasReservas, int Recurrentes);
public sealed record ResumenPerfilHuespedModel(int EstanciasCompletadas, int NochesCompletadas, DateTime? UltimaVisita, EstanciaActualHuespedModel? EstanciaActual, ReservaModel? ProximaReserva, IReadOnlyList<string> Atenciones, IReadOnlyList<ActividadHuespedFila> ActividadReciente, int Reservas);
public sealed record ReservaHistorialHuespedFila(int ReservaId, DateTime Llegada, DateTime Salida, int Noches, string Habitaciones, string Relacion, string Estado, string Paquete, bool Grupo);
public sealed record ServicioHistorialHuespedFila(DateTime Fecha, string Servicio, string Categoria, string Cobertura, decimal Cargo, string Estado, string Asociacion);
public sealed record EstanciaActualHuespedModel(int HuespedId, int EstanciaId, int ReservaId, DateTime CheckIn, DateTime CheckOut, string Relacion, IReadOnlyList<HabitacionModel> Habitaciones, IReadOnlyList<HuespedModel> Huespedes, string Paquete, bool EsAcompanante);
public sealed record ResumenFinancieroHuespedModel(decimal Cargos, decimal Pagos, decimal Saldo, string Nota);
public sealed record BeneficioHuespedFila(string Servicio, string Categoria, string Cobertura, string Disponibilidad, string Precio, bool RequiereCita);
public sealed record SolicitudEspecialHuespedFila(SolicitudEspecialModel Solicitud, string Reserva, string HuespedesRelacionados, string Estado);
public sealed record SolicitudServicioHuespedFila(SolicitudServicioModel Solicitud, string Servicio, string Categoria, string Habitacion, string Cita, string Cobertura);
public sealed record ActividadHuespedFila(DateTime Fecha, string Tipo, string Detalle, string Origen);
public sealed record ConsentimientoHistorialModel(int Id, int HuespedId, bool EstadoAnterior, bool EstadoNuevo, DateTime Fecha, string Usuario, string Observaciones);
public sealed record ContactoHuespedModel(bool Completo, string Detalle);
