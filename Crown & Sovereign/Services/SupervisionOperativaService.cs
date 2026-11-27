using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Read-only aggregation for the Gerente operational supervision screens.</summary>
public sealed class SupervisionOperativaService
{
    private readonly HabitacionService _habitaciones = new();

    public SupervisionSnapshot Obtener(string periodo = "Hoy", DateTime? desdePersonalizado = null, DateTime? hastaPersonalizado = null)
    {
        var hoy = DateTime.Today;
        var (desde, hasta) = ResolverPeriodo(periodo, desdePersonalizado, hastaPersonalizado, hoy);
        var roomStates = _habitaciones.ListarOperativas(new FiltroHabitacionesOperativas());
        var roomRows = roomStates.Select(RoomRow).OrderBy(item => item.Piso).ThenBy(item => item.Numero).ToList();
        var arrivals = BuildArrivals(hoy);
        var tasks = BuildTasks(desde, hasta);
        var inspections = BuildInspections(desde, hasta);
        var incidents = BuildIncidents(desde, hasta, hoy);
        var orders = BuildOrders(desde, hasta, hoy);
        var services = BuildServices(desde, hasta, hoy);
        var products = BuildProducts();
        var resources = BuildResources(hoy);
        var consumptions = BuildConsumptions(desde, hasta);
        var alerts = BuildAlerts(roomRows, arrivals, tasks, inspections, incidents, orders, services, products, resources, hoy);
        var ready = roomRows.Count(item => item.Estado == "Lista");
        var occupied = roomRows.Count(item => item.Estado == "Ocupada");
        var blocked = roomRows.Count(item => item.BloqueoActivo);
        var prep = roomRows.Count(item => item.Estado is "En preparación" or "Por verificar");
        var incomingAtRisk = arrivals.Where(item => item.Condicion != "Preparada").Select(item => item.ReservaId).Distinct().Count();
        var urgentIncidents = SampleData.Incidencias.Count(item => IsOpen(item.Estado) && IsPriority(item.Prioridad));
        var coordination = SampleData.SolicitudesServicio.Where(NeedsCoordination).Select(item => item.Id).Distinct().Count();
        var periodTasks = SampleData.Tareas.Where(item => item.Hora >= desde && item.Hora < hasta).ToList();
        var scheduled = periodTasks.Count;
        var completed = periodTasks.Count(item => item.Estado is "Completada" or "Inspeccionada");
        var inProgress = SampleData.Tareas.Count(item => item.Estado == "En progreso");
        var pending = SampleData.Tareas.Count(item => item.Estado == "Pendiente");
        var indicators = new[]
        {
            new SupervisionIndicador("Habitaciones listas", ready.ToString(), $"de {roomRows.Count} · estado actual", "Habitaciones"),
            new SupervisionIndicador("Ingresos en riesgo", incomingAtRisk.ToString(), "reservas distintas por revisar", "Habitaciones"),
            new SupervisionIndicador("Incidencias prioritarias", urgentIncidents.ToString(), "sin resolver · alta o urgente", "Mantenimiento"),
            new SupervisionIndicador("Servicios por coordinar", coordination.ToString(), "solicitudes abiertas", "Servicios")
        };
        var diagnostics = BuildDiagnostics(roomStates, tasks, incidents, orders, services, resources);
        return new(DateTime.Now, periodo, $"Eventos cerrados con fecha entre {desde:dd/MM/yyyy} y {hasta.AddDays(-1):dd/MM/yyyy}; pendientes abiertos, habitación y stock muestran el estado actual y permanecen visibles al consultar historia.",
            indicators, alerts.OrderBy(item => SeverityRank(item.Nivel)).ThenBy(item => item.Fecha ?? DateTime.MaxValue).ToList(),
            roomRows, arrivals, tasks, BuildWorkload(tasks), inspections, incidents, orders, services, products, resources, consumptions,
            diagnostics, roomRows.Count, ready, occupied, prep, blocked, scheduled, completed, inProgress, pending);
    }

