using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

public partial class ReportarIncidenciaDialog : Window
{
    private readonly HabitacionOperacionModel _room;
    public ReportarIncidenciaDialog(HabitacionOperacionModel room)
    {
        InitializeComponent(); _room = room; RoomText.Text = $"Habitación {room.Habitacion.Numero} · {room.Habitacion.Tipo} · Piso {room.Habitacion.Piso}";
        CategoryBox.ItemsSource = new[] { "Equipamiento", "Electricidad", "Plomería", "Mobiliario", "Climatización", "Limpieza", "Seguridad", "Otros" }; CategoryBox.SelectedIndex = 0;
        PriorityBox.ItemsSource = new[] { "Baja", "Media", "Alta", "Urgente" }; PriorityBox.SelectedItem = "Media"; SafeRadio.IsChecked = true;
        PreventUseRadio.Checked += (_, _) => WarningText.Text = "Atención: se marcará como revisión operativa pendiente, pero no se creará un bloqueo formal ni se permitirá tratarla como lista hasta que se evalúe.";
        UrgentRadio.Checked += (_, _) => WarningText.Text = "Se notificará al equipo de Mantenimiento para evaluación prioritaria. No se aplicará un bloqueo formal automáticamente.";
        SafeRadio.Checked += (_, _) => WarningText.Text = "El reporte no bloquea la habitación. El equipo de Mantenimiento evalúa la medida correspondiente.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var impact = PreventUseRadio.IsChecked == true ? "Posible impedimento para alojar huéspedes" : UrgentRadio.IsChecked == true ? "Requiere revisión urgente" : "Puede seguir utilizándose";
        try { new MantenimientoService().ReportarDesdeRecepcion(_room.Habitacion.Id, CategoryBox.SelectedItem?.ToString() ?? "Otros", DescriptionBox.Text, PriorityBox.SelectedItem?.ToString() ?? "Media", impact, NotesBox.Text); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
