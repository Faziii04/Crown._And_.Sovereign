using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ServiciosEstanciaControl : UserControl
{
    public ServiciosEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId)
    {
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var services = new ServicioService();
        BenefitsGrid.ItemsSource = services.ConsultarBeneficios(stay.HuespedId);
        RequestsGrid.ItemsSource = services.ConsultarSolicitudesYCitasPorEstancia(stayId);
        UsageGrid.ItemsSource = services.ConsultarUtilizacionesPorEstancia(stayId).Select(item => new { item.Fecha, Nombre = services.Listar().First(service => service.Id == item.ServicioId).Nombre, Cobertura = item.Incluido ? "Incluido en paquete" : "Adicional", item.Cargo, item.Estado }).ToList();
    }
}
