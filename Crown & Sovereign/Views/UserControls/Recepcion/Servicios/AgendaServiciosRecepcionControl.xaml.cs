using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class AgendaServiciosRecepcionControl : UserControl
{
    public AgendaServiciosRecepcionControl(){InitializeComponent();}
    public void Recargar(){TodayView.Recargar();CalendarView.Recargar();PendingView.Recargar();}
    public void MostrarHoy(){TodayView.Fecha=DateTime.Today;TodayView.Recargar();}
}
