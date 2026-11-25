using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class BeneficiosHuespedControl : UserControl
{
    public BeneficiosHuespedControl() => InitializeComponent();
    public void Mostrar(int guestId) { var guest = SampleData.Huespedes.First(item => item.Id == guestId); PackageText.Text = $"Plan {guest.Paquete} · {guest.RolEnReserva}"; DataContext = new ServicioService().ConsultarBeneficios(guestId); }
}
