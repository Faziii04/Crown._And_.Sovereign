using System.Windows;
using Crown___Sovereign.Models;
namespace Crown___Sovereign.Views.Dialogs.Recepcion;
public partial class SolicitudEspecialHuespedDialog:Window
{
 public int? ReservaId=>(ReservationBox.SelectedItem as ReservaModel)?.Id; public string Descripcion=>DescriptionBox.Text;
 public SolicitudEspecialHuespedDialog(IReadOnlyList<ReservaModel> reservations){InitializeComponent();ReservationBox.ItemsSource=reservations;ReservationBox.SelectedIndex=0;}
 private void Save_Click(object sender,RoutedEventArgs e){if(ReservaId is null||string.IsNullOrWhiteSpace(Descripcion)){ErrorText.Text="Selecciona una reserva y describe la solicitud.";return;}DialogResult=true;}
}
