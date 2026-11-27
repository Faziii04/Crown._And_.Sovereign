using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class SolicitudesInmediatasControl : UserControl
{
    private readonly SolicitudServicioService _service=new();
    public SolicitudesInmediatasControl(){InitializeComponent();Recargar();}
    public void Recargar()=>RequestsGrid.ItemsSource=_service.Listar().Where(item=>!item.RequiresAppointment&&item.Status is ("Pendiente" or "Confirmada" or "En atención")).ToList();
    private void Start_Click(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:int id})return;
        try{_service.IniciarAtencion(id);Recargar();}catch(Exception ex){MessageBox.Show(ex.Message,"No se pudo iniciar",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
    private void Complete_Click(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:int id})return;
        try
        {
            var request=SampleData.SolicitudesServicio.FirstOrDefault(item=>item.Id==id);if(request is null)return;
            int? agreementId=null;
            if(new ConvenioService().EsExterno(request.ServicioId))
            {
                var date=request.FechaDeseada??DateTime.Now;
                var selector=new SeleccionarProveedorDialog(request.ServicioId,date){Owner=Window.GetWindow(this)};
                if(selector.ShowDialog()!=true)return;
                agreementId=selector.ConvenioSeleccionadoId;
            }
            _service.CompletarAtencion(id,agreementId);Recargar();
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"No se pudo completar",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
}
