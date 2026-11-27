namespace Crown___Sovereign.Models;

public sealed record DashboardMetric(string Titulo, string Valor, string Contexto, string Tono = "Neutral");
public sealed record DashboardArrival(string Hora, string Huesped, string Habitacion, string Tipo, string Accion, int ReservaId);
public sealed record DashboardAlert(string Titulo, string Detalle, string Estado, string Accion, string Modulo);
public sealed record DashboardPeriod(string Nombre, DateTime Desde, DateTime Hasta, DateTime ComparacionDesde, DateTime ComparacionHasta)
{
    public bool IncluirComparacion { get; init; } = true;
    public string Etiqueta => $"{Desde:dd MMM yyyy} – {Hasta:dd MMM yyyy}";
}
public sealed record DashboardBar(string Etiqueta, decimal Valor, double Ancho, string Detalle);
public sealed record DashboardCategory(string Nombre, decimal Monto, double Porcentaje, int Cantidad);
public sealed record DashboardDecision(int Id, string Tipo, string Huesped, decimal Monto, DateTime Fecha, string Motivo, string Estado);
public sealed record DashboardExecutiveSummary(
    DashboardPeriod Periodo, IReadOnlyList<DashboardMetric> Metricas, IReadOnlyList<DashboardBar> Tendencia,
    IReadOnlyList<DashboardCategory> Distribucion, IReadOnlyList<DashboardDecision> Decisiones,
    IReadOnlyList<DashboardMetric> Operacion, IReadOnlyList<DashboardAlert> Alertas,
    IReadOnlyList<DashboardMetric> Comercial, IReadOnlyList<DashboardBar> ReservasPorCanal,
    decimal Ingresos, decimal Pagos, decimal Devoluciones, decimal Anulaciones, int ReservasRegistradas,
    decimal OcupacionPromedio, int LlegadasHoy, int SalidasHoy, int HabitacionesBloqueadas, int ProductosBajoMinimo,
    int PaquetesVendidos, int ServiciosPrestados, int RecursosSolicitados, int RecursosEntregados, int RecursosPendientes, decimal IngresosRecursos, int ConveniosPorVencer, decimal SaldoActualFolios,
    string NotaIngresos, string NotaReservas, string NotaOcupacion);
