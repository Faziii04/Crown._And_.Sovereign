using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class ExtenderEstanciaDialog : Window
{
    private readonly int _stayId;
    private readonly EstanciaService _stays = new();
    public ExtenderEstanciaDialog(int stayId)
    {
        InitializeComponent(); _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        GuestText.Text = $"{SampleData.Huespedes.First(item => item.Id == stay.HuespedId).Nombre} · Estancia #{stay.Id} · {string.Join(", ", _stays.ObtenerHabitacionesEstancia(stayId).Select(item => item.Numero))}";
        CurrentDepartureText.Text = stay.CheckOut.ToString("dddd, d 'de' MMMM 'de' yyyy");
        DeparturePicker.SelectedDate = stay.CheckOut.Date.AddDays(1);
        RefreshQuote();
    }
    private void Date_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) RefreshQuote(); }
    private void RefreshQuote()
    {
        if (DeparturePicker.SelectedDate is not DateTime departure) return;
        try
        {
            var quote = _stays.CotizarExtension(_stayId, departure);
            RoomAvailabilityText.Text = $"✓ {string.Join(", ", quote.Rooms)} disponible durante todo el periodo · {quote.AdditionalNights} noche(s)";
            RoomAvailabilityText.Foreground = System.Windows.Media.Brushes.SeaGreen;
            LodgingText.Text = $"Alojamiento adicional · Bs {quote.Lodging:N2}";
            ResourcesText.Text = $"Recursos por noche · Bs {quote.Resources:N2} (los recursos por estancia no cambian)";
            TotalText.Text = $"Total adicional · Bs {quote.Total:N2}";
            ValidationText.Text = ""; ConfirmButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            RoomAvailabilityText.Text = ex.Message; RoomAvailabilityText.Foreground = System.Windows.Media.Brushes.Firebrick;
            LodgingText.Text = ""; ResourcesText.Text = ""; TotalText.Text = ""; ValidationText.Text = ""; ConfirmButton.IsEnabled = false;
        }
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (DeparturePicker.SelectedDate is not DateTime departure) { ValidationText.Text = "Selecciona una fecha de salida."; return; }
        try { _stays.ExtenderEstancia(_stayId, departure); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; RefreshQuote(); }
    }
}
