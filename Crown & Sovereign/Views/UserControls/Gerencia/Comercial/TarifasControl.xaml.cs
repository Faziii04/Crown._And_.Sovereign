using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class TarifasControl:UserControl
{
    private readonly GerenciaService _service=new();
    public event Action? DatosActualizados;
    public TarifasControl(){InitializeComponent();ChannelFilter.ItemsSource=SampleData.CanalesReserva;ChannelFilter.DisplayMemberPath="Nombre";ChannelFilter.SelectedValuePath="Id";ChannelFilter.SelectedIndex=0;SeasonHeaders.ItemsSource=SampleData.Temporadas.OrderBy(item=>item.Desde).ToList();Refrescar();}
    private int ChannelId=>ChannelFilter.SelectedValue is int id?id:SampleData.CanalesReserva[0].Id;
    public void Refrescar(){var selected=ChannelId;ChannelFilter.ItemsSource=SampleData.CanalesReserva;ChannelFilter.SelectedValue=selected;ReloadMatrix();}
    private void ReloadMatrix(){if(ChannelFilter.SelectedValue is not int)return;var rates=_service.MatrizTarifas(ChannelId);var ordered=SampleData.Temporadas.OrderBy(item=>item.Desde).ToList();SeasonHeaders.ItemsSource=ordered;MatrixRows.ItemsSource=SampleData.TarifasHotel.Select(rate=>new FilaMatrizTarifasModel(rate.TipoHabitacion,rate.Id,rate.Precio,ordered.Select(season=>rates.Single(cell=>cell.TarifaId==rate.Id&&cell.TemporadaId==season.Id)).ToList())).ToList();}
    private void Filter_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)ReloadMatrix();}
    private void EditCell_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not CeldaMatrizTarifa cell)return;var dialog=new ConfiguracionTarifaDialog(cell,ChannelId){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void EditBase_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not FilaMatrizTarifasModel row)return;var rate=SampleData.TarifasHotel.First(item=>item.Id==row.TarifaId);var dialog=new TarifaDialog(rate){Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void Bulk_Click(object sender,RoutedEventArgs e){var dialog=new AjusteMasivoTarifasDialog{Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void Copy_Click(object sender,RoutedEventArgs e){var dialog=new CopiarTarifasTemporadaDialog{Owner=Window.GetWindow(this)};if(dialog.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
}
