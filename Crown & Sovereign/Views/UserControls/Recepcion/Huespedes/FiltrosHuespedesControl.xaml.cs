using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class FiltrosHuespedesControl : UserControl
{
 private string _estado="Todos"; public event Action? FiltroCambiado;
 public FiltrosHuespedesControl(){InitializeComponent();StayFilter.ItemsSource=new[]{"Todas las estancias","Con estancia","Sin estancia","Sin historial","Con estancias completadas"};StayFilter.SelectedIndex=0;FloorFilter.ItemsSource=new[]{"Todos","1","2","3","4","5"};FloorFilter.SelectedIndex=0;SortBox.ItemsSource=new[]{"Nombre A–Z","Nombre Z–A","Próxima llegada","Última estancia","Estancias completadas"};SortBox.SelectedIndex=0;}
 public FiltroDirectorioHuespedes ObtenerFiltro(){int? floor=int.TryParse(FloorFilter.SelectedItem?.ToString(),out var f)?f:null;return new(){Busqueda=SearchBox.Text,Estado=_estado,Estancias=StayFilter.SelectedIndex==0?"Todas":StayFilter.SelectedItem?.ToString()??"Todas",SolicitudesPendientes=PendingCheck.IsChecked==true?true:null,ContactoIncompleto=ContactCheck.IsChecked==true,ConsentimientoRevision=ConsentCheck.IsChecked==true,Piso=floor,Habitacion=RoomFilter.Text,Orden=SortBox.SelectedItem?.ToString()??"Nombre A–Z"};}
 public void Limpiar(){SearchBox.Clear();_estado="Todos";StayFilter.SelectedIndex=0;PendingCheck.IsChecked=false;ContactCheck.IsChecked=false;ConsentCheck.IsChecked=false;FloorFilter.SelectedIndex=0;RoomFilter.Clear();SortBox.SelectedIndex=0;}
 private void Changed(object s,RoutedEventArgs e)=>FiltroCambiado?.Invoke(); private void Changed(object s,TextChangedEventArgs e)=>FiltroCambiado?.Invoke();
 private void Set(string value){_estado=value;FiltroCambiado?.Invoke();} private void All_Click(object s,RoutedEventArgs e)=>Set("Todos"); private void Active_Click(object s,RoutedEventArgs e)=>Set("En hotel"); private void Upcoming_Click(object s,RoutedEventArgs e)=>Set("Próximos"); private void Repeat_Click(object s,RoutedEventArgs e)=>Set("Recurrentes");
 private void Clear_Click(object s,RoutedEventArgs e){Limpiar();FiltroCambiado?.Invoke();}
}
