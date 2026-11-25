using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class ResumenServiciosRecepcionControl : UserControl
{
    public event Action<string>? NavegacionSolicitada;
    public ResumenServiciosRecepcionControl(){InitializeComponent();Recargar();}
    public void Recargar()
    {
        var pending=SampleData.SolicitudesServicio.Where(item=>item.Estado is "Pendiente" or "Solicitada" or "Aceptada" or "En atención" or "En curso").ToList();
        var appointments=SampleData.Citas.Where(item=>item.Inicio.Date==DateTime.Today&&item.Estado is not ("Cancelada" or "No presentado")).ToList();
        var linkedAppointmentIds=SampleData.SolicitudesServicio.Where(item=>item.CitaServicioId is not null).Select(item=>item.CitaServicioId!.Value).ToHashSet();
        var confirm=SampleData.SolicitudesServicio.Count(item=>item.Estado is "Pendiente" or "Solicitada" or "Aceptada")+SampleData.Citas.Count(item=>item.Estado=="Solicitada"&&!linkedAppointmentIds.Contains(item.Id));
        var active=SampleData.SolicitudesServicio.Count(item=>item.Estado is "En atención" or "En curso")+SampleData.Citas.Count(item=>item.Estado=="En curso");
        MetricsList.ItemsSource=new[]{new{Key="Pendientes",Title="Solicitudes pendientes",Count=pending.Count,Detail=$"{pending.Count(item=>!item.RequiereCita)} inmediatas"},new{Key="Agenda",Title="Citas de hoy",Count=appointments.Count,Detail=$"{appointments.Count(item=>item.Estado=="Confirmada")} confirmadas"},new{Key="Confirmar",Title="Por confirmar",Count=confirm,Detail="Solicitudes por coordinar"},new{Key="Atencion",Title="En atención",Count=active,Detail="Servicios activos"}};
    }
    private void Metric_Click(object sender,RoutedEventArgs e){if(sender is Button{Tag:string target})NavegacionSolicitada?.Invoke(target);}
}
