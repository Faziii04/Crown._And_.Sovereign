using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class ServiciosRecepcionControl : UserControl
{
    public ServiciosRecepcionControl()
    {
        InitializeComponent();
        RequestView.SolicitudCreada += ActualizarResumen;
        RequestView.HuespedActualizado += RequestsView.EstablecerHuesped;
        SummaryView.NavegacionSolicitada += AbrirResumen;
        RequestsView.EstablecerHuesped(RequestView.HuespedActual);
    }
    private void ActualizarResumen(){RequestsView.Recargar();AgendaView.Recargar();SummaryView.Recargar();}
    private void AbrirResumen(string key)
    {
        if(key=="Agenda"){WorkspaceTabs.SelectedIndex=2;AgendaView.MostrarHoy();}
        else{WorkspaceTabs.SelectedIndex=1;RequestsView.ScopeSolicitudes(key=="Pendientes"||key=="Confirmar"?"Todas las solicitudes":"Estancia seleccionada",key=="Confirmar"?"Por confirmar":key=="Atencion"?"En atención":"Pendiente");}
    }
}
