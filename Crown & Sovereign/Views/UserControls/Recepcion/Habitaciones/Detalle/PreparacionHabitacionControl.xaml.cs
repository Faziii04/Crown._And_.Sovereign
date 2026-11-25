using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public sealed record PasoPreparacionView(string Marca, string Titulo, string Nota, Brush Color);

public partial class PreparacionHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    public event Action? DatosActualizados;
    public PreparacionHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; TitleText.Text = $"Preparación de la habitación {room.Habitacion.Numero}"; StateText.Text = $"Estado actual: {room.EstadoLimpieza} · clasificación operativa: {room.Clasificacion}";
        var task = room.TareaActual;
        TaskText.Text = task is null ? "No hay una tarea de limpieza activa." : $"{task.Tipo} · estado {task.Estado} · prioridad {task.Prioridad}\nProgramada: {task.Hora:dd/MM HH:mm}\nResponsable: {(string.IsNullOrWhiteSpace(task.Empleado) ? "Pendiente de asignación por el equipo" : task.Empleado)}";
        TaskNotesText.Text = task?.Observaciones ?? "Sin observaciones registradas.";
        var step = room.EstadoLimpieza;
        var steps = new[] { "Solicitud registrada", "Limpieza asignada", "Limpieza completada", "Inspección pendiente", "Lista para ingreso" };
        var reached = step switch { "Limpia" => 5, "Pendiente de inspección" => 4, "En limpieza" => 2, "Inspección fallida" => 3, "Pendiente de limpieza" => 1, _ => 0 };
        ProgressList.ItemsSource = steps.Select((title, index) => new PasoPreparacionView(index < reached ? "✓" : index == reached ? "●" : "○", title, index == reached ? "· estado actual" : "", index < reached ? Brushes.SeaGreen : index == reached ? Brushes.DarkGoldenrod : Brushes.Gray)).ToList();
        HistoryList.ItemsSource = new LimpiezaService().ListarHabitacion(room.Habitacion.Id).Take(5).Select(item => $"{item.Hora:dd/MM HH:mm} · {item.Tipo} · {item.Estado} · prioridad {item.Prioridad}").ToList();
    }
    private void Request_Click(object sender, RoutedEventArgs e)
    {
        if (_room is null) return;
        var dialog = new SolicitarLimpiezaDialog(_room) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Mostrar(new HabitacionService().ObtenerEstadoOperativo(_room.Habitacion.Id)); DatosActualizados?.Invoke(); }
    }
}
