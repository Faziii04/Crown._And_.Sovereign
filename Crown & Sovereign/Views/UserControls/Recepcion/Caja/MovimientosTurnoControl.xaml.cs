using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class MovimientosTurnoControl : UserControl
{
    private readonly CajaService _cash = new();
    private int _shiftId;
    public MovimientosTurnoControl()
    {
        InitializeComponent();
        TypeBox.ItemsSource = new[] { "Todos los movimientos", "Cobro", "Devolución", "Anulación" };
        TypeBox.SelectedIndex = 0;
        MethodBox.ItemsSource = new[] { "Todos los medios" }.Concat(SampleData.MetodosPago.Where(item => item.Activo).Select(item => item.Nombre)).ToList();
        MethodBox.SelectedIndex = 0;
    }
    public void Mostrar(int shiftId) { _shiftId = shiftId; Recargar(); }
    public void Recargar()
    {
        if (_shiftId <= 0) { MovementsGrid.ItemsSource = null; CountText.Text = "Sin movimientos para este turno."; return; }
        var rows = _cash.ListarMovimientos(_shiftId, TypeBox.SelectedItem?.ToString() ?? "Todos los movimientos", SearchBox.Text, MethodBox.SelectedItem?.ToString());
        MovementsGrid.ItemsSource = rows;
        CountText.Text = $"{rows.Count} movimientos asociados únicamente al turno #{_shiftId:0000}";
    }
    private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e) => Recargar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
}
