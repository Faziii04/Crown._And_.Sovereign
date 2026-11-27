namespace Crown___Sovereign.Models;

public sealed class RateRow
{ public string Tipo { get; set; }=""; public string Temporada { get; set; }=""; public decimal Precio { get; set; } public int HabitacionId { get; set; } }
public sealed class AppointmentRow
{ public int Id { get; set; } public DateTime Hora { get; set; } public string Servicio { get; set; }=""; public string Huesped { get; set; }=""; public string Habitacion { get; set; }=""; public string Responsable { get; set; }=""; public string Estado { get; set; }=""; }
public sealed class LostObjectRow
{ public int Id { get; set; } public string Descripcion { get; set; }=""; public string Habitacion { get; set; }=""; public DateTime Encontrado { get; set; } public string Estado { get; set; }=""; public string Observaciones { get; set; }=""; }
public sealed class AuditRow
{ public DateTime Fecha { get; set; } public string Usuario { get; set; }=""; public string Cambio { get; set; }=""; public string Antes { get; set; }=""; public string Despues { get; set; }=""; }
public sealed record StayDirectoryEntry(int Id, int ReservationId, string Guest, string Room, int Floor, string Package, DateTime CheckIn, DateTime CheckOut, string Status, int GuestId = 0, DateTime? CheckOutReal = null)
{
    public string RoomType { get; init; } = "";
    public string Floors { get; init; } = "";
    public string GuestDocument { get; init; } = "";
    public string Group { get; init; } = "";
    public decimal Balance { get; init; }
    public bool AttentionRequired { get; init; }
}
public sealed record StayDirectoryMetrics(int Active, int DeparturesToday, int AttentionRequired, int WithBalance);
public sealed record StayExtensionQuote(int StayId, int ReservationId, DateTime CurrentDeparture, DateTime NewDeparture, int AdditionalNights, IReadOnlyList<string> Rooms, decimal Lodging, decimal Resources, decimal Total);
public sealed record StayResourceRow(int RecursoId, string Nombre, int Cantidad, string Modalidad, decimal Importe, string Estado, string Observaciones, int? CargoId, string EstadoRaw);
public sealed record StayActivityRow(DateTime Fecha, string Tipo, string Descripcion, string Usuario);
public sealed record StayServiceRequestRow(DateTime Fecha, string Tipo, string Servicio, string Detalle, string Estado);
public sealed record FolioLine(DateTime Fecha, string Tipo, string Concepto, decimal Importe)
{
    public string Origen { get; init; } = "";
    public string Estado { get; init; } = "Aplicado";
}
public sealed record CashDeskSummary(bool Abierta, decimal Apertura, decimal Efectivo, decimal NoEfectivo, decimal Devoluciones, decimal Esperado);
public sealed record CashDeskShiftSummary(TurnoCajaModel Turno, decimal EfectivoCobros, decimal EfectivoDevoluciones, decimal NoEfectivo, decimal TotalPagos, decimal EfectivoEsperado, IReadOnlyList<MetodoPagoResumen> PorMetodo);
public sealed record MetodoPagoResumen(string Metodo, decimal Importe, bool EsEfectivo);
public sealed record CashMovementRow(int Id, DateTime Fecha, string Tipo, string Huesped, int FolioId, string Metodo, decimal Monto, string Concepto, int? PagoId, string Estado);
public sealed record PaymentDirectoryRow(PagoModel Pago, string Huesped, string Documento, int? ReservaId, int? EstanciaId, string Folio, string Habitacion, string EstadoVista, decimal Reembolsado)
{ public int Id => Pago.Id; public DateTime Fecha => Pago.Fecha; public string Metodo => Pago.Metodo; public decimal Monto => Pago.Monto; public string Estado => EstadoVista; public string Concepto => Pago.Concepto; public int FolioId => Pago.FolioId; }
public sealed record FolioSelectorRow(int FolioId, string Huésped, string Contexto, string Habitación, string Tipo, string Estado, decimal Saldo, int GuestId, int? ReservaId, int? EstanciaId);
public sealed record CorrectionDirectoryRow(int Id, string Tipo, string Huesped, int PagoId, int FolioId, decimal Monto, string Metodo, string Motivo, string Estado, string SolicitadoPor, string AutorizadoPor, DateTime Fecha, bool EsEjecutable)
{ public string Responsables => $"{SolicitadoPor} · {AutorizadoPor}"; }
public sealed record InvoiceDirectoryRow(FacturaModel Factura, string Huesped, string Folio, string Contexto, bool Atribuida)
{ public string Numero => Factura.Numero; public DateTime Fecha => Factura.Fecha; public decimal Subtotal => Factura.Subtotal; public decimal Impuesto => Factura.Impuesto; public decimal Total => Factura.Total; public string Estado => Factura.Estado; }
public sealed record InvoiceDraftModel(int FolioId, string Cliente, string Documento, decimal Subtotal, decimal PorcentajeImpuesto, decimal Impuesto, decimal Total, IReadOnlyList<FacturaLineaModel> Lineas);
public sealed record ProductCartLine(ProductoModel Producto, int Cantidad)
{ public decimal Subtotal => Producto.Precio * Cantidad; public string Cobertura => Producto.IncluidoEnPaquete ? "Puede estar incluido en paquete" : "Cargo adicional"; }
public sealed record InventoryMovementRow(DateTime Fecha, string Producto, string Tipo, int Cantidad, string Motivo, int ExistenciaResultante);
