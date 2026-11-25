using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

public partial class SolicitarLimpiezaDialog : Window
{
    private readonly HabitacionOperacionModel _room;
    public SolicitarLimpiezaDialog(HabitacionOperacionModel room)
    {
        InitializeComponent(); _room = room; RoomText.Text = $"Habitación {room.Habitacion.Numero} · {room.Habitacion.Tipo} · Piso {room.Habitacion.Piso}";
        ReasonBox.ItemsSource = new[] { "Preparación después de checkout", "Limpieza solicitada por huésped", "Limpieza adicional", "Preparación urgente para llegada", "Otro motivo" }; ReasonBox.SelectedIndex = room.LlegadaHoy ? 3 : 0;
        PriorityBox.ItemsSource = new[] { "Baja", "Normal", "Alta", "Urgente" }; PriorityBox.SelectedItem = room.LlegadaHoy ? "Alta" : "Normal";
        ArrivalText.Text = room.ProximaReserva is null ? "Sin próxima llegada registrada." : $"Próxima llegada: {room.ProximaReserva.Llegada:dd/MM/yyyy} · {room.ProximaReserva.Huesped}";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { new LimpiezaService().SolicitarDesdeRecepcion(_room.Habitacion.Id, ReasonBox.SelectedItem?.ToString() ?? "Otro motivo", PriorityBox.SelectedItem?.ToString() ?? "Normal", NotesBox.Text); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
