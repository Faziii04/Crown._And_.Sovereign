using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class InformacionHuespedControl:UserControl
{
 public InformacionHuespedControl()=>InitializeComponent(); public void Mostrar(HuespedModel guest){DocumentText.Text=$"Documento: {guest.Documento}";EmailText.Text=$"Correo: {(string.IsNullOrWhiteSpace(guest.Email)?"No registrado":guest.Email)}";PhoneText.Text=$"Teléfono: {(string.IsNullOrWhiteSpace(guest.Telefono)?"No registrado":guest.Telefono)}";ContactCompleteness.Text=new HuespedService().ObtenerCompletitudContacto(guest.Id).Detalle;}
}
