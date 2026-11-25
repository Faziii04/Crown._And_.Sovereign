using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class HistorialHuespedControl : UserControl
{
 private readonly HuespedService _service=new(); private int? _guestId;
 public event Action<int>? ReservaSolicitada;
 public HistorialHuespedControl(){InitializeComponent();ReservationFilter.ItemsSource=new[]{"Todas","Completadas","Próximas","Canceladas"};ReservationFilter.SelectedIndex=0;}
 public void Mostrar(int guestId){_guestId=guestId;Refrescar();}
 private void Refrescar(){if(_guestId is not int id)return;var reservations=_service.ObtenerHistorialReservas(id,ReservationFilter.SelectedItem?.ToString()??"Todas");ReservationGrid.ItemsSource=reservations;ServiceGrid.ItemsSource=_service.ObtenerHistorialServicios(id);HistoryNote.Text=$"{reservations.Count} reserva(s) vinculada(s) por identificador de huésped. No se agrupan registros por nombre.";}
 private void Filter_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)Refrescar();}
 private void OpenReservation_Click(object sender,System.Windows.RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is Crown___Sovereign.Models.ReservaHistorialHuespedFila row)ReservaSolicitada?.Invoke(row.ReservaId);}
}
