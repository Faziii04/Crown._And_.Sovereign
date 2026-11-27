using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class TemporadasControl:UserControl
{
    public event Action? DatosActualizados;
    public TemporadasControl(){InitializeComponent();Refrescar();}
    public void Refrescar(){var today=DateTime.Today;var rows=SampleData.Temporadas.OrderBy(item=>item.Desde).Select(item=>new TemporadaResumenModel(item.Id,item.Nombre,item.Desde,item.Hasta,item.Hasta.Date<=today?"Finalizada":item.Desde.Date<=today?"Vigente":"Próxima",SampleData.TarifasConfiguraciones.Count(rate=>rate.TemporadaId==item.Id))).ToList();SeasonsGrid.ItemsSource=rows;Timeline.ItemsSource=rows;}
    private void New_Click(object sender,RoutedEventArgs e){var d=new TemporadaDialog{Owner=Window.GetWindow(this)};if(d.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void Edit_Click(object sender,System.Windows.Input.MouseButtonEventArgs e){if(SeasonsGrid.SelectedItem is not TemporadaResumenModel row)return;var d=new TemporadaDialog(SampleData.Temporadas.First(item=>item.Id==row.Id)){Owner=Window.GetWindow(this)};if(d.ShowDialog()==true){Refrescar();DatosActualizados?.Invoke();}}
    private void Delete_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not TemporadaResumenModel row)return;var answer=MessageBox.Show(Window.GetWindow(this),$"¿Eliminar la temporada «{row.Nombre}»? Solo se podrán eliminar periodos sin precios ni reservas relacionadas.","Eliminar temporada",MessageBoxButton.YesNo,MessageBoxImage.Question);if(answer!=MessageBoxResult.Yes)return;try{new GerenciaService().EliminarTemporada(row.Id);Refrescar();DatosActualizados?.Invoke();}catch(Exception ex){MessageBox.Show(Window.GetWindow(this),ex.Message,"Se conservó el periodo",MessageBoxButton.OK,MessageBoxImage.Information);}}
}
