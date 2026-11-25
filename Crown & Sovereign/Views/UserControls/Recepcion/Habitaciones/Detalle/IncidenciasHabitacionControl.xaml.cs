using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public sealed record IncidenciaHabitacionFila(IncidenciaModel Incidencia, string Detalle, string Impacto);

public partial class IncidenciasHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    public event Action? DatosActualizados;
    public IncidenciasHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; TitleText.Text = $"Incidencias de la habitación {room.Habitacion.Numero}";
        var incidents = new MantenimientoService().ListarHabitacion(room.Habitacion.Id);
        OperationalText.Text = $"Estado operativo: {(room.Bloqueada ? "Bloqueada" : room.RequiereRevisionOperativa ? "Revisión pendiente" : "Operable")}";
        CountText.Text = $"{incidents.Count(item => item.Estado != "Resuelta")} incidencia(s) abierta(s)";
        IncidentList.ItemsSource = incidents.Select(item => new IncidenciaHabitacionFila(item, $"{item.Categoria} · {item.FechaReporte:dd/MM/yyyy HH:mm} · {item.Estado}{(string.IsNullOrWhiteSpace(item.ReportadaPor) ? "" : $" · {item.ReportadaPor}")}", item.BloqueaHabitacion ? "Habitación bloqueada" : item.RequiereRevisionOperativa ? "Revisión urgente · no es un bloqueo formal" : "No bloquea la habitación")).ToList();
        var empty = incidents.Count == 0; IncidentList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible; EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Report_Click(object sender, RoutedEventArgs e)
    {
        if (_room is null) return;
        var dialog = new ReportarIncidenciaDialog(_room) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Mostrar(new HabitacionService().ObtenerEstadoOperativo(_room.Habitacion.Id)); DatosActualizados?.Invoke(); }
    }
    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is IncidenciaModel incident) new DetalleIncidenciaDialog(incident) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
