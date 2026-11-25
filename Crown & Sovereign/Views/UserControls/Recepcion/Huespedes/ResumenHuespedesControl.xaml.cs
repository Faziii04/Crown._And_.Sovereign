using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class ResumenHuespedesControl : UserControl
{
 public event Action<string>? FiltroSolicitado;
 public ResumenHuespedesControl(){InitializeComponent();Loaded+=(_,_)=>Actualizar();}
 public void Actualizar(){var m=new HuespedService().ObtenerIndicadores();RegisteredCount.Text=m.Registrados.ToString();ActiveCount.Text=m.EnHotel.ToString();UpcomingCount.Text=m.ProximasReservas.ToString();RepeatCount.Text=m.Recurrentes.ToString();}
 private void Registered_Click(object s,RoutedEventArgs e)=>FiltroSolicitado?.Invoke("Todos"); private void Active_Click(object s,RoutedEventArgs e)=>FiltroSolicitado?.Invoke("En hotel"); private void Upcoming_Click(object s,RoutedEventArgs e)=>FiltroSolicitado?.Invoke("Próximos"); private void Repeat_Click(object s,RoutedEventArgs e)=>FiltroSolicitado?.Invoke("Recurrentes");
}
