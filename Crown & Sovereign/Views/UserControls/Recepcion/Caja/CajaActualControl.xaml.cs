using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class CajaActualControl : UserControl
{
    private readonly CajaService _cash = new();
    public event Action? SolicitarCierre;
    public event Action? SolicitarApertura;
    public CajaActualControl() => InitializeComponent();
    public void Mostrar()
    {
        var shift = _cash.ObtenerTurnoActual();
        if (shift is null)
        {
            OpenScroll.Visibility = Visibility.Collapsed;
            ClosedPanel.Visibility = Visibility.Visible;
            var last = _cash.ListarTurnos(filtro: "Cerrados").FirstOrDefault();
            var closure = last is null ? null : _cash.ObtenerCierre(last.Id);
            LastShiftText.Text = last is null ? "Sin turnos cerrados" : $"#{last.Id:0000} · {last.Apertura:dd/MM HH:mm}";
            LastExpectedText.Text = closure is null ? "—" : $"Bs {closure.Esperado:N2}";
            LastCountedText.Text = closure is null ? "—" : $"Bs {closure.Contado:N2}";
            LastDifferenceText.Text = closure is null ? "—" : $"Bs {closure.Diferencia:N2}";
            return;
        }
        ClosedPanel.Visibility = Visibility.Collapsed;
        OpenScroll.Visibility = Visibility.Visible;
        var summary = _cash.ObtenerResumenTurno(shift.Id);
        ShiftLabel.Text = $"Turno #{shift.Id:0000} · {SesionActual.Usuario?.Nombre} · apertura {shift.Apertura:dd/MM HH:mm}";
        ExpectedCashView.Mostrar(summary);
        PaymentMethodsView.Mostrar(summary);
        ShiftMovementsView.Mostrar(shift.Id);
    }
    private void CloseShift_Click(object sender, RoutedEventArgs e) => SolicitarCierre?.Invoke();
    private void OpenShift_Click(object sender, RoutedEventArgs e) => SolicitarApertura?.Invoke();
}
