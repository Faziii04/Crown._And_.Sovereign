using System.Globalization;
using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class ConfiguracionTarifaDialog:Window
{
    private readonly CeldaMatrizTarifa _cell;private readonly int _channelId;
    public ConfiguracionTarifaDialog(CeldaMatrizTarifa cell,int channelId){InitializeComponent();_cell=cell;_channelId=channelId;var season=SampleData.Temporadas.First(item=>item.Id==cell.TemporadaId);var channel=SampleData.CanalesReserva.First(item=>item.Id==channelId);Context.Text=$"{cell.TipoHabitacion} · {season.Nombre} ({season.Desde:dd/MM/yyyy}–{season.Hasta:dd/MM/yyyy}, fecha final excluida) · {channel.Nombre}\nPrecio base actual: Bs {SampleData.TarifasHotel.First(item=>item.Id==cell.TarifaId).Precio:N2}";Price.Text=cell.Precio.ToString(CultureInfo.CurrentCulture);}
    private void Save_Click(object sender,RoutedEventArgs e){if(!decimal.TryParse(Price.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value)||value<=0){MessageBox.Show(this,"Escribe un importe válido mayor que cero.","Revisa el precio",MessageBoxButton.OK,MessageBoxImage.Information);return;}try{new GerenciaService().GuardarTarifaConfiguracion(_cell.TarifaId,_cell.TemporadaId,_channelId,value);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar",MessageBoxButton.OK,MessageBoxImage.Information);}}
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();
}
