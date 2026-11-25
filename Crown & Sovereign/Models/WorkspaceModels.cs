using System.Collections.ObjectModel;

namespace Crown___Sovereign.Models;

public sealed class WorkspaceMetric
{ public string Titulo { get; set; }=""; public string Valor { get; set; }=""; public string Nota { get; set; }=""; }
public sealed class WorkspaceCard
{ public int Id { get; set; } public string Titulo { get; set; }=""; public string Detalle { get; set; }=""; public string Estado { get; set; }=""; public string Accion { get; set; }="Abrir"; public string TipoAccion { get; set; }="Detalle"; }
public sealed class ReservationRow
{ public int Reserva { get; set; } public string Huesped { get; set; }=""; public string Habitacion { get; set; }=""; public DateTime Llegada { get; set; } public DateTime Salida { get; set; } public string Estado { get; set; }=""; public string Canal { get; set; }=""; public string Paquete { get; set; }=""; }
public sealed class AvailabilityRow
{ public string TipoHabitacion { get; set; }=""; public DateTime Fecha { get; set; } public int Disponibles { get; set; } public int Total { get; set; } public string Nivel { get; set; }=""; }
public sealed class CalendarRow
{ public string Habitacion { get; set; }=""; public string Piso { get; set; }=""; public string Tipo { get; set; }=""; public string Dia1 { get; set; }=""; public string Dia2 { get; set; }=""; public string Dia3 { get; set; }=""; public string Dia4 { get; set; }=""; public string Dia5 { get; set; }=""; public string Dia6 { get; set; }=""; public string Dia7 { get; set; }=""; }
public sealed class MovementRow
{ public DateTime Fecha { get; set; } public string Tipo { get; set; }=""; public string Concepto { get; set; }=""; public decimal Importe { get; set; } public string Metodo { get; set; }=""; public int HuespedId { get; set; } }
public sealed class InvoiceRow
{ public string Numero { get; set; }=""; public string Huesped { get; set; }=""; public string Estado { get; set; }=""; public decimal Total { get; set; } }
public sealed class GuestChoice
{ public int Id { get; set; } public string Titulo { get; set; }=""; public string Nombre { get; set; }=""; public int Habitacion { get; set; } public string Paquete { get; set; }=""; }
public sealed class FormFieldModel
{ public string Etiqueta { get; set; }=""; public string Valor { get; set; }=""; }
public sealed class UserListRow
{ public int Id { get; set; } public string Usuario { get; set; }=""; public string Empleado { get; set; }=""; public string Rol { get; set; }=""; public string Estado { get; set; }=""; }
