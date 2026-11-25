using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Disponibilidad;

public partial class ResultadosDisponibilidadControl:UserControl
{
    public event Action? VolverSolicitado;
    public event Action<TipoDisponibilidadModel>? HabitacionesSolicitadas;
    public ResultadosDisponibilidadControl()=>InitializeComponent();
    public void Mostrar(ReservationSearchCriteria criteria,IReadOnlyList<TipoDisponibilidadModel> results)
    {
        var nights=(criteria.Salida.Date-criteria.Llegada.Date).Days;
        SearchSummary.Text=$"{criteria.Llegada:dd MMM} – {criteria.Salida:dd MMM} · {nights} noches · {criteria.Personas} personas · {criteria.Habitaciones} habitaciones";
        ResultsList.ItemsSource=results;EmptyText.Visibility=results.Count==0?Visibility.Visible:Visibility.Collapsed;
    }
    private void Back_Click(object sender,RoutedEventArgs e)=>VolverSolicitado?.Invoke();
    private void SelectRooms_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.Tag is TipoDisponibilidadModel type)HabitacionesSolicitadas?.Invoke(type);}
}
