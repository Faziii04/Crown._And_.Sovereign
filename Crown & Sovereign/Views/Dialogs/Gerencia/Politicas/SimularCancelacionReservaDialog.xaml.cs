using System.Globalization;
using System.Windows;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

public partial class SimularCancelacionReservaDialog : Window
{
    private readonly PoliticaHotelService _service;
    public SimularCancelacionReservaDialog(PoliticaHotelService service)
    {
        InitializeComponent(); _service = service; ArrivalDate.SelectedDate = DateTime.Today.AddDays(3); CancelDate.SelectedDate = DateTime.Today;
        PolicyText.Text = $"Plazo: {_service.Valor("reservation_cancel_hours")} · Check-in estándar {_service.Valor("checkin")}.";
    }
    private void Simulate_Click(object sender, RoutedEventArgs e)
    {
        if (ArrivalDate.SelectedDate is not DateTime arrival || CancelDate.SelectedDate is not DateTime cancel || !TimeOnly.TryParse(CancelTime.Text, CultureInfo.CurrentCulture, out var time))
        { ResultTitle.Text = "Revisa los datos"; ResultText.Text = "Selecciona fechas válidas y una hora en formato 24 h."; return; }
        var result = _service.SimularCancelacionReserva(arrival, cancel.Date.Add(time.ToTimeSpan()));
        ResultTitle.Text = result.Estado; ResultText.Text = result.Mensaje + " " + result.BaseCalculo; LimitationText.Text = result.AlcanceHistorico;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
