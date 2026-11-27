using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class CitaServicioDialog : Window
{
    private readonly ActiveGuestChoice _guest;
    private readonly ServicioModel _service;
    private readonly CitaServicioModel? _existing;
    public CitaServicioDialog(ActiveGuestChoice guest, ServicioModel service, CitaServicioModel? existing = null)
    {
        InitializeComponent(); _guest = guest; _service = service; _existing = existing;
        GuestText.Text = $"{guest.Name} · Hab. {guest.Room} · {guest.Package}"; ServiceText.Text = service.Nombre;
        DatePicker.SelectedDate = existing?.Inicio.Date ?? DateTime.Today.AddDays(1); TimePicker.ItemsSource = Enumerable.Range(8, 12).Select(hour => $"{hour:00}:00").ToList(); TimePicker.SelectedItem = existing?.Inicio.ToString("HH:mm") ?? "10:00";
        var linked = SampleData.EmpleadosServicios.Where(item => item.ServicioId == service.Id && item.Activo).Select(item => item.EmpleadoId).ToHashSet();
        EmployeePicker.ItemsSource = SampleData.Empleados.Where(item => linked.Count == 0 ? item.Cargo == "Servicios" : linked.Contains(item.Id)).OrderBy(item => item.Nombre).ToList();
        EmployeeHint.Text = linked.Count == 0 ? "Sin empleados asociados al servicio; se muestran colaboradores de servicios. Verifica la configuración." : "Solo se muestran empleados asociados a este servicio.";
        EmployeePicker.SelectedItem = existing is null||existing.AsignadoA=="Por asignar"?null:SampleData.Empleados.FirstOrDefault(item => item.Nombre == existing.AsignadoA);
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DatePicker.SelectedDate is not DateTime date || TimePicker.SelectedItem is not string time || EmployeePicker.SelectedItem is not EmpleadoModel employee) { ValidationText.Text = "Completa fecha, hora y responsable."; return; }
        try { if (_existing is null) new CitaServicioService().Programar(_guest.Id, _service.Id, date.Add(TimeSpan.Parse(time)), employee.Nombre); else new CitaServicioService().Reprogramar(_existing.Id, date.Add(TimeSpan.Parse(time)), employee.Nombre); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