    public IReadOnlyList<SupervisionHabitacionFila> ObtenerAlternativas(int habitacionOrigenId, int? reservaId)
    {
        var source = SampleData.Habitaciones.FirstOrDefault(item => item.Id == habitacionOrigenId);
        var reservation = reservaId is int id ? SampleData.Reservas.FirstOrDefault(item => item.Id == id) : null;
        if (source is null || reservation is null) return [];
        var assignment = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HabitacionId == habitacionOrigenId);
        var assignedRoomCount = SampleData.ReservaHabitaciones.Count(item => item.ReservaId == reservation.Id);
        var assignedGuestCount = assignment is null ? 0 : SampleData.ReservaHabitacionHuespedes.Count(item => item.ReservaId == reservation.Id && item.ReservaHabitacionId == assignment.ReservaHabitacionId);
        var requiredCapacity = assignedGuestCount > 0 ? assignedGuestCount : assignedRoomCount <= 1 ? Math.Max(1, reservation.Personas) : int.MaxValue;
        var states = _habitaciones.ListarOperativas(new FiltroHabitacionesOperativas());
        var availability = new HabitacionDisponibilidadService();
        if (requiredCapacity == int.MaxValue) return [];
        return states.Where(item => item.Habitacion.Id != source.Id && item.Habitacion.Capacidad >= requiredCapacity
                && !item.Ocupada && !item.Bloqueada && item.EstadoLimpieza == "Limpia" && !item.RequiereRevisionOperativa
                && availability.EsDisponible(item.Habitacion.Id, reservation.Llegada, reservation.Salida, reservation.Id))
            .OrderBy(item => Math.Abs(item.Habitacion.Piso - source.Piso)).ThenBy(item => item.Habitacion.Tipo == source.Tipo ? 0 : 1)
            .ThenBy(item => item.Habitacion.Numero).Take(8).Select(item => RoomRow(item)).ToList();
    }

    public void NotificarSituacion(SupervisionAlerta alert, string mensaje)
    {
        if (!NavigationService.Puede("Operaciones")) throw new UnauthorizedAccessException("Tu rol no puede enviar avisos de supervisión operativa.");
        if (string.IsNullOrWhiteSpace(mensaje)) throw new InvalidOperationException("Escribe un mensaje para el equipo operativo.");
        var module = alert.Seccion switch
        {
            "Habitaciones" => alert.Titulo.Contains("bloqueo", StringComparison.OrdinalIgnoreCase) ? "Mantenimiento" : "Limpieza",
            "Limpieza" => "Limpieza", "Mantenimiento" => "Mantenimiento", "Servicios" => "Agenda de servicios",
            "Inventario y recursos" => "Productos e inventario", _ => "Inicio"
        };
        var type = alert.Seccion switch
        {
            "Habitaciones" => "Habitación", "Limpieza" => "Tarea de limpieza", "Mantenimiento" => "Incidencia u orden",
            "Servicios" => "Solicitud de servicio", "Inventario y recursos" => "Producto o recurso", _ => "Situación operativa"
        };
        new NotificacionService().Crear(mensaje.Trim(), "Encargado de servicios", module, type, alert.RegistroId);
    }

    private static (DateTime Start, DateTime End) ResolverPeriodo(string periodo, DateTime? customStart, DateTime? customEnd, DateTime today)
    {
        var start = periodo switch
        {
            "Ayer" => today.AddDays(-1), "Últimos 7 días" => today.AddDays(-6), "Últimos 30 días" => today.AddDays(-29),
            "Este mes" => new DateTime(today.Year, today.Month, 1), "Mes anterior" => new DateTime(today.Year, today.Month, 1).AddMonths(-1),
            "Periodo personalizado" => (customStart ?? today).Date, _ => today
        };
        var end = periodo switch
        {
            "Ayer" => today, "Últimos 7 días" or "Últimos 30 días" or "Hoy" => today.AddDays(1),
            "Este mes" => new DateTime(today.Year, today.Month, 1).AddMonths(1),
            "Mes anterior" => new DateTime(today.Year, today.Month, 1),
            "Periodo personalizado" => (customEnd ?? today).Date.AddDays(1), _ => today.AddDays(1)
        };
        if (end <= start) end = start.AddDays(1);
        return (start, end);
    }

    private static SupervisionHabitacionFila RoomRow(HabitacionOperacionModel state)
    {
        var room = state.Habitacion;
        var primary = state.Bloqueada ? "Bloqueada" : state.Ocupada ? "Ocupada" : state.EstadoLimpieza == "Limpia" && !state.RequiereRevisionOperativa ? "Lista" : state.EstadoLimpieza is "Sin verificar" or "" ? "Por verificar" : "En preparación";
        var attention = state.Bloqueada || state.RequiereRevisionOperativa || state.EstadoLimpieza is "Pendiente de inspección" or "Inspección fallida" or "Pendiente de limpieza" or "En limpieza" || state.LlegadaHoy && primary != "Lista";
        var reasons = new List<string>();
        if (state.Bloqueada) reasons.Add("Bloqueo activo");
        if (state.EstadoLimpieza != "Limpia") reasons.Add(state.EstadoLimpieza);
        if (state.RequiereRevisionOperativa) reasons.Add("Incidencia requiere revisión");
        return new(room.Id, room.Numero, room.Piso, room.Tipo, primary, state.Ocupada ? "Ocupada" : "Desocupada", state.EstadoLimpieza,
            room.Bloqueada, state.ProximaReserva is null ? "—" : state.ProximaReserva.Llegada.ToString("dd/MM HH:mm"), state.ReservaActual?.Salida.ToString("dd/MM HH:mm") ?? "—",
            state.Ocupada ? "No disponible mientras esté ocupada" : primary == "Lista" ? "Preparada" : "Pendiente de preparación", attention,
            reasons.Count == 0 ? "—" : string.Join(" · ", reasons), state.Huespedes.Count == 0 ? "Sin ocupante identificado" : string.Join(", ", state.Huespedes.Select(item => item.Nombre)),
            state.ReservaActual is null ? "—" : $"Reserva #{state.ReservaActual.Id}", state.ProximaReserva?.Id);
    }

    private static List<SupervisionIngresoFila> BuildArrivals(DateTime today)
    {
        var end = today.AddDays(7);
        var activeReservations = SampleData.Reservas.Where(item => item.Estado is "Confirmada" or "Pendiente" && item.Llegada.Date >= today && item.Llegada.Date < end).OrderBy(item => item.Llegada).ToList();
        return activeReservations.Select(reservation =>
        {
            var assigned = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservation.Id).Select(item => SampleData.Habitaciones.FirstOrDefault(room => room.Id == item.HabitacionId)).Where(item => item is not null).Cast<HabitacionModel>().DistinctBy(item => item.Id).ToList();
            if (assigned.Count == 0 && SampleData.Habitaciones.FirstOrDefault(item => item.Id == reservation.Habitacion) is { } fallback) assigned.Add(fallback);
            var states = assigned.Select(room => new HabitacionService().ObtenerEstadoOperativo(room.Id)).ToList();
            var condition = assigned.Count == 0 ? "Sin asignación" : states.Any(item => item.Bloqueada) ? "Bloqueo activo" : states.Any(item => item.Ocupada && item.ReservaActual?.Salida.Date > reservation.Llegada.Date) ? "Conflicto de ocupación" : states.Any(item => item.Ocupada && item.ReservaActual?.Salida.Date == reservation.Llegada.Date) ? "Depende de salida y preparación" : states.Any(item => item.EstadoLimpieza is "Pendiente de inspección" or "Inspección fallida") ? "Inspección pendiente" : states.Any(item => item.EstadoLimpieza is "Pendiente de limpieza" or "En limpieza") ? "Limpieza pendiente" : states.Any(item => item.RequiereRevisionOperativa) ? "Revisión operativa" : states.Any(item => item.EstadoLimpieza is "Sin verificar" or "") ? "Preparación sin verificar" : "Preparada";
            var primary = condition is "Bloqueo activo" or "Conflicto de ocupación" ? "Crítica" : condition == "Preparada" ? "Informativa" : "Requiere atención";
            return new SupervisionIngresoFila(reservation.Id, reservation.Huesped, reservation.Llegada, assigned.Count == 0 ? "Sin habitación" : string.Join(", ", assigned.Select(item => item.Numero)), condition, condition == "Preparada" ? "Habitación preparada" : condition == "Sin asignación" ? "Asignar habitación" : condition is "Bloqueo activo" or "Conflicto de ocupación" ? "Riesgo crítico" : "Revisar preparación", primary);
        }).ToList();
    }

    private static List<SupervisionTareaFila> BuildTasks(DateTime start, DateTime end)
        => SampleData.Tareas.Where(item => item.Hora >= start && item.Hora < end || IsOpenTask(item.Estado)).OrderBy(item => item.Hora)
            .Select(item => new SupervisionTareaFila(item.Id, item.Habitacion, SampleData.Habitaciones.FirstOrDefault(room => room.Id == item.Habitacion)?.Numero ?? "—", item.Piso,
                item.Tipo, item.Hora, string.IsNullOrWhiteSpace(item.Empleado) ? "Sin asignar" : item.Empleado, item.Prioridad, item.Estado,
                item.Estado == "Pendiente" && item.Hora < DateTime.Now ? "Hora programada superada; no hay hora real de inicio en el modelo" : item.Estado == "Completada" && !SampleData.Inspecciones.Any(i => i.TareaId == item.Id) ? "Pendiente de inspección" : "—", item.Observaciones))
            .ToList();

    private static List<SupervisionCargaFila> BuildWorkload(IReadOnlyList<SupervisionTareaFila> tasks)
        => tasks.GroupBy(item => item.Responsable).Select(group => new SupervisionCargaFila(group.Key, group.Count(), group.Count(item => item.Estado == "Pendiente"), group.Count(item => item.Estado == "En progreso"), group.Count(item => item.Estado is "Completada" or "Inspeccionada")))
            .OrderByDescending(item => item.Asignadas).ThenBy(item => item.Responsable).ToList();

    private static List<SupervisionInspeccionFila> BuildInspections(DateTime start, DateTime end)
    {
        var results = SampleData.Inspecciones.Where(item => item.Fecha >= start && item.Fecha < end)
            .Select(item => { var task = SampleData.Tareas.FirstOrDefault(t => t.Id == item.TareaId); return new SupervisionInspeccionFila(item.Id, item.TareaId, task is null ? "—" : SampleData.Habitaciones.FirstOrDefault(r => r.Id == task.Habitacion)?.Numero ?? "—", item.Fecha, item.Aprobada ? "Aprobada" : "Rechazada", item.Observaciones); }).ToList();
        var inspectedTasks = SampleData.Inspecciones.Select(item => item.TareaId).ToHashSet();
        var pending = SampleData.Tareas.Where(item => item.Estado == "Completada" && !inspectedTasks.Contains(item.Id))
            .Select(item => new SupervisionInspeccionFila(0, item.Id, SampleData.Habitaciones.FirstOrDefault(r => r.Id == item.Habitacion)?.Numero ?? "—", item.Hora, "Pendiente", "La tarea está completada y no hay inspección registrada para ese ciclo.")).ToList();
        results.AddRange(pending);
        return results.OrderByDescending(item => item.Resultado == "Pendiente").ThenByDescending(item => item.Fecha).ToList();
    }

    private static List<SupervisionIncidenciaFila> BuildIncidents(DateTime start, DateTime end, DateTime today)
        => SampleData.Incidencias.Where(item => item.FechaReporte >= start && item.FechaReporte < end || IsOpen(item.Estado))
            .Select(item =>
            {
                var room = SampleData.Habitaciones.FirstOrDefault(value => value.Id == item.Habitacion);
                var orders = SampleData.OrdenesMantenimiento.Where(value => value.IncidenciaId == item.Id).ToList();
                var affected = SampleData.ReservaHabitaciones.Where(link => link.HabitacionId == item.Habitacion).Select(link => SampleData.Reservas.FirstOrDefault(res => res.Id == link.ReservaId))
                    .Where(res => res is not null && res.Estado is "Confirmada" or "Pendiente" && res.Salida.Date > today).Cast<ReservaModel>().DistinctBy(res => res.Id).ToList();
                return new SupervisionIncidenciaFila(item.Id, item.Habitacion, room?.Numero ?? "—", item.Descripcion, item.Categoria, item.Prioridad, item.Estado,
                    room?.Bloqueada == true, item.BloqueaHabitacion && room?.Bloqueada != true, item.FechaReporte, item.ImpactoObservado,
                    orders.Count == 0 ? "Sin orden" : string.Join(", ", orders.Select(order => $"#{order.Id} · {order.Estado}")), affected.Count.ToString());
            }).OrderByDescending(item => IsPriority(item.Prioridad)).ThenByDescending(item => item.Reportada).ToList();

    private static List<SupervisionOrdenFila> BuildOrders(DateTime start, DateTime end, DateTime today)
        => SampleData.OrdenesMantenimiento.Where(item =>
                item.Estado is not ("Completada" or "Resuelta" or "Cancelada") || item.FechaEstimada is DateTime due && due >= start && due < end || SampleData.Incidencias.FirstOrDefault(i => i.Id == item.IncidenciaId) is { } incident && incident.FechaReporte >= start && incident.FechaReporte < end)
            .Select(item => { var incident = SampleData.Incidencias.FirstOrDefault(i => i.Id == item.IncidenciaId); return new SupervisionOrdenFila(item.Id, item.IncidenciaId, incident is null ? "—" : SampleData.Habitaciones.FirstOrDefault(r => r.Id == incident.Habitacion)?.Numero ?? "—", incident?.Descripcion ?? "Incidencia no disponible", item.AsignadoA, item.FechaEstimada, item.Estado, item.FechaEstimada is DateTime date && date < today && item.Estado is not ("Completada" or "Resuelta")); })
            .OrderByDescending(item => item.Vencida).ThenBy(item => item.Estimada ?? DateTime.MaxValue).ToList();

    private static List<SupervisionServicioFila> BuildServices(DateTime start, DateTime end, DateTime today)
    {
        var rows = new List<SupervisionServicioFila>();
        var appointmentService = new CitaServicioService();
        var appointmentConflictIds = SampleData.Citas.Where(item => item.Estado is not ("Cancelada" or "No presentado" or "Completada") && !string.IsNullOrWhiteSpace(item.AsignadoA) && item.AsignadoA != "Por asignar")
            .Where(item => appointmentService.TieneConflictoHorario(item.AsignadoA, item.Inicio, item.Fin, item.Id)).Select(item => item.Id).ToHashSet();
        foreach (var request in SampleData.SolicitudesServicio.Where(item => item.Solicitada >= start && item.Solicitada < end || IsOpen(item.Estado)))
        {
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == request.HuespedId);
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == request.ServicioId);
            var appointment = request.CitaServicioId is int id ? SampleData.Citas.FirstOrDefault(item => item.Id == id) : SampleData.Citas.FirstOrDefault(item => item.SolicitudId == request.Id);
            var providerLink = SampleData.ConveniosServicios.FirstOrDefault(item => item.ServicioId == request.ServicioId && item.Activo);
            var agreement = request.ConvenioId is int agreementId ? SampleData.Convenios.FirstOrDefault(item => item.Id == agreementId) : providerLink is null ? null : SampleData.Convenios.FirstOrDefault(item => item.Id == providerLink.ConvenioId);
            var room = guest is null ? "—" : SampleData.Habitaciones.FirstOrDefault(item => item.Id == guest.Habitacion)?.Numero ?? "—";
            var overlap = appointment is not null && appointmentConflictIds.Contains(appointment.Id);
            var eligibilityIssue = appointment is not null && service is not null && appointment.AsignadoA is not ("" or "Por asignar") && !appointmentService.EsResponsableElegible(service.Id, appointment.AsignadoA);
            var conflict = overlap || eligibilityIssue;
            var providerIssue = providerLink is not null && (agreement is null || !agreement.Activo || agreement.VigenciaHasta.Date < (request.FechaDeseada ?? appointment?.Inicio ?? today).Date);
            var detail = string.Join(" · ", new[] { request.Descripcion, providerIssue ? "Convenio no vigente para la fecha; no confirma disponibilidad del proveedor" : null, overlap ? "Horario superpuesto con otra cita del responsable" : null, eligibilityIssue ? "Responsable no registrado como elegible para el servicio" : null }.Where(value => !string.IsNullOrWhiteSpace(value)));
            rows.Add(new(request.Id, "Solicitud", guest?.Nombre ?? "—", room, service?.Nombre ?? "Servicio", appointment?.Inicio ?? request.FechaDeseada, request.Responsable is { Length: > 0 } responsible ? responsible : appointment?.AsignadoA ?? "Sin asignar", agreement?.Empresa ?? (providerLink is null ? "Interno / sin convenio vinculado" : "Proveedor por confirmar"), request.Estado, detail, request.Id, conflict));
        }
        foreach (var appointment in SampleData.Citas.Where(item => item.SolicitudId is null && (item.Inicio >= start && item.Inicio < end || item.Estado is not ("Cancelada" or "No presentado" or "Completada"))))
        {
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == appointment.HuespedId);
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == appointment.ServicioId);
            var room = guest is null ? "—" : SampleData.Habitaciones.FirstOrDefault(item => item.Id == guest.Habitacion)?.Numero ?? "—";
            var overlap = appointmentConflictIds.Contains(appointment.Id);
            var eligibilityIssue = service is not null && appointment.AsignadoA is not ("" or "Por asignar") && !appointmentService.EsResponsableElegible(service.Id, appointment.AsignadoA);
            var conflict = overlap || eligibilityIssue;
            var detail = overlap ? "Horario superpuesto con otra cita asignada al responsable." : eligibilityIssue ? "El responsable no figura como elegible para este servicio." : "Cita consultiva; la coordinación se ejecuta en Operaciones.";
            rows.Add(new(appointment.Id, "Cita", guest?.Nombre ?? "—", room, service?.Nombre ?? "Servicio", appointment.Inicio, appointment.AsignadoA, "—", appointment.Estado, detail, null, conflict));
        }
        foreach (var transfer in SampleData.Traslados.Where(item => item.SolicitudId is null && item.Salida >= start && item.Salida < end && item.Estado is not ("Cancelado" or "Completado")))
        {
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == transfer.HuespedId);
            rows.Add(new(transfer.Id, "Traslado", guest?.Nombre ?? "—", guest is null ? "—" : SampleData.Habitaciones.FirstOrDefault(r => r.Id == guest.Habitacion)?.Numero ?? "—", transfer.Origen + " → " + transfer.Destino, transfer.Salida, "Sin responsable registrado", "Proveedor no registrado", transfer.Estado, "El esquema de muestra no registra coordinación ni confirmación del proveedor.", null, false));
        }
        var fulfilledAcquisitions = SampleData.UsosServicio.Where(item => item.ServicioAdquiridoId.HasValue).Select(item => item.ServicioAdquiridoId!.Value).ToHashSet();
        foreach (var purchased in SampleData.ServiciosAdquiridos.Where(item => !fulfilledAcquisitions.Contains(item.ServicioAdquiridoId) && item.Estado is not ("CANCELADO" or "COMPLETADO")))
        {
            var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == purchased.ReservaId);
            var guest = purchased.HuespedId is int guestId ? SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId) : reservation is null ? null : SampleData.Huespedes.FirstOrDefault(item => item.Nombre == reservation.Huesped);
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == purchased.ServicioId);
            rows.Add(new(purchased.ServicioAdquiridoId, "Compromiso adquirido", guest?.Nombre ?? reservation?.Huesped ?? "—", guest is null ? "—" : SampleData.Habitaciones.FirstOrDefault(item => item.Id == guest.Habitacion)?.Numero ?? "—", service?.Nombre ?? "Servicio adquirido", reservation?.Llegada, "Pendiente de coordinar", "—", purchased.Estado, $"Cantidad adquirida: {purchased.Cantidad}; no hay utilización vinculada registrada.", null, false));
        }
        return rows.OrderBy(item => item.Fecha ?? DateTime.MaxValue).ToList();
    }

    private static List<SupervisionProductoFila> BuildProducts()
        => SampleData.Productos.Select(product =>
        {
            var serviceNames = SampleData.ServiciosProductos.Where(link => link.ProductoId == product.Id).Select(link => SampleData.Servicios.FirstOrDefault(s => s.Id == link.ServicioId)?.Nombre).Where(name => name is not null).Distinct().ToList();
            return new SupervisionProductoFila(product.Id, product.Nombre, product.Categoria, product.Stock, product.StockMinimo, product.Stock <= 0 ? "Agotado" : product.Stock < product.StockMinimo ? "Bajo mínimo" : "Disponible", serviceNames.Count == 0 ? "—" : string.Join(", ", serviceNames));
        }).OrderBy(item => item.Stock <= 0 ? 0 : item.Stock < item.Minimo ? 1 : 2).ThenBy(item => item.Producto).ToList();

    private static List<SupervisionRecursoFila> BuildResources(DateTime today)
        => SampleData.ReservaRecursos.Select(allocation =>
        {
            var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == allocation.ReservaId);
            var resource = SampleData.RecursosAdicionales.FirstOrDefault(item => item.RecursoId == allocation.RecursoId);
            var guest = reservation is null ? null : SampleData.Huespedes.FirstOrDefault(item => item.Nombre == reservation.Huesped);
            var actualDeparture = reservation is not null && (reservation.Estado == "Finalizada" || SampleData.Estancias.Any(stay => stay.ReservaId == reservation.Id && stay.CheckOutReal.HasValue));
            var deliveredOverdue = allocation.Estado.Equals("ENTREGADO", StringComparison.OrdinalIgnoreCase) && actualDeparture;
            var from = reservation is null || reservation.Llegada.Date < today ? today : reservation.Llegada.Date;
            var to = reservation is null || reservation.Salida.Date <= from ? from.AddDays(1) : reservation.Salida.Date;
            var availability = resource?.StockTotal is null ? "Stock no cuantificado" : new RecursoAdicionalService().ConsultarDisponibilidad(allocation.RecursoId, from, to, allocation.ReservaId) is int count ? $"{count} disponibles antes de esta asignación" : "Disponibilidad no verificable";
            return new SupervisionRecursoFila(allocation.RecursoId, allocation.ReservaId, resource?.Nombre ?? "Recurso", guest?.Nombre ?? reservation?.Huesped ?? "—", guest is null ? "—" : SampleData.Habitaciones.FirstOrDefault(item => item.Id == guest.Habitacion)?.Numero ?? "—", allocation.Cantidad, allocation.Estado, reservation?.Llegada ?? today, reservation?.Salida ?? today, availability, deliveredOverdue);
        }).OrderByDescending(item => item.DevolucionPendiente).ThenBy(item => item.Llegada).ToList();

    private static List<SupervisionConsumoFila> BuildConsumptions(DateTime start, DateTime end)
        => SampleData.Consumos.Where(item => item.Fecha >= start && item.Fecha < end).Select(item => new SupervisionConsumoFila(item.Fecha,
            SampleData.Productos.FirstOrDefault(product => product.Id == item.ProductoId)?.Nombre ?? "Producto", SampleData.Huespedes.FirstOrDefault(guest => guest.Id == item.HuespedId)?.Nombre ?? "—", item.Cantidad, item.Incluido, string.IsNullOrWhiteSpace(item.Origen) ? "Origen no registrado" : item.Origen)).OrderByDescending(item => item.Fecha).ToList();

    private static List<SupervisionAlerta> BuildAlerts(IReadOnlyList<SupervisionHabitacionFila> rooms, IReadOnlyList<SupervisionIngresoFila> arrivals,
        IReadOnlyList<SupervisionTareaFila> tasks, IReadOnlyList<SupervisionInspeccionFila> inspections, IReadOnlyList<SupervisionIncidenciaFila> incidents,
        IReadOnlyList<SupervisionOrdenFila> orders, IReadOnlyList<SupervisionServicioFila> services, IReadOnlyList<SupervisionProductoFila> products,
        IReadOnlyList<SupervisionRecursoFila> resources, DateTime today)
    {
        var alerts = new List<SupervisionAlerta>();
        foreach (var arrival in arrivals.Where(item => item.Condicion != "Preparada"))
            alerts.Add(new(arrival.Prioridad, $"Reserva #{arrival.ReservaId}", $"Ingreso de {arrival.Huesped}", $"{arrival.Llegada:dd/MM} · Habitación(es): {arrival.Habitaciones} · {arrival.Condicion}", arrival.Condicion == "Bloqueo activo" ? "Capacidad de alojamiento comprometida" : "Revisar preparación antes del ingreso", arrival.Llegada, "Habitaciones", arrival.ReservaId));
        foreach (var room in rooms.Where(item => item.BloqueoActivo))
            alerts.Add(new(room.Ocupacion == "Ocupada" ? "Crítica" : "Requiere atención", $"Habitación {room.Numero}", "Bloqueo operativo activo", room.NotaAtencion, room.Ocupacion == "Ocupada" ? "La habitación figura ocupada y bloqueada" : "No contar como disponible", null, "Mantenimiento", room.Id));
        foreach (var item in incidents.Where(item => IsOpen(item.Estado) && IsPriority(item.Prioridad)))
            alerts.Add(new(item.Prioridad.Equals("Urgente", StringComparison.OrdinalIgnoreCase) ? "Crítica" : "Requiere atención", $"Incidencia #{item.Id} · Hab. {item.Habitacion}", item.Descripcion, $"{item.Prioridad} · {item.Estado}", item.BloqueoActivo ? "Bloqueo actual registrado" : item.RequiereBloqueoSinRegistro ? "Revisar bloqueo: la incidencia lo requiere pero no hay bloqueo actual" : "Incidencia abierta", item.Reportada, "Mantenimiento", item.Id));
        foreach (var task in tasks.Where(item => item.Estado == "Pendiente" && item.Programada < DateTime.Now || item.Responsable == "Sin asignar" && item.Estado == "Pendiente"))
            alerts.Add(new("Requiere atención", $"Tarea #{task.Id} · Hab. {task.Habitacion}", task.Responsable == "Sin asignar" ? "Tarea de limpieza sin asignar" : "Inicio pendiente tras hora programada", $"{task.Tipo} · {task.Programada:dd/MM HH:mm} · {task.Prioridad}", task.Atencion, task.Programada, "Limpieza", task.Id));
        foreach (var inspection in inspections.Where(item => item.Resultado is "Pendiente" or "Rechazada"))
            alerts.Add(new("Requiere atención", $"Inspección · Hab. {inspection.Habitacion}", inspection.Resultado == "Pendiente" ? "Inspección pendiente" : "Inspección no aprobada", inspection.Observaciones, "Preparación de habitación pendiente", inspection.Fecha, "Limpieza", inspection.TareaId));
        foreach (var order in orders.Where(item => item.Vencida)) alerts.Add(new("Requiere atención", $"Orden #{order.Id} · Hab. {order.Habitacion}", "Fecha estimada vencida", $"{order.Incidencia} · responsable: {order.Responsable}", "Consultar avance con Operaciones", order.Estimada, "Mantenimiento", order.Id));
        foreach (var service in services.Where(item => item.Conflicto || item.Proveedor.Contains("no registrado", StringComparison.OrdinalIgnoreCase) || item.Proveedor.Contains("por confirmar", StringComparison.OrdinalIgnoreCase) || item.Tipo == "Solicitud" && NeedsCoordinationByState(item.Estado)))
            alerts.Add(new(service.Conflicto ? "Crítica" : "Requiere atención", $"{service.Tipo} · {service.Servicio}", service.Conflicto ? "Conflicto de coordinación" : "Servicio por coordinar", $"{service.Huesped} · {service.Fecha:dd/MM HH:mm} · {service.Proveedor}", service.Detalle, service.Fecha, "Servicios", service.SolicitudId ?? service.Id));
        foreach (var product in products.Where(item => item.Stock < item.Minimo)) alerts.Add(new("Requiere atención", product.Producto, product.Stock <= 0 ? "Producto agotado" : "Stock bajo mínimo", $"{product.Stock} disponibles · mínimo configurado {product.Minimo}", product.ServiciosAfectados == "—" ? "Revisar existencias" : $"Servicios relacionados: {product.ServiciosAfectados}", today, "Inventario y recursos", product.Id));
        foreach (var resource in resources.Where(item => item.DevolucionPendiente)) alerts.Add(new("Requiere atención", $"{resource.Recurso} · Reserva #{resource.ReservaId}", "Recurso aún marcado como entregado después de la salida", $"{resource.Huesped} · salida {resource.Salida:dd/MM}", "Confirmar devolución en Operaciones; no se libera automáticamente", resource.Salida, "Inventario y recursos", resource.Id));
        if (alerts.Count == 0) alerts.Add(new("Informativa", "Operación", "No se detectaron situaciones que requieran atención", "Según la información operativa disponible.", "", null, "Resumen", 0));
        return alerts;
    }

    private static List<string> BuildDiagnostics(IReadOnlyList<HabitacionOperacionModel> rooms, IReadOnlyList<SupervisionTareaFila> tasks,
        IReadOnlyList<SupervisionIncidenciaFila> incidents, IReadOnlyList<SupervisionOrdenFila> orders, IReadOnlyList<SupervisionServicioFila> services, IReadOnlyList<SupervisionRecursoFila> resources)
    {
        var result = new List<string>();
        foreach (var room in rooms.Where(item => item.Habitacion.Limpieza == "Limpia" && item.Bloqueada)) result.Add($"Habitación {room.Habitacion.Numero}: figura limpia, pero conserva bloqueo operativo activo.");
        foreach (var item in incidents.Where(item => item.RequiereBloqueoSinRegistro)) result.Add($"Incidencia #{item.Id} · Hab. {item.Habitacion}: requiere bloqueo, pero la habitación no figura bloqueada. Confirmar con Operaciones.");
        foreach (var item in tasks.Where(item => item.Estado is "Completada" or "Inspeccionada" && item.Programada == default)) result.Add($"Tarea #{item.Id}: completada sin hora programada válida; no se puede inferir duración.");
        foreach (var item in services.Where(item => item.Conflicto)) result.Add($"{item.Tipo} #{item.Id}: responsable asignado a citas cuyos horarios se superponen.");
        foreach (var item in resources.Where(item => item.DevolucionPendiente)) result.Add($"{item.Recurso} · Reserva #{item.ReservaId}: sigue marcado ENTREGADO tras la salida prevista.");
        return result;
    }

    private static bool IsOpen(string state) => state is not ("Resuelta" or "Completada" or "Cancelada" or "Rechazada" or "Devuelta" or "Cerrada");
    private static bool NeedsCoordination(SolicitudServicioModel request)
    {
        if (!NeedsCoordinationByState(request.Estado)) return false;
        var appointment = request.CitaServicioId is int appointmentId ? SampleData.Citas.FirstOrDefault(item => item.Id == appointmentId) : SampleData.Citas.FirstOrDefault(item => item.SolicitudId == request.Id);
        return appointment is null || appointment.Estado is "Solicitada" or "Pendiente" or "Sin asignar" || string.IsNullOrWhiteSpace(appointment.AsignadoA) || appointment.AsignadoA == "Por asignar";
    }
    private static bool NeedsCoordinationByState(string state) => state is "Solicitada" or "Pendiente" or "Nueva" or "En coordinación";
    private static bool IsOpenTask(string state) => state is not ("Completada" or "Inspeccionada" or "Cancelada");
    private static bool IsPriority(string priority) => priority is "Alta" or "Urgente";
    private static int SeverityRank(string level) => level == "Crítica" ? 0 : level == "Requiere atención" ? 1 : 2;
}
