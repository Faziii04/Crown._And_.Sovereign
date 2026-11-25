using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class PerfilHuespedControl:UserControl
{
 private HuespedModel? _guest; private EstanciaActualHuespedModel? _stay; private ReservaModel? _upcoming; private bool _expanded;
 public event Action<HuespedModel>? EdicionSolicitada; public event Action<bool>? VistaCompletaCambiada; public event Action<string,int>? NavegacionSolicitada;
 public PerfilHuespedControl(){InitializeComponent();HistoryView.ReservaSolicitada+=id=>NavegacionSolicitada?.Invoke("Reservas",id);}
 public void Limpiar(){_guest=null;_stay=null;_upcoming=null;GuestName.Text="Selecciona un huésped";ProfileContext.Text="El perfil aparecerá aquí.";InitialsText.Text="?";ProfileTabs.IsEnabled=false;ExpandButton.IsEnabled=false;}
 public void Mostrar(HuespedModel guest){_guest=guest;ProfileTabs.IsEnabled=true;ExpandButton.IsEnabled=true;InitialsText.Text=string.Concat(guest.Nombre.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part=>char.ToUpperInvariant(part[0])));GuestName.Text=guest.Nombre;var service=new HuespedService();var summary=service.ObtenerResumenPerfil(guest.Id);_stay=summary.EstanciaActual;_upcoming=summary.ProximaReserva;var rooms=_stay?.Habitaciones??[];var context=_stay is not null?$"En hotel · Hab. {string.Join(", ",rooms.Select(room=>room.Numero))} · {_stay.Paquete}":_upcoming is not null?$"Próxima llegada {_upcoming.Llegada:dd/MM} · Reserva #{_upcoming.Id}":"Sin estancia activa ni próxima reserva";ProfileContext.Text=$"Doc. {guest.Documento} · {context}";RoomButton.Visibility=rooms.Count>0?Visibility.Visible:Visibility.Collapsed;FolioButton.Visibility=_stay is not null&&!_stay.EsAcompanante?Visibility.Visible:Visibility.Collapsed;ReservationButton.Visibility=_upcoming is not null?Visibility.Visible:Visibility.Collapsed;NewReservationButton.Visibility=_stay is null&&_upcoming is null?Visibility.Visible:Visibility.Collapsed;OverviewView.Mostrar(guest);InformationView.Mostrar(guest);HistoryView.Mostrar(guest.Id);CurrentStayView.Mostrar(guest);RequestsView.Mostrar(guest);ConsentView.Mostrar(guest);}
 private void Expand_Click(object s,RoutedEventArgs e){_expanded=!_expanded;ExpandButton.Content=_expanded?"› Directorio":"‹ Directorio";VistaCompletaCambiada?.Invoke(_expanded);}
 private void Edit_Click(object s,RoutedEventArgs e){if(_guest is not null)EdicionSolicitada?.Invoke(_guest);}
 private void Stay_Click(object s,RoutedEventArgs e){if(_guest is not null&&_stay is not null)NavegacionSolicitada?.Invoke("Estancias",_guest.Id);else MessageBox.Show("Este huésped no tiene una estancia activa.","Estancia");}
 private void Folio_Click(object s,RoutedEventArgs e){if(_guest is not null&&_stay is not null)NavegacionSolicitada?.Invoke("Folio",_guest.Id);}
 private void Room_Click(object s,RoutedEventArgs e){if(_stay?.Habitaciones.FirstOrDefault() is { } room)NavegacionSolicitada?.Invoke("Habitaciones",room.Id);}
 private void OpenReservation_Click(object s,RoutedEventArgs e){if(_upcoming is not null)NavegacionSolicitada?.Invoke("Reservas",_upcoming.Id);}
 private void Reservation_Click(object s,RoutedEventArgs e){if(_guest is null)return;var draft=new BorradorReserva{HuespedPrincipalId=_guest.Id,Llegada=DateTime.Today.AddDays(1),Salida=DateTime.Today.AddDays(3),Personas=1};var dialog=new ReservaDialog(borrador:draft){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true)Mostrar(_guest);}
}
