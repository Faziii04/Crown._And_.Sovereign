using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class DetalleServicioControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    private ServicioModel? _selected;
    public event EventHandler? ConfigurationChanged;
    public DetalleServicioControl()
    {
        InitializeComponent();
        if (DetailTabs.Items[4] is TabItem imagesTab) imagesTab.Visibility = SesionActual.Usuario?.Rol == "Gerente" ? Visibility.Visible : Visibility.Collapsed;
        OperationPage.ConfigurationChanged += (_, _) => { if (_selected is not null) Mostrar(_selected); ConfigurationChanged?.Invoke(this, EventArgs.Empty); };
        ProductsPage.ConfigurationChanged += (_, _) => { if (_selected is not null) Mostrar(_selected); ConfigurationChanged?.Invoke(this, EventArgs.Empty); };
        ImagesPage.ConfigurationChanged += (_, _) => { if (_selected is not null) Mostrar(_selected); ConfigurationChanged?.Invoke(this, EventArgs.Empty); };
    }
    public void Mostrar(ServicioModel? service)
    {
        _selected = service;
        EmptyState.Visibility = service is null ? Visibility.Visible : Visibility.Collapsed;
        DetailTabs.Visibility = service is null ? Visibility.Collapsed : Visibility.Visible;
        EditButton.IsEnabled = service is not null;
        if (service is null) { ServiceName.Text = "Selecciona un servicio"; ServicePath.Text = ""; StatusLine.Text = ""; PriceText.Text = ""; DurationText.Text = ""; EmployeeCount.Text = ""; PackageCount.Text = ""; StateButton.Content = "Estado"; return; }
        ServiceName.Text = service.Nombre;
        var parts = new Stack<string>(); ServicioModel? current = service; var visited = new HashSet<int>();
        while (current is not null && visited.Add(current.Id)) { parts.Push(current.Nombre); current = SampleData.Servicios.FirstOrDefault(item => item.Id == current.PadreId); }
        ServicePath.Text = $"{service.Categoria} · {string.Join(" › ", parts)}";
        StatusLine.Text = $"{(service.Activo ? "Activo" : "Inactivo")}  ·  {(service.Reservable ? "Reservable" : "Agrupación organizativa")}  ·  {(service.RequiereCita ? "Requiere cita" : "Sin cita requerida")}";
        PriceText.Text = $"Bs {service.Precio:N2}"; DurationText.Text = service.RequiereCita ? $"{service.DuracionMinutos} min" : "No aplica";
        EmployeeCount.Text = _service.EmpleadosDe(service.Id).Count.ToString(); PackageCount.Text = _service.PaquetesDe(service.Id).Count.ToString();
        StateButton.Content = service.Activo ? "Desactivar" : "Activar"; StateButton.IsEnabled = true;
        StateButton.Visibility = SesionActual.Usuario?.Rol == "Gerente" ? Visibility.Visible : Visibility.Collapsed;
        InfoPage.Mostrar(service); OperationPage.Mostrar(service); CommercialPage.Mostrar(service); ProductsPage.Mostrar(service); ImagesPage.Mostrar(service);
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var dialog = new ServicioDialog(_selected, _selected.PadreId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Guardado is { } saved) { Mostrar(saved); ConfigurationChanged?.Invoke(this, EventArgs.Empty); }
    }
    private void State_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var next = !_selected.Activo; var deps = _service.Dependencias(_selected.Id);
        var notice = next ? "El servicio volverá a ofrecerse para nuevas solicitudes." :
            $"No se ofrecerá para nuevas solicitudes. Se conservarán {deps.BeneficiosAdquiridos} beneficios adquiridos y {deps.UsosHistoricos} registros históricos; hay {deps.Solicitudes + deps.Citas} solicitudes o citas abiertas.";
        if (MessageBox.Show(notice + "\n\n¿Continuar?", next ? "Activar servicio" : "Desactivar servicio", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _service.CambiarActivo(_selected, next); Mostrar(_selected); ConfigurationChanged?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo actualizar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
