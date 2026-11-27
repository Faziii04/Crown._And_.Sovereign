using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class GerenciaDashboardControl : UserControl
{
    private readonly DashboardService _service = new();
    private DashboardPeriod _period = DashboardService.PeriodoPredeterminado();
    public event EventHandler<string>? NavigationRequested;
    public GerenciaDashboardControl()
    {
        InitializeComponent();
        DecisionPanel.DecisionsChanged += (_, _) => LoadSummary();
        CommercialPanel.NavigationRequested += (_, destination) => NavigationRequested?.Invoke(this, destination);
        LoadSummary();
    }
    private void PeriodFilter_PeriodChanged(object? sender, DashboardPeriod e) { _period = e; LoadSummary(); }
    private void LoadSummary()
    {
        var summary = _service.Gerencia(_period);
        MetricCards.ItemsSource = summary.Metricas;
        FinancialPanel.Cargar(summary);
        DecisionPanel.Cargar(summary.Decisiones);
        OperationsPanel.Cargar(summary.Operacion, summary.Alertas);
        CommercialPanel.Cargar(summary.Comercial, summary.ReservasPorCanal);
        NotesText.Text = $"{summary.NotaIngresos} {summary.NotaReservas} {summary.NotaOcupacion} Llegadas y salidas cuentan reservas, no personas. Los servicios completados son volumen de uso; los ingresos de servicios provienen solo de cargos elegibles.";
    }
    private void Reports_Click(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke(this, "Reportes");
    private void DecisionPanel_DecisionsRequested(object? sender, EventArgs e) => NavigationRequested?.Invoke(this, "Finanzas");
    private void OperationsPanel_ModuleRequested(object? sender, EventArgs e) => NavigationRequested?.Invoke(this, "Operaciones");
}
