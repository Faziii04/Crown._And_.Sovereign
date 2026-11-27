using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Data;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CierreCompletadoDialog : Window
{
    private readonly CierreCajaModel _closure;
    private readonly decimal _cashCancellations;
    public CierreCompletadoDialog(CierreCajaModel closure)
    {
        InitializeComponent(); _closure = closure;
        var shift = closure.TurnoCajaId is int id ? SampleData.TurnosCaja.FirstOrDefault(item => item.Id == id) : null;
        _cashCancellations = SampleData.MovimientosCaja.Where(item => item.TurnoId == closure.TurnoCajaId && item.Tipo == "Anulación" && (SampleData.MetodosPago.FirstOrDefault(method => method.Nombre == item.Metodo)?.EsEfectivo ?? false)).Sum(item => item.Monto);
        DateText.Text = closure.Fecha.ToString("dd/MM/yyyy HH:mm"); ShiftText.Text = $"Turno #{closure.TurnoCajaId:0000} · {SampleData.Usuarios.FirstOrDefault(item => item.Id == closure.UsuarioId)?.Nombre ?? "Usuario"}";
        OpeningText.Text = $"Fondo inicial: Bs {closure.AperturaMonto:N2}"; CashText.Text = $"Cobros en efectivo: Bs {closure.EfectivoCobros:N2}"; RefundText.Text = $"Devoluciones en efectivo: Bs {closure.EfectivoDevoluciones:N2}"; CancellationText.Text = $"Anulaciones en efectivo: Bs {_cashCancellations:N2}";
        ExpectedText.Text = $"Efectivo esperado: Bs {closure.Esperado:N2}"; CountedText.Text = $"Efectivo contado: Bs {closure.Contado:N2}";
        DifferenceText.Text = closure.Diferencia == 0 ? "Sin diferencia" : $"Diferencia: Bs {closure.Diferencia:N2}"; NotesText.Text = closure.Observaciones;
    }
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var document = new FlowDocument { PagePadding = new Thickness(48), ColumnWidth = 480 };
        document.Blocks.Add(new Paragraph(new Run("CROWN & SOVEREIGN · CIERRE DE CAJA")) { FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (var value in new[] { $"Fecha: {_closure.Fecha:dd/MM/yyyy HH:mm}", $"Turno: #{_closure.TurnoCajaId:0000}", $"Fondo inicial: Bs {_closure.AperturaMonto:N2}", $"Cobros en efectivo: Bs {_closure.EfectivoCobros:N2}", $"Devoluciones: Bs {_closure.EfectivoDevoluciones:N2}", $"Anulaciones en efectivo: Bs {_cashCancellations:N2}", $"Esperado: Bs {_closure.Esperado:N2}", $"Contado: Bs {_closure.Contado:N2}", $"Diferencia: Bs {_closure.Diferencia:N2}", $"Observaciones: {_closure.Observaciones}" }) document.Blocks.Add(new Paragraph(new Run(value)) { Margin = new Thickness(0, 6, 0, 6) });
        var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) { document.PageHeight = dialog.PrintableAreaHeight; document.PageWidth = dialog.PrintableAreaWidth; dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Cierre de caja"); }
    }
}
