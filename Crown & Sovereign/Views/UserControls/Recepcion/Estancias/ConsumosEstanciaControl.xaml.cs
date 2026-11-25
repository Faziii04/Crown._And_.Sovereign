using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ConsumosEstanciaControl : UserControl
{
    private readonly InventarioService _inventario = new();
    public ConsumosEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId) => DataContext = _inventario.ConsultarConsumosPorEstancia(stayId);
}
