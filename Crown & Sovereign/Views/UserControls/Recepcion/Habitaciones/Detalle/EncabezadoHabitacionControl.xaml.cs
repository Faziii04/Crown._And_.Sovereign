using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class EncabezadoHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    private string _action = "Detalles";
    public event Action? VolverSolicitado;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public EncabezadoHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; BreadcrumbText.Text = $"Recepción / Habitaciones / Piso {room.Habitacion.Piso} / Habitación {room.Habitacion.Numero}";
        TitleText.Text = $"Habitación {room.Habitacion.Numero}"; SubtitleText.Text = $"{room.Habitacion.Tipo} · Piso {room.Habitacion.Piso} · Capacidad: {room.Habitacion.Capacidad} personas";
        StatusText.Text = $"{room.Clasificacion} · {room.EstadoMantenimiento}";
        (_action, PrimaryButton.Content) = HabitacionCardAcciones.Principal(room);
        PrimaryButton.Visibility = _action == "Detalles" ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Back_Click(object sender, RoutedEventArgs e) => VolverSolicitado?.Invoke();
    private void Primary_Click(object sender, RoutedEventArgs e) { if (_room is not null) AccionSolicitada?.Invoke(_room, _action); }
    private void Actions_Click(object sender, RoutedEventArgs e)
    {
        if (_room is null) return;
        var menu = new ContextMenu();
        foreach (var (label, action) in new[] { ("Crear reserva", "Crear reserva"), ("Consultar disponibilidad", "Disponibilidad"), ("Solicitar limpieza", "Solicitar limpieza"), ("Reportar incidencia", "Reportar incidencia"), ("Cambiar habitación", "Cambiar habitación") })
        {
            var item = new MenuItem { Header = label, Tag = action }; item.Click += (_, _) => AccionSolicitada?.Invoke(_room, action); menu.Items.Add(item);
        }
        menu.PlacementTarget = ActionsButton; menu.IsOpen = true;
    }
}
