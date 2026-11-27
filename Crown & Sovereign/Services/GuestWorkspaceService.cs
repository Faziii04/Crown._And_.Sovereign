using System.Globalization;
using System.Text;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed partial class HuespedService
{
    public ResumenHuespedesModel ObtenerIndicadores()
    {
        var guests = SampleData.Huespedes;
        var active = guests.Count(guest => ObtenerEstanciaActual(guest.Id) is not null);
        var periodEnd = DateTime.Today.AddDays(7);
        var upcoming = guests.Count(guest => ReservasDelHuesped(guest.Id).Any(reservation => reservation.Estado is "Confirmada" or "Pendiente" && reservation.Llegada.Date >= DateTime.Today && reservation.Llegada.Date <= periodEnd));
        var repeat = guests.Count(guest => EstanciasCompletadas(guest.Id).Count >= 2);
        return new(guests.Select(item => item.Id).Distinct().Count(), active, upcoming, repeat);
    }

    public IReadOnlyList<HuespedDirectorioFila> BuscarDirectorio(FiltroDirectorioHuespedes filtro)
    {
        IEnumerable<HuespedDirectorioFila> rows = SampleData.Huespedes.Select(ConstruirFilaDirectorio);
        if (filtro.Estado == "En hotel" || filtro.Estancias == "Con estancia") rows = rows.Where(item => item.TieneEstancia);
        if (filtro.Estancias == "Sin estancia") rows = rows.Where(item => !item.TieneEstancia);
        if (filtro.Proximas == true || filtro.Estado == "Próximos") rows = rows.Where(item => item.TieneProximaReserva);
        if (filtro.Recurrentes == true || filtro.Estado == "Recurrentes") rows = rows.Where(item => item.EsRecurrente);
        if (filtro.Estancias == "Sin historial") rows = rows.Where(item => item.EstanciasCompletadas == 0 && !TieneReservas(item.Id));
        if (filtro.Estancias == "Con estancias completadas") rows = rows.Where(item => item.EstanciasCompletadas > 0);
        if (filtro.SolicitudesPendientes == true) rows = rows.Where(item => ObtenerSolicitudesEspeciales(item.Id).Any(request => request.Solicitud.Estado is "Pendiente" or "Solicitada") || ObtenerSolicitudesServicio(item.Id).Any(request => request.Solicitud.Estado is "Pendiente" or "Solicitada"));
        if (filtro.ContactoIncompleto) rows = rows.Where(item => !ObtenerCompletitudContacto(item.Id).Completo);
        if (filtro.ConsentimientoRevision) rows = rows.Where(item => !Obtener(item.Id)!.ConsentimientoDatos);
        if (filtro.Piso is int floor) rows = rows.Where(item => HabitacionesActuales(item.Id).Any(room => room.Piso == floor));
        if (!string.IsNullOrWhiteSpace(filtro.Habitacion) && filtro.Habitacion != "Todas las habitaciones") rows = rows.Where(item => HabitacionesActuales(item.Id).Any(room => room.Numero.Equals(filtro.Habitacion, StringComparison.OrdinalIgnoreCase)));

        var query = Normalizar(filtro.Busqueda.Trim());
        if (query.Length > 0)
        {
            rows = rows.Where(row =>
            {
                var guest = Obtener(row.Id)!;
                var rooms = HabitacionesActuales(row.Id);
                var reservations = ReservasDelHuesped(row.Id);
                var source = Normalizar($"{guest.Nombre} {guest.Documento} {guest.Email} {guest.Telefono} {string.Join(' ', rooms.Select(room => room.Numero))} {string.Join(' ', reservations.Select(reservation => reservation.Id))}");
                return source.Contains(query, StringComparison.Ordinal);
            });
        }

        rows = filtro.Orden switch
        {
            "Nombre Z–A" => rows.OrderByDescending(item => item.Nombre),
            "Próxima llegada" => rows.OrderBy(item => item.ProximaLlegada ?? DateTime.MaxValue).ThenBy(item => item.Nombre),
            "Última estancia" => rows.OrderByDescending(item => item.UltimaEstancia ?? DateTime.MinValue).ThenBy(item => item.Nombre),
            "Estancias completadas" => rows.OrderByDescending(item => item.EstanciasCompletadas).ThenBy(item => item.Nombre),
            _ => rows.OrderBy(item => Normalizar(item.Nombre), StringComparer.Ordinal)
        };
        return rows.ToList();
    }

    public HuespedDirectorioFila ConstruirFilaDirectorio(int guestId) => ConstruirFilaDirectorio(Obtener(guestId) ?? throw new InvalidOperationException("No se encontró el huésped."));

    public ResumenPerfilHuespedModel ObtenerResumenPerfil(int guestId)
    {
        var completed = EstanciasCompletadas(guestId);
        var last = completed.Select(stay => (DateTime?)stay.CheckOut).Max();
        var attention = new List<string>();
        var pendingSpecial = ObtenerSolicitudesEspeciales(guestId).Count(item => item.Solicitud.Estado is "Pendiente" or "Solicitada");
        var pendingServices = ObtenerSolicitudesServicio(guestId).Count(item => item.Solicitud.Estado is "Pendiente" or "Solicitada");
        var upcomingAppointments = SampleData.Citas.Count(item => item.HuespedId == guestId && item.Inicio >= DateTime.Now && item.Estado is "Solicitada" or "Confirmada");
        if (pendingSpecial + pendingServices > 0) attention.Add($"{pendingSpecial + pendingServices} solicitud(es) pendiente(s)");
        if (upcomingAppointments > 0) attention.Add($"{upcomingAppointments} cita(s) próxima(s)");
        if (!ObtenerCompletitudContacto(guestId).Completo) attention.Add("Información de contacto incompleta");
        var finance = ObtenerResumenFinanciero(guestId);
        if (finance.Saldo > 0) attention.Add($"Saldo individual registrado: Bs {finance.Saldo:N2}");
        var reservations = ReservasDelHuesped(guestId);
        return new(completed.Count, completed.Sum(stay => Math.Max(0, (stay.CheckOut.Date - stay.CheckIn.Date).Days)), last,
            ObtenerEstanciaActual(guestId), ObtenerProximaReserva(guestId), attention, ObtenerActividadReciente(guestId).Take(8).ToList(), reservations.Count);
    }

    public EstanciaActualHuespedModel? ObtenerEstanciaActual(int guestId)
    {
        var reservation = SampleData.ReservaHuespedes.Where(link => link.HuespedId == guestId)
            .Select(link => SampleData.Reservas.FirstOrDefault(item => item.Id == link.ReservaId))
            .Where(item => item is { Estado: "En estancia" }).OrderByDescending(item => item!.Llegada).FirstOrDefault();
        var stay = reservation is null ? null : SampleData.Estancias.FirstOrDefault(item => item.ReservaId == reservation.Id && item.Estado == "Activa");
        if (reservation is null || stay is null) return null;
        var selectedLink = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HuespedId == guestId);
        var rooms = HabitacionesAsignadas(guestId, reservation.Id);
        var guestIds = SampleData.ReservaHuespedes.Where(item => item.ReservaId == reservation.Id).Select(item => item.HuespedId).Distinct().ToList();
        var guests = guestIds.Select(id => Obtener(id)).Where(item => item is not null).Cast<HuespedModel>().ToList();
        var purchasedPackage = SampleData.ReservaPaquetes.FirstOrDefault(item => item.ReservaId == reservation.Id);
        var packageName = purchasedPackage is null ? reservation.Paquete : SampleData.Paquetes.FirstOrDefault(item => item.Id == purchasedPackage.PaqueteId)?.Nombre ?? reservation.Paquete;
        return new(guestId, stay.Id, reservation.Id, stay.CheckIn, stay.CheckOut, selectedLink?.Principal == true ? "Titular" : "Acompañante", rooms, guests, packageName, selectedLink?.Principal != true);
    }

    public ReservaModel? ObtenerProximaReserva(int guestId) => ReservasDelHuesped(guestId)
        .Where(item => item.Estado is "Confirmada" or "Pendiente" && item.Llegada.Date >= DateTime.Today)
        .OrderBy(item => item.Llegada).FirstOrDefault();

    public IReadOnlyList<ReservaHistorialHuespedFila> ObtenerHistorialReservas(int guestId, string filtro = "Todas")
    {
        var completedReservationIds = SampleData.Estancias.Where(item => item.Estado == "Finalizada").Select(item => item.ReservaId).ToHashSet();
        return ReservasDelHuesped(guestId).Where(reservation => filtro switch
        {
            "Completadas" => reservation.Estado == "Finalizada" || completedReservationIds.Contains(reservation.Id),
            "Próximas" => reservation.Estado is "Confirmada" or "Pendiente" && reservation.Llegada.Date >= DateTime.Today,
            "Canceladas" => reservation.Estado == "Cancelada",
            _ => true
        }).OrderByDescending(item => item.Llegada).Select(reservation =>
        {
            var guestLink = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HuespedId == guestId);
            var rooms = HabitacionesAsignadas(guestId, reservation.Id);
            var roomText = rooms.Count > 0 ? string.Join(", ", rooms.Select(item => item.Numero)) : "Asignación no identificada";
            return new ReservaHistorialHuespedFila(reservation.Id, reservation.Llegada, reservation.Salida, Math.Max(0, (reservation.Salida.Date - reservation.Llegada.Date).Days), roomText, guestLink?.Principal == true ? "Titular" : "Acompañante", reservation.Estado, reservation.Paquete, reservation.GrupoId.HasValue);
        }).ToList();
    }

    public IReadOnlyList<ServicioHistorialHuespedFila> ObtenerHistorialServicios(int guestId)
        => SampleData.UsosServicio.Where(item => item.HuespedId == guestId && item.Estado is not ("Cancelado" or "No presentado"))
            .OrderByDescending(item => item.Fecha).Select(item =>
            {
                var service = SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId);
                return new ServicioHistorialHuespedFila(item.Fecha, service?.Nombre ?? "Servicio no disponible", service?.Categoria ?? "—", item.Incluido ? "Incluido" : "Adicional", item.Cargo, item.Estado, "Uso asociado al huésped");
            }).ToList();

    public IReadOnlyList<SolicitudEspecialHuespedFila> ObtenerSolicitudesEspeciales(int guestId)
    {
        var reservationIds = ReservasDelHuesped(guestId).Select(item => item.Id).ToHashSet();
        return SampleData.SolicitudesEspeciales.Where(item => reservationIds.Contains(item.ReservaId)).OrderByDescending(item => item.Id)
            .Select(item => new SolicitudEspecialHuespedFila(item, $"Reserva #{item.ReservaId}", "Solicitud asociada a la reserva; huésped solicitante no identificado", item.Estado)).ToList();
    }

    public IReadOnlyList<SolicitudServicioHuespedFila> ObtenerSolicitudesServicio(int guestId)
        => SampleData.SolicitudesServicio.Where(item => item.HuespedId == guestId).OrderByDescending(item => item.Solicitada).Select(item =>
        {
            var service = SampleData.Servicios.FirstOrDefault(entry => entry.Id == item.ServicioId);
            var appointment = SampleData.Citas.FirstOrDefault(entry => entry.HuespedId == guestId && entry.ServicioId == item.ServicioId && entry.Inicio.Date == item.FechaDeseada?.Date);
            var room = Obtener(guestId)?.Habitacion is int roomId && roomId > 0 ? SampleData.Habitaciones.FirstOrDefault(entry => entry.Id == roomId) : null;
            var coverage = Obtener(guestId)?.Paquete == "Sin paquete" ? "No incluido según el perfil" : service?.Cobertura ?? "No disponible";
            return new SolicitudServicioHuespedFila(item, service?.Nombre ?? "Servicio no disponible", service?.Categoria ?? "—", room?.Numero ?? "—", appointment is null ? "Sin cita asignada" : $"{appointment.Inicio:dd/MM HH:mm} · {appointment.Estado}", coverage);
        }).ToList();

    public ResumenFinancieroHuespedModel ObtenerResumenFinanciero(int guestId)
    {
        var charges = SampleData.Cargos.Where(item => item.HuespedId == guestId).Sum(item => item.Monto);
        var payments = SampleData.Movimientos.Where(item => item.HuespedId == guestId && item.Tipo == "Pago").Sum(item => item.Monto);
        return new(charges, payments, Math.Max(0, charges - payments), "Movimientos asociados al huésped. El modelo actual no vincula cargos y pagos a una estancia o folio específico.");
    }

    public IReadOnlyList<BeneficioHuespedFila> ObtenerBeneficios(int guestId)
    {
        var guest = Obtener(guestId);
        if (guest is null) return [];
        var packageName = ObtenerEstanciaActual(guestId)?.Paquete ?? guest.Paquete;
        return new ServicioService().ConsultarBeneficios(guestId, packageName).Select(item => new BeneficioHuespedFila(item.Service, item.Category, item.Coverage, "No hay saldo vinculado al huésped", item.Price, item.RequiresAppointment)).ToList();
    }

    public ContactoHuespedModel ObtenerCompletitudContacto(int guestId)
    {
        var guest = Obtener(guestId);
        if (guest is null) return new(false, "No se encontró el huésped.");
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(guest.Telefono)) missing.Add("teléfono");
        if (string.IsNullOrWhiteSpace(guest.Email)) missing.Add("correo electrónico");
        return new(missing.Count == 0, missing.Count == 0 ? "Información de contacto disponible." : $"Falta registrar: {string.Join(" y ", missing)}.");
    }

    public IReadOnlyList<ActividadHuespedFila> ObtenerActividadReciente(int guestId)
    {
        var events = new List<ActividadHuespedFila>();
        foreach (var reservation in ReservasDelHuesped(guestId))
        {
            var stay = SampleData.Estancias.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HuespedId == guestId);
            if (stay is not null) events.Add(new(stay.CheckIn, "Check-in", $"Reserva #{reservation.Id} · {reservation.Estado}", "Estancia"));
            else events.Add(new(reservation.Llegada, "Reserva", $"Reserva #{reservation.Id} · {reservation.Estado}", "Reservas"));
        }
        foreach (var usage in SampleData.UsosServicio.Where(item => item.HuespedId == guestId))
        {
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == usage.ServicioId)?.Nombre ?? "Servicio";
            events.Add(new(usage.Fecha, usage.Estado == "Completado" ? "Servicio completado" : "Servicio", service, "Servicios"));
        }
        foreach (var request in SampleData.SolicitudesServicio.Where(item => item.HuespedId == guestId))
        {
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == request.ServicioId)?.Nombre ?? "Servicio";
            events.Add(new(request.Solicitada, "Solicitud de servicio", $"{service} · {request.Estado}", "Servicios"));
        }
        foreach (var payment in SampleData.Movimientos.Where(item => item.HuespedId == guestId && item.Tipo == "Pago")) events.Add(new(payment.Fecha, "Pago", $"Bs {payment.Monto:N2} · {payment.Concepto}", "Caja"));
        foreach (var charge in SampleData.Cargos.Where(item => item.HuespedId == guestId)) events.Add(new(charge.Fecha, "Cargo", $"Bs {charge.Monto:N2} · {charge.Concepto}", "Folio"));
        return events.OrderByDescending(item => item.Fecha).ToList();
    }

    public IReadOnlyList<ConsentimientoHistorialModel> ObtenerHistorialConsentimiento(int guestId) => SampleData.ConsentimientosHistorial.Where(item => item.HuespedId == guestId).OrderByDescending(item => item.Fecha).ToList();

    public void ActualizarConsentimiento(int guestId, bool nuevoEstado, string observaciones)
    {
        if (!NavigationService.Puede("Huéspedes")) throw new UnauthorizedAccessException("Tu rol no puede actualizar el consentimiento del huésped.");
        var guest = Obtener(guestId) ?? throw new InvalidOperationException("No se encontró el huésped.");
        if (guest.ConsentimientoDatos == nuevoEstado) return;
        var anterior = guest.ConsentimientoDatos;
        guest.ConsentimientoDatos = nuevoEstado;
        var id = HuespedRepositorio.AnotarConsentimiento(guestId, observaciones.Trim())
            ?? (SampleData.ConsentimientosHistorial.Count == 0 ? 1 : SampleData.ConsentimientosHistorial.Max(item => item.Id) + 1);
        SampleData.ConsentimientosHistorial.Insert(0, new(id, guestId, anterior, nuevoEstado, DateTime.Now, SesionActual.Usuario?.Nombre ?? "Recepción", observaciones.Trim()));
    }

    public SolicitudEspecialModel RegistrarSolicitudEspecial(int guestId, int reservaId, string descripcion)
    {
        if (!NavigationService.Puede("Huéspedes")) throw new UnauthorizedAccessException("Tu rol no puede registrar solicitudes especiales.");
        if (string.IsNullOrWhiteSpace(descripcion)) throw new InvalidOperationException("Describe la solicitud.");
        if (!SampleData.ReservaHuespedes.Any(item => item.ReservaId == reservaId && item.HuespedId == guestId)) throw new InvalidOperationException("La reserva seleccionada no está asociada a este huésped.");
        var item = new SolicitudEspecialModel(Persistencia.SiguienteId("solicitud_especial", "solicitud_id", () => SampleData.SolicitudesEspeciales.Count == 0 ? 1 : SampleData.SolicitudesEspeciales.Max(row => row.Id) + 1), reservaId, descripcion.Trim(), "Pendiente");
        HuespedRepositorio.InsertarSolicitud(item);
        SampleData.SolicitudesEspeciales.Insert(0, item);
        return item;
    }

    public IReadOnlyList<HuespedModel> BuscarPorDocumento(string documento)
        => SampleData.Huespedes.Where(item => item.Documento.Equals(documento.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    private HuespedDirectorioFila ConstruirFilaDirectorio(HuespedModel guest)
    {
        var stay = ObtenerEstanciaActual(guest.Id);
        var next = ObtenerProximaReserva(guest.Id);
        var completed = EstanciasCompletadas(guest.Id);
        var room = stay?.Habitaciones.FirstOrDefault() ?? (guest.Habitacion > 0 ? SampleData.Habitaciones.FirstOrDefault(item => item.Id == guest.Habitacion) : null);
        var last = completed.Select(item => (DateTime?)item.CheckOut).Max();
        var status = stay is not null ? $"En hotel · Hab. {room?.Numero ?? "—"}" : next is not null ? $"Próxima llegada: {next.Llegada:dd MMM}" : last is DateTime date ? $"Última estancia: {date:dd MMM}" : "Sin estancias registradas";
        var color = stay is not null ? "#39816A" : next is not null ? "#507BA5" : "#64748B";
        return new(guest.Id, string.Concat(guest.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => char.ToUpperInvariant(part[0]))), guest.Nombre, guest.Documento, status, stay is not null ? "En hotel" : next is not null ? "Próxima reserva" : completed.Count > 0 ? "Historial" : "Sin historial", color, next?.Llegada, last, completed.Count, stay is not null, next is not null, completed.Count >= 2);
    }

    private IReadOnlyList<ReservaModel> ReservasDelHuesped(int guestId)
        => SampleData.ReservaHuespedes.Where(item => item.HuespedId == guestId).Select(item => SampleData.Reservas.FirstOrDefault(reservation => reservation.Id == item.ReservaId)).Where(item => item is not null).Cast<ReservaModel>().DistinctBy(item => item.Id).ToList();

    private List<EstanciaModel> EstanciasCompletadas(int guestId)
    {
        var reservationIds = ReservasDelHuesped(guestId).Select(item => item.Id).ToHashSet();
        return SampleData.Estancias.Where(item => item.Estado == "Finalizada" && reservationIds.Contains(item.ReservaId)).GroupBy(item => item.ReservaId).Select(group => group.First()).ToList();
    }

    private IReadOnlyList<HabitacionModel> HabitacionesAsignadas(int guestId, int reservationId)
    {
        var assignmentIds = SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservationId && item.HuespedId == guestId).Select(item => item.ReservaHabitacionId).ToHashSet();
        return SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservationId && assignmentIds.Contains(item.ReservaHabitacionId)).Select(item => SampleData.Habitaciones.FirstOrDefault(room => room.Id == item.HabitacionId)).Where(item => item is not null).Cast<HabitacionModel>().DistinctBy(item => item.Id).ToList();
    }

    private IReadOnlyList<HabitacionModel> HabitacionesActuales(int guestId)
    {
        var stay = ObtenerEstanciaActual(guestId);
        if (stay is not null) return stay.Habitaciones;
        var guest = Obtener(guestId);
        return guest?.Habitacion > 0 && SampleData.Habitaciones.FirstOrDefault(room => room.Id == guest.Habitacion) is { } room ? [room] : [];
    }

    private bool TieneReservas(int guestId) => ReservasDelHuesped(guestId).Count > 0;

    private static string Normalizar(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(char.ToLowerInvariant(character));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
