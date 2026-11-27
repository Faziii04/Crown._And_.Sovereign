using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ServiciosEstanciaControl : UserControl
{
    private int _stayId;
    public event EventHandler? DatosActualizados;
    public ServiciosEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var services = new ServicioService();
        var benefitService = new ServicioBeneficioService();
        BenefitsGrid.ItemsSource = services.ListarReservables().Select(service =>
        {
            var evaluation = benefitService.Evaluar(stayId, service.Id);
            return new { Service = service.Nombre, Category = service.Categoria, Coverage = evaluation.Label, Allowance = evaluation.Allowance == int.MaxValue ? "Sin límite" : evaluation.Allowance.ToString(), Consumed = evaluation.Consumed, RemainingText = evaluation.CoverageType == "Ilimitado" ? "Sin límite" : evaluation.Remaining.ToString(), Price = evaluation.EstimatedCharge, RequiresAppointment = evaluation.RequiresAppointment };
        }).ToList();
        AcquiredGrid.ItemsSource = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == stay.ReservaId)
            .Select(item =>
            {
                var service = SampleData.Servicios.FirstOrDefault(entry => entry.Id == item.ServicioId);
                var guest = item.HuespedId is int guestId ? SampleData.Huespedes.FirstOrDefault(entry => entry.Id == guestId)?.Nombre : null;
                var used = SampleData.UsosServicio.Count(use => use.ServicioAdquiridoId == item.ServicioAdquiridoId && use.EstanciaId == stayId && use.Estado is "Completado" or "Confirmado");
                var cargo = item.CargoId is int cargoId ? SampleData.Cargos.FirstOrDefault(entry => entry.Id == cargoId) : null;
                return new { Service = service?.Nombre ?? $"Servicio #{item.ServicioId}", Recipient = guest ?? "Compartido / sin destinatario", Purchased = item.Cantidad, Used = used, Remaining = item.Estado == "CANCELADO" ? 0 : Math.Max(0, item.Cantidad - used), Price = item.PrecioUnitarioAcordado, State = item.Estado, Charge = cargo is null ? "Sin cargo asociado" : $"Cargo #{cargo.Id} · Bs {cargo.Monto:N2}" };
            }).ToList();
        RequestsGrid.ItemsSource = services.ConsultarSolicitudesYCitasPorEstancia(stayId);
        UsageGrid.ItemsSource = services.ConsultarUtilizacionesPorEstancia(stayId).Select(item => new { item.Fecha, Nombre = services.Listar().First(service => service.Id == item.ServicioId).Nombre, Cobertura = item.ServicioAdquiridoId is not null ? "Adquirido previamente" : item.Incluido ? "Incluido en paquete" : "Cargo adicional", item.Cargo, item.Estado }).ToList();
        RequestButton.IsEnabled = stay.Estado == "Activa";
    }
    public void SetReadOnly(bool readOnly) => RequestButton.IsEnabled = !readOnly;
    private void Request_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new SolicitudServicioHuespedDialog(SampleData.Estancias.First(item => item.Id == _stayId).HuespedId, _stayId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); }
    }
}
