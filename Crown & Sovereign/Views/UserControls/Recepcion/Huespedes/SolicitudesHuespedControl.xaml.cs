using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class SolicitudesHuespedControl:UserControl
{
 private readonly HuespedService _service=new(); private HuespedModel? _guest;
 public SolicitudesHuespedControl()=>InitializeComponent();
 public void Mostrar(HuespedModel guest){_guest=guest;var special=_service.ObtenerSolicitudesEspeciales(guest.Id);SpecialRequests.ItemsSource=special.Count==0?new[]{"No hay solicitudes especiales asociadas a las reservas de este huésped."}:special.Select(item=>$"{item.Reserva} · {item.Solicitud.Descripcion} · {item.Estado} — {item.HuespedesRelacionados}").ToList();var requests=_service.ObtenerSolicitudesServicio(guest.Id);ServiceRequests.ItemsSource=requests.Count==0?new[]{"No hay solicitudes de servicio registradas."}:requests.Select(item=>$"{item.Servicio} · {item.Solicitud.Estado} · {item.Cita} · {item.Cobertura}").ToList();ServiceHistoryGrid.ItemsSource=_service.ObtenerHistorialServicios(guest.Id);}
 private void SpecialRequest_Click(object sender,RoutedEventArgs e){if(_guest is null)return;var reservations=SampleData.ReservaHuespedes.Where(link=>link.HuespedId==_guest.Id).Select(link=>SampleData.Reservas.FirstOrDefault(reservation=>reservation.Id==link.ReservaId)).Where(reservation=>reservation is not null).Cast<ReservaModel>().OrderByDescending(reservation=>reservation.Llegada).ToList();if(reservations.Count==0){MessageBox.Show("Para asociar una solicitud especial se necesita una reserva vinculada al huésped.","Solicitud especial");return;}var dialog=new SolicitudEspecialHuespedDialog(reservations){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true&&dialog.ReservaId is int id){_service.RegistrarSolicitudEspecial(_guest.Id,id,dialog.Descripcion);Mostrar(_guest);}}
 private void ServiceRequest_Click(object sender,RoutedEventArgs e){if(_guest is null)return;if(_guest.Habitacion<=0){MessageBox.Show("Solo se pueden solicitar servicios para huéspedes con una estancia activa.","Servicio");return;}var dialog=new SolicitudServicioHuespedDialog(_guest.Id){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true)Mostrar(_guest);}
}
