using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class ComercialServicioControl : UserControl
{
    private ServicioModel? _service;
    public ComercialServicioControl() => InitializeComponent();
    public void Mostrar(ServicioModel service)
    {
        _service = service; PriceText.Text = $"Precio oficial al huésped: Bs {service.Precio:N2} · Precio independiente de cobertura y costo externo.";
        ProviderGrid.ItemsSource = SampleData.ConveniosServicios.Where(item => item.ServicioId == service.Id).Select(link =>
        {
            var agreement = SampleData.Convenios.FirstOrDefault(item => item.Id == link.ConvenioId);
            var current = agreement is not null && ConvenioService.EsVigente(agreement, DateTime.Today) && link.Activo;
            return new ServicioProveedorFila(agreement?.Empresa ?? "Proveedor no disponible", agreement?.Nombre ?? "Convenio no disponible",
                agreement is null ? "—" : $"{agreement.VigenciaDesde:dd/MM/yy} – {agreement.VigenciaHasta:dd/MM/yy}", link.CostoProveedor,
                service.Precio > 0 ? service.Precio - link.CostoProveedor : null, current ? "Vigente" : link.Activo ? "No vigente" : "Inactivo");
        }).ToList();
        PackageGrid.ItemsSource = new CatalogoServiciosService().PaquetesDe(service.Id).Select(item => new ServicioPaqueteFila(item.Paquete.Nombre,
            item.Beneficio.TipoCobertura, item.Beneficio.LimiteUsos?.ToString() ?? "Sin límite", item.Beneficio.Compartido ? "Compartido" : "Por huésped")).ToList();
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_service is null) return;
        var dialog = new ServicioDialog(_service, _service.PadreId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Guardado is { } saved) Mostrar(saved);
    }
}
