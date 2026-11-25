using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class DetalleHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    public event Action? VolverSolicitado;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public event Action<int>? EstanciaSolicitada;
    public event Action<int>? ReservaSolicitada;
    public event Action? DatosActualizados;
    public DetalleHabitacionControl()
    {
        InitializeComponent();
        Header.VolverSolicitado += () => VolverSolicitado?.Invoke(); Header.AccionSolicitada += OnAction;
        SummaryView.AccionSolicitada += OnAction; SummaryView.EstanciaSolicitada += id => EstanciaSolicitada?.Invoke(id); SummaryView.ReservaSolicitada += id => ReservaSolicitada?.Invoke(id);
        AvailabilityView.DatosActualizados += () => DatosActualizados?.Invoke(); AvailabilityView.ReservaSolicitada += id => ReservaSolicitada?.Invoke(id);
        AvailabilityView.CalendarioGeneralSolicitado += (room, start, days) => AccionSolicitada?.Invoke(room, "Calendario general");
        PreparationView.DatosActualizados += () => DatosActualizados?.Invoke(); IncidentView.DatosActualizados += () => DatosActualizados?.Invoke();
    }
    public int SelectedTab { get => DetailTabs.SelectedIndex; set => DetailTabs.SelectedIndex = Math.Clamp(value, 0, DetailTabs.Items.Count - 1); }
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; Header.Mostrar(room); RoomStates.Mostrar(room); SummaryView.Mostrar(room); AvailabilityView.Mostrar(room); PreparationView.Mostrar(room); IncidentView.Mostrar(room); CharacteristicsView.Mostrar(room);
        RelocationAlert.Visibility = room.Bloqueada && room.Ocupada ? Visibility.Visible : Visibility.Collapsed;
        RelocationText.Text = $"Se requiere atención inmediata · Habitación {room.Habitacion.Numero} ocupada y bloqueada · {room.HuespedPrincipal} · {room.Incidencias.FirstOrDefault(item => item.BloqueaHabitacion)?.Descripcion}";
    }
    private void OnAction(HabitacionOperacionModel room, string action) => AccionSolicitada?.Invoke(room, action);
    private void Relocate_Click(object sender, RoutedEventArgs e) { if (_room is not null) AccionSolicitada?.Invoke(_room, "Cambiar habitación"); }
}
