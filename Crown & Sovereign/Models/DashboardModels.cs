namespace Crown___Sovereign.Models;

public sealed record DashboardMetric(string Titulo, string Valor, string Contexto, string Tono = "Neutral");
public sealed record DashboardArrival(string Hora, string Huesped, string Habitacion, string Tipo, string Accion, int ReservaId);
public sealed record DashboardAlert(string Titulo, string Detalle, string Estado, string Accion, string Modulo);
