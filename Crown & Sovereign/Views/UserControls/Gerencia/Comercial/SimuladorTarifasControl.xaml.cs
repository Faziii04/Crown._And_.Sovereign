using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class SimuladorTarifasControl:UserControl
{
    private readonly TarifaService _rates=new();
    public SimuladorTarifasControl(){InitializeComponent();Arrival.SelectedDate=DateTime.Today.AddDays(1);Departure.SelectedDate=DateTime.Today.AddDays(3);RoomTypes.ItemsSource=SampleData.Tipos;RoomTypes.SelectedIndex=0;Channel.ItemsSource=SampleData.CanalesReserva;Channel.SelectedIndex=0;}
    private void Input_Changed(object sender,RoutedEventArgs e){if(IsLoaded&&Arrival.SelectedDate is DateTime start&&Departure.SelectedDate is DateTime end&&end.Date>start.Date&&RoomTypes.SelectedItems.Count>0&&Channel.SelectedItem is not null)Calculate_Click(sender,e);}
    private void Calculate_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var start=Arrival.SelectedDate??throw new InvalidOperationException("Elige la llegada.");var end=Departure.SelectedDate??throw new InvalidOperationException("Elige la salida.");var types=RoomTypes.SelectedItems.Cast<string>().ToList();if(types.Count==0)throw new InvalidOperationException("Selecciona al menos un tipo de habitación.");var rooms=types.Select(type=>SampleData.Habitaciones.FirstOrDefault(item=>item.Tipo==type)??throw new InvalidOperationException($"No hay habitaciones de tipo {type}. ")).ToList();var selected=Channel.SelectedItem as CanalReservaModel??throw new InvalidOperationException("Elige un canal.");
            var nights=rooms.SelectMany(room=>_rates.DesglosarAlojamiento(room,start,end,selected.Nombre)).ToList();Breakdown.ItemsSource=nights;Total.Text=$"Total alojamiento: Bs {nights.Sum(item=>item.Precio):N2}";Explanation.Text="Una habitación de cada tipo seleccionado. La salida no se cobra. El importe después de comisión estima el ingreso del canal; no es utilidad ni se suma al precio del huésped.";
            Comparison.ItemsSource=SampleData.CanalesReserva.Select(channel=>{var amount=rooms.Sum(room=>_rates.CalcularAlojamiento(room,start,end,channel.Nombre));var commission=decimal.Round(amount*channel.Comision/100m,2);return new{channel.Nombre,Alojamiento=amount,Comision=commission,Neto=amount-commission};}).ToList();
        }
        catch(Exception ex){MessageBox.Show(Window.GetWindow(this),ex.Message,"No se pudo simular",MessageBoxButton.OK,MessageBoxImage.Information);Breakdown.ItemsSource=null;Comparison.ItemsSource=null;Total.Text="";}
    }
}
