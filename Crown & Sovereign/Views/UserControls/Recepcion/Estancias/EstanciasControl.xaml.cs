using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class EstanciasControl : UserControl
{
    private readonly EstanciaService _service = new();
    public EstanciasControl()
    {
        InitializeComponent();
        FloorFilter.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" };
        FloorFilter.SelectedIndex = 0;
        TypeFilter.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).Distinct().ToList();
        TypeFilter.SelectedIndex = 0;
        StatusFilter.ItemsSource = new[] { "Activas", "Finalizadas" };
        StatusFilter.SelectedIndex = 0;
        DepartureFilter.ItemsSource = new[] { "Todas las activas", "Salidas de hoy", "Salidas vencidas", "Requieren atención", "Con saldo", "Finalizadas" };
        DepartureFilter.SelectedIndex = 0;
        StayDetail.VolverASolicitado += (_, _) => WorkspaceTabs.SelectedIndex = 0;
        StayDetail.SalidaCompletada += (_, _) => { WorkspaceTabs.SelectedIndex = 0; Recargar(); };
        StayDetail.DatosActualizados += (_, _) => Recargar();
        StayGrid.SizeChanged += (_, _) => AplicarColumnasCompactas();
        Recargar();
    }
    private void AplicarColumnasCompactas()
    {
        if (StayGrid.Columns.Count < 10 || StayGrid.ActualWidth <= 0) return;
        var compact = StayGrid.ActualWidth < 1050;
        foreach (var index in new[] { 3, 4, 5 }) StayGrid.Columns[index].Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        StayGrid.Columns[1].Width = new DataGridLength(compact ? 2.2 : 1.6, DataGridLengthUnitType.Star);
        StayGrid.Columns[2].Width = new DataGridLength(compact ? 1.35 : 1.1, DataGridLengthUnitType.Star);
    }
    private void Recargar()
    {
        var floorName = FloorFilter.SelectedItem?.ToString() ?? "Todos los pisos";
        var floor = int.TryParse(floorName.Replace("Piso ", ""), out var parsed) ? parsed : (int?)null;
        var quick = DepartureFilter.SelectedItem?.ToString() ?? "Todas las activas";
        var finalized = StatusFilter.SelectedItem?.ToString() == "Finalizadas" || quick == "Finalizadas";
        var state = finalized ? "Finalizada" : "Activa";
        if (StatusFilter.SelectedItem?.ToString() == "Finalizadas") quick = "Finalizadas";
        else if (quick == "Finalizadas") { StatusFilter.SelectedIndex = 1; state = "Finalizada"; }
        else if (StatusFilter.SelectedIndex == 1) { StatusFilter.SelectedIndex = 0; }
        StayGrid.ItemsSource = _service.Listar(state, SearchBox.Text, floor, quick, TypeFilter.SelectedItem?.ToString());
        var metrics = _service.ObtenerIndicadoresEstancias();
        Metrics.ItemsSource = new[]
        {
            new { Title = "Estancias activas", Value = metrics.Active.ToString() },
            new { Title = "Salidas de hoy", Value = metrics.DeparturesToday.ToString() },
            new { Title = "Requieren atención", Value = metrics.AttentionRequired.ToString() },
            new { Title = "Con saldo pendiente", Value = metrics.WithBalance.ToString() }
        };
    }
    private void Open(StayDirectoryEntry? row)
    {
        if (row is null) return;
        StayDetail.Mostrar(row.Id);
        WorkspaceTabs.SelectedIndex = 1;
    }
    public void AbrirHuesped(int guestId, bool abrirFolio = false)
    {
        Recargar();
        var activeRows = _service.ListarActivas();
        var row = activeRows.FirstOrDefault(item => item.GuestId == guestId || SampleData.ReservaHuespedes.Any(link => link.ReservaId == item.ReservationId && link.HuespedId == guestId));
        if (row is null) return;
        Open(row);
        if (abrirFolio) StayDetail.SeleccionarFolio();
    }
    private void Filter_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Stay_DoubleClick(object sender, MouseButtonEventArgs e) => Open(StayGrid.SelectedItem as StayDirectoryEntry);
    private void OpenStay_Click(object sender, RoutedEventArgs e) => Open((sender as FrameworkElement)?.DataContext as StayDirectoryEntry);
}
