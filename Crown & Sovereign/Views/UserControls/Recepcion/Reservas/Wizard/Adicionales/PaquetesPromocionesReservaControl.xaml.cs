using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard.Adicionales;

public partial class PaquetesPromocionesReservaControl : UserControl
{
    public event EventHandler? SeleccionCambiada;
    public ComboBox Paquete => PackagePicker;
    public ComboBox Cantidad => PackageQuantity;
    public ComboBox Promocion => PromotionPicker;
    public void MostrarPrecioAcordado(decimal? price) => PackagePrice.Text = price is decimal value ? $"Precio acordado: Bs {value:N2} por paquete" : "";
    public PaquetesPromocionesReservaControl()
    {
        InitializeComponent(); PackageQuantity.ItemsSource = Enumerable.Range(1, 5).ToList(); PackageQuantity.SelectedIndex = 0;
    }
    private void Package_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PackagePicker.SelectedItem is string name)
        {
            var package = SampleData.Paquetes.FirstOrDefault(item => item.Nombre == name);
            PackageDescription.Text = package?.Descripcion ?? "Sin paquete adquirido.";
            PackagePrice.Text = package is null ? "" : $"Bs {package.Precio:N2} por paquete";
            PackageQuantity.IsEnabled = package is not null;
        }
        SeleccionCambiada?.Invoke(this, EventArgs.Empty);
    }
}
