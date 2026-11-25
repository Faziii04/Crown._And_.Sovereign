using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

namespace Crown___Sovereign.Views.UserControls.Shared.Cards;

public partial class HabitacionDetalladaCardControl : UserControl
{
    public event Action<HabitacionOperacionModel>? DetalleSolicitado;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public HabitacionDetalladaCardControl()
    {
        InitializeComponent(); Loaded += (_, _) => Configurar();
        QuickPreview.DetallesSolicitados += room => { PreviewPopup.IsOpen = false; DetalleSolicitado?.Invoke(room); };
        QuickPreview.AccionSolicitada += (room, action) => { PreviewPopup.IsOpen = false; AccionSolicitada?.Invoke(room, action); };
    }
    private HabitacionOperacionModel? Room => DataContext as HabitacionOperacionModel;
    private void Configurar()
    {
        if (Room is not { } room) return;
        var menu = new ContextMenu();
        foreach (var (label, action) in AccionesContextuales(room))
        {
            var item = new MenuItem { Header = label, Tag = action };
            item.Click += (_, _) => AccionSolicitada?.Invoke(room, action);
            menu.Items.Add(item);
        }
        CardBorder.ContextMenu = menu;
        QuickPreview.Mostrar(room); var primaryAction = HabitacionCardAcciones.Principal(room); PrimaryAction.Content = primaryAction.Label; PrimaryAction.Tag = primaryAction.Action;
        StatusText.Text = room.Bloqueada && room.Ocupada ? "! Bloqueada y ocupada" : room.Clasificacion;
        GuestText.Text = room.HuespedPrincipal; PeopleText.Text = room.Ocupada ? $"{room.Huespedes.Count} huésped(es) · capacidad {room.Habitacion.Capacidad}" : $"Capacidad: {room.Habitacion.Capacidad} personas";
        DepartureText.Text = room.ReservaActual is not null ? $"Salida: {room.ReservaActual.Salida:dd MMM}" : room.ProximaReserva is not null ? $"Llegada: {room.ProximaReserva.Llegada:dd MMM} · salida {room.ProximaReserva.Salida:dd MMM}" : "Sin movimiento próximo";
        OperationalText.Text = $"Limpieza: {room.EstadoLimpieza}\nMantenimiento: {room.EstadoMantenimiento}{(room.Incidencias.FirstOrDefault() is { } incident ? $"\n{incident.Descripcion}" : "")}";
        var color = room.Clasificacion switch { "Lista para ingreso" => "#39816A", "Ocupada" => "#507BA5", "Por preparar" => "#AD7B34", _ => "#B65757" };
        CardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
    private static IEnumerable<(string Label, string Action)> AccionesContextuales(HabitacionOperacionModel room)
    {
        yield return room.Ocupada ? ("Ver estancia", "Estancia") : ("Crear reserva", "Crear reserva");
        if (room.ProximaReserva is not null) yield return ("Ver reserva", "Reserva");
        yield return ("Consultar disponibilidad", "Disponibilidad");
        yield return ("Ver preparación", "Preparación");
        yield return ("Solicitar limpieza", "Solicitar limpieza");
        yield return ("Ver incidencias", "Incidencias");
        yield return ("Reportar incidencia", "Reportar incidencia");
        if (room.Bloqueada && room.Ocupada) yield return ("Cambiar habitación", "Cambiar habitación");
        if (room.SalidaHoy) yield return ("Ver salida", "Checkout");
    }
    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null) return; if (Room is not { } room) return; if (e.ClickCount >= 2) { PreviewPopup.IsOpen = false; DetalleSolicitado?.Invoke(room); } else { QuickPreview.Mostrar(room); PreviewPopup.IsOpen = true; } }
    private void Details_Click(object sender, RoutedEventArgs e) { if (Room is { } room) DetalleSolicitado?.Invoke(room); }
    private void Action_Click(object sender, RoutedEventArgs e) { if (Room is { } room && PrimaryAction.Tag is string action) AccionSolicitada?.Invoke(room, action); }
    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match) return match;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
