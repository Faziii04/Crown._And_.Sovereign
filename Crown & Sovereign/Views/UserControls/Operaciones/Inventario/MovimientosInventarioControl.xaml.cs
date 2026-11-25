using System.Windows.Controls;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Inventario;
public partial class MovimientosInventarioControl : UserControl
{
    private readonly InventarioService _service = new();
    public MovimientosInventarioControl() { InitializeComponent(); TypeBox.ItemsSource = new[] { "Todos los movimientos", "Entrada", "Salida", "Consumo", "Ajuste" }; TypeBox.SelectedIndex = 0; }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => Refresh();
    private void Filter_Changed(object sender, TextChangedEventArgs e) => Refresh();
    private void Refresh() { if (MovementGrid is not null) MovementGrid.ItemsSource = _service.ConsultarMovimientos(SearchBox.Text, TypeBox.SelectedItem as string); }
}
