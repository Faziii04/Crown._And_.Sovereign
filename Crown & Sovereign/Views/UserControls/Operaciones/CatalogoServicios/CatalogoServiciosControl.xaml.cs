using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class CatalogoServiciosControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    public CatalogoServiciosControl() { InitializeComponent(); CatalogPage.CatalogChanged += (_, _) => Actualizar(); Loaded += (_, _) => Actualizar(); }
    private void Actualizar()
    {
        ActiveCount.Text = _service.TotalActivos.ToString(); BookableCount.Text = _service.TotalReservables.ToString();
        AppointmentCount.Text = _service.TotalConCita.ToString(); ReviewCount.Text = _service.Revisiones().Count.ToString();
        ReadinessPage.Refrescar();
    }
    private void Activos_Click(object sender, RoutedEventArgs e) => CatalogPage.Filtrar("Activos");
    private void Reservables_Click(object sender, RoutedEventArgs e) => CatalogPage.Filtrar("Reservables");
    private void Appointments_Click(object sender, RoutedEventArgs e) => CatalogPage.Filtrar("Requieren cita");
    private void Review_Click(object sender, RoutedEventArgs e) { ReadinessPage.Refrescar(); if (VisualTreeHelperEx.FindParent<TabControl>(this) is { } tabs) tabs.SelectedIndex = 1; }
}

internal static class VisualTreeHelperEx
{
    public static T? FindParent<T>(DependencyObject? item) where T : DependencyObject
    {
        while (item is not null) { if (item is T found) return found; item = System.Windows.Media.VisualTreeHelper.GetParent(item); }
        return null;
    }
}
