using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class TemporadaDialog:Window
{
    private readonly TemporadaModel? _season;
    public TemporadaDialog(TemporadaModel? season=null){InitializeComponent();_season=season;if(season is not null){Heading.Text="Editar temporada";SeasonName.Text=season.Nombre;Start.SelectedDate=season.Desde;End.SelectedDate=season.Hasta;}else{Start.SelectedDate=DateTime.Today.AddMonths(9);End.SelectedDate=DateTime.Today.AddMonths(12);}}
    private void Save_Click(object sender,RoutedEventArgs e){if(Start.SelectedDate is not DateTime from||End.SelectedDate is not DateTime to){MessageBox.Show(this,"Completa las dos fechas.","Fechas requeridas",MessageBoxButton.OK,MessageBoxImage.Information);return;}try{new GerenciaService().GuardarTemporada(_season?.Id,SeasonName.Text,from,to);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar la temporada",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();
}
