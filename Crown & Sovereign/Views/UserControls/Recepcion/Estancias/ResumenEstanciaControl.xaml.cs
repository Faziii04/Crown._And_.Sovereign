using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ResumenEstanciaControl : UserControl
{
    public ResumenEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId)
    {
        var stayModel = SampleData.Estancias.First(item => item.Id == stayId);
        var guest = SampleData.Huespedes.First(item => item.Id == stayModel.HuespedId);
        var assignedRoomId = SampleData.ReservaHabitaciones.FirstOrDefault(link => link.ReservaId == stayModel.ReservaId)?.HabitacionId ?? guest.Habitacion;
        var room = SampleData.Habitaciones.First(item => item.Id == assignedRoomId);
        var balance = new FolioService().CalcularSaldoPorEstancia(stayId);
        var folioId = stayModel.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == stayId).Id;
        var charges = SampleData.Cargos.Where(item => item.FolioId == folioId && !item.Anulado).Sum(item => item.Monto);
        var paid = SampleData.Pagos.Where(item => item.FolioId == folioId && item.Estado != "Anulado").Sum(item => item.Monto);
        DataContext = new { Metricas = new[] { new { Titulo = "Noches alojadas", Valor = Math.Max(1, (DateTime.Today - stayModel.CheckIn.Date).Days).ToString(), Contexto = $"Salida {stayModel.CheckOut:dd/MM}" }, new { Titulo = "Cargos", Valor = $"Bs {charges:N2}", Contexto = "Folio actual" }, new { Titulo = "Pagos", Valor = $"Bs {paid:N2}", Contexto = "Recibidos" }, new { Titulo = "Saldo", Valor = $"Bs {balance:N2}", Contexto = balance > 0 ? "Pendiente" : "Al día" } } };
        GuestInfo.Text = $"{guest.Nombre}\nDocumento {guest.Documento}\n{guest.Email} · {guest.Telefono}\n{guest.RolEnReserva} · {guest.Paquete}";
        RoomInfo.Text = $"Habitación {room.Numero} · Piso {room.Piso}\n{room.Tipo} · capacidad {room.Capacidad}\nLimpieza: {room.Limpieza}\n{room.Comodidades}";
    }
}
