using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class CitasPendientesRecepcionControl : UserControl
{
    private readonly SolicitudServicioService _service=new();
    public CitasPendientesRecepcionControl(){InitializeComponent();Recargar();}
    public void Recargar()=>RequestsGrid.ItemsSource=_service.Listar().Where(item=>item.RequiresAppointment&&item.Status is ("Solicitada" or "Pendiente" or "Confirmada")).ToList();
}
