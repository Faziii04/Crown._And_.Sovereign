using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Threading;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Simulador;

public partial class AdicionalesSimulacionControl : UserControl
{
    private int? _packageId = int.MinValue;
    public event EventHandler? CambiosCambiados;
    public IReadOnlyList<SimuladorLinea> ServiciosSeleccionados => _services.Where(item => item.Seleccionado).ToList();
    public IReadOnlyList<SimuladorLinea> RecursosSeleccionados => _resources.Where(item => item.Seleccionado).ToList();
    public IReadOnlyList<string> BeneficiosIncluidos { get; private set; } = [];
    private List<SimuladorLinea> _services = [];
    private readonly List<SimuladorLinea> _resources = [];

    public AdicionalesSimulacionControl()
    {
        InitializeComponent();
        _resources.AddRange(SampleData.RecursosAdicionales.Where(item => item.Activo).OrderBy(item => item.Tipo).ThenBy(item => item.Nombre)
            .Select(item => new SimuladorLinea { Id = item.RecursoId, Nombre = item.Nombre, Categoria = item.Tipo, Precio = item.Precio, Modalidad = item.TipoCobro }));
        foreach (var line in _resources) line.PropertyChanged += Line_PropertyChanged;
        ResourcesGrid.ItemsSource = _resources;
        ConfigurarPaquete(null);
    }

    public void ConfigurarPaquete(int? packageId)
    {
        if (_packageId == packageId) return;
        var prior = _services.ToDictionary(item => item.Id, item => (item.Seleccionado, item.Cantidad));
        _packageId = packageId;
        var includedIds = packageId is int id ? SampleData.PaquetesServicios.Where(item => item.PaqueteId == id).Select(item => item.ServicioId).ToHashSet() : [];
        _services = SampleData.Servicios.Where(item => item.Activo && item.Reservable && !includedIds.Contains(item.Id))
            .OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).Select(item =>
            {
                var vendors = SampleData.ConveniosServicios.Where(link => link.ServicioId == item.Id && link.Activo).Select(link => SampleData.Convenios.FirstOrDefault(agreement => agreement.Id == link.ConvenioId)?.Empresa).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
                var line = new SimuladorLinea { Id = item.Id, Nombre = item.Nombre, Categoria = item.Categoria, Precio = item.Precio, Modalidad = vendors.Count == 0 ? "Servicio del hotel" : $"Con proveedor · {string.Join(" / ", vendors)}" };
                if (prior.TryGetValue(item.Id, out var old)) { line.Seleccionado = old.Seleccionado; line.Cantidad = old.Cantidad; }
                line.PropertyChanged += Line_PropertyChanged; return line;
            }).ToList();
        ServicesGrid.ItemsSource = _services;
        BeneficiosIncluidos = packageId is int selected ? SampleData.PaquetesServicios.Where(item => item.PaqueteId == selected).Select(item =>
        {
            var service = SampleData.Servicios.FirstOrDefault(value => value.Id == item.ServicioId);
            var name = service?.Nombre ?? "Servicio no disponible";
            var allowance = item.TipoCobertura == "Ilimitado" ? "ilimitado" : $"{item.LimiteUsos} {(item.TipoCobertura == "Por día" ? "por día" : "por estancia")}";
            var products = SampleData.ServiciosProductos.Where(product => product.ServicioId == item.ServicioId).Select(product => SampleData.Productos.FirstOrDefault(value => value.Id == product.ProductoId) is { } p ? $" · {p.Nombre} × {product.CantidadIncluida}" : "");
            var vendorNames = service is null ? [] : SampleData.ConveniosServicios.Where(link => link.ServicioId == service.Id && link.Activo).Select(link => SampleData.Convenios.FirstOrDefault(agreement => agreement.Id == link.ConvenioId)?.Empresa).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
            var vendor = vendorNames.Count > 0 ? $" · coordinación con proveedor ({string.Join(" / ", vendorNames)})" : "";
            return $"• {name}: {allowance}{string.Concat(products)}{vendor}";
        }).ToList() : [];
        CambiosCambiados?.Invoke(this, EventArgs.Empty);
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName is nameof(SimuladorLinea.Seleccionado) or nameof(SimuladorLinea.Cantidad)) CambiosCambiados?.Invoke(this, EventArgs.Empty); }
    private void Line_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => Dispatcher.BeginInvoke(() => CambiosCambiados?.Invoke(this, EventArgs.Empty), DispatcherPriority.Background);
}
