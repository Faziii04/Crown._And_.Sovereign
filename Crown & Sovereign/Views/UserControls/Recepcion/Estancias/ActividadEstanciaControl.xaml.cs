using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ActividadEstanciaControl : UserControl
{
    private readonly EstanciaService _estancias = new();
    public ActividadEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId) => DataContext = _estancias.ConsultarActividad(stayId);
}
