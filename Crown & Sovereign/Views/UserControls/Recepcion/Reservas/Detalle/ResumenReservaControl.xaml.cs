using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Detalle;

public partial class ResumenReservaControl : UserControl
{
    public ResumenReservaControl() => InitializeComponent();
    public void Cargar(ReservaModel reservation)
    {
        GuestText.Text = reservation.Huesped; DatesText.Text = $"{reservation.Llegada:dd/MM/yyyy} – {reservation.Salida:dd/MM/yyyy} · {(reservation.Salida.Date-reservation.Llegada.Date).Days} noches"; StatusText.Text = reservation.Estado;
        var roomIds = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservation.Id).Select(item => item.HabitacionId).Append(reservation.Habitacion).Distinct().ToList();
        var rooms = roomIds.Select(id => SampleData.Habitaciones.First(room => room.Id == id)).ToList();
        var summary = new ReservaService().CalcularResumenEconomico(reservation);
        PriceBreakdown.Text = $"Alojamiento ({rooms.Count} habitación(es)): Bs {summary.Alojamiento:N2}\nPaquetes adquiridos: Bs {summary.Paquetes:N2}\nRecursos adicionales: Bs {summary.Recursos:N2}\nServicios adicionales adquiridos: Bs {summary.Servicios:N2}\nDescuentos: − Bs {summary.Descuentos:N2}\nImpuestos: Bs {summary.Impuestos:N2}\nCanal: {reservation.Canal} · Paquete: {reservation.Paquete}" + (summary.PreciosPendientes ? "\nHay condiciones históricas de precio pendientes de revisión." : "");
        TotalText.Text = $"Bs {summary.Total:N2}";
        ResourcePreparationText.Text = string.Join("\n", new ReservaService().ObtenerRecursosDePreparacion(reservation.Id).DefaultIfEmpty("No hay recursos pendientes de preparación."));
        var acquired = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservation.Id).Select(item => $"{SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Nombre ?? "Servicio"} × {item.Cantidad} · {item.Estado} · Bs {item.PrecioUnitarioAcordado.GetValueOrDefault() * item.Cantidad:N2}").ToList();
        AcquiredServicesText.Text = string.Join("\n", acquired.DefaultIfEmpty("No hay servicios adquiridos previamente."));
        var request = SampleData.SolicitudesEspeciales.FirstOrDefault(item => item.ReservaId == reservation.Id);
        SpecialRequestText.Text = string.IsNullOrWhiteSpace(request?.Descripcion) ? "Sin solicitudes especiales registradas." : $"Solicitud especial: {request.Descripcion}";
    }
}
