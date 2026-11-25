using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Recepcion;
public partial class ConsentimientoHuespedDialog:Window
{
 private readonly HuespedModel _guest; public ConsentimientoHuespedDialog(HuespedModel guest){InitializeComponent();_guest=guest;GuestName.Text=guest.Nombre;YesRadio.IsChecked=guest.ConsentimientoDatos;NoRadio.IsChecked=!guest.ConsentimientoDatos;}
 private void Save_Click(object sender,RoutedEventArgs e){try{new HuespedService().ActualizarConsentimiento(_guest.Id,YesRadio.IsChecked==true,NoteBox.Text);DialogResult=true;}catch(Exception ex){ErrorText.Text=ex.Message;}}
}
