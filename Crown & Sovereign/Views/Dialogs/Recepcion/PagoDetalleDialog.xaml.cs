using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class PagoDetalleDialog : Window
{
    private readonly PaymentDirectoryRow _row;
    public PagoDetalleDialog(PaymentDirectoryRow row)
    {
        InitializeComponent(); _row = row;
        SubtitleText.Text = $"Pago #{row.Id:0000} · folio {row.Folio} · {row.Fecha:dd/MM/yyyy HH:mm}";
        PaymentText.Text = $"Huésped: {row.Huesped}\nDocumento: {row.Documento}\nEstancia: {row.EstanciaId?.ToString() ?? "—"} · Reserva: {row.ReservaId?.ToString() ?? "—"}\nHabitación: {row.Habitacion}\nMétodo: {row.Metodo}\nImporte: Bs {row.Monto:N2}\nDevuelto: Bs {row.Reembolsado:N2}\nEstado: {row.Estado}\nReferencia: {row.Pago.Referencia}\nTurno: #{row.Pago.TurnoCajaId:0000}\nRegistrado por usuario: {row.Pago.UsuarioId}";
        RefundButton.IsEnabled = row.Estado is not ("Anulado" or "Devuelto"); CancelButton.IsEnabled = row.Estado is not ("Anulado" or "Devuelto" or "Parcialmente devuelto");
    }
    private void Refund_Click(object sender, RoutedEventArgs e) => Request("Devolución");
    private void Cancel_Click(object sender, RoutedEventArgs e) => Request("Anulación");
    private void Request(string type)
    {
        var dialog = new SolicitudCorreccionPagoDialog(_row, type) { Owner = this };
        if (dialog.ShowDialog() == true) CorrectionText.Text = $"Solicitud de {type.ToLowerInvariant()} creada para revisión de Gerencia.";
    }
}
