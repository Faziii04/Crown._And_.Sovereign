using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Recepcion;
public partial class SolicitudServicioHuespedDialog:Window
{
 private readonly int _guestId; public SolicitudServicioHuespedDialog(int guestId){InitializeComponent();_guestId=guestId;ServiceBox.ItemsSource=SampleData.Servicios.Where(item=>item.Activo&&item.PadreId is not null).ToList();ServiceBox.SelectedIndex=0;DateBox.SelectedDate=DateTime.Today.AddDays(1);}
 private void Service_Changed(object sender,SelectionChangedEventArgs e){AppointmentHint.Text=ServiceBox.SelectedItem is ServicioModel service&&service.RequiereCita?"Este servicio requiere una fecha y hora para solicitar una cita.":"La fecha es opcional para este servicio.";}
 private void Save_Click(object sender,RoutedEventArgs e){try{if(ServiceBox.SelectedItem is not ServicioModel service)throw new InvalidOperationException("Selecciona un servicio.");DateTime? desired=null;if(service.RequiereCita){if(DateBox.SelectedDate is null||!TimeSpan.TryParseExact(TimeBox.Text,"hh\\:mm",CultureInfo.InvariantCulture,out var time))throw new InvalidOperationException("Indica una fecha y hora válida para la cita.");desired=DateBox.SelectedDate.Value.Date.Add(time);}new SolicitudServicioService().Crear(_guestId,service.Id,desired,DescriptionBox.Text);DialogResult=true;}catch(Exception ex){ErrorText.Text=ex.Message;}}
}
