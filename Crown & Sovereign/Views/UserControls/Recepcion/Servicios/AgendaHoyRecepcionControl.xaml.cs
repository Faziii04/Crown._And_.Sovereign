using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class AgendaHoyRecepcionControl : UserControl
{
    private readonly CitaServicioService _service=new();
    public DateTime Fecha{get=>DateBox.SelectedDate??DateTime.Today;set=>DateBox.SelectedDate=value;}
    public AgendaHoyRecepcionControl(){InitializeComponent();DateBox.SelectedDate=DateTime.Today;Recargar();}
    public void Recargar(){var rows=_service.ConsultarAgenda(Fecha);AgendaGrid.ItemsSource=rows;SummaryText.Text=$"{rows.Count} citas · {rows.Count(item=>item.Status=="Confirmada")} confirmadas · {rows.Count(item=>item.Status=="Solicitada")} por confirmar";}
    private void Date_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)Recargar();}
}
