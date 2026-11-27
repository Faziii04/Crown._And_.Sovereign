using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class FinanzasControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    private FinancialPeriod _period;
    public event EventHandler<string>? NavigationRequested;

    public FinanzasControl()
    {
        InitializeComponent();
        _period = _service.FromDashboardPeriod(DashboardService.PeriodoPredeterminado());
        PeriodFilter.PeriodChanged += Period_Changed;
        LoadPeriod();
        BalancesControl.MovementsRequested += (_, folioId) =>
        {
            FinanceTabs.SelectedIndex = 1;
            MovementsControl.BuscarFolio(folioId);
        };
    }

    public void AbrirCorreccion(string tipo, int id)
    {
        FinanceTabs.SelectedIndex = 4;
        AuthorizationsControl.MostrarCorreccion(tipo, id);
    }

    private void Period_Changed(object? sender, DashboardPeriod selection)
    {
        try { _period = _service.FromDashboardPeriod(selection); LoadPeriod(); }
        catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Periodo no válido", MessageBoxButton.OK, MessageBoxImage.Information); }
    }

    private void LoadPeriod()
    {
        var summary = _service.Resumen(_period);
        ChargesMetric.Text = Money(summary.Charges);
        CollectionsMetric.Text = Money(summary.NetCollections);
        BalanceMetric.Text = Money(summary.CurrentOutstanding);
        ApprovalsMetric.Text = summary.PendingAuthorizations.ToString();
        SummaryControl.Actualizar(_period, summary);
        MovementsControl.Actualizar(_period);
        BalancesControl.Actualizar();
        InvoicesControl.Actualizar(_period);
        AuthorizationsControl.Actualizar();
        ClosuresControl.Actualizar();
    }

    private void Report_Click(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke(this, "Reportes");
    public static string Money(decimal value) => $"Bs {value:N2}";
}
