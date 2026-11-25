using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AgendaServicios;

public partial class AgendaHoyControl : UserControl
{
    private readonly CitaServicioService _service = new();
    public event Action? AgendaActualizada;
    public AgendaHoyControl() { InitializeComponent(); Recargar(); }
    public void Recargar()
    {
        var rows = _service.ConsultarAgenda(DateTime.Today);
        MetricsView.ItemsSource = new[] { new { Titulo = "Próximas", Valor = rows.Count(item => item.Start >= DateTime.Now).ToString() }, new { Titulo = "En curso", Valor = rows.Count(item => item.Status == "En curso").ToString() }, new { Titulo = "Solicitudes", Valor = rows.Count(item => item.Status == "Solicitada").ToString() } };
        AppointmentsList.ItemsSource = rows;
    }
    private void Manage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        var appointment = _service.Listar().FirstOrDefault(item => item.Id == id);
        if (appointment is null) return;
        if (new GestionarCitaDialog(appointment) { Owner = Window.GetWindow(this) }.ShowDialog() == true) { Recargar(); AgendaActualizada?.Invoke(); }
    }
}
