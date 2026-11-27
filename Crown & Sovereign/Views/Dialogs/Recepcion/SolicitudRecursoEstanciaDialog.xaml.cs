using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class SolicitudRecursoEstanciaDialog : Window
{
    private readonly int _stayId;
    private int _quantity = 1;
    private readonly RecursoAdicionalService _resources = new();
    public SolicitudRecursoEstanciaDialog(int stayId)
    {
        InitializeComponent(); _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        ContextText.Text = $"{SampleData.Huespedes.First(item => item.Id == stay.HuespedId).Nombre} · Estancia #{stay.Id} · salida {stay.CheckOut:dd/MM/yyyy}";
        ResourceBox.ItemsSource = _resources.ListarActivos();
        ResourceBox.SelectedIndex = 0;
    }
    private RecursoAdicionalModel? SelectedResource => ResourceBox.SelectedItem as RecursoAdicionalModel;
    private void Resource_Changed(object sender, SelectionChangedEventArgs e) => RefreshQuote();
    private void Increase_Click(object sender, RoutedEventArgs e) { _quantity = Math.Min(20, _quantity + 1); RefreshQuote(); }
    private void Decrease_Click(object sender, RoutedEventArgs e) { _quantity = Math.Max(1, _quantity - 1); RefreshQuote(); }
    private void RefreshQuote()
    {
        if (!IsLoaded || SelectedResource is not { } resource) return;
        QuantityText.Text = _quantity.ToString();
        DescriptionText.Text = resource.Descripcion;
        var stay = SampleData.Estancias.First(item => item.Id == _stayId);
        var availability = _resources.ConsultarDisponibilidad(resource.RecursoId, DateTime.Today, stay.CheckOut.Date, stay.ReservaId);
        var existing = SampleData.ReservaRecursos.FirstOrDefault(item => item.ReservaId == stay.ReservaId && item.RecursoId == resource.RecursoId);
        var existingQuantity = existing?.Cantidad ?? 0;
        AvailabilityText.Text = availability is int remaining ? $"Libre frente a otras reservas: {remaining} unidad(es) · ya solicitadas aquí: {existingQuantity}" : $"Sin control de stock limitado · ya solicitadas aquí: {existingQuantity}";
        var nightlyLimitation = resource.TipoCobro == "POR_NOCHE" && resource.Precio > 0;
        var existingUnsupported = existing is not null && (existing.Estado is not ("PENDIENTE" or "SIN_VERIFICAR") || existing.CargoId.HasValue || existing.PrecioUnitarioAcordado is null || existing.TipoCobroAplicado is null);
        WarningText.Text = existingUnsupported ? "La asignación existente ya tiene condiciones históricas o un estado operativo que no permite cambiar cantidades." : nightlyLimitation ? "Este recurso se cobra por noche y la fecha de inicio no se guarda durante la estancia. El esquema actual impide calcular un cargo fiable." : "";
        PriceText.Text = $"Importe por asignación: Bs {(resource.TipoCobro == "POR_ESTANCIA" ? resource.Precio * (existingQuantity + _quantity) : 0m):N2}";
        SaveButton.IsEnabled = !nightlyLimitation && !existingUnsupported;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedResource is not { } resource) { WarningText.Text = "Selecciona un recurso."; return; }
        try { _resources.SolicitarDuranteEstancia(_stayId, resource.RecursoId, _quantity, NotesBox.Text); DialogResult = true; }
        catch (Exception ex) { WarningText.Text = ex.Message; }
    }
}
