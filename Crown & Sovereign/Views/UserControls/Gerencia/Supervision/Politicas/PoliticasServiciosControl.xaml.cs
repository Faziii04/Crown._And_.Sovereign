using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Politicas;

public partial class PoliticasServiciosControl : UserControl
{
    private readonly PoliticaHotelService _service = new();
    private PoliticaServicioModel _saved = Draft();
    private bool _loading;
    public event Action? ConfigurationChanged;
    public PoliticasServiciosControl()
    {
        InitializeComponent();
        LateType.ItemsSource = new[] { "Ninguna", "Monto fijo", "Porcentaje" };
        NoShowType.ItemsSource = new[] { "Ninguna", "Monto fijo", "Porcentaje" };
        Refresh();
    }
    private void Refresh()
    {
        _loading = true;
        var current = _service.PoliticaServicioActual;
        _saved = current ?? Draft();
        MissingBanner.Visibility = current is null ? Visibility.Visible : Visibility.Collapsed;
        HoursBox.Text = _saved.HorasCancelacionGratuita.ToString();
        LateType.SelectedItem = DisplayType(_saved.TipoPenalizacionCancelacion); LateValue.Text = _saved.ValorPenalizacionCancelacion.ToString(System.Globalization.CultureInfo.InvariantCulture);
        LateConsume.IsChecked = _saved.ConsumirBeneficioCancelacionTardia;
        NoShowType.SelectedItem = DisplayType(_saved.TipoPenalizacionInasistencia); NoShowValue.Text = _saved.ValorPenalizacionInasistencia.ToString(System.Globalization.CultureInfo.InvariantCulture);
        NoShowConsume.IsChecked = _saved.ConsumirBeneficioInasistencia;
        _loading = false; DirtyText.Text = current is null ? "Sin configurar · borrador no guardado" : "Configuración guardada";
    }
    private PoliticaServicioModel ReadDraft()
    {
        if (!int.TryParse(HoursBox.Text, out var hours)) throw new InvalidOperationException("Las horas de cancelación deben ser un número entero.");
        if (!decimal.TryParse(LateValue.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var late)) throw new InvalidOperationException("El valor por cancelación tardía no es válido.");
        if (!decimal.TryParse(NoShowValue.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var noShow)) throw new InvalidOperationException("El valor por inasistencia no es válido.");
        return new(_saved.HotelId, hours, Code(NoShowType.SelectedItem as string), noShow, Code(LateType.SelectedItem as string), late,
            NoShowConsume.IsChecked == true, LateConsume.IsChecked == true);
    }
    private void Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        try { var draft = ReadDraft(); DirtyText.Text = draft == _saved ? "Sin cambios pendientes." : "Cambios sin guardar. No se aplican a nuevas operaciones hasta guardar."; }
        catch { DirtyText.Text = "Borrador incompleto o inválido; revise los valores antes de guardar."; }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var draft = ReadDraft(); PoliticaHotelService.ValidarPoliticaServicio(draft);
            if (draft == _saved) { DirtyText.Text = "No hay cambios para guardar."; return; }
            var confirmation = $"Guardar política de servicios?\n\nCancelación gratuita: {_saved.HorasCancelacionGratuita} → {draft.HorasCancelacionGratuita} horas\nCancelación tardía: {_saved.TipoPenalizacionCancelacion} {_saved.ValorPenalizacionCancelacion} → {draft.TipoPenalizacionCancelacion} {draft.ValorPenalizacionCancelacion}\nInasistencia: {_saved.TipoPenalizacionInasistencia} {_saved.ValorPenalizacionInasistencia} → {draft.TipoPenalizacionInasistencia} {draft.ValorPenalizacionInasistencia}\n\nLas citas nuevas pueden conservar un snapshot. No se cambian citas existentes.";
            if (MessageBox.Show(confirmation, "Confirmar cambios de política", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _service.GuardarServicio(draft); Refresh(); ConfigurationChanged?.Invoke();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo guardar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Reset_Click(object sender, RoutedEventArgs e) => Refresh();
    private void Simulate_Click(object sender, RoutedEventArgs e)
    {
        try { var draft = ReadDraft(); PoliticaHotelService.ValidarPoliticaServicio(draft); new SimularPoliticaServicioDialog(_service, draft) { Owner = Window.GetWindow(this) }.ShowDialog(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se puede simular", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private static PoliticaServicioModel Draft() => new(1, 0, "NINGUNA", 0m, "NINGUNA", 0m, false, false);
    private static string DisplayType(string code) => code.Trim().ToUpperInvariant() switch { "NINGUNA" or "NINGUNO" or "SIN PENALIZACIÓN" or "SIN PENALIZACION" => "Ninguna", "MONTO_FIJO" or "MONTO FIJO" or "FIJO" => "Monto fijo", _ => "Porcentaje" };
    private static string Code(string? display) => display switch { "Monto fijo" => "MONTO_FIJO", "Porcentaje" => "PORCENTAJE", _ => "NINGUNA" };
}
