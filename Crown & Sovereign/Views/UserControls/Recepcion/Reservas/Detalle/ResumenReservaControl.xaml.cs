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
        var total = rooms.Sum(room => new TarifaService().CalcularAlojamiento(room, reservation.Llegada, reservation.Salida));
        var packages = SampleData.ReservaPaquetes.Where(item => item.ReservaId == reservation.Id).Sum(item => item.PrecioCompra * item.Cantidad);
        var extras = SampleData.ServiciosReserva.Where(item => item.ReservaId == reservation.Id).Sum(item => SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Precio ?? 0m);
        PriceBreakdown.Text = $"Alojamiento ({rooms.Count} habitación(es)): Bs {total:N2}\nPaquetes y servicios registrados: Bs {packages + extras:N2}\nCanal: {reservation.Canal} · Paquete: {reservation.Paquete}";
        TotalText.Text = $"Bs {total + packages + extras:N2}";
        var request = SampleData.SolicitudesEspeciales.FirstOrDefault(item => item.ReservaId == reservation.Id);
        SpecialRequestText.Text = string.IsNullOrWhiteSpace(request?.Descripcion) ? "Sin solicitudes especiales registradas." : $"Solicitud especial: {request.Descripcion}";
    }
}
