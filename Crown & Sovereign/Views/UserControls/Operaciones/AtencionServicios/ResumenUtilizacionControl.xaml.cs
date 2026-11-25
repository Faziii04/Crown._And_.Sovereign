using System.Windows.Controls;
using Crown___Sovereign.Data;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class ResumenUtilizacionControl : UserControl
{
    public ResumenUtilizacionControl() => InitializeComponent();
    public void Mostrar(int guestId)
    {
        var usages = SampleData.UsosServicio.Where(item => item.HuespedId == guestId).ToList();
        DataContext = new[] { new { Titulo = "Usos confirmados", Valor = usages.Count.ToString(), Contexto = "Historial de estancia" }, new { Titulo = "Beneficios incluidos", Valor = usages.Count(item => item.Incluido).ToString(), Contexto = "Sin cargo adicional" }, new { Titulo = "Cargos adicionales", Valor = $"Bs {usages.Sum(item => item.Cargo):N2}", Contexto = "Agregados al folio" } };
    }
}
