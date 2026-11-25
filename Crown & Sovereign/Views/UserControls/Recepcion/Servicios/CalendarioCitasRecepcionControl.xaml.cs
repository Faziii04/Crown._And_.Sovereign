using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class CalendarioCitasRecepcionControl : UserControl
{
    private readonly CitaServicioService _service=new();
    public CalendarioCitasRecepcionControl(){InitializeComponent();FromBox.SelectedDate=DateTime.Today;ToBox.SelectedDate=DateTime.Today.AddDays(14);Recargar();}
    public void Recargar(){var from=FromBox.SelectedDate??DateTime.Today;var to=ToBox.SelectedDate??from.AddDays(14);AgendaGrid.ItemsSource=_service.ConsultarAgenda().Where(item=>item.Start.Date>=from.Date&&item.Start.Date<=to.Date).ToList();}
    private void Date_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)Recargar();}
}
