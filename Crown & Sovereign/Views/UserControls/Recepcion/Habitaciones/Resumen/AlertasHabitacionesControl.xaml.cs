using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Resumen;

public partial class AlertasHabitacionesControl : UserControl
{
    public event Action<AlertaHabitacionItem>? AlertaSeleccionada;
    public int CantidadAlertas { get; private set; }
    public AlertasHabitacionesControl() => InitializeComponent();
    public void Mostrar(IReadOnlyList<HabitacionOperacionModel> rooms)
    {
        var alerts = new List<AlertaHabitacionItem>();
        foreach (var room in rooms)
        {
            if (room.Bloqueada && room.Ocupada) alerts.Add(new("Reubicación inmediata", $"Habitación {room.Habitacion.Numero} ocupada y bloqueada · {room.HuespedPrincipal}", "Bloqueadas", room.Habitacion.Id));
            if (room.LlegadaHoy && room.Clasificacion != "Lista para ingreso") alerts.Add(new("Llegada en riesgo", $"Llegada hoy · habitación {room.Habitacion.Numero} · {room.EstadoLimpieza}", "Llegadas en riesgo", room.Habitacion.Id));
            if (room.EstadoLimpieza is "Pendiente de inspección" or "Inspección fallida") alerts.Add(new("Inspección pendiente", $"Habitación {room.Habitacion.Numero} · {room.EstadoLimpieza}", "Pendientes de inspección", room.Habitacion.Id));
            if (room.Incidencias.Any(item => item.Prioridad == "Urgente" && item.Estado != "Resuelta")) alerts.Add(new("Incidencia urgente", $"Habitación {room.Habitacion.Numero} · {room.Incidencias.First(item => item.Prioridad == "Urgente").Descripcion}", "Con incidencias", room.Habitacion.Id));
            if (room.CambioMismoDia) alerts.Add(new("Cambio de huésped hoy", $"Habitación {room.Habitacion.Numero} · salida y llegada previstas hoy", "Salidas hoy", room.Habitacion.Id));
        }
        CantidadAlertas = alerts.Count;
        var top = alerts.Take(5).ToList();
        AlertList.ItemsSource = top;
        AlertPanel.Visibility = top.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Alert_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.Tag is AlertaHabitacionItem alert) AlertaSeleccionada?.Invoke(alert); }
}
