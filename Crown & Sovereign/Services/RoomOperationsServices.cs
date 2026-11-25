using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed partial class HabitacionService
{
    public HabitacionOperacionModel ObtenerEstadoOperativo(int habitacionId)
        => Proyectar(SampleData.Habitaciones.FirstOrDefault(room => room.Id == habitacionId) ?? throw new InvalidOperationException("No se encontró la habitación."));

    public IReadOnlyList<HabitacionOperacionModel> ListarOperativas(FiltroHabitacionesOperativas filtro)
    {
        IEnumerable<HabitacionOperacionModel> rooms = SampleData.Habitaciones.Select(Proyectar);
        if (filtro.Piso.HasValue) rooms = rooms.Where(item => item.Habitacion.Piso == filtro.Piso.Value);
        if (filtro.Tipo is not ("Todos" or "Todos los tipos")) rooms = rooms.Where(item => item.Habitacion.Tipo == filtro.Tipo);
        if (filtro.Ocupacion is "Ocupada") rooms = rooms.Where(item => item.Ocupada);
        if (filtro.Ocupacion is "Desocupada") rooms = rooms.Where(item => !item.Ocupada);
        if (filtro.Limpieza is not ("Todos" or "Todos los estados")) rooms = rooms.Where(item => item.EstadoLimpieza == filtro.Limpieza);
        if (filtro.Mantenimiento == "Con incidencia") rooms = rooms.Where(item => item.Incidencias.Count > 0);
        if (filtro.Mantenimiento == "Sin incidencias") rooms = rooms.Where(item => item.Incidencias.Count == 0);
        if (filtro.Mantenimiento == "Bloqueada") rooms = rooms.Where(item => item.Bloqueada);
        if (filtro.Mantenimiento == "Operativa") rooms = rooms.Where(item => !item.Bloqueada);
        if (filtro.CapacidadMinima is int capacity) rooms = rooms.Where(item => item.Habitacion.Capacidad >= capacity);
        if (filtro.Comodidades.Count > 0) rooms = rooms.Where(item => filtro.Comodidades.All(amenity => item.Habitacion.Comodidades.Contains(amenity, StringComparison.OrdinalIgnoreCase)));
        if (filtro.Llegada == "Hoy") rooms = rooms.Where(item => item.LlegadaHoy);
        else if (filtro.Llegada == "Con reserva futura") rooms = rooms.Where(item => item.ProximaReserva is not null);
        else if (filtro.Llegada == "En riesgo") rooms = rooms.Where(item => item.LlegadaHoy && item.Clasificacion != "Lista para ingreso");
        if (filtro.Salida == "Hoy") rooms = rooms.Where(item => item.SalidaHoy);
        else if (filtro.Salida == "Con estancia activa") rooms = rooms.Where(item => item.EstanciaActual is not null);

        rooms = filtro.FiltroRapido switch
        {
            "Listas para ingreso" => rooms.Where(item => item.Clasificacion == "Lista para ingreso"),
            "Ocupadas" => rooms.Where(item => item.Clasificacion == "Ocupada"),
            "Por preparar" => rooms.Where(item => item.Clasificacion == "Por preparar" && !item.Ocupada),
            "Bloqueadas" => rooms.Where(item => item.Bloqueada),
            "Llegadas hoy" => rooms.Where(item => item.LlegadaHoy),
            "Salidas hoy" => rooms.Where(item => item.SalidaHoy),
            "Por limpiar" => rooms.Where(item => item.EstadoLimpieza is "Pendiente de limpieza" or "Sucia"),
            "Con incidencias" => rooms.Where(item => item.Incidencias.Count > 0),
            "Pendientes de inspección" => rooms.Where(item => item.EstadoLimpieza is "Pendiente de inspección" or "Inspección fallida"),
            "Llegadas en riesgo" => rooms.Where(item => item.LlegadaHoy && (item.Clasificacion != "Lista para ingreso" || item.RequiereRevisionOperativa)),
            "Salidas pendientes" => rooms.Where(item => item.SalidaHoy),
            _ => rooms
        };

        var query = filtro.Busqueda.Trim();
        if (!string.IsNullOrWhiteSpace(query))
            rooms = rooms.Where(item => $"{item.Habitacion.Numero} {item.Habitacion.Tipo} {item.Habitacion.Comodidades} {string.Join(" ", item.Huespedes.Select(guest => guest.Nombre))} {item.ReservaActual?.Id} {item.ProximaReserva?.Id} {item.ReservaActual?.Huesped} {item.ProximaReserva?.Huesped}".Contains(query, StringComparison.OrdinalIgnoreCase));

        rooms = filtro.Orden switch
        {
            "Próxima llegada" => rooms.OrderBy(item => item.ProximaReserva?.Llegada ?? DateTime.MaxValue),
            "Próxima salida" => rooms.OrderBy(item => item.ReservaActual?.Salida ?? DateTime.MaxValue),
            "Tipo de habitación" => rooms.OrderBy(item => item.Habitacion.Tipo).ThenBy(item => item.Habitacion.Numero),
            "Prioridad operativa" => rooms.OrderBy(item => Prioridad(item)).ThenBy(item => item.Habitacion.Numero),
            _ => rooms.OrderBy(item => item.Habitacion.Piso).ThenBy(item => item.Habitacion.Numero)
        };
        return rooms.ToList();
    }

    public ResumenOperativoHabitaciones ObtenerResumenOperativo()
    {
        var rooms = SampleData.Habitaciones.Select(Proyectar).ToList();
        return new(rooms.Count(item => item.Clasificacion == "Lista para ingreso"), rooms.Count(item => item.Clasificacion == "Ocupada"),
            rooms.Count(item => item.Clasificacion == "Por preparar"), rooms.Count(item => item.Clasificacion == "Bloqueada"),
            rooms.Count(item => item.LlegadaHoy), rooms.Count(item => item.SalidaHoy), rooms.Count(item => item.Bloqueada && item.Ocupada || item.RequiereRevisionOperativa || item.LlegadaHoy && item.Clasificacion != "Lista para ingreso"),
            rooms.Count(item => item.Bloqueada && item.Ocupada), rooms.Count(item => item.EstadoLimpieza is "Pendiente de inspección" or "Inspección fallida"));
    }

    public IReadOnlyList<ResumenPisoHabitaciones> ObtenerResumenPisos()
        => SampleData.Habitaciones.Select(Proyectar).GroupBy(item => item.Habitacion.Piso).OrderBy(group => group.Key)
            .Select(group => new ResumenPisoHabitaciones(group.Key, group.Count(), group.Count(item => item.Clasificacion == "Lista para ingreso"), group.Count(item => item.Clasificacion == "Ocupada"), group.Count(item => item.Clasificacion == "Por preparar"), group.Count(item => item.Clasificacion == "Bloqueada"))).ToList();

    private static HabitacionOperacionModel Proyectar(HabitacionModel room)
    {
        var incidents = SampleData.Incidencias.Where(item => item.Habitacion == room.Id && item.Estado != "Resuelta").OrderByDescending(item => item.Prioridad == "Urgente").ThenByDescending(item => item.FechaReporte).ToList();
        var blocked = room.Bloqueada || incidents.Any(item => item.BloqueaHabitacion);
        var activeLink = SampleData.ReservaHabitaciones.Where(link => link.HabitacionId == room.Id)
            .Select(link => (Link: link, Reservation: SampleData.Reservas.FirstOrDefault(reservation => reservation.Id == link.ReservaId)))
            .Where(item => item.Reservation is { Estado: "En estancia" }).OrderBy(item => item.Reservation!.Salida).FirstOrDefault();
        var activeStay = activeLink.Reservation is null ? null : SampleData.Estancias.FirstOrDefault(stay => stay.ReservaId == activeLink.Reservation.Id && stay.Estado == "Activa");
        var currentGuests = activeLink.Reservation is null ? [] : SampleData.ReservaHabitacionHuespedes
            .Where(link => link.ReservaId == activeLink.Reservation.Id && link.ReservaHabitacionId == activeLink.Link.ReservaHabitacionId)
            .Select(link => SampleData.Huespedes.FirstOrDefault(guest => guest.Id == link.HuespedId)).Where(guest => guest is not null).Cast<HuespedModel>().ToList();
        if (currentGuests.Count == 0 && activeLink.Reservation is not null)
        {
            var main = SampleData.ReservaHuespedes.FirstOrDefault(link => link.ReservaId == activeLink.Reservation!.Id && link.Principal)?.HuespedId;
            if (main is int id && SampleData.Huespedes.FirstOrDefault(guest => guest.Id == id) is { } guest) currentGuests.Add(guest);
        }
        if (currentGuests.Count == 0 && room.Estado == "Ocupada" && !string.IsNullOrWhiteSpace(room.Huesped))
            currentGuests.AddRange(SampleData.Huespedes.Where(guest => guest.Habitacion == room.Id));

        var next = SampleData.ReservaHabitaciones.Where(link => link.HabitacionId == room.Id)
            .Select(link => SampleData.Reservas.FirstOrDefault(reservation => reservation.Id == link.ReservaId))
            .Where(reservation => reservation is not null && reservation.Estado is "Confirmada" or "Pendiente" && reservation.Llegada.Date >= DateTime.Today)
            .Cast<ReservaModel>().OrderBy(reservation => reservation.Llegada).FirstOrDefault();
        var task = SampleData.Tareas.Where(item => item.Habitacion == room.Id && item.Estado != "Inspeccionada").OrderByDescending(item => item.Hora).FirstOrDefault();
        var inspection = task is null ? null : SampleData.Inspecciones.Where(item => item.TareaId == task.Id).OrderByDescending(item => item.Fecha).FirstOrDefault();
        var cleaning = room.Limpieza;
        if (task?.Estado == "En progreso") cleaning = "En limpieza";
        else if (task?.Estado == "Completada" && inspection?.Aprobada != true) cleaning = "Pendiente de inspección";
        else if (inspection is { Aprobada: false }) cleaning = "Inspección fallida";
        else if (task?.Estado == "Inspeccionada") cleaning = "Limpia";
        else if (string.IsNullOrWhiteSpace(cleaning)) cleaning = "Sin verificar";
        var occupied = activeLink.Reservation is not null || room.Estado == "Ocupada";
        var precaution = incidents.Any(item => item.RequiereRevisionOperativa);
        var maintenance = blocked ? "Bloqueada" : incidents.Count > 0 ? "Incidencia abierta · no bloqueante" : "Operativa";
        var classification = blocked ? "Bloqueada" : occupied ? "Ocupada" : cleaning == "Limpia" && !precaution ? "Lista para ingreso" : "Por preparar";
        return new HabitacionOperacionModel { Habitacion = room, Clasificacion = classification, Ocupada = occupied, Bloqueada = blocked,
            RequiereRevisionOperativa = precaution, EstadoLimpieza = cleaning, EstadoMantenimiento = maintenance, Huespedes = currentGuests,
            ReservaActual = activeLink.Reservation, EstanciaActual = activeStay, ProximaReserva = next, TareaActual = task, UltimaInspeccion = inspection, Incidencias = incidents };
    }
    private static int Prioridad(HabitacionOperacionModel item)
        => item.Bloqueada && item.Ocupada ? 0 : item.LlegadaHoy && item.Clasificacion != "Lista para ingreso" ? 1 : item.RequiereRevisionOperativa ? 2 : item.Clasificacion == "Por preparar" ? 3 : 4;
}

