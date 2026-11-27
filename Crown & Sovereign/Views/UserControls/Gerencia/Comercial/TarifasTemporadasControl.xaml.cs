using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class TarifasTemporadasControl : UserControl
{
    public TarifasTemporadasControl(){InitializeComponent();RatesView.DatosActualizados+=WorkspaceChanged;SeasonsView.DatosActualizados+=WorkspaceChanged;ChannelsView.DatosActualizados+=WorkspaceChanged;RefreshIndicators();}
    private void RefreshIndicators(){var now=DateTime.Today;CurrentSeason.Text=SampleData.Temporadas.FirstOrDefault(item=>item.Desde.Date<=now&&now<item.Hasta.Date)?.Nombre??"Tarifa base";NextSeason.Text=SampleData.Temporadas.Where(item=>item.Desde.Date>now).OrderBy(item=>item.Desde).FirstOrDefault()?.Nombre??"Sin programar";ConfiguredCount.Text=SampleData.TarifasConfiguraciones.Count.ToString();ReviewCount.Text=Math.Max(0,SampleData.TarifasHotel.Count*SampleData.Temporadas.Count*SampleData.CanalesReserva.Count-SampleData.TarifasConfiguraciones.Count).ToString();}
    private void NewSeason_Click(object sender,RoutedEventArgs e){var dialog=new TemporadaDialog{Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true)WorkspaceChanged();}
    private void OpenSimulator_Click(object sender,RoutedEventArgs e)=>WorkspaceTabs.SelectedIndex=3;
    private void WorkspaceChanged(){RatesView.Refrescar();SeasonsView.Refrescar();ChannelsView.Refrescar();RefreshIndicators();}
}
