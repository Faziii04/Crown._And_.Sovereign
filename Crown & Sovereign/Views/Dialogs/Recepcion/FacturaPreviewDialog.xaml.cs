using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class FacturaPreviewDialog : Window
{
    public FacturaPreviewDialog(FacturaModel invoice)
    {
        InitializeComponent();
        _invoice = invoice;
        var isAttributed = invoice.Lineas.Count > 0 && SampleData.Folios.Any(item => item.Id == invoice.FolioId);
        var folio = isAttributed ? SampleData.Folios.FirstOrDefault(item => item.Id == invoice.FolioId) : null;
        var stay = isAttributed && invoice.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId) : null;
        var reservation = isAttributed && invoice.ReservaId is int reservationId ? SampleData.Reservas.FirstOrDefault(item => item.Id == reservationId) : null;
        var guest = folio?.HuespedPrincipalId is int guestId ? SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId) : stay is not null ? SampleData.Huespedes.FirstOrDefault(item => item.Id == stay.HuespedId) : null;
        InvoiceNumber.Text = invoice.Numero; InvoiceDate.Text = invoice.Fecha.ToString("dd/MM/yyyy"); GuestName.Text = invoice.Cliente.Length > 0 ? invoice.Cliente : guest?.Nombre ?? "Cliente histórico no atribuido"; GuestDocument.Text = invoice.DocumentoCliente.Length > 0 ? invoice.DocumentoCliente : guest?.Documento ?? "—";
        var roomId = invoice.HabitacionId ?? reservation?.Habitacion ?? (stay is null ? null : SampleData.Huespedes.FirstOrDefault(item => item.Id == stay.HuespedId)?.Habitacion);
        var room = roomId is int id ? SampleData.Habitaciones.FirstOrDefault(item => item.Id == id)?.Numero : null;
        Description.Text = invoice.Lineas.Count == 0 ? "Factura heredada · detalle de cargos no disponible · no atribuida a un folio verificado" : string.Join(Environment.NewLine, invoice.Lineas.Select(item => item.Concepto));
        Amount.Text = invoice.Lineas.Count == 0 ? "—" : string.Join(Environment.NewLine, invoice.Lineas.Select(item => $"Bs {item.Importe:N2}"));
        if (room is not null) Description.Text = $"Habitación {room}" + Environment.NewLine + Description.Text;
        TotalText.Text = $"Bs {invoice.Total:N2}";
    }
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var print = new System.Windows.Controls.PrintDialog();
        if (print.ShowDialog() != true) return;
        var document = new FlowDocument { PagePadding = new Thickness(48), ColumnWidth = 480 };
        document.Blocks.Add(new Paragraph(new Run("CROWN & SOVEREIGN · FACTURA")) { FontSize = 18, FontWeight = FontWeights.Bold });
        document.Blocks.Add(new Paragraph(new Run($"N.º {InvoiceNumber.Text} · {InvoiceDate.Text}")));
        document.Blocks.Add(new Paragraph(new Run($"Cliente: {GuestName.Text} · Documento: {GuestDocument.Text}")));
        document.Blocks.Add(new Paragraph(new Run($"Folio verificado: {(SampleData.Folios.Any(item => item.Id == _invoice.FolioId) && _invoice.Lineas.Count > 0 ? $"#{_invoice.FolioId}" : "No atribuido")}")));
        foreach (var line in _invoice.Lineas) document.Blocks.Add(new Paragraph(new Run($"{line.Cantidad} × {line.Concepto} · Bs {line.Importe:N2}")) { Margin = new Thickness(0, 5, 0, 5) });
        document.Blocks.Add(new Paragraph(new Run($"Subtotal: Bs {_invoice.Subtotal:N2}")) { Margin = new Thickness(0, 12, 0, 0) });
        document.Blocks.Add(new Paragraph(new Run($"Impuesto ({_invoice.PorcentajeImpuesto:N2}%): Bs {_invoice.Impuesto:N2}")));
        document.Blocks.Add(new Paragraph(new Run($"TOTAL: Bs {_invoice.Total:N2}")) { FontSize = 16, FontWeight = FontWeights.Bold });
        document.PageWidth = print.PrintableAreaWidth; document.PageHeight = print.PrintableAreaHeight;
        print.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Factura {_invoice.Numero}");
    }
    private readonly FacturaModel _invoice;
}
