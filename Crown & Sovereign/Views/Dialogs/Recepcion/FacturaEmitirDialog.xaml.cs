using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class FacturaEmitirDialog : Window
{
    private readonly InvoiceDraftModel _draft;
    private readonly FacturaService _service = new();
    public FacturaModel? FacturaEmitida { get; private set; }
    public FacturaEmitirDialog(InvoiceDraftModel draft)
    {
        InitializeComponent(); _draft = draft; FolioText.Text = $"Folio #{draft.FolioId} · Se emitirán solo cargos no facturados."; ClientBox.Text = draft.Cliente; DocumentBox.Text = draft.Documento; LinesGrid.ItemsSource = draft.Lineas; ActualizarTotales();
    }
    private void Client_Changed(object sender, TextChangedEventArgs e) { }
    private void ActualizarTotales()
    {
        SubtotalText.Text = $"Subtotal · Bs {_draft.Subtotal:N2}"; TaxText.Text = $"Impuesto ({_draft.PorcentajeImpuesto:N2}%) · Bs {_draft.Impuesto:N2}"; TotalText.Text = $"Total · Bs {_draft.Total:N2}";
    }
    private void Emit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ClientBox.Text)) { ErrorText.Text = "Indica el nombre o razón social del cliente."; return; }
        try
        {
            FacturaEmitida = _service.Emitir(_draft with { Cliente = ClientBox.Text.Trim(), Documento = DocumentBox.Text.Trim() }); DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
