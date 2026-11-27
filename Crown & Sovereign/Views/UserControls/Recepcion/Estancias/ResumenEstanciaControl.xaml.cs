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
        var rooms = new EstanciaService().ObtenerHabitacionesEstancia(stayId);
        var reservation = SampleData.Reservas.First(item => item.Id == stayModel.ReservaId);
        var balance = new FolioService().CalcularSaldoPorEstancia(stayId);
        var folioId = stayModel.FolioId ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == stayId)?.Id;
        var charges = folioId is int id ? SampleData.Cargos.Where(item => item.FolioId == id && !item.Anulado).ToList() : [];
        var paid = folioId is int folioKey ? SampleData.Pagos.Where(item => item.FolioId == folioKey && item.Estado != "Anulado").Sum(item => item.Monto) : 0m;
        var elapsedEnd = stayModel.Estado == "Finalizada" ? stayModel.CheckOutReal ?? stayModel.CheckOut : DateTime.Now < stayModel.CheckOut ? DateTime.Now : stayModel.CheckOut;
        var elapsedNights = Math.Max(0, (elapsedEnd.Date - stayModel.CheckIn.Date).Days);
        var remainingDays = (stayModel.CheckOut.Date - DateTime.Today).Days;
        var remainingText = remainingDays < 0 ? $"Vencida {Math.Abs(remainingDays)} noche(s)" : $"{remainingDays} noche(s)";
        var upcoming = SampleData.Citas.Where(item => item.EstanciaId == stayId && item.Inicio >= DateTime.Now && item.Estado is "Solicitada" or "Confirmada" or "En curso").OrderBy(item => item.Inicio).FirstOrDefault();
        var nextText = upcoming is null ? "Sin cita próxima" : $"{SampleData.Servicios.FirstOrDefault(item => item.Id == upcoming.ServicioId)?.Nombre} · {upcoming.Inicio:dd/MM HH:mm}";
        DataContext = new { Metricas = new[]
        {
            new { Titulo = "Noches alojadas", Valor = elapsedNights.ToString(), Contexto = $"Ingreso real {stayModel.CheckIn:dd/MM/yyyy}" },
            new { Titulo = "Noches restantes", Valor = remainingText, Contexto = $"Salida prevista {stayModel.CheckOut:dd/MM/yyyy HH:mm}" },
            new { Titulo = "Saldo pendiente", Valor = $"Bs {balance:N2}", Contexto = balance > 0 ? "Folio actual" : "Al día" },
            new { Titulo = "Próxima actividad", Valor = nextText, Contexto = upcoming?.Estado ?? "Agenda de estancia" }
        } };
        var guests = SampleData.ReservaHuespedes.Where(item => item.ReservaId == stayModel.ReservaId).Select(item => (Link: item, Guest: SampleData.Huespedes.FirstOrDefault(person => person.Id == item.HuespedId))).Where(item => item.Guest is not null).ToList();
        GuestInfo.Text = string.Join(Environment.NewLine, guests.Select(item => $"{item.Guest!.Nombre} · {(item.Link.Principal ? "Titular" : "Acompañante")}")) + $"\n\n{guest.Documento} · {guest.Email} · {guest.Telefono}";
        var packageName = string.Join(", ", SampleData.ReservaPaquetes.Where(item => item.ReservaId == stayModel.ReservaId).Select(item => SampleData.Paquetes.FirstOrDefault(package => package.Id == item.PaqueteId)?.Nombre).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct());
        if (packageName.Length == 0) packageName = reservation.Paquete;
        RoomInfo.Text = string.Join(Environment.NewLine, rooms.Select(room => $"Habitación {room.Numero} · {room.Tipo} · Piso {room.Piso} · {room.Estado} · {room.Limpieza}")) + $"\n\nNoches reservadas: {(reservation.Salida.Date - reservation.Llegada.Date).Days}\nPaquete: {packageName}";
        var lodging = charges.Where(item => item.Concepto.StartsWith("Alojamiento", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Monto);
        var packageCharges = charges.Where(item => item.Concepto.StartsWith("Paquete", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Monto);
        var resourceCharges = charges.Where(item => item.Concepto.StartsWith("Recurso", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Monto);
        var serviceCharges = charges.Where(item => item.Concepto.StartsWith("Servicio", StringComparison.OrdinalIgnoreCase) || item.Concepto.StartsWith("Masaje", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Monto);
        FinanceInfo.Text = $"Alojamiento publicado: Bs {lodging:N2}\nPaquetes: Bs {packageCharges:N2}\nRecursos: Bs {resourceCharges:N2}\nServicios: Bs {serviceCharges:N2}\nTotal cargos: Bs {charges.Sum(item => item.Monto):N2}\nTotal pagado: Bs {paid:N2}\nSaldo pendiente: Bs {balance:N2}";
        var acquired = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == stayModel.ReservaId && item.Estado != "CANCELADO").Select(item =>
        {
            var used = SampleData.UsosServicio.Count(use => use.ServicioAdquiridoId == item.ServicioAdquiridoId && use.EstanciaId == stayId && use.Estado is "Completado" or "Confirmado");
            return $"{SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Nombre} · {Math.Max(0, item.Cantidad - used)} de {item.Cantidad} sesión(es) restantes";
        }).ToList();
        var resourceLines = SampleData.ReservaRecursos.Where(item => item.ReservaId == stayModel.ReservaId && item.Estado != "CANCELADO").Select(item => $"{SampleData.RecursosAdicionales.FirstOrDefault(resource => resource.RecursoId == item.RecursoId)?.Nombre} × {item.Cantidad} · {item.Estado}").ToList();
        ServicesResourcesInfo.Text = (acquired.Count > 0 ? string.Join(Environment.NewLine, acquired) : "Sin servicios adquiridos previamente.") + (resourceLines.Count > 0 ? $"\n{string.Join(Environment.NewLine, resourceLines)}" : "\nSin recursos adicionales solicitados.");
        var pendingRequests = SampleData.SolicitudesServicio.Count(item => item.EstanciaId == stayId && item.Estado is "Solicitada" or "Pendiente" or "Confirmada" or "En atención" or "En curso");
        var keys = SampleData.Llaves.Count(item => item.EstanciaId == stayId && item.Estado == "Activa");
        var pendingResources = SampleData.ReservaRecursos.Where(item => item.ReservaId == stayModel.ReservaId && item.Estado is "PENDIENTE" or "ASIGNADO" or "SIN_VERIFICAR").Sum(item => item.Cantidad);
        AlertsInfo.Text = string.Join(Environment.NewLine, new[]
        {
            upcoming is null ? null : $"Próxima cita: {nextText} · {upcoming.Estado}",
            pendingRequests == 0 ? null : $"{pendingRequests} solicitud(es) de servicio abiertas.",
            pendingResources == 0 ? null : $"{pendingResources} recurso(s) pendientes de preparación o entrega.",
            keys == 0 ? null : $"{keys} llave(s) aún activas.",
            stayModel.Estado == "Activa" && remainingDays < 0 ? $"Salida vencida hace {Math.Abs(remainingDays)} día(s)." : null
        }.Where(item => item is not null)!);
        if (string.IsNullOrWhiteSpace(AlertsInfo.Text)) AlertsInfo.Text = "Sin avisos operativos pendientes.";
    }
}
