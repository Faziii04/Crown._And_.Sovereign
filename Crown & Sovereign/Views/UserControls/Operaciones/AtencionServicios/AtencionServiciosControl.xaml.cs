using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class AtencionServiciosControl : UserControl
{
    private ActiveGuestChoice? _guest;
    private readonly ServicioService _services = new();
    public AtencionServiciosControl()
    {
        InitializeComponent();
        GuestSelector.HuespedSeleccionado += MostrarHuesped;
        AvailableServicesView.UtilizacionSolicitada += RegistrarUso;
        if (GuestSelector.HuespedActual is { } guest) MostrarHuesped(guest);
    }
    private void MostrarHuesped(ActiveGuestChoice? guest)
    {
        _guest = guest;
        if(guest is null){GuestTitle.Text="Selecciona un huésped alojado";GuestSubtitle.Text="Busca por nombre o habitación.";return;}
        GuestTitle.Text = guest.Name;
        GuestSubtitle.Text = $"Hab. {guest.Room} · Piso {guest.Floor} · {guest.Package} · {guest.Role} · Reserva {guest.Reservation}";
        AvailableServicesView.Mostrar(guest.Id); BenefitsView.Mostrar(guest.Id); UsageSummaryView.Mostrar(guest.Id); UsageHistoryView.Mostrar(guest.Id);
    }
    private void RegistrarUso(ServicioModel service)
    {
        if (_guest is null) return;
        if (service.RequiereCita)
        {
            var appointment = new CitaServicioDialog(_guest, service) { Owner = Window.GetWindow(this) };
            if (appointment.ShowDialog() == true) MessageBox.Show("La cita quedó programada. Confirma la utilización una vez prestado el servicio.", "Agenda de servicios");
            return;
        }
        var guest = SampleData.Huespedes.First(item => item.Id == _guest.Id);
        var dialog = new ConfirmarUtilizacionDialog(_guest, service) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        try { _services.RegistrarUtilizacion(service, guest); MostrarHuesped(_guest); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se registró el servicio", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
