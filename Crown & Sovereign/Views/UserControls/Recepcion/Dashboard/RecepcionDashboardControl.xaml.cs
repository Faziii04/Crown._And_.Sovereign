using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Dashboard;

public partial class RecepcionDashboardControl : UserControl
{
    public event EventHandler<string>? NavigationRequested;
    public RecepcionDashboardControl()
    {
        InitializeComponent();
        var service = new DashboardService();
        DataContext = new
        {
            Metricas = service.Recepcion(),
            Movimientos = service.LlegadasYSalidas(),
            EstadoHabitaciones = $"{SampleData.Habitaciones.Count(r => r.Estado == "Ocupada")} ocupadas · {SampleData.Habitaciones.Count(r => r.Limpieza == "Limpia")} limpias · {SampleData.Habitaciones.Count(r => r.Bloqueada)} con bloqueo",
            Alertas = $"{SampleData.Tareas.Count(t => t.Estado == "Pendiente")} tareas de limpieza pendientes"
        };
    }
    private void ArrivalAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DashboardArrival item }) MessageBox.Show($"{item.Accion} de {item.Huesped} · Habitación {item.Habitacion}. La operación está disponible desde Estancias.", item.Tipo);
    }
    private void NewReservation_Click(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke(this, "Reservas");
    private void NewGuest_Click(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke(this, "Huéspedes");
    private void Rooms_Click(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke(this, "Habitaciones");
}
