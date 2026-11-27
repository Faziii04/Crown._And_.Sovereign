using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class ResumenFinancieroControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    public ResumenFinancieroControl() => InitializeComponent();

    public void Actualizar(FinancialPeriod period, FinancialSummary summary)
    {
        GrossText.Text = FinanzasControl.Money(summary.GrossCollections);
        RefundText.Text = FinanzasControl.Money(summary.Refunds);
        CancellationText.Text = FinanzasControl.Money(summary.Cancellations);
        NetText.Text = FinanzasControl.Money(summary.NetCollections);
        ComparisonText.Text = period.ComparePrevious ? $"Periodo anterior: {FinanzasControl.Money(summary.PreviousNetCollections)}" : "Comparación desactivada";
        CurrentBalancesText.Text = $"Saldo pendiente actual: {FinanzasControl.Money(summary.CurrentOutstanding)}";
        PendingText.Text = $"Solicitudes por autorizar: {summary.PendingAuthorizations}";
        ExecutionText.Text = $"Aprobadas por ejecutar: {summary.ApprovedAwaitingExecution}";
        OpenShiftsText.Text = $"Turnos abiertos: {summary.OpenShifts}";
        ClosuresText.Text = $"Cierres por revisar: {summary.ClosuresAwaitingReview}";
        InvoiceText.Text = $"Facturas del periodo: {summary.Invoices} · notas de crédito: {FinanzasControl.Money(summary.CreditNotes)}";
        PaymentMethodsList.ItemsSource = summary.PaymentMethods;
        var trend = summary.Trend;
        var maxCharge = Math.Max(1m, trend.Max(item => item.Charges));
        var maxCollections = Math.Max(1m, trend.Max(item => item.Collections));
        TrendList.ItemsSource = trend.Select(item => new FinancialTrendDisplay(item.Label, item.Charges, item.Collections,
            (double)(item.Charges / maxCharge * 100m), (double)(item.Collections / maxCollections * 100m))).ToList();
        var diagnostics = _service.Diagnosticos().Take(4).ToList();
        DiagnosticsList.ItemsSource = diagnostics;
        NoDiagnosticsText.Visibility = diagnostics.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        UnsupportedCategoryNote.Text = "El desglose por categorías no se calcula aquí: los cargos de la muestra guardan el concepto como texto y no tienen un vínculo estructurado a concepto_cargo. Las facturas representan importes documentados, no cobros.";
    }
}
