using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class DetalleSolicitudServicioControl : UserControl
{
    private ServiceRequestView? _request;
    public event Action<int>? CancelacionSolicitada;
    public DetalleSolicitudServicioControl(){InitializeComponent();}
    public void Mostrar(ServiceRequestView? request)
    {
        _request=request;ProgressView.Mostrar(request);CancelButton.IsEnabled=request?.Status is "Pendiente" or "Solicitada";
        SummaryText.Text=request is null?"Selecciona una solicitud para consultar alcance, cobertura y acciones.":$"{request.Guest} · Hab. {request.Room} · Estancia #{request.StayId} · {request.Service} · {request.Coverage}\n{(request.EstimatedCharge>0?$"Cargo Bs {request.EstimatedCharge:N2}":"Sin cargo registrado")} · {(request.ConfirmedDate is DateTime confirmed?$"Cita confirmada {confirmed:dd/MM HH:mm}":request.RequiresAppointment?"Horario preferido sin confirmar":"Servicio inmediato")}{(string.IsNullOrWhiteSpace(request.Description)?"":$"\n{request.Description}")}";
    }
    private void Cancel_Click(object sender,RoutedEventArgs e){if(_request is not null)CancelacionSolicitada?.Invoke(_request.Id);}
}
