using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Supervision;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class OperacionesControl : UserControl
{
    private readonly SupervisionOperativaService _service = new();
    private bool _loading;

    public OperacionesControl()
    {
        InitializeComponent();
        SummaryView.AlertNavigationRequested += (_, alert) => AbrirAlerta(alert);
        SummaryView.NotificationRequested += (_, alert) => new NotificarSituacionOperativaDialog(alert) { Owner = Window.GetWindow(this) }.ShowDialog();
        if (FromPicker.SelectedDate is null) FromPicker.SelectedDate = DateTime.Today;
        if (ToPicker.SelectedDate is null) ToPicker.SelectedDate = DateTime.Today;
        Actualizar();
    }

    public void AbrirSeccion(string seccion)
    {
        Sections.SelectedIndex = seccion switch
        {
            "Habitaciones" => 1, "Limpieza" => 2, "Mantenimiento" => 3, "Servicios" => 4,
            "Inventario y recursos" or "Inventario" => 5, _ => 0
        };
    }

    private void AbrirAlerta(SupervisionAlerta alert)
    {
        AbrirSeccion(alert.Seccion);
        switch (alert.Seccion)
        {
            case "Habitaciones": RoomsView.AbrirAlerta(alert); break;
            case "Limpieza": HousekeepingView.AbrirAlerta(alert); break;
            case "Mantenimiento": MaintenanceView.AbrirAlerta(alert); break;
            case "Servicios": ServicesView.AbrirAlerta(alert); break;
            case "Inventario y recursos": InventoryView.AbrirAlerta(alert); break;
        }
    }

    private void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _loading) return;
        var custom = SelectedPeriod == "Periodo personalizado";
        FromPicker.Visibility = RangeSeparator.Visibility = ToPicker.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        if (!custom || FromPicker.SelectedDate is not null && ToPicker.SelectedDate is not null) Actualizar();
    }

    private string SelectedPeriod => (PeriodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hoy";
    private void Refresh_Click(object sender, RoutedEventArgs e) => Actualizar();
    private void Metric_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is string section) AbrirSeccion(section); }
    private void Section_Changed(object sender, SelectionChangedEventArgs e) { }

    private void Actualizar()
    {
        if (_loading || PeriodBox is null) return;
        _loading = true;
        try
        {
            var snapshot = _service.Obtener(SelectedPeriod, FromPicker.SelectedDate, ToPicker.SelectedDate);
            MetricList.ItemsSource = snapshot.Indicadores;
            SummaryView.Cargar(snapshot); RoomsView.Cargar(snapshot); HousekeepingView.Cargar(snapshot);
            MaintenanceView.Cargar(snapshot); ServicesView.Cargar(snapshot); InventoryView.Cargar(snapshot);
            UpdatedText.Text = $"Actualizado: {snapshot.Actualizado:HH:mm:ss}";
            PeriodNote.Text = snapshot.BasePeriodo;
        }
        finally { _loading = false; }
    }
}
