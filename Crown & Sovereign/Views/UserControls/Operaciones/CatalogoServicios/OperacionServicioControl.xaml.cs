using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class OperacionServicioControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    private ServicioModel? _current;
    public event EventHandler? ConfigurationChanged;
    public OperacionServicioControl() => InitializeComponent();
    public void Mostrar(ServicioModel service)
    {
        _current = service; OperationalText.Text = $"{(service.Activo ? "Activo" : "Inactivo")} · {(service.Reservable ? "Se puede solicitar individualmente" : "Agrupación organizativa")} · {(service.RequiereCita ? "Requiere cita" : "Sin cita")}";
        DurationText.Text = service.RequiereCita ? $"Duración estimada: {service.DuracionMinutos} min" : "Duración no aplica";
        EmployeeGrid.ItemsSource = _service.EmpleadosDe(service.Id);
        var assigned = EmployeeGrid.ItemsSource.Cast<EmpleadoModel>().Select(item => item.Id).ToHashSet();
        EmployeeBox.ItemsSource = SampleData.Empleados.Where(item => item.Cargo == "Servicios" && !assigned.Contains(item.Id)).OrderBy(item => item.Nombre).ToList();
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || EmployeeBox.SelectedItem is not EmpleadoModel employee) return;
        try { _service.AsociarEmpleado(_current.Id, employee.Id); Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo asociar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || EmployeeGrid.SelectedItem is not EmpleadoModel employee) return;
        try { _service.AsociarEmpleado(_current.Id, employee.Id, false); Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo actualizar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