public sealed class NotificacionService
{
    public void Crear(string mensaje, string destino)
    {
        SampleData.Notificaciones.Insert(0, new(SampleData.Notificaciones.Count == 0 ? 1 : SampleData.Notificaciones.Max(item => item.Id) + 1, mensaje, destino, DateTime.Now, false));
    }
}

public sealed partial class EstanciaService
{
    public IReadOnlyList<HabitacionOperacionModel> HabitacionesCompatiblesParaCambio(int reservaId, int habitacionOrigenId)
    {
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId) ?? throw new InvalidOperationException("No se encontró la reserva de la estancia.");
        var source = SampleData.Habitaciones.FirstOrDefault(item => item.Id == habitacionOrigenId) ?? throw new InvalidOperationException("No se encontró la habitación actual.");
        var assignment = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservaId && item.HabitacionId == habitacionOrigenId);
        var guestIds = assignment is null ? [] : SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservaId && item.ReservaHabitacionId == assignment.ReservaHabitacionId).Select(item => item.HuespedId).Distinct().ToList();
        if (guestIds.Count == 0) guestIds = SampleData.Huespedes.Where(item => item.Habitacion == habitacionOrigenId).Select(item => item.Id).ToList();
        var capacity = Math.Max(1, guestIds.Count);
        var availability = new HabitacionDisponibilidadService();
        return new HabitacionService().ListarOperativas(new FiltroHabitacionesOperativas())
            .Where(item => item.Habitacion.Id != source.Id && item.Habitacion.Capacidad >= capacity && !item.Ocupada && item.EstadoLimpieza == "Limpia" && !item.Bloqueada && !item.RequiereRevisionOperativa)
            .Where(item => !SampleData.ReservaHabitaciones.Any(link => link.ReservaId == reservaId && link.HabitacionId == item.Habitacion.Id))
            .Where(item => availability.EsDisponible(item.Habitacion.Id, DateTime.Today, reservation.Salida.Date, reservaId))
            .OrderBy(item => Math.Abs(item.Habitacion.Piso - source.Piso)).ThenBy(item => item.Habitacion.Tipo == source.Tipo ? 0 : 1).ThenBy(item => item.Habitacion.Numero).ToList();
    }

    public void CambiarHabitacion(int reservaId, int habitacionOrigenId, int habitacionDestinoId, string motivo, string observaciones)
    {
        if (!NavigationService.Puede("Habitaciones")) throw new UnauthorizedAccessException("Tu rol no puede realizar cambios de habitación.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Selecciona el motivo del cambio.");
        var stay = SampleData.Estancias.FirstOrDefault(item => item.ReservaId == reservaId && item.Estado == "Activa") ?? throw new InvalidOperationException("No se encontró una estancia activa para la reserva.");
        var reservation = SampleData.Reservas.First(item => item.Id == reservaId);
        var source = SampleData.Habitaciones.First(item => item.Id == habitacionOrigenId);
        var destination = SampleData.Habitaciones.First(item => item.Id == habitacionDestinoId);
        var sourceAssignment = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservaId && item.HabitacionId == habitacionOrigenId);
        if (sourceAssignment is null) throw new InvalidOperationException("La habitación actual no está asignada a esta reserva.");
        var guestIds = SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservaId && item.ReservaHabitacionId == sourceAssignment.ReservaHabitacionId).Select(item => item.HuespedId).Distinct().ToList();
        if (guestIds.Count == 0) guestIds = SampleData.Huespedes.Where(item => item.Habitacion == source.Id).Select(item => item.Id).ToList();
        if (guestIds.Count == 0) throw new InvalidOperationException("No se identificaron huéspedes asignados a la habitación actual.");
        if (destination.Capacidad < guestIds.Count) throw new InvalidOperationException("La capacidad de la habitación destino no es suficiente para los huéspedes afectados.");
        var destinationState = new HabitacionService().ObtenerEstadoOperativo(destination.Id);
        if (destinationState.Ocupada || destinationState.Bloqueada || destinationState.EstadoLimpieza != "Limpia" || destinationState.RequiereRevisionOperativa)
            throw new InvalidOperationException("La habitación destino debe estar desocupada, limpia, operativa y sin revisión pendiente.");
        if (SampleData.ReservaHabitaciones.Any(item => item.ReservaId == reservaId && item.HabitacionId == destination.Id)) throw new InvalidOperationException("La habitación destino ya pertenece a otra asignación de esta reserva.");
        if (!new HabitacionDisponibilidadService().EsDisponible(destination.Id, DateTime.Today, reservation.Salida.Date, reservaId)) throw new InvalidOperationException("La habitación destino tiene un conflicto para el resto de la estancia.");

        SampleData.ReservaHabitaciones.Remove(sourceAssignment);
        var newAssignmentId = SampleData.ReservaHabitaciones.Count == 0 ? 1 : SampleData.ReservaHabitaciones.Max(item => item.ReservaHabitacionId) + 1;
        SampleData.ReservaHabitaciones.Add(new(newAssignmentId, reservaId, destination.Id));
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item => item.ReservaId == reservaId && item.ReservaHabitacionId == sourceAssignment.ReservaHabitacionId);
        foreach (var guestId in guestIds) SampleData.ReservaHabitacionHuespedes.Add(new(newAssignmentId, reservaId, guestId));
        if (reservation.Habitacion == source.Id) reservation.Habitacion = destination.Id;
        source.Estado = "Disponible"; source.Huesped = null; source.Limpieza = "Pendiente de limpieza";
        destination.Estado = "Ocupada"; destination.Limpieza = "Limpia";
        var guests = guestIds.Select(id => SampleData.Huespedes.First(item => item.Id == id)).ToList();
        destination.Huesped = guests.FirstOrDefault()?.Nombre;
        foreach (var guest in guests) guest.Habitacion = destination.Id;
        foreach (var key in SampleData.Llaves.Where(item => item.EstanciaId == stay.Id && item.Estado == "Activa" && item.Codigo.Contains(source.Numero, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var index = SampleData.Llaves.FindIndex(item => item.Id == key.Id);
            SampleData.Llaves[index] = key with { Estado = "Revocada" };
        }
        foreach (var suffix in new[] { "A", "B" }) SampleData.Llaves.Add(new(SampleData.Llaves.Count == 0 ? 1 : SampleData.Llaves.Max(item => item.Id) + 1, stay.Id, $"CS-{destination.Numero}-{suffix}", "Activa"));
        SampleData.CambiosHabitacion.Add(new(SampleData.CambiosHabitacion.Count + 1, reservaId, stay.Id, source.Id, destination.Id, guestIds, DateTime.Now, motivo, observaciones.Trim()));
        SampleData.Auditoria.Insert(0, new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", "Cambio de habitación", source.Numero, $"{destination.Numero} · {motivo}"));
        new NotificacionService().Crear($"Huéspedes reubicados de {source.Numero} a {destination.Numero}.", "Recepción");
    }
}
