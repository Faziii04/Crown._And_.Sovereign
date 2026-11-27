using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones;

public partial class ResumenPaquetesPromocionesControl : UserControl
{
    public ResumenPaquetesPromocionesControl() => InitializeComponent();
    public void Actualizar(int paquetes, int promociones, int vencimientos, int revision)
    { PackagesCount.Text = paquetes.ToString(); PromotionsCount.Text = promociones.ToString(); ExpiringCount.Text = vencimientos.ToString(); ReviewCount.Text = revision.ToString(); }
}
