using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class SolicitudServicioDialog : Window
{
    private readonly ActiveGuestChoice _guest;
    private readonly ServicioModel _service;
    public SolicitudServicioDialog(ActiveGuestChoice guest, ServicioModel service)
    {
        InitializeComponent(); _guest = guest; _service = service;
        GuestText.Text = $"{guest.Name} · Hab. {guest.Room} · Piso {guest.Floor}"; ServiceText.Text = service.Nombre;
        var benefit=new ServicioBeneficioService().Evaluar(guest.EstanciaId,service.Id);
        CoverageText.Text = $"{benefit.Label} · {benefit.Detail} · {(benefit.EstimatedCharge>0?$"Estimado Bs {benefit.EstimatedCharge:N2}":"Sin cargo estimado")}";
        AppointmentFields.Visibility = service.RequiereCita||service.RequiereTraslado ? Visibility.Visible : Visibility.Collapsed;
        if(service.RequiereTraslado&&!service.RequiereCita)((TextBlock)((StackPanel)AppointmentFields).Children[0]).Text="Fecha y hora deseadas para el traslado";
        TransferFields.Visibility = service.RequiereTraslado ? Visibility.Visible : Visibility.Collapsed;
        DatePicker.SelectedDate = guest.CheckOut.Date>DateTime.Today.AddDays(1)?DateTime.Today.AddDays(1):DateTime.Today; TimePicker.ItemsSource = Enumerable.Range(8, 12).Select(hour => $"{hour:00}:00").ToList(); TimePicker.SelectedIndex = 2;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DateTime? desired = null;
        if (_service.RequiereCita||_service.RequiereTraslado)
        {
            if (DatePicker.SelectedDate is not DateTime date || TimePicker.SelectedItem is not string time) { ValidationText.Text = "Selecciona la fecha y la hora para la cita."; return; }
            desired = date.Add(TimeSpan.Parse(time));
            if (desired <= DateTime.Now) { ValidationText.Text = "La fecha de la cita debe ser futura."; return; }
        }
        if(!int.TryParse(PassengersBox.Text,out var passengers)||passengers<1){ValidationText.Text="Indica una cantidad válida de pasajeros.";return;}
        try { new SolicitudServicioService().Crear(_guest.Id,_guest.EstanciaId,_service.Id,desired,DescriptionBox.Text.Trim(),OriginBox.Text,DestinationBox.Text,passengers,ContactBox.Text); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
