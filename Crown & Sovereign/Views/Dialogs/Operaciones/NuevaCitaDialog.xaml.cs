using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class NuevaCitaDialog : Window
{
    private readonly HuespedService _guests=new();
    public NuevaCitaDialog()
    {
        InitializeComponent();GuestBox.ItemsSource=_guests.ListarActivos();ServiceBox.ItemsSource=SampleData.Servicios.Where(item=>item.Activo&&item.RequiereCita&&item.Reservable).ToList();EmployeePicker.ItemsSource=SampleData.Empleados.Where(item=>item.Cargo=="Servicios").ToList();
        GuestBox.SelectedIndex=-1;ServiceBox.SelectedIndex=-1;EmployeePicker.SelectedIndex=-1;DatePicker.SelectedDate=DateTime.Today.AddDays(1);TimePicker.ItemsSource=Enumerable.Range(8,12).Select(hour=>$"{hour:00}:00").ToList();TimePicker.SelectedIndex=-1;
    }
    private void Guest_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(GuestBox.SelectedItem is not ActiveGuestChoice guest){StayText.Text="La estancia se seleccionará con el huésped.";return;}
        StayText.Text=$"Estancia #{guest.EstanciaId} · Reserva {guest.Reservation} · Hab. {guest.Room} · Checkout {guest.CheckOut:dd/MM/yyyy}";
    }
    private void Save_Click(object sender,RoutedEventArgs e)
    {
        if(GuestBox.SelectedItem is not ActiveGuestChoice guest||ServiceBox.SelectedItem is not ServicioModel service||EmployeePicker.SelectedItem is not EmpleadoModel employee||DatePicker.SelectedDate is not DateTime date||TimePicker.SelectedItem is not string time){ValidationText.Text="Selecciona huésped, servicio, fecha, hora y responsable.";return;}
        try{new CitaServicioService().Programar(guest.Id,service.Id,date.Add(TimeSpan.Parse(time)),employee.Nombre);DialogResult=true;}
        catch(Exception exception){ValidationText.Text=exception.Message;}
    }
}
