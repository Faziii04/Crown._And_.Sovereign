using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;

public partial class ServiciosDisponiblesControl : UserControl
{
    public event Action<ServicioModel>? UtilizacionSolicitada;
    public ServiciosDisponiblesControl() => InitializeComponent();
    public void Mostrar(int guestId)
    {
        var stay=SampleData.Estancias.First(item=>item.HuespedId==guestId&&item.Estado=="Activa");
        DataContext = SampleData.Servicios.Where(service => service.Reservable).Select(service =>
        {
            var benefit=new ServicioBeneficioService().Evaluar(stay.Id,service.Id,huespedId:guestId);
            var coverage=benefit.Label;
            var price=benefit.EstimatedCharge==0?"Sin cargo adicional estimado":$"Estimado Bs {benefit.EstimatedCharge:N2}";
            return (Service:service, Benefit:benefit, Card:new ServiceUsageCard(service, coverage, price, service.RequiereCita ? "Programar cita" : "Registrar utilización",benefit.Detail,benefit.Available));
        }).Where(item=>item.Service.Activo||item.Benefit.PurchasedRemaining>0||item.Benefit.Remaining>0||item.Benefit.CoverageType=="Ilimitado").Select(item=>item.Card).GroupBy(card => card.Service.Categoria).SelectMany(group => group).ToList();
    }
    private void Register_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: ServicioModel service }) UtilizacionSolicitada?.Invoke(service); }
}
