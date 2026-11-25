using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Shared.Cards;

public partial class ServicioCardControl : UserControl
{
    public event Action<ServicioModel>? SolicitudSolicitada;
    private ActiveGuestChoice? _guest;
    public ActiveGuestChoice? Guest { get=>_guest; set { _guest=value; Actualizar(); } }
    public ServiceBenefitEvaluation? Evaluation { get; private set; }
    public ServicioCardControl() { InitializeComponent(); Loaded += (_, _) => Actualizar(); DataContextChanged += (_, _) => Actualizar(); }
    private void Actualizar()
    {
        if (DataContext is not ServicioModel service || CoverageText is null) return;
        Evaluation = Guest is null ? null : new ServicioBeneficioService().Evaluar(Guest.EstanciaId, service.Id);
        CoverageText.Text = Evaluation is {Available:false}?"No disponible temporalmente":Evaluation?.Label ?? "Selecciona un huésped";
        DetailText.Text = Evaluation is {Available:false}?"Consulta nuevamente con Operaciones; este servicio no admite solicitudes por el momento.":Evaluation?.Detail ?? "La cobertura depende de los paquetes comprados para la estancia.";
        AppointmentText.Text = service.RequiereCita ? $"Cita · {service.DuracionMinutos} min · horario sujeto a confirmación" : "Solicitud directa a Operaciones";
        PriceText.Text = Evaluation is null ? $"Precio de referencia Bs {service.Precio:N2}" : Evaluation.EstimatedCharge > 0 ? $"Estimado Bs {Evaluation.EstimatedCharge:N2}" : "Sin cargo adicional estimado";
        RequestButton.IsEnabled = Evaluation?.Available == true;
        RequestButton.Content = service.RequiereCita ? "Solicitar horario" : "Solicitar servicio";
        RequestButton.Content = service.RequiereCita ? "Solicitar cita" : "Solicitar servicio";
    }
    private void Request_Click(object sender, RoutedEventArgs e) { if (DataContext is ServicioModel service) SolicitudSolicitada?.Invoke(service); }
}
