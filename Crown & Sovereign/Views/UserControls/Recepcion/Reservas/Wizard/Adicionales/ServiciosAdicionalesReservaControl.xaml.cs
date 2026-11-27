using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard.Adicionales;

public partial class ServiciosAdicionalesReservaControl : UserControl
{
    private BorradorReserva? _draft;
    private List<ServicioAdicionalOpcion> _options = [];
    public event EventHandler? DraftChanged;
    public ServiciosAdicionalesReservaControl() { InitializeComponent(); CoverageBox.ItemsSource = new[] { "Todos", "Incluidos", "Adicionales", "Requieren cita" }; CoverageBox.SelectedIndex = 0; }

    public void Configurar(BorradorReserva draft, IEnumerable<HuespedModel> guests)
    {
        _draft = draft;
        var selectedPackage = draft.PaqueteId;
        var recipients = new[] { new HuespedReservaChoice(null, "Compartido / sin destinatario") }
            .Concat(guests.Select(guest => new HuespedReservaChoice(guest.Id, $"{guest.Nombre} · {guest.RolEnReserva}"))).ToList();
        var serviceCatalog = new ServicioService().ListarReservables();
        var benefitService = new ServicioBeneficioService();
        _options = serviceCatalog
            .Select(service =>
            {
                var benefit = benefitService.EvaluarBorrador(selectedPackage, service.Id, draft.Llegada, draft.Salida, draft.CantidadPaquete);
                var unlimited = benefit.Unlimited;
                var included = benefit.IncludedAllowance;
                var line = draft.ServiciosAdquiridos.FirstOrDefault(item => item.ServicioId == service.Id);
                return new ServicioAdicionalOpcion
                {
                    Servicio = service, Categoria = service.PadreId is int parentId ? $"{service.Categoria} / {serviceCatalog.FirstOrDefault(parent => parent.Id == parentId)?.Nombre ?? "Otro"}" : service.Categoria,
                    Proveedor = string.Join(" · ", new ConvenioService().CompararProveedores(service.Id).Select(provider => provider.Convenio.Empresa)),
                    Cobertura = benefit.Coverage,
                    Incluidos = included, IncluidoIlimitado = unlimited, Cantidad = line?.Cantidad ?? 0,
                    PrecioUnitarioAcordado = line is null ? service.Precio : line.PrecioUnitarioAcordado,
                    HuespedId = line?.HuespedId, Observaciones = line?.Observaciones ?? "", Destinatarios = recipients
                };
            }).ToList();
        var categories = new[] { "Todas las categorías" }.Concat(_options.Select(item => item.Categoria).Distinct().Order()).ToList();
        var previousCategory = CategoryBox.SelectedItem?.ToString();
        CategoryBox.ItemsSource = categories;
        CategoryBox.SelectedItem = previousCategory is not null && categories.Contains(previousCategory) ? previousCategory : categories[0];
        RefreshList(); UpdateTotal();
    }
    public void ActualizarCobertura(int? packageId, IEnumerable<HuespedModel> guests)
    {
        if (_draft is null) return;
        _draft.PaqueteId = packageId;
        Configurar(_draft, guests);
    }
    private void RefreshList()
    {
        var search = SearchBox.Text?.Trim() ?? ""; var category = CategoryBox.SelectedItem?.ToString(); var filter = CoverageBox.SelectedItem?.ToString();
        var filtered = _options.Where(item => (CategoryBox.SelectedItem?.ToString() is not string selectedCategory || selectedCategory == "Todas las categorías" || item.Categoria == selectedCategory)
            && (search.Length == 0 || $"{item.Nombre} {item.Categoria} {item.Descripcion}".Contains(search, StringComparison.OrdinalIgnoreCase))
            && (filter is null or "Todos" || filter == "Incluidos" && (item.Incluidos > 0 || item.IncluidoIlimitado) || filter == "Adicionales" && !item.IncluidoIlimitado || filter == "Requieren cita" && item.RequiereCita)).ToList();
        ServiceList.ItemsSource = filtered; EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => RefreshList();
    private void Increase_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ServicioAdicionalOpcion item || _draft is null) return;
        if (!item.HabilitarCompra) return;
        item.Cantidad++; Sync(item);
    }
    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ServicioAdicionalOpcion item || _draft is null) return;
        item.Cantidad = Math.Max(0, item.Cantidad - 1); Sync(item);
    }
    private void Recipient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ServicioAdicionalOpcion item && _draft is not null && item.Cantidad > 0) Sync(item);
    }
    private void Observation_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ServicioAdicionalOpcion item && _draft is not null && item.Cantidad > 0) Sync(item);
    }
    private void Sync(ServicioAdicionalOpcion item)
    {
        var line = _draft!.ServiciosAdquiridos.FirstOrDefault(value => value.ServicioId == item.Servicio.Id);
        if (item.Cantidad == 0) { if (line is not null) _draft.ServiciosAdquiridos.Remove(line); }
        else if (line is null) _draft.ServiciosAdquiridos.Add(new() { ServicioId = item.Servicio.Id, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, HuespedId = item.HuespedId, Observaciones = item.Observaciones });
        else { line.Cantidad = item.Cantidad; line.HuespedId = item.HuespedId; line.Observaciones = item.Observaciones; }
        UpdateTotal(); DraftChanged?.Invoke(this, EventArgs.Empty);
    }
    private void UpdateTotal()
    {
        if (_draft is null) { TotalText.Text = ""; SelectedText.Text = ""; return; }
        TotalText.Text = $"Total servicios: Bs {_draft.ServiciosAdquiridos.Sum(item => item.PrecioUnitarioAcordado.GetValueOrDefault() * item.Cantidad):N2}";
        SelectedText.Text = _draft.ServiciosAdquiridos.Count == 0 ? "Todavía no hay servicios adicionales seleccionados." : "Adquiridos: " + string.Join(" · ", _draft.ServiciosAdquiridos.Select(item => $"{SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Nombre} × {item.Cantidad}"));
    }
}
