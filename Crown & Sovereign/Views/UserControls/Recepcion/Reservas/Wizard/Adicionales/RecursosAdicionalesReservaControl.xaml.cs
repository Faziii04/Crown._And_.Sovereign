using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard.Adicionales;

public partial class RecursosAdicionalesReservaControl : UserControl
{
    private readonly RecursoAdicionalService _service = new();
    private BorradorReserva? _draft;
    private DateTime _arrival, _departure;
    private int? _excludeReservationId;
    private List<RecursoAdicionalOpcion> _options = [];
    public event EventHandler? DraftChanged;
    public RecursosAdicionalesReservaControl() { InitializeComponent(); CategoryBox.ItemsSource = new[] { "Todas las categorías", "MOBILIARIO", "ROPA_DE_CAMA", "EQUIPAMIENTO" }; CategoryBox.SelectedIndex = 0; }

    public void Configurar(BorradorReserva draft, DateTime arrival, DateTime departure, int? excludeReservationId)
    {
        _draft = draft; _arrival = arrival.Date; _departure = departure.Date; _excludeReservationId = excludeReservationId;
        var ids = draft.RecursosAdicionales.Select(item => item.RecursoId).ToHashSet();
        var resources = _service.ListarActivos().Concat(SampleData.RecursosAdicionales.Where(item => ids.Contains(item.RecursoId))).DistinctBy(item => item.RecursoId);
        _options = resources.Select(resource =>
        {
            var line = draft.RecursosAdicionales.FirstOrDefault(item => item.RecursoId == resource.RecursoId);
            var available = _service.ConsultarDisponibilidad(resource.RecursoId, _arrival, _departure, excludeReservationId);
            return new RecursoAdicionalOpcion
            {
                Recurso = resource, PrecioUnitarioAcordado = line is null ? resource.Precio : line.PrecioUnitarioAcordado,
                TipoCobroAplicado = line is null ? resource.TipoCobro : line.TipoCobroAplicado,
                Cantidad = line?.Cantidad ?? 0, CantidadMaxima = available ?? 999,
                Noches = Math.Max(1, (_departure - _arrival).Days),
                DisponibilidadTexto = available is int count ? count == 0 ? "Sin disponibilidad para estas fechas" : $"Disponibles: {count}" : "Sin control de stock limitado",
                DisponibilidadDetalle = available is int ? "Disponibilidad calculada para el periodo seleccionado." : "El inventario no controla una cantidad máxima.",
                Observaciones = line?.Observaciones ?? ""
            };
        }).ToList();
        RefreshList(); UpdateTotal();
    }

    public void ActualizarFechas(DateTime arrival, DateTime departure)
    {
        if (_draft is null) return;
        Configurar(_draft, arrival, departure, _excludeReservationId);
    }

    private void RefreshList()
    {
        var search = SearchBox.Text?.Trim() ?? ""; var category = CategoryBox.SelectedItem?.ToString();
        var filtered = _options.Where(item => (category is null or "Todas las categorías" || item.Tipo.Equals(category, StringComparison.OrdinalIgnoreCase))
            && (search.Length == 0 || $"{item.Nombre} {item.Tipo} {item.Descripcion}".Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
        ResourceList.ItemsSource = filtered; EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => RefreshList();
    private void Increase_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RecursoAdicionalOpcion item || _draft is null) return;
        var next = item.Cantidad + 1;
        try { _service.ValidarCantidadDisponible(item.Recurso.RecursoId, _arrival, _departure, next, _excludeReservationId); item.Cantidad = next; Sync(item); }
        catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Disponibilidad del recurso", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RecursoAdicionalOpcion item || _draft is null) return;
        item.Cantidad = Math.Max(0, item.Cantidad - 1); Sync(item);
    }
    private void Observation_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is RecursoAdicionalOpcion item && _draft is not null && item.Cantidad > 0) Sync(item);
    }
    private void Sync(RecursoAdicionalOpcion item)
    {
        var line = _draft!.RecursosAdicionales.FirstOrDefault(value => value.RecursoId == item.Recurso.RecursoId);
        if (item.Cantidad == 0) { if (line is not null) _draft.RecursosAdicionales.Remove(line); }
        else if (line is null) _draft.RecursosAdicionales.Add(new() { RecursoId = item.Recurso.RecursoId, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, TipoCobroAplicado = item.TipoCobroAplicado, Observaciones = item.Observaciones });
        else { line.Cantidad = item.Cantidad; line.Observaciones = item.Observaciones; }
        UpdateTotal(); DraftChanged?.Invoke(this, EventArgs.Empty);
    }
    private void UpdateTotal()
    {
        if (_draft is null) { TotalText.Text = ""; SelectedText.Text = ""; return; }
        TotalText.Text = $"Total recursos: Bs {_draft.RecursosAdicionales.Sum(item => item.PrecioUnitarioAcordado is decimal price ? _service.CalcularImporte(price, item.TipoCobroAplicado ?? "POR_ESTANCIA", item.Cantidad, Math.Max(1, (_departure - _arrival).Days)) : 0m):N2}";
        SelectedText.Text = _draft.RecursosAdicionales.Count == 0 ? "Sin recursos seleccionados." : "Seleccionados: " + string.Join(" · ", _draft.RecursosAdicionales.Select(item => $"{SampleData.RecursosAdicionales.FirstOrDefault(resource => resource.RecursoId == item.RecursoId)?.Nombre} × {item.Cantidad}"));
    }
}
