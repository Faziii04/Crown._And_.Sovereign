using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class HistorialUtilizacionControl : UserControl
{
    private readonly ServicioService _service = new();
    public HistorialUtilizacionControl() => InitializeComponent();
    public void Mostrar(int guestId) => DataContext = _service.ConsultarHistorial(guestId);
}
