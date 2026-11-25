using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class GestionarCitaDialog : Window
{
    private readonly CitaServicioModel _appointment;
    private readonly CitaServicioService _service = new();
    public GestionarCitaDialog(CitaServicioModel appointment)
    {
        InitializeComponent(); _appointment = appointment;
        var guest = SampleData.Huespedes.First(item => item.Id == appointment.HuespedId); var service = SampleData.Servicios.First(item => item.Id == appointment.ServicioId);
        GuestText.Text = $"{guest.Nombre} · Hab. {SampleData.Habitaciones.First(room => room.Id == guest.Habitacion).Numero}"; ServiceText.Text = service.Nombre; DateText.Text = $"{appointment.Inicio:dddd, dd MMMM yyyy} · {appointment.Inicio:HH:mm}–{appointment.Fin:HH:mm}"; EmployeeText.Text = $"Responsable: {appointment.AsignadoA}"; StatusText.Text = appointment.Estado;
        PrimaryAction.Content = appointment.Estado == "En curso" ? "Completar" : "Iniciar";
        PrimaryAction.IsEnabled = appointment.Estado is "Solicitada" or "Confirmada" or "En curso";
    }
    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        try { if (_appointment.Estado == "En curso") _service.Completar(_appointment.Id); else _service.Iniciar(_appointment.Id); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Cancelar esta cita?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _service.Cancelar(_appointment.Id); DialogResult = true; } catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
    private void NoShow_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Marcar al huésped como no presentado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _service.MarcarNoPresentado(_appointment.Id); DialogResult = true;
    }
    private void Reschedule_Click(object sender, RoutedEventArgs e)
    {
        var guest = new HuespedService().ListarActivos().FirstOrDefault(item => item.Id == _appointment.HuespedId);
        if (guest is null) { ValidationText.Text = "El huésped ya no tiene estancia activa."; return; }
        var service = SampleData.Servicios.First(item => item.Id == _appointment.ServicioId);
        if (new CitaServicioDialog(guest, service, _appointment) { Owner = this }.ShowDialog() == true) DialogResult = true;
    }
}
