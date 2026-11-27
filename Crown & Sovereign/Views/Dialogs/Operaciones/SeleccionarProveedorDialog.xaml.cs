using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class SeleccionarProveedorDialog : Window
{
    public int? ConvenioSeleccionadoId { get; private set; }
    public SeleccionarProveedorDialog(int servicioId, DateTime fecha)
    {
        InitializeComponent();
        var service = SampleData.Servicios.First(item => item.Id == servicioId);
        IntroText.Text = $"{service.Nombre} · servicio previsto para {fecha:dd/MM/yyyy HH:mm}. Elige explícitamente el proveedor que realizará esta operación.";
        PriceText.Text = $"Precio oficial de catálogo al huésped: Bs {service.Precio:N2}. La cobertura adquirida puede cambiar el cobro real.";
        ProviderList.ItemsSource = new ConvenioService().CompararProveedores(servicioId, fecha);
        EmptyText.Visibility = ProviderList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ConfirmButton.IsEnabled = ProviderList.Items.Count > 0;
    }
    private void Provider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderList.SelectedItem is ConvenioServicioVista row && row.EstadoMargen == "Margen negativo")
            WarningText.Text = $"Atención: el costo de Bs {row.Relacion.CostoProveedor:N2} supera el precio oficial de Bs {row.PrecioOficial:N2}. Este es un aviso comercial; confirma que la asignación sea intencional.";
        else WarningText.Text = "";
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (ProviderList.SelectedItem is not ConvenioServicioVista row) { WarningText.Text = "Selecciona un proveedor."; return; }
        ConvenioSeleccionadoId = row.Convenio.Id; DialogResult = true;
    }
}
