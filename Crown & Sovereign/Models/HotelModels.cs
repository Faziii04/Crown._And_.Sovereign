namespace Crown___Sovereign.Models;

public sealed record UsuarioModel(int Id, string Usuario, string ClaveDemo, string Nombre, string Rol, bool Activo = true);
public sealed record EmpleadoModel(int Id, string Nombre, string Cargo, string Email, string Telefono, string? Rol);
public sealed record RolModel(int Id, string Nombre, string Descripcion);
public sealed record HotelModel(int Id, string Nombre, string Direccion, int Categoria, string Telefono, string Email);
public sealed record ConfiguracionModel(int Id, string Clave, string Valor, DateTime Actualizada);
public sealed record PoliticaModel(int Id, string Nombre, string Descripcion, string Valor);
public sealed record ModuloModel(int Id, string Nombre, string Descripcion, bool Activo);
public sealed record TipoHabitacionModel(int Id, string Nombre, int Capacidad, string Descripcion);
public sealed record PisoModel(int Id, string Nombre);
public sealed record ComodidadModel(int Id, string Nombre, string Descripcion, bool Activa);
public sealed class HabitacionModel
{
    public int Id { get; init; } public string Numero { get; set; } = ""; public int Piso { get; set; }
    public string Tipo { get; set; } = ""; public int Capacidad { get; set; } public string Estado { get; set; } = "Disponible";
    public string Limpieza { get; set; } = "Limpia"; public bool Bloqueada { get; set; } public string? Huesped { get; set; }
    public string Comodidades { get; set; } = "Wi-Fi · TV · Aire acondicionado";
}
public sealed class HuespedModel
{
    public int Id { get; init; } public string Nombre { get; set; } = ""; public string Documento { get; init; } = "";
    public string Email { get; set; } = ""; public string Telefono { get; set; } = ""; public int Habitacion { get; set; }
    public string Paquete { get; set; } = "Sin paquete"; public string RolEnReserva { get; set; } = "Principal";
    public bool ConsentimientoDatos { get; set; } = true;
}
public sealed class ReservaModel
{
    public int Id { get; init; } public string Huesped { get; set; } = ""; public int Habitacion { get; set; }
    public DateTime Llegada { get; set; } public DateTime Salida { get; set; } public string Estado { get; set; } = "Confirmada";
    public string Canal { get; set; } = "Directa"; public string Paquete { get; set; } = "Sin paquete"; public int? GrupoId { get; set; } public int? PromocionId { get; set; }
    public int Personas { get; set; } = 1;
}
public sealed record CanalReservaModel(int Id, string Nombre, decimal Comision);
public sealed record GrupoReservaModel(int Id, string Nombre, DateTime Creado, DateTime? Llegada = null, DateTime? Salida = null, string Estado = "Activo");
public sealed record ReservaHabitacionModel(int ReservaHabitacionId, int ReservaId, int HabitacionId);
public sealed record ReservaHuespedModel(int ReservaId, int HuespedId, bool Principal);
public sealed record ReservaHabitacionHuespedModel(int ReservaHabitacionId, int ReservaId, int HuespedId);
public sealed record ReservaPaqueteModel(int ReservaId, int PaqueteId, int Cantidad, decimal PrecioCompra);
public sealed class RecursoAdicionalModel
{
    public int RecursoId { get; init; } public string Nombre { get; set; } = ""; public string Tipo { get; set; } = "";
    public decimal Precio { get; set; } public string Descripcion { get; set; } = ""; public bool Activo { get; set; } = true;
    public string TipoCobro { get; set; } = "POR_ESTANCIA"; public int? StockTotal { get; set; } public bool RequierePreparacion { get; set; }
    public string? Imagen { get; set; }
}
public sealed record ReservaRecursoModel(int ReservaId, int RecursoId, int Cantidad, decimal? PrecioUnitarioAcordado, string? TipoCobroAplicado, string Estado = "PENDIENTE", string Observaciones = "", int? CargoId = null);
public sealed record ReservaServicioAdquiridoModel(int ServicioAdquiridoId, int ReservaId, int ServicioId, int? HuespedId, int? ReservaHabitacionId, int Cantidad, decimal? PrecioUnitarioAcordado, DateTime FechaRegistro, string Estado = "ADQUIRIDO", int? CargoId = null, string Observaciones = "");
/// <summary>Purchased benefit snapshot; later edits to the commercial package catalog do not rewrite prior purchases.</summary>
public sealed record ReservaPaqueteServicioModel(int ReservaId, int PaqueteId, int ServicioId, string TipoCobertura, int? LimiteUsos, bool Compartido, string? Nota = null);
public sealed record ReservaPaqueteServicioProductoModel(int ReservaId, int PaqueteId, int ServicioId, int ProductoId, int CantidadIncluida);
public sealed record ListaEsperaModel(int Id, string Huesped, DateTime Llegada, DateTime Salida, string TipoHabitacion, string Estado, int? HuespedId = null, int Personas = 1, int Habitaciones = 1, string Observaciones = "");
public sealed record ContactoListaEsperaModel(int Id, int SolicitudId, DateTime Fecha, string Medio, string Resultado, string Observaciones, string Usuario);
public sealed record RecursoReservaModel(int Id, int ReservaId, string Nombre, int Cantidad, decimal PrecioCompra);
public sealed record SolicitudEspecialModel(int Id, int ReservaId, string Descripcion, string Estado);
public sealed record TarifaModel(int Id, string TipoHabitacion, string Temporada, decimal Precio, DateTime Desde, DateTime Hasta);
public sealed record TemporadaModel(int Id, string Nombre, DateTime Desde, DateTime Hasta);
public sealed record PaqueteModel(int Id, string Nombre, string Descripcion, decimal Precio, bool Activo);
public sealed record PromocionModel(int Id, string Nombre, string TipoDescuento, decimal Valor, DateTime Desde, DateTime Hasta);
public sealed record ConvenioModel(int Id, string Empresa, string Categoria, DateTime VigenciaDesde, DateTime VigenciaHasta, bool Activo);
public sealed record EstanciaModel(int Id, int ReservaId, int HuespedId, DateTime CheckIn, DateTime CheckOut, string Estado)
{ public int? FolioId { get; init; } public DateTime? CheckOutReal { get; init; } }
public sealed record LlaveModel(int Id, int EstanciaId, string Codigo, string Estado);
public sealed record FolioModel(int Id, int? EstanciaId, decimal Saldo, string Estado)
{ public int? ReservaId { get; init; } public int? HuespedPrincipalId { get; init; } public string Tipo { get; init; } = "Estancia"; }
public sealed record CargoDetalleModel(int CargoId, int ConceptoId, int Cantidad, decimal PrecioUnitario);
public sealed record ReservaServicioModel(int Id, int ReservaId, int HuespedId, int ServicioId, string Estado, bool Incluido) { public int? ServicioAdquiridoId { get; init; } }
public sealed record PaqueteServicioModel(int PaqueteId, int ServicioId, string TipoCobertura, int? LimiteUsos, bool Compartido = false);
public sealed record ServicioProductoModel(int ServicioId, int ProductoId, int CantidadIncluida);
public sealed record HabitacionComodidadModel(int HabitacionId, int ComodidadId, bool Disponible);
public sealed record TurnoCajaModel(int Id, int UsuarioId, DateTime Apertura, decimal MontoInicial, string Estado)
{ public DateTime? Cierre { get; init; } public decimal? EfectivoContado { get; init; } public int? CierreCajaId { get; init; } public string ObservacionesCierre { get; init; } = ""; }
public sealed record MovimientoCajaModel(int Id, int TurnoId, DateTime Fecha, string Tipo, decimal Monto, string Concepto)
{ public int? PagoId { get; init; } public int? FolioId { get; init; } public string Metodo { get; init; } = ""; public int UsuarioId { get; init; } public int? CorreccionId { get; init; } }
public sealed record DevolucionModel(int Id, int PagoId, decimal Monto, string Motivo, string Estado)
{ public DateTime Solicitada { get; init; } public int UsuarioSolicitaId { get; init; } public int? UsuarioAutorizaId { get; init; } public DateTime? FechaAutorizacion { get; init; } public DateTime? Ejecutada { get; init; } public int? TurnoEjecucionId { get; init; } public string MetodoEjecucion { get; init; } = ""; public string Descripcion { get; init; } = ""; }
public sealed record AnulacionModel(int Id, int PagoId, string Motivo, string Estado)
{ public DateTime Solicitada { get; init; } public int UsuarioSolicitaId { get; init; } public int? UsuarioAutorizaId { get; init; } public DateTime? FechaAutorizacion { get; init; } public DateTime? Ejecutada { get; init; } public int? TurnoEjecucionId { get; init; } public string Descripcion { get; init; } = ""; }
public sealed record NotaCreditoModel(int Id, int FacturaId, decimal Monto, string Motivo, string Estado);
public sealed record ConceptoCargoModel(int Id, string Nombre, string Categoria);
public sealed record PagoModel(int Id, int FolioId, decimal Monto, string Metodo, DateTime Fecha, string Estado)
{ public int TurnoCajaId { get; init; } public int UsuarioId { get; init; } public string Referencia { get; init; } = ""; public string Concepto { get; init; } = "Pago de folio"; public bool EsAnticipo { get; init; } }
public sealed record MetodoPagoModel(int Id, string Nombre, bool Activo, bool EsEfectivo = false);
public sealed record FacturaModel(int Id, int FolioId, string Numero, DateTime Fecha, decimal Total, string Estado)
{ public decimal Subtotal { get; init; } public decimal Impuesto { get; init; } public decimal PorcentajeImpuesto { get; init; } public string Cliente { get; init; } = ""; public string DocumentoCliente { get; init; } = ""; public IReadOnlyList<FacturaLineaModel> Lineas { get; init; } = []; public int UsuarioEmiteId { get; init; } public int? ReservaId { get; init; } public int? EstanciaId { get; init; } public int? HabitacionId { get; init; } public string NotaCredito { get; init; } = ""; }
public sealed record FacturaLineaModel(int CargoId, string Concepto, int Cantidad, decimal PrecioUnitario, decimal Importe);
public sealed record ImpuestoModel(int Id, string Nombre, decimal Porcentaje, bool Activo);
public sealed record CitaServicioModel(int Id, int HuespedId, int ServicioId, DateTime Inicio, DateTime Fin, string AsignadoA, string Estado)
{ public int? EstanciaId { get; init; } public int? SolicitudId { get; init; } public int? UsuarioConfirmaId { get; init; } public DateTime? Confirmada { get; init; } }
public sealed record SolicitudServicioModel(int Id, int HuespedId, int ServicioId, DateTime Solicitada, DateTime? FechaDeseada, string Descripcion, string Estado, bool RequiereCita)
{
    public int? EstanciaId { get; init; } public int? ReservaId { get; init; } public int UsuarioSolicitaId { get; init; }
    public int? CitaServicioId { get; init; } public DateTime? Aceptada { get; init; } public DateTime? InicioAtencion { get; init; }
    public DateTime? Completada { get; init; } public DateTime? Cancelada { get; init; } public int? ResponsableId { get; init; }
    public string Responsable { get; init; } = ""; public string MotivoCancelacion { get; init; } = "";
    public string OrigenTraslado { get; init; } = ""; public string DestinoTraslado { get; init; } = ""; public int? Pasajeros { get; init; }
    public string ContactoTraslado { get; init; } = ""; public bool ProcesamientoCompleto { get; init; }
    public int? UsoServicioId { get; init; } public int? CargoId { get; init; } public string CoberturaEstimada { get; init; } = "";
}
public sealed record UsoServicioModel(int Id, int HuespedId, int ServicioId, DateTime Fecha, bool Incluido, decimal Cargo, string Estado)
{ public int? EstanciaId { get; init; } public int? ServicioAdquiridoId { get; init; } }
public sealed record TrasladoModel(int Id, int HuespedId, string Origen, string Destino, DateTime Salida, string Estado)
{ public int? EstanciaId { get; init; } public int? ServicioId { get; init; } public int? SolicitudId { get; init; } public int Pasajeros { get; init; } = 1; public string Observaciones { get; init; } = ""; }
public sealed class CargoModel(int id, string concepto, decimal monto, DateTime fecha, int huespedId = 0, int? estanciaId = null)
{ public int Id { get; } = id; public string Concepto { get; } = concepto; public decimal Monto { get; } = monto; public DateTime Fecha { get; } = fecha; public int HuespedId { get; } = huespedId; public int? EstanciaId { get; } = estanciaId; public int? FolioId { get; init; } public bool Anulado { get; init; } }
public sealed class ServicioModel
{
    public int Id { get; init; } public string Nombre { get; set; } = ""; public string Categoria { get; set; } = "";
    public decimal Precio { get; set; } public bool RequiereCita { get; set; } public string Cobertura { get; set; } = "No incluido";
    public int? UsosRestantes { get; set; } public int? PadreId { get; set; } public bool Activo { get; set; } = true; public bool Reservable { get; set; } = true; public bool Disponible { get; set; } = true; public int DuracionMinutos { get; set; } = 60;
    public string Descripcion { get; set; } = ""; public string Proveedor { get; set; } = "Interno"; public int? ProductoAsociadoId { get; set; }
    public bool RequiereTraslado { get; set; } public string Condiciones { get; set; } = "";
}
public sealed class ProductoModel
{
    public int Id { get; init; } public string Nombre { get; set; } = ""; public string Categoria { get; set; } = "";
    public decimal Precio { get; set; } public int Stock { get; set; } public int StockMinimo { get; set; } = 8; public bool IncluidoEnPaquete { get; set; }
}
public sealed record ConsumoModel(int Id, int HuespedId, int ProductoId, int Cantidad, DateTime Fecha, bool Incluido)
{ public int? EstanciaId { get; init; } }
public sealed record MovimientoInventarioModel(int Id, int ProductoId, DateTime Fecha, string Tipo, int Cantidad, string Motivo);
public sealed class TareaModel
{
    public int Id { get; init; } public int Habitacion { get; init; } public int Piso { get; init; }
    public string Tipo { get; init; } = "Salida"; public string Estado { get; set; } = "Pendiente";
    public string Empleado { get; init; } = ""; public string Prioridad { get; init; } = "Normal"; public DateTime Hora { get; init; }
    public string MotivoSolicitud { get; init; } = ""; public string Observaciones { get; init; } = ""; public string SolicitadoPor { get; init; } = "";
}
public sealed record ChecklistModel(int Id, string Nombre, IReadOnlyList<string> Items);
public sealed record InspeccionModel(int Id, int TareaId, DateTime Fecha, bool Aprobada, string Observaciones);
public sealed record OrdenMantenimientoModel(int Id, int IncidenciaId, string AsignadoA, DateTime? FechaEstimada, string Estado);
public sealed record ObjetoOlvidadoModel(int Id, string Descripcion, int? HabitacionId, DateTime Encontrado, string Estado, string Observaciones);
public sealed class IncidenciaModel
{
    public int Id { get; init; } public int Habitacion { get; init; } public string Descripcion { get; init; } = "";
    public string Prioridad { get; init; } = "Media"; public string Estado { get; set; } = "Abierta"; public bool BloqueaHabitacion { get; set; }
    public string Categoria { get; init; } = "Otros"; public DateTime FechaReporte { get; init; } = DateTime.Now;
    public string ReportadaPor { get; init; } = ""; public string Observaciones { get; init; } = "";
    public bool RequiereRevisionOperativa { get; init; }
    public int? GuestId { get; init; } public string ImpactoObservado { get; init; } = "Puede seguir utilizándose";
}
public sealed record MovimientoModel(DateTime Fecha, string Tipo, string Concepto, decimal Monto, string Medio, int HuespedId = 0)
{ public int? EstanciaId { get; init; } public int? TurnoCajaId { get; init; } public int? PagoId { get; init; } public int? FolioId { get; init; } }
public sealed record CajaModel(int Id, string Estado, decimal Apertura, DateTime FechaApertura);
public sealed record CierreCajaModel(int Id, DateTime Fecha, decimal Esperado, decimal Contado, decimal Diferencia, string Observaciones)
{ public int TurnoCajaId { get; init; } public int UsuarioId { get; init; } public DateTime Apertura { get; init; } public decimal AperturaMonto { get; init; } public decimal EfectivoCobros { get; init; } public decimal EfectivoDevoluciones { get; init; } public decimal NoEfectivo { get; init; } }
public sealed record NotificacionModel(int Id, string Mensaje, string Destino, DateTime Fecha, bool Leida);
public sealed record ReporteModel(int Id, string Nombre, string Categoria, DateTime Generado, string Estado);
public sealed record AuditoriaModel(DateTime Fecha, string Usuario, string Cambio, string Antes, string Despues);
