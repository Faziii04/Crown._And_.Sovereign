using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class ImagenesServicioControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    private ServicioModel? _current;
    public event EventHandler? ConfigurationChanged;
    public ImagenesServicioControl() => InitializeComponent();
    public void Mostrar(ServicioModel service) { _current = service; ImageGrid.ItemsSource = _service.ImagenesDe(service.Id); }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        try { _service.GuardarImagen(_current.Id, UrlBox.Text, DescriptionBox.Text, MainCheck.IsChecked == true); UrlBox.Clear(); DescriptionBox.Clear(); MainCheck.IsChecked = false; Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Imagen no guardada", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ImageGrid.SelectedItem is not ServicioImagenModel image) return;
        _service.QuitarImagen(image.Id); if (_current is not null) Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}
