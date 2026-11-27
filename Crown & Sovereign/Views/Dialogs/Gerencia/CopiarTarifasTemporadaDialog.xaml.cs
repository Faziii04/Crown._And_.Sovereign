using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class CopiarTarifasTemporadaDialog:Window
{
    private bool _reviewed;
    public CopiarTarifasTemporadaDialog(){InitializeComponent();Source.ItemsSource=SampleData.Temporadas.OrderBy(item=>item.Desde).ToList();Target.ItemsSource=SampleData.Temporadas.OrderBy(item=>item.Desde).ToList();Source.SelectedIndex=0;if(Target.Items.Count>1)Target.SelectedIndex=1;}
    private void Selection_Changed(object sender,RoutedEventArgs e){if(!IsLoaded)return;_reviewed=false;ApplyButton.IsEnabled=false;}
    private void Preview_Click(object sender,RoutedEventArgs e){try{if(Source.SelectedItem is not TemporadaModel source||Target.SelectedItem is not TemporadaModel target)throw new InvalidOperationException("Selecciona ambas temporadas.");if(source.Id==target.Id)throw new InvalidOperationException("Elige temporadas distintas.");var configs=SampleData.TarifasConfiguraciones.Where(item=>item.TemporadaId==source.Id).ToList();if(configs.Count==0)throw new InvalidOperationException("La temporada de origen no tiene precios configurados.");var targetKeys=SampleData.TarifasConfiguraciones.Where(item=>item.TemporadaId==target.Id).Select(item=>(item.TarifaId,item.CanalId)).ToHashSet();var existing=configs.Count(item=>targetKeys.Contains((item.TarifaId,item.CanalId)));var creates=configs.Count-existing;var replaces=ReplaceExisting.IsChecked==true?existing:0;var skips=existing-replaces;Summary.Text=$"Se crearán {creates} configuración(es), se {(ReplaceExisting.IsChecked==true?"reemplazarán":"conservarán") } {replaces+skips} ya existentes. {(skips>0?$"Se omitirán {skips} precio(s) del destino.":"")}";_reviewed=true;ApplyButton.IsEnabled=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se puede copiar",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Apply_Click(object sender,RoutedEventArgs e){if(!_reviewed||Source.SelectedItem is not TemporadaModel source||Target.SelectedItem is not TemporadaModel target)return;try{new GerenciaService().CopiarTemporada(source.Id,target.Id,ReplaceExisting.IsChecked==true);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se copiaron las tarifas",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();
}
