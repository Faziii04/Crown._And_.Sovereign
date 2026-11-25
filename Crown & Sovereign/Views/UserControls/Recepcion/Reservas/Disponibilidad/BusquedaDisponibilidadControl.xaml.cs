using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Disponibilidad;

public partial class BusquedaDisponibilidadControl : UserControl
{
    public event Action<ReservationSearchCriteria>? BuscarSolicitado;
    public event Action? LimpiarSolicitado;
    public BusquedaDisponibilidadControl()
    {
        InitializeComponent();ArrivalPicker.SelectedDate=DateTime.Today.AddDays(1);DeparturePicker.SelectedDate=DateTime.Today.AddDays(2);
        GuestsBox.ItemsSource=Enumerable.Range(1,16).ToList();GuestsBox.SelectedIndex=1;RoomsBox.ItemsSource=Enumerable.Range(1,8).ToList();RoomsBox.SelectedIndex=0;
        TypeBox.ItemsSource=new[]{"Todos los tipos"}.Concat(Crown___Sovereign.Data.SampleData.Tipos).ToList();TypeBox.SelectedIndex=0;
    }
    public ReservationSearchCriteria Criterios()=>new(){Llegada=ArrivalPicker.SelectedDate??DateTime.Today.AddDays(1),Salida=DeparturePicker.SelectedDate??DateTime.Today.AddDays(2),Personas=GuestsBox.SelectedItem is int guests?guests:1,Habitaciones=RoomsBox.SelectedItem is int rooms?rooms:1,TipoHabitacion=TypeBox.SelectedItem?.ToString(),Comodidades=ComodidadesSeleccionadas()};
    public void FijarFechas(DateTime llegada,DateTime salida){ArrivalPicker.SelectedDate=llegada.Date;DeparturePicker.SelectedDate=salida.Date;}
    private string[] ComodidadesSeleccionadas()=>new[]{("Balcón",BalconyBox.IsChecked==true),("Jacuzzi",JacuzziBox.IsChecked==true),("Minibar",MinibarBox.IsChecked==true),("Wi-Fi",WifiBox.IsChecked==true),("Aire acondicionado",AirBox.IsChecked==true),("TV",TvBox.IsChecked==true),("Accesibilidad",AccessibleBox.IsChecked==true)}.Where(item=>item.Item2).Select(item=>item.Item1).ToArray();
    private void Search_Click(object sender,RoutedEventArgs e){var criteria=Criterios();try{HabitacionDisponibilidadService.ValidarFechas(criteria.Llegada,criteria.Salida);if(criteria.Personas<1||criteria.Habitaciones<1)throw new InvalidOperationException("Personas y habitaciones deben ser cantidades positivas.");BuscarSolicitado?.Invoke(criteria);}catch(Exception ex){MessageBox.Show(Window.GetWindow(this),ex.Message,"Revisa la búsqueda",MessageBoxButton.OK,MessageBoxImage.Warning);}}
    private void Clear_Click(object sender,RoutedEventArgs e){ArrivalPicker.SelectedDate=DateTime.Today.AddDays(1);DeparturePicker.SelectedDate=DateTime.Today.AddDays(2);GuestsBox.SelectedItem=2;RoomsBox.SelectedItem=1;TypeBox.SelectedIndex=0;foreach(var check in new[]{BalconyBox,JacuzziBox,MinibarBox,WifiBox,AirBox,TvBox,AccessibleBox})check.IsChecked=false;LimpiarSolicitado?.Invoke();}
}
