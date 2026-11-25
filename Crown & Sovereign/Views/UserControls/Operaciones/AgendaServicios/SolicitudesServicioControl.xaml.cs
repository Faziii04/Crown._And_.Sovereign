using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AgendaServicios;

public partial class SolicitudesServicioControl : UserControl
{
    private readonly SolicitudServicioService _requests = new();
    private readonly HuespedService _guests = new();
    public event Action? SolicitudActualizada;
    public SolicitudesServicioControl()
    {
        InitializeComponent(); StatusFilter.ItemsSource = new[] { "Todos los estados", "Solicitada", "Pendiente", "Confirmada" }; StatusFilter.SelectedIndex = 0; Recargar();
    }
    public void Recargar()
    {
        var status = StatusFilter.SelectedItem?.ToString(); var query = SearchBox.Text;
        RequestsGrid.ItemsSource = _requests.Listar().Where(item => item.RequiresAppointment && item.Status is not ("Cancelada" or "Completada" or "No presentado"))
            .Where(item => status is null or "Todos los estados" || item.Status == status)
            .Where(item => string.IsNullOrWhiteSpace(query) || $"{item.Guest} {item.Room} {item.Service}".Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Schedule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ServiceRequestView request) return;
        var source=SampleData.SolicitudesServicio.FirstOrDefault(item=>item.Id==request.Id);
        if(source is null)return;
        var guest = _guests.ListarActivos().FirstOrDefault(item => item.Id == source.HuespedId&&item.EstanciaId==source.EstanciaId);
        var service = SampleData.Servicios.FirstOrDefault(item => item.Id == source.ServicioId);
        if (guest is null || service is null) return;
        var appointment = source.CitaServicioId is int appointmentId?SampleData.Citas.FirstOrDefault(item=>item.Id==appointmentId):null;
        var dialog = new CitaServicioDialog(guest, service, appointment) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Recargar(); SolicitudActualizada?.Invoke(); }
    }
}
