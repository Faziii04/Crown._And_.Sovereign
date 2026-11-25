using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class CatalogoServiciosControl : UserControl
{
    private readonly ServicioService _service = new();
    private ServicioModel? _selected;
    public CatalogoServiciosControl() { InitializeComponent(); Recargar(); }
    private void Recargar()
    {
        var all = _service.Listar();
        ServiceTree.ItemsSource = all.Where(item => item.PadreId is null).OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).Select(parent => new ServiceTreeNode { Service = parent, Children = all.Where(child => child.PadreId == parent.Id).OrderBy(child => child.Nombre).Select(child => new ServiceTreeNode { Service = child }).ToList() }).ToList();
    }
    private void Service_Selected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selected = (e.NewValue as ServiceTreeNode)?.Service;
        if (_selected is null) return;
        ServiceName.Text = _selected.Nombre; ServiceCategory.Text = _selected.Categoria; DescriptionText.Text = _selected.PadreId is null ? $"Categoría de servicios con { _service.Listar().Count(item => item.PadreId == _selected.Id) } subservicios vinculados." : $"Servicio de { _selected.Categoria.ToLowerInvariant() } · {( _selected.RequiereCita ? "requiere coordinación de horario" : "disponible bajo solicitud" )}.";
        PriceText.Text = $"Bs {_selected.Precio:N2}"; AppointmentText.Text = _selected.RequiereCita ? "Requiere cita" : "Sin cita"; CoverageText.Text = _selected.Cobertura; DurationText.Text = $"{_selected.DuracionMinutos} min"; ProviderText.Text = $"{_selected.Proveedor} · {(SampleData.Productos.FirstOrDefault(product => product.Id == _selected.ProductoAsociadoId)?.Nombre ?? "Sin producto asociado")}";
        EditButton.IsEnabled = true;
    }
    private void NewRoot_Click(object sender, RoutedEventArgs e) => AbrirDialogo(null, null);
    private void NewChild_Click(object sender, RoutedEventArgs e) { if (_selected is null) { MessageBox.Show("Selecciona un servicio principal para agregar un subservicio."); return; } AbrirDialogo(null, _selected.PadreId ?? _selected.Id); }
    private void Edit_Click(object sender, RoutedEventArgs e) { if (_selected is not null) AbrirDialogo(_selected, _selected.PadreId); }
    private void AbrirDialogo(ServicioModel? service, int? parentId)
    {
        var dialog = new ServicioDialog(service, parentId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Recargar();
    }
}
