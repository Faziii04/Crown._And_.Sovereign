using System.Globalization;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

public partial class SimularPoliticaServicioDialog : Window
{
    private readonly PoliticaHotelService _service;
    private readonly PoliticaServicioModel _policy;
    public SimularPoliticaServicioDialog(PoliticaHotelService service, PoliticaServicioModel policy)
    {
        InitializeComponent(); _service = service; _policy = policy;
        ScenarioBox.ItemsSource = new[] { "Cancelación", "No-show" }; ScenarioBox.SelectedIndex = 0;
        AppointmentDate.SelectedDate = DateTime.Today.AddDays(1); ActionDate.SelectedDate = DateTime.Today;
    }
    private void Simulate_Click(object sender, RoutedEventArgs e)
    {
        if (AppointmentDate.SelectedDate is not DateTime date || ActionDate.SelectedDate is not DateTime actionDate ||
            !TimeOnly.TryParse(AppointmentTime.Text, CultureInfo.CurrentCulture, out var time) || !TimeOnly.TryParse(ActionTime.Text, CultureInfo.CurrentCulture, out var actionTime))
        { ResultTitle.Text = "Revisa fechas y horarios"; ResultMessage.Text = "Usa una hora válida en formato 24 h."; return; }
        decimal? price = null;
        if (!string.IsNullOrWhiteSpace(BaseAmount.Text))
        {
            if (!decimal.TryParse(BaseAmount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) || parsed < 0)
            { ResultTitle.Text = "Importe inválido"; ResultMessage.Text = "El importe de referencia debe ser un número no negativo."; return; }
            price = parsed;
        }
        var result = _service.SimularServicio(date.Date.Add(time.ToTimeSpan()), actionDate.Date.Add(actionTime.ToTimeSpan()), ScenarioBox.SelectedIndex == 1,
            price, CoveredBox.IsChecked == true, UnlimitedBox.IsChecked == true, _policy);
        ResultTitle.Text = result.Estado; ResultMessage.Text = result.Mensaje; ResultBasis.Text = "Base: " + result.BaseCalculo;
        ResultImpact.Text = result.AlcanceHistorico + (result.ConsumeBeneficio ? " · Efecto de consumo propuesto." : "");
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
