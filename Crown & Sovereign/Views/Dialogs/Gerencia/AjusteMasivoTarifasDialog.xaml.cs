using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class AjusteMasivoTarifasDialog:Window
{
    private readonly GerenciaService _service=new();private bool _previewReady;
    public AjusteMasivoTarifasDialog(){InitializeComponent();Scope.SelectedIndex=0;Mode.SelectedIndex=0;}
    private (bool Base,bool Percent,decimal Amount) ReadInput(){if(!decimal.TryParse(Amount.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value))throw new InvalidOperationException("Escribe un valor numérico válido.");return(Scope.SelectedIndex==0,Mode.SelectedIndex==0,value);}
    private void Input_Changed(object sender,RoutedEventArgs e)=>Invalidate();
    private void Input_Changed(object sender,TextChangedEventArgs e)=>Invalidate();
    private void Invalidate(){if(!IsLoaded)return;_previewReady=false;ApplyButton.IsEnabled=false;}
    private void Preview_Click(object sender,RoutedEventArgs e){try{var input=ReadInput();var values=_service.PrevisualizarAjuste(input.Base,input.Amount,input.Percent);if(values.Count==0)throw new InvalidOperationException("No hay importes existentes dentro de este alcance.");if(values.Any(item=>item.Despues<=0))throw new InvalidOperationException("El ajuste produciría al menos un precio no válido. No se aplicó ningún cambio.");PreviewGrid.ItemsSource=values;Summary.Text=$"Vista previa de {values.Count} precios. Revisa cada combinación antes de aplicar; los precios faltantes se mantienen sin cambios.";_previewReady=true;ApplyButton.IsEnabled=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Vista previa no disponible",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Apply_Click(object sender,RoutedEventArgs e){if(!_previewReady)return;try{var input=ReadInput();_service.AplicarAjuste(input.Base,input.Amount,input.Percent);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se aplicó el ajuste",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();
}
