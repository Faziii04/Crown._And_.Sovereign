using System.Data;

namespace Crown___Sovereign.Models;

public sealed record DefinicionReporte(string Id, string Nombre, string Categoria, string Descripcion, string Alcance);
public sealed record IndicadorReporte(string Nombre, string Valor, string Contexto = "")
{
    public decimal? ValorNumerico { get; init; }
}
public sealed record PuntoReporte(string Etiqueta, decimal Valor, string Serie = "")
{
    public double Proporcion { get; init; }
}
public sealed record ParametrosReporte(string Id, string Periodo, DateTime Desde, DateTime Hasta, string Grupo = "", string TipoHabitacion = "Todos", string Piso = "Todos", string Canal = "Todos", string MetodoPago = "Todos", string Categoria = "Todas", string Empleado = "Todos", bool Comparar = false);
public sealed record ResultadoReporte(string Id, string Titulo, string Categoria, DateTime Generado, string Usuario, DateTime Desde, DateTime Hasta,
    string Parametros, IReadOnlyList<IndicadorReporte> Indicadores, IReadOnlyList<PuntoReporte> Serie, DataTable Detalle,
    string Metodologia, IReadOnlyList<string> Advertencias, string PeriodoComparacion = "", IReadOnlyList<IndicadorReporte>? Comparacion = null);
public sealed record ReporteReciente(string Titulo, string Periodo, DateTime Generado, string ReporteId, ParametrosReporte Parametros);

public sealed record PoliticaServicioModel(int HotelId, int HorasCancelacionGratuita, string TipoPenalizacionInasistencia,
    decimal ValorPenalizacionInasistencia, string TipoPenalizacionCancelacion, decimal ValorPenalizacionCancelacion,
    bool ConsumirBeneficioInasistencia, bool ConsumirBeneficioCancelacionTardia);
public sealed record ConfiguracionAuditModel(int Id, int HotelId, int UsuarioId, string Cambio, string ValorAnterior, string ValorNuevo, DateTime Fecha);
public sealed record DefinicionPolitica(string Codigo, string Nombre, string Categoria, string Descripcion, string ValorPredeterminado, string TipoEditor, bool Aplicada, string Flujo);
public sealed record SimulacionPoliticaResultado(string Estado, string Mensaje, decimal Penalizacion, string BaseCalculo, bool ConsumeBeneficio, string AlcanceHistorico);
