using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class FacturacionFinancieraControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    private FinancialPeriod _period = new("Este mes", DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today, DateTime.Today.AddDays(1));
    public FacturacionFinancieraControl()
    {
        InitializeComponent();
        InvoiceGrid.ItemsSource = Array.Empty<object>();
    }
    public void Actualizar(FinancialPeriod period) { _period = period; Recargar(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) => Recargar();
    private void Recargar()
    {
        if (InvoiceGrid is null) return;
        InvoiceGrid.ItemsSource = _service.Facturas(_period, SearchBox.Text.Trim());
        InvoiceGrid.SelectedIndex = InvoiceGrid.Items.Count > 0 ? 0 : -1;
    }
    private void Invoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InvoiceGrid.SelectedItem is not FinancialInvoiceRow row)
        {
            InvoiceDetail.Text = "Selecciona una factura para consultar los importes y sus líneas disponibles.";
            CreditNetText.Text = ""; InvoiceLines.ItemsSource = null; CreditGrid.ItemsSource = null; return;
        }
        InvoiceDetail.Text = $"{row.Guest} · {row.Context} · Folio {row.Folio}\nSubtotal {FinanzasControl.Money(row.Subtotal)} + impuesto {FinanzasControl.Money(row.Tax)} = total {FinanzasControl.Money(row.Total)}. Créditos documentados {FinanzasControl.Money(row.CreditNotes)}; neto documentado {FinanzasControl.Money(row.DocumentedNet)}. No equivale a cobro.";
        InvoiceLines.ItemsSource = row.Invoice.Lineas;
        var credits = _service.NotasCredito(row.Invoice.Id);
        CreditGrid.ItemsSource = credits;
        CreditNetText.Text = credits.Count == 0 ? "Sin notas de crédito" : $"Neto doc. {FinanzasControl.Money(row.DocumentedNet)}";
    }
}
