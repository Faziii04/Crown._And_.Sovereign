using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Paquetes;

public partial class CatalogoPaquetesControl : UserControl
{
    public event Action<PaqueteModel?>? PaqueteSeleccionado;
    public event EventHandler? FiltroCambiado;
    public CatalogoPaquetesControl() { InitializeComponent(); }
    public void Recargar(IEnumerable<PaqueteTarjetaView> items, int? selectedId = null)
    {
        PackagesList.ItemsSource = items.ToList();
        PackagesList.SelectedItem = PackagesList.Items.Cast<PaqueteTarjetaView>().FirstOrDefault(item => item.Paquete.Id == selectedId);
    }
    private void Search_TextChanged(object sender, EventArgs e) => FiltroCambiado?.Invoke(this, EventArgs.Empty);
    private void Packages_SelectionChanged(object sender, SelectionChangedEventArgs e) => PaqueteSeleccionado?.Invoke((PackagesList.SelectedItem as PaqueteTarjetaView)?.Paquete);
    public string Filtro => SearchBox.Text;
    public string EstadoFiltro => (StatusPicker.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
    public void Seleccionar(int? id) => PackagesList.SelectedItem = PackagesList.Items.Cast<PaqueteTarjetaView>().FirstOrDefault(item => item.Paquete.Id == id);
}
