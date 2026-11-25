using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class CajaControl : UserControl
{
    private readonly CajaService _cash = new();
    public CajaControl()
    {
        InitializeComponent();
        CurrentCashView.SolicitarCierre += AbrirCierre;
        CurrentCashView.SolicitarApertura += AbrirApertura;
        Actualizar();
    }
    private void Actualizar()
    {
        var shift = _cash.ObtenerTurnoActual();
        var user = SesionActual.Usuario;
        if (shift is null)
        {
            ShiftDot.Fill = System.Windows.Media.Brushes.DarkGoldenrod;
            ShiftTitle.Text = "Caja cerrada";
            ShiftSubtitle.Text = "No tienes un turno abierto. Puedes consultar pagos, facturas y turnos anteriores.";
            ShiftActionButton.Content = "Abrir turno";
        }
        else
        {
            ShiftDot.Fill = System.Windows.Media.Brushes.SeaGreen;
            ShiftTitle.Text = $"Caja abierta · Turno #{shift.Id:0000}";
            ShiftSubtitle.Text = $"Recepcionista: {user?.Nombre ?? "Usuario"} · Apertura {shift.Apertura:dd MMM · HH:mm} · Fondo Bs {shift.MontoInicial:N2}";
            ShiftActionButton.Content = "Cerrar turno";
        }
        CurrentCashView.Mostrar();
        PaymentsView.Recargar(); InvoicesView.Recargar(); CorrectionsView.Recargar(); ClosuresView.Recargar();
    }
    private void ShiftAction_Click(object sender, RoutedEventArgs e)
    {
        if (_cash.ObtenerTurnoActual() is null) AbrirApertura(); else AbrirCierre();
    }
    private void AbrirApertura()
    {
        var dialog = new AperturaCajaDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Actualizar();
    }
    private void AbrirCierre()
    {
        if (_cash.ObtenerTurnoActual() is not { } shift) return;
        var dialog = new CierreCajaDialog(shift.Id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Actualizar();
    }
    private void RegisterPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_cash.ObtenerTurnoActual() is null) { MessageBox.Show("Abre tu turno para registrar un pago.", "Caja cerrada", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var selector = new SelectorFolioPagoDialog() { Owner = Window.GetWindow(this) };
        if (selector.ShowDialog() != true || selector.FolioIdSeleccionado is not int folioId) return;
        if (new PagoDialog(folioId) { Owner = Window.GetWindow(this) }.ShowDialog() == true) Actualizar();
    }
}
