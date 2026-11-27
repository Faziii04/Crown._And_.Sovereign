using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.Dialogs.Gerencia;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class CanalesReservaControl:UserControl
{
    public event Action? DatosActualizados;
    public CanalesReservaControl(){InitializeComponent();Refrescar();}
    public void Refrescar()=>ChannelsGrid.ItemsSource=SampleData.CanalesReserva.Select(item=>new CanalReservaResumenModel(item.Id,item.Nombre,item.Comision,SampleData.TarifasConfiguraciones.Count(rate=>rate.CanalId==item.Id))).ToList();
    private void Add_Click(object sender,RoutedEventArgs e){var dialog=new CanalReservaDialog{Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void Edit_Click(object sender,System.Windows.Input.MouseButtonEventArgs e){if(ChannelsGrid.SelectedItem is not CanalReservaResumenModel row)return;var dialog=new CanalReservaDialog(SampleData.CanalesReserva.First(item=>item.Id==row.Id)){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
}
