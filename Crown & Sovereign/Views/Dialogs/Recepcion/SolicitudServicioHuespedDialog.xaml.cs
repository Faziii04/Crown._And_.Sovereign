using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class SolicitudServicioHuespedDialog : Window
{
    private readonly int _initialGuestId;
    private readonly int? _stayId;
    public SolicitudServicioHuespedDialog(int guestId) : this(guestId, null, null) { }
    public SolicitudServicioHuespedDialog(int guestId, int? stayId, int? serviceId = null)
    {
        InitializeComponent(); _initialGuestId = guestId; _stayId = stayId;
        var guestIds = stayId is int id ? SampleData.ReservaHuespedes.Where(item => item.ReservaId == SampleData.Estancias.First(stay => stay.Id == id).ReservaId).Select(item => item.HuespedId).Distinct().ToList() : [guestId];
        if (guestIds.Count == 0) guestIds.Add(guestId);
        GuestBox.ItemsSource = SampleData.Huespedes.Where(item => guestIds.Contains(item.Id)).ToList();
        GuestBox.SelectedItem = SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId);
        GuestBox.IsEnabled = stayId is not null && GuestBox.Items.Count > 1;
        ServiceBox.ItemsSource = new ServicioService().ListarReservables();
        ServiceBox.SelectedItem = SampleData.Servicios.FirstOrDefault(item => item.Id == serviceId) ?? ServiceBox.Items.Cast<ServicioModel>().FirstOrDefault();
        DateBox.SelectedDate = DateTime.Today.AddDays(1);
        if (stayId is int stayKey)
        {
            var stay = SampleData.Estancias.First(item => item.Id == stayKey);
            StayText.Text = $"Estancia #{stay.Id} · Reserva #{stay.ReservaId} · salida prevista {stay.CheckOut:dd/MM/yyyy}";
        }
        else StayText.Text = "Se utilizará la estancia activa del huésped seleccionado.";
        Service_Changed(this, null!);
    }
    private void Service_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ServiceBox.SelectedItem is not ServicioModel service) return;
        AppointmentHint.Text = service.RequiereCita ? "Este servicio requiere una fecha y hora solicitadas. El horario quedará pendiente de confirmación." : "La fecha es opcional para este servicio.";
        if (_stayId is int stayId)
        {
            var coverage = new ServicioBeneficioService().Evaluar(stayId, service.Id);
            CoverageText.Text = $"Cobertura disponible: {coverage.Label}. {coverage.Detail} Cargo adicional estimado: Bs {coverage.EstimatedCharge:N2} por uso.";
        }
        else CoverageText.Text = "La cobertura se volverá a validar cuando Operaciones procese la solicitud.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (GuestBox.SelectedItem is not HuespedModel guest) throw new InvalidOperationException("Selecciona el huésped destinatario.");
            if (ServiceBox.SelectedItem is not ServicioModel service) throw new InvalidOperationException("Selecciona un servicio.");
            DateTime? desired = null;
            if (service.RequiereCita || service.RequiereTraslado)
            {
                if (DateBox.SelectedDate is null || !TimeSpan.TryParseExact(TimeBox.Text, "hh\\:mm", CultureInfo.InvariantCulture, out var time)) throw new InvalidOperationException("Indica una fecha y hora válida para solicitar el servicio.");
                desired = DateBox.SelectedDate.Value.Date.Add(time);
            }
            var requests = new SolicitudServicioService();
            if (_stayId is int stayId) requests.Crear(guest.Id, stayId, service.Id, desired, DescriptionBox.Text);
            else requests.Crear(_initialGuestId, service.Id, desired, DescriptionBox.Text);
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
