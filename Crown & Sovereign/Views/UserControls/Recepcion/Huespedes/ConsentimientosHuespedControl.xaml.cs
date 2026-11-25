using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class ConsentimientosHuespedControl:UserControl
{
 private HuespedModel? _guest; public ConsentimientosHuespedControl()=>InitializeComponent();
 public void Mostrar(HuespedModel guest){_guest=guest;ConsentStatus.Text=guest.ConsentimientoDatos?"Consentimiento registrado: Sí":"Consentimiento registrado: No";var history=new HuespedService().ObtenerHistorialConsentimiento(guest.Id);ConsentHistory.ItemsSource=history;ConsentEmpty.Visibility=history.Count==0?Visibility.Visible:Visibility.Collapsed;}
 private void Consent_Click(object sender,RoutedEventArgs e){if(_guest is null)return;var dialog=new ConsentimientoHuespedDialog(_guest){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true)Mostrar(_guest);}
}
