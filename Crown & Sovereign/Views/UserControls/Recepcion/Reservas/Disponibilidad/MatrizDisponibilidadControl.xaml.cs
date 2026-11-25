using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Disponibilidad;

public partial class MatrizDisponibilidadControl : UserControl
{
    private readonly HabitacionDisponibilidadService _service=new();
    private int _days=7;
    private (DateTime Date,string Type,int Available)? _selected;
    public event Action<ReservationSearchCriteria>? ReservaSolicitada;
    public MatrizDisponibilidadControl(){InitializeComponent();StartDatePicker.SelectedDate=DateTime.Today;HorizonBox.ItemsSource=new[]{7,14,30};HorizonBox.SelectedItem=7;Recargar();}
    public void Recargar(DateTime? start=null,int? days=null){if(start.HasValue)StartDatePicker.SelectedDate=start.Value.Date;if(days is int count){_days=Math.Clamp(count,1,30);HorizonBox.SelectedItem=_days;}var from=StartDatePicker.SelectedDate??DateTime.Today;var dates=Enumerable.Range(0,_days).Select(i=>{var date=from.AddDays(i);return new{date,Label=date.ToString("ddd dd MMM",new System.Globalization.CultureInfo("es-BO"))};}).ToList();DataContext=new{Fechas=dates,Lineas=_service.ObtenerDisponibilidadPorNoche(from,_days)};_selected=null;SelectionText.Text="Selecciona una fecha para consultar habitaciones.";ReserveCellButton.IsEnabled=false;}
    private void Date_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)Recargar();}
    private void Horizon_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded&&HorizonBox.SelectedItem is int days){_days=days;Recargar();}}
    private void Previous_Click(object sender,RoutedEventArgs e){_days=HorizonBox.SelectedItem is int days?days:_days;Recargar((StartDatePicker.SelectedDate??DateTime.Today).AddDays(-_days));}
    private void Next_Click(object sender,RoutedEventArgs e){_days=HorizonBox.SelectedItem is int days?days:_days;Recargar((StartDatePicker.SelectedDate??DateTime.Today).AddDays(_days));}
    private void Today_Click(object sender,RoutedEventArgs e)=>Recargar(DateTime.Today);
    private void Cell_Click(object sender,MouseButtonEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is not ReservationAvailabilityCell cell)return;
        var type=(sender as FrameworkElement)?.Tag?.ToString()??"";if(type=="Total")return;
        _selected=(cell.Date,type,cell.Available);SelectionText.Text=$"{type} · {cell.Date:dddd dd MMMM} · {cell.Available} de {cell.Total} habitaciones disponibles";
        ReserveCellButton.IsEnabled=cell.Available>0;
        if(e.ClickCount>=2&&cell.Available>0)ReserveSelected();
    }
    private void ReserveCell_Click(object sender,RoutedEventArgs e)=>ReserveSelected();
    private void ReserveSelected(){if(_selected is not{Available:>0} selection)return;ReservaSolicitada?.Invoke(new(){Llegada=selection.Date,Salida=selection.Date.AddDays(1),Personas=1,Habitaciones=1,TipoHabitacion=selection.Type});}
}
