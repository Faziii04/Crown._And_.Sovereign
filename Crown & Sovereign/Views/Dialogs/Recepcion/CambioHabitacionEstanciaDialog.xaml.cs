using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CambioHabitacionEstanciaDialog : Window
{
    private readonly int _stayId;
    private readonly EstanciaService _stays = new();
    public CambioHabitacionEstanciaDialog(int stayId)
    {
        InitializeComponent(); _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var rooms = _stays.ObtenerHabitacionesEstancia(stayId);
        GuestText.Text = $"{SampleData.Huespedes.First(item => item.Id == stay.HuespedId).Nombre} · Estancia #{stay.Id} · salida {stay.CheckOut:dd/MM/yyyy}";
        SourceBox.ItemsSource = rooms; SourceBox.SelectedIndex = 0;
        ReasonBox.ItemsSource = new[] { "Problema de mantenimiento", "Preferencia del huésped", "Ruido", "Reasignación operativa", "Mejora autorizada", "Otro" };
        ReasonBox.SelectedIndex = 0;
    }
    private void Source_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || SourceBox.SelectedItem is not HabitacionModel room) return;
        var stay = SampleData.Estancias.First(item => item.Id == _stayId);
        DestinationGrid.ItemsSource = _stays.HabitacionesCompatiblesParaCambio(stay.ReservaId, room.Id);
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SourceBox.SelectedItem is not HabitacionModel source || DestinationGrid.SelectedItem is not HabitacionOperacionModel target) { ValidationText.Text = "Selecciona la habitación actual y una habitación destino disponible."; return; }
        try
        {
            var stay = SampleData.Estancias.First(item => item.Id == _stayId && item.Estado == "Activa");
            new EstanciaService().CambiarHabitacion(stay.ReservaId, source.Id, target.Habitacion.Id, ReasonBox.SelectedItem?.ToString() ?? "Otro", NotesBox.Text);
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
