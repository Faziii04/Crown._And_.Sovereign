using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class ProgresoSolicitudServicioControl : UserControl
{
    public ProgresoSolicitudServicioControl(){InitializeComponent();}
    public void Mostrar(ServiceRequestView? request)
    {
        if(request is null){StepsList.ItemsSource=Array.Empty<ServiceProgressStep>();return;}
        var steps=new List<ServiceProgressStep>{new("Solicitud recibida",$"{request.Guest} · {request.Service} · {request.Coverage}","Completado",request.RequestedAt)};
        if(request.RequiresAppointment)steps.Add(new("Coordinación de cita",request.ConfirmedDate is DateTime confirmed?$"Confirmada para {confirmed:dd/MM/yyyy HH:mm} · {request.Responsible}":"Horario solicitado; pendiente de confirmación.",request.ConfirmedDate is null?"Pendiente":"Completado",request.ConfirmedDate));
        else steps.Add(new("Atención operativa",request.Responsible=="Por asignar"?"Pendiente de asignación.":$"Responsable: {request.Responsible}",request.Status is "En atención" or "Completada"?"Completado":"Pendiente",null));
        if(request.Status is "En atención" or "En curso" or "Completada")steps.Add(new("Servicio en atención",request.Status is "Completada"?"La entrega quedó registrada.":"El equipo está prestando el servicio.",request.Status=="Completada"?"Completado":"En curso",request.CompletedDate));
        if(request.Status is "Cancelada" or "No presentado")steps.Add(new(request.Status,request.Outcome,"Final",null));
        StepsList.ItemsSource=steps;
    }
}
