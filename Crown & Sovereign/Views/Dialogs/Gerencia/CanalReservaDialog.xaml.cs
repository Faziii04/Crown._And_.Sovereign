using System.Globalization;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class CanalReservaDialog:Window
{
    private readonly CanalReservaModel? _channel;
    public CanalReservaDialog(CanalReservaModel? channel=null){InitializeComponent();_channel=channel;if(channel is not null){Heading.Text="Editar canal";ChannelName.Text=channel.Nombre;Commission.Text=channel.Comision.ToString(CultureInfo.CurrentCulture);}}
    private void Save_Click(object sender,RoutedEventArgs e){if(!decimal.TryParse(Commission.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var percent)||percent is <0 or >100){MessageBox.Show(this,"La comisión debe ser un porcentaje entre 0 y 100.","Comisión inválida",MessageBoxButton.OK,MessageBoxImage.Information);return;}try{var service=new GerenciaService();if(_channel is null)service.CrearCanal(ChannelName.Text,percent);else service.ActualizarCanal(_channel.Id,ChannelName.Text,percent);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar el canal",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();
}
