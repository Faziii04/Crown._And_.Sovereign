using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Reportes;

public sealed record BarraReporte(string Etiqueta, double Ancho, string Texto);

public partial class VistaPreviaReporteControl : UserControl
{
    public ResultadoReporte? Resultado { get; private set; }
    public VistaPreviaReporteControl() => InitializeComponent();
    public void Cargar(ResultadoReporte report)
    {
        Resultado = report;
        TitleText.Text = $"Crown & Sovereign · {report.Titulo}";
        MetaText.Text = $"{report.Categoria} · {report.Desde:dd/MM/yyyy} – {report.Hasta:dd/MM/yyyy} · Generado {report.Generado:dd/MM/yyyy HH:mm} por {report.Usuario}";
        StatusText.Text = report.Parametros;
        Kpis.ItemsSource = report.Indicadores;
        var max = report.Serie.Select(x => x.Proporcion).DefaultIfEmpty(0).Max();
        ChartItems.ItemsSource = report.Serie.Select(x => new BarraReporte(x.Etiqueta, 460 * x.Proporcion, $"{x.Valor:N0} {x.Serie}"));
        CompareItems.ItemsSource = report.Comparacion?.Select(x => $"{x.Nombre}: {x.Contexto} · anterior {x.Valor}") ?? ["No se solicitó comparación."];
        WarningItems.ItemsSource = report.Advertencias.Count == 0 ? ["Sin alertas de calidad identificadas para los datos consultados."] : report.Advertencias;
        DetailGrid.ItemsSource = report.Detalle.DefaultView;
        MethodText.Text = report.Metodologia;
        FiltersText.Text = report.Parametros + (string.IsNullOrEmpty(report.PeriodoComparacion) ? "" : $"\nPeriodo comparable: {report.PeriodoComparacion}");
    }
}
