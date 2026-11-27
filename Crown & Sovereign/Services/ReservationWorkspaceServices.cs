using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Única regla de disponibilidad para búsqueda, matriz, calendario y lista de espera.</summary>
public sealed class HabitacionDisponibilidadService
{
    private static readonly HashSet<string> EstadosNoBloqueantes = new(StringComparer.OrdinalIgnoreCase) { "Cancelada", "Rechazada", "Anulada" };

    public bool EsDisponible(int habitacionId, DateTime llegada, DateTime salida, int? excluirReservaId = null)
    {
        if (salida.Date <= llegada.Date) return false;
        var room = SampleData.Habitaciones.FirstOrDefault(item => item.Id == habitacionId);
        if (room is null || room.Bloqueada) return false;
        var reservations = SampleData.Reservas.Where(item => !EstadosNoBloqueantes.Contains(item.Estado) && item.Id != excluirReservaId);
        foreach (var reservation in reservations)
        {
            var assignedRooms = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservation.Id).Select(item => item.HabitacionId);
            if (reservation.Habitacion != habitacionId && !assignedRooms.Contains(habitacionId)) continue;
            if (reservation.Llegada.Date < salida.Date && reservation.Salida.Date > llegada.Date) return false;
        }
        return true;
    }

    public IReadOnlyList<HabitacionDisponibleModel> BuscarHabitaciones(ReservationSearchCriteria criteria, string? tipo = null, int? excluirReservaId = null)
    {
        ValidarFechas(criteria.Llegada, criteria.Salida);
        if (criteria.Personas < 1 || criteria.Habitaciones < 1) throw new InvalidOperationException("Personas y habitaciones deben ser cantidades positivas.");
        return SampleData.Habitaciones
            .Where(room => string.IsNullOrWhiteSpace(tipo) || tipo == "Todos los tipos" || room.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase))
            .Where(room => string.IsNullOrWhiteSpace(criteria.TipoHabitacion) || criteria.TipoHabitacion == "Todos los tipos" || room.Tipo.Equals(criteria.TipoHabitacion, StringComparison.OrdinalIgnoreCase))
            .Where(room => TieneComodidades(room, criteria.Comodidades))
            .Where(room => EsDisponible(room.Id, criteria.Llegada, criteria.Salida, excluirReservaId))
            .Select(room => new HabitacionDisponibleModel(room.Id, room.Numero, room.Piso, room.Tipo, room.Capacidad, room.Comodidades,
                new TarifaService().TarifaPorNoche(room, criteria.Llegada), new TarifaService().CalcularAlojamiento(room, criteria.Llegada, criteria.Salida), room.Limpieza))
            .OrderBy(room => room.Tipo).ThenBy(room => room.Piso).ThenBy(room => room.Numero).ToList();
    }

    public IReadOnlyList<TipoDisponibilidadModel> BuscarTipos(ReservationSearchCriteria criteria, int? excluirReservaId = null)
    {
        var rooms = BuscarHabitaciones(criteria, excluirReservaId: excluirReservaId);
        return rooms.GroupBy(room => room.Tipo).Select(group =>
        {
            var picked = group.OrderByDescending(room => room.Capacidad).Take(criteria.Habitaciones).ToList();
            var enough = group.Count() >= criteria.Habitaciones && picked.Sum(room => room.Capacidad) >= criteria.Personas;
            return new TipoDisponibilidadModel(group.Key, group.Max(room => room.Capacidad), enough ? group.Count() : 0,
                group.Min(room => room.TarifaNoche), picked.Sum(room => room.TotalEstancia),
                group.SelectMany(room => SepararComodidades(room.Comodidades)).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList(),
                enough ? group.ToList() : []);
        }).Where(item => item.Coincidentes > 0).OrderBy(item => item.TotalEstimado).ToList();
    }

    public IReadOnlyList<ReservationAvailabilityLine> ObtenerDisponibilidadPorNoche(DateTime desde, int dias)
    {
        var fechas = Enumerable.Range(0, Math.Clamp(dias, 1, 30)).Select(i => desde.Date.AddDays(i)).ToList();
        var rows = SampleData.Tipos.Select(type => (Tipo: type, Habitaciones: SampleData.Habitaciones.Where(room => room.Tipo == type).ToList())).Append((Tipo: "Total", Habitaciones: SampleData.Habitaciones.ToList()));
        return rows.Select(row => new ReservationAvailabilityLine
        {
            RoomType = row.Tipo,
            IsTotal = row.Tipo == "Total",
            Days = fechas.Select(date =>
            {
                var count = row.Habitaciones.Count(room => EsDisponible(room.Id, date, date.AddDays(1)));
                return new ReservationAvailabilityCell(date, date.ToString("ddd dd", new System.Globalization.CultureInfo("es-BO")), count, row.Habitaciones.Count,
                    count == 0 ? "Sin disponibilidad" : count <= 2 ? "Limitada" : "Normal");
            }).ToList()
        }).ToList();
    }

    public void ValidarSeleccion(ReservationSearchCriteria criteria, IReadOnlyCollection<int> habitaciones, int? excluirReservaId = null)
    {
        if (habitaciones.Count != criteria.Habitaciones) throw new InvalidOperationException($"Selecciona {criteria.Habitaciones} habitaciones.");
        if (habitaciones.Distinct().Count() != habitaciones.Count) throw new InvalidOperationException("No repitas habitaciones en la selección.");
        var available = BuscarHabitaciones(criteria, excluirReservaId: excluirReservaId).Where(item => habitaciones.Contains(item.Id)).ToList();
        if (available.Count != habitaciones.Count) throw new InvalidOperationException("Una habitación dejó de estar disponible para toda la estancia.");
        if (available.Sum(item => item.Capacidad) < criteria.Personas) throw new InvalidOperationException("La capacidad conjunta de las habitaciones no cubre a todos los huéspedes.");
    }

    public int CapacidadCompatible(ReservationSearchCriteria criteria) => BuscarHabitaciones(criteria).Sum(room => room.Capacidad);

    public static void ValidarFechas(DateTime llegada, DateTime salida)
    {
        if (salida.Date <= llegada.Date) throw new InvalidOperationException("La salida debe ser posterior a la llegada.");
    }

    private static bool TieneComodidades(HabitacionModel room, IReadOnlyCollection<string> required)
    {
        if (required.Count == 0) return true;
        var amenities = SepararComodidades(room.Comodidades).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return required.All(amenities.Contains);
    }
    private static IEnumerable<string> SepararComodidades(string values) => values.Split(['·', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class TarifaService
{
    public decimal TarifaPorNoche(HabitacionModel room, DateTime date)
    {
        var matching = SampleData.TarifasHotel.Where(rate => rate.TipoHabitacion == room.Tipo && rate.Desde.Date <= date.Date && rate.Hasta.Date >= date.Date).OrderByDescending(rate => rate.Desde).FirstOrDefault();
        return matching?.Precio ?? SampleData.Tarifas.GetValueOrDefault(room.Id, 0m);
    }
    public decimal CalcularAlojamiento(HabitacionModel room, DateTime llegada, DateTime salida)
    {
        HabitacionDisponibilidadService.ValidarFechas(llegada, salida);
        return Enumerable.Range(0, (salida.Date - llegada.Date).Days).Sum(offset => TarifaPorNoche(room, llegada.Date.AddDays(offset)));
    }
    public decimal CalcularTotal(BorradorReserva draft)
        => CalcularResumenEconomico(draft).Total;

    public ResumenEconomicoReserva CalcularResumenEconomico(BorradorReserva draft)
    {
        var rooms = SampleData.Habitaciones.Where(room => draft.HabitacionesSeleccionadas.Contains(room.Id)).ToList();
        var lodging = rooms.Sum(room => CalcularAlojamiento(room, draft.Llegada, draft.Salida));
        var package = SampleData.Paquetes.FirstOrDefault(item => item.Id == draft.PaqueteId);
        var packages = (draft.PaqueteId.HasValue ? draft.PrecioPaqueteAcordado ?? package?.Precio ?? 0m : 0m) * Math.Max(0, draft.PaqueteId.HasValue ? draft.CantidadPaquete : 0);
        var resourceService = new RecursoAdicionalService();
        var pricesPending = draft.RecursosAdicionales.Any(item => item.PrecioUnitarioAcordado is null || item.TipoCobroAplicado is null)
            || draft.ServiciosAdquiridos.Any(item => item.PrecioUnitarioAcordado is null);
        var resources = draft.RecursosAdicionales.Sum(item => item.PrecioUnitarioAcordado is decimal agreed && item.TipoCobroAplicado is string billing
            ? resourceService.CalcularImporte(agreed, billing, item.Cantidad, Math.Max(0, (draft.Salida.Date - draft.Llegada.Date).Days)) : 0m);
        var services = draft.ServiciosAdquiridos.Sum(item => item.PrecioUnitarioAcordado is decimal agreed ? item.Cantidad * agreed : 0m);
        var legacyAddOns = draft.ServiciosAdquiridos.Count == 0 ? draft.ServiciosAdicionales.Distinct().Sum(id => SampleData.Servicios.FirstOrDefault(item => item.Id == id)?.Precio ?? 0m) : 0m;
        services += legacyAddOns;
        var total = lodging + packages + resources + services;
        var promotion = SampleData.Promociones.FirstOrDefault(item => item.Id == draft.PromocionId && item.Desde.Date <= draft.Llegada.Date && item.Hasta.Date >= draft.Llegada.Date);
        var discounts = 0m;
        if (promotion?.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase) == true) discounts = lodging * promotion.Valor / 100m;
        else if (promotion?.TipoDescuento.Equals("Noches", StringComparison.OrdinalIgnoreCase) == true) discounts = rooms.Sum(room => Enumerable.Range(0, Math.Min((int)promotion.Valor, (draft.Salida.Date - draft.Llegada.Date).Days)).Sum(offset => TarifaPorNoche(room, draft.Llegada.Date.AddDays(offset))));
        discounts = Math.Min(total, Math.Max(0m, discounts));
        const decimal taxes = 0m; // Current in-memory rate catalog does not mark prices as tax-exclusive.
        return new(lodging, packages, resources, services, discounts, taxes, Math.Max(0m, total - discounts + taxes), pricesPending);
    }
}

public sealed class RecursoAdicionalService
{
    public IReadOnlyList<RecursoAdicionalModel> ListarActivos() => SampleData.RecursosAdicionales.Where(item => item.Activo).OrderBy(item => item.Tipo).ThenBy(item => item.Nombre).ToList();

    public int? ConsultarDisponibilidad(int recursoId, DateTime llegada, DateTime salida, int? excluirReservaId = null)
    {
        HabitacionDisponibilidadService.ValidarFechas(llegada, salida);
        var resource = SampleData.RecursosAdicionales.FirstOrDefault(item => item.RecursoId == recursoId) ?? throw new InvalidOperationException("No se encontró el recurso adicional.");
        if (resource.StockTotal is null) return null;
        var periods = SampleData.ReservaRecursos.Where(item => item.RecursoId == recursoId && item.ReservaId != excluirReservaId && item.Cantidad > 0
                && item.Estado is not ("CANCELADO" or "DEVUELTO"))
            .Where(item => SampleData.Reservas.Any(reservation => reservation.Id == item.ReservaId && reservation.Estado is not ("Cancelada" or "Rechazada" or "Anulada")))
            .Select(item => (Item: item, Reservation: SampleData.Reservas.First(reservation => reservation.Id == item.ReservaId)))
            .Select(pair => (pair.Item, pair.Reservation, End: EsPendienteDevolucion(pair.Reservation) ? DateTime.MaxValue.Date : pair.Reservation.Salida.Date))
            .Where(pair => pair.Reservation.Llegada.Date < salida.Date && pair.End > llegada.Date).ToList();
        var peak = 0;
        var boundaries = periods.SelectMany(pair => new[] { Max(pair.Reservation.Llegada.Date, llegada.Date), Min(pair.End, salida.Date) })
            .Append(llegada.Date).Append(salida.Date).Distinct().OrderBy(item => item).ToList();
        for (var i = 0; i + 1 < boundaries.Count; i++)
        {
            if (boundaries[i] >= boundaries[i + 1]) continue;
            var concurrent = periods.Where(pair => pair.Reservation.Llegada.Date < boundaries[i + 1] && pair.End > boundaries[i]).Sum(pair => pair.Item.Cantidad);
            peak = Math.Max(peak, concurrent);
        }
        return Math.Max(0, resource.StockTotal.Value - peak);
    }

    public void ValidarCantidadDisponible(int recursoId, DateTime llegada, DateTime salida, int cantidad, int? excluirReservaId = null)
    {
        if (cantidad < 0) throw new InvalidOperationException("La cantidad no puede ser negativa.");
        if (cantidad == 0) return;
        var resource = SampleData.RecursosAdicionales.FirstOrDefault(item => item.RecursoId == recursoId && (item.Activo || excluirReservaId.HasValue)) ?? throw new InvalidOperationException("El recurso no está disponible para nuevas reservas.");
        var available = ConsultarDisponibilidad(recursoId, llegada, salida, excluirReservaId);
        if (available is int remaining && cantidad > remaining) throw new InvalidOperationException($"Disponibilidad insuficiente para {resource.Nombre}. Cantidad solicitada: {cantidad}; disponible: {remaining}.");
    }

    public decimal CalcularImporte(decimal precioUnitario, string tipoCobro, int cantidad, int cantidadNoches)
    {
        if (precioUnitario < 0 || cantidad < 0 || cantidadNoches < 0) throw new InvalidOperationException("Los importes y cantidades deben ser válidos.");
        return tipoCobro switch
        {
            "POR_NOCHE" => precioUnitario * cantidad * cantidadNoches,
            "POR_ESTANCIA" => precioUnitario * cantidad,
            _ => throw new InvalidOperationException("El recurso tiene una modalidad de cobro no válida.")
        };
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static bool EsPendienteDevolucion(ReservaModel reservation)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.ReservaId == reservation.Id && item.Estado == "Activa");
        return reservation.Estado == "Finalizada" || stay is not null && stay.CheckOut.Date <= DateTime.Today;
    }
}

public sealed class GrupoReservaService
{
    public IReadOnlyList<GrupoReservaResumenModel> Listar(string busqueda = "") => SampleData.GruposReservas
        .Where(group => string.IsNullOrWhiteSpace(busqueda) || group.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
        .Select(group => Resumen(group)).OrderBy(item => item.Llegada).ToList();

    public GrupoReservaResumenModel Resumen(GrupoReservaModel group)
    {
        var reservations = SampleData.Reservas.Where(item => item.GrupoId == group.Id).ToList();
        var roomAssignments = SampleData.ReservaHabitaciones.Where(item => reservations.Any(reservation => reservation.Id == item.ReservaId)).ToList();
        var guests = SampleData.ReservaHuespedes.Where(item => reservations.Any(reservation => reservation.Id == item.ReservaId)).Select(item => item.HuespedId).Distinct().Count();
        var arrived = reservations.Count(item => item.Estado == "En estancia" || SampleData.Estancias.Any(stay => stay.ReservaId == item.Id && stay.Estado == "Activa"));
        var earliest = reservations.Select(item => item.Llegada).DefaultIfEmpty(group.Llegada ?? DateTime.Today).Min();
        var latest = reservations.Select(item => item.Salida).DefaultIfEmpty(group.Salida ?? earliest.AddDays(1)).Max();
        return new(group, earliest, latest, guests, Math.Max(roomAssignments.Count, reservations.Count), roomAssignments.Select(item => item.HabitacionId).Distinct().Count(), reservations.Count, arrived, group.Estado);
    }

    public IReadOnlyList<GrupoHuespedAsignacionModel> ConsultarHuespedes(int grupoId, bool soloSinAsignar = false)
    {
        var reservations = SampleData.Reservas.Where(item => item.GrupoId == grupoId).ToList();
        return reservations.SelectMany(reservation => SampleData.ReservaHuespedes.Where(guest => guest.ReservaId == reservation.Id).Select(guest =>
        {
            var assignment = SampleData.ReservaHabitacionHuespedes.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HuespedId == guest.HuespedId);
            var roomId = assignment is null ? null : SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservation.Id && item.ReservaHabitacionId == assignment.ReservaHabitacionId)?.HabitacionId;
            var room = roomId is int id ? SampleData.Habitaciones.FirstOrDefault(item => item.Id == id) : null;
            var person = SampleData.Huespedes.First(item => item.Id == guest.HuespedId);
            return new GrupoHuespedAsignacionModel(person.Id, person.Nombre, person.Documento, room?.Numero ?? "Sin asignar", reservation.Id, guest.Principal);
        })).Where(item => !soloSinAsignar || item.Habitacion == "Sin asignar").ToList();
    }

    public IReadOnlyList<HabitacionModel> HabitacionesAsignadas(int reservaId) => SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservaId).Select(item => SampleData.Habitaciones.First(room => room.Id == item.HabitacionId)).ToList();

    public void AsignarHabitacion(int reservaId, int habitacionId)
    {
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId && item.GrupoId.HasValue) ?? throw new InvalidOperationException("La reserva no pertenece a un grupo.");
        var room = SampleData.Habitaciones.First(item => item.Id == habitacionId);
        if (!new HabitacionDisponibilidadService().EsDisponible(room.Id, reservation.Llegada, reservation.Salida, reservation.Id)) throw new InvalidOperationException("La habitación no está disponible para toda la estancia del grupo.");
        if (SampleData.ReservaHabitaciones.Any(item => item.ReservaId == reservaId && item.HabitacionId == room.Id)) return;
        SampleData.ReservaHabitaciones.Add(new(SiguienteAsignacionId(), reservaId, room.Id));
        Guardar(reservaId);
    }

    public void AsignarHuesped(int reservaId, int habitacionId, int guestId)
    {
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId && item.GrupoId.HasValue) ?? throw new InvalidOperationException("La reserva no pertenece a un grupo.");
        var guestLink = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservaId && item.HuespedId == guestId) ?? throw new InvalidOperationException("El huésped no pertenece a esta reserva de grupo.");
        var assignment = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservaId && item.HabitacionId == habitacionId) ?? throw new InvalidOperationException("Añade la habitación a esta reserva antes de asignar huéspedes.");
        var room = SampleData.Habitaciones.First(item => item.Id == habitacionId);
        if (!new HabitacionDisponibilidadService().EsDisponible(room.Id, reservation.Llegada, reservation.Salida, reservation.Id)) throw new InvalidOperationException("La habitación ya no está disponible durante toda la estancia.");
        var currentCount = SampleData.ReservaHabitacionHuespedes.Count(item => item.ReservaHabitacionId == assignment.ReservaHabitacionId);
        if (currentCount >= room.Capacidad && !SampleData.ReservaHabitacionHuespedes.Any(item => item.ReservaId == reservaId && item.HuespedId == guestId)) throw new InvalidOperationException("La capacidad de la habitación ya está completa.");
        if (SampleData.ReservaHabitacionHuespedes.Any(item => item.ReservaId == reservaId && item.HuespedId == guestId)) throw new InvalidOperationException("Este huésped ya tiene una habitación asignada.");
        SampleData.ReservaHabitacionHuespedes.Add(new(assignment.ReservaHabitacionId, reservaId, guestId));
        Guardar(reservaId);
    }

    public void MoverHuesped(int reservaId, int habitacionId, int guestId)
    {
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId && item.GrupoId.HasValue) ?? throw new InvalidOperationException("La reserva no pertenece a un grupo.");
        if (!SampleData.ReservaHuespedes.Any(item => item.ReservaId == reservaId && item.HuespedId == guestId)) throw new InvalidOperationException("El huésped no pertenece a esta reserva de grupo.");
        var target = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservaId && item.HabitacionId == habitacionId) ?? throw new InvalidOperationException("Añade la habitación a esta reserva antes de asignar huéspedes.");
        var room = SampleData.Habitaciones.First(item => item.Id == habitacionId);
        if (!new HabitacionDisponibilidadService().EsDisponible(room.Id, reservation.Llegada, reservation.Salida, reservation.Id)) throw new InvalidOperationException("La habitación ya no está disponible durante toda la estancia.");
        var occupants = SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservaId && item.ReservaHabitacionId == target.ReservaHabitacionId && item.HuespedId != guestId).Count();
        if (occupants >= room.Capacidad) throw new InvalidOperationException("La capacidad de la habitación ya está completa.");
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item => item.ReservaId == reservaId && item.HuespedId == guestId);
        SampleData.ReservaHabitacionHuespedes.Add(new(target.ReservaHabitacionId, reservaId, guestId));
        Guardar(reservaId);
    }

    public void QuitarHuesped(int reservaId, int guestId)
    {
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item => item.ReservaId == reservaId && item.HuespedId == guestId);
        Guardar(reservaId);
    }

    // Si la base rechaza el cambio, la reserva vuelve a como estaba guardada.
    private static void Guardar(int reservaId)
    {
        try { ReservaRepositorio.Guardar(reservaId); }
        catch (Exception ex)
        {
            ReservaRepositorio.Recargar(reservaId);
            throw new InvalidOperationException("No se pudo guardar el cambio del grupo en la base de datos. " + ex.Message, ex);
        }
    }

    private static int SiguienteAsignacionId() => Persistencia.SiguienteId("reserva_habitacion", "reserva_habitacion_id",
        () => SampleData.ReservaHabitaciones.Count == 0 ? 1 : SampleData.ReservaHabitaciones.Max(item => item.ReservaHabitacionId) + 1);
}

public sealed class ListaEsperaService
{
    private readonly HabitacionDisponibilidadService _availability = new();
    public IReadOnlyList<CoincidenciaEsperaModel> BuscarCoincidencias(string busqueda = "", string? estado = null, string? tipo = null)
    {
        return SampleData.ListaEspera.Where(item => item.Estado is not ("Convertida" or "Cerrada"))
            .Where(item => string.IsNullOrWhiteSpace(estado) || estado == "Todos los estados" || item.Estado == estado)
            .Where(item => string.IsNullOrWhiteSpace(tipo) || tipo == "Todos los tipos" || item.TipoHabitacion == tipo)
            .Select(item =>
            {
                var criteria = new ReservationSearchCriteria { Llegada = item.Llegada, Salida = item.Salida, Personas = item.Personas, Habitaciones = item.Habitaciones, TipoHabitacion = item.TipoHabitacion };
                var rooms = _availability.BuscarHabitaciones(criteria, item.TipoHabitacion);
                var guest = item.HuespedId is int guestId ? SampleData.Huespedes.FirstOrDefault(person => person.Id == guestId) : SampleData.Huespedes.FirstOrDefault(person => person.Nombre == item.Huesped);
                return new CoincidenciaEsperaModel(item, guest?.Nombre ?? item.Huesped, guest?.Telefono ?? "Sin teléfono", guest?.Email ?? "Sin correo", rooms.Count, rooms);
            }).Where(item => string.IsNullOrWhiteSpace(busqueda) || $"{item.Huesped} {item.Solicitud.TipoHabitacion}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.HabitacionesCoincidentes >= item.Solicitud.Habitaciones).ThenBy(item => item.Solicitud.Llegada).ToList();
    }
    public ListaEsperaModel Registrar(string huesped, int? guestId, DateTime llegada, DateTime salida, string tipo, int personas, int habitaciones, string observaciones)
    {
        HabitacionDisponibilidadService.ValidarFechas(llegada, salida);
        if (string.IsNullOrWhiteSpace(huesped) || personas < 1 || habitaciones < 1) throw new InvalidOperationException("Completa el huésped y las cantidades solicitadas.");
        var person = guestId is int id ? SampleData.Huespedes.FirstOrDefault(item => item.Id == id) : SampleData.Huespedes.FirstOrDefault(item => item.Nombre == huesped.Trim());
        if (Persistencia.Activa && person is null) throw new InvalidOperationException("Selecciona un huésped registrado para la lista de espera.");
        var request = new ListaEsperaModel(SiguienteSolicitudId(), huesped.Trim(), llegada.Date, salida.Date, tipo, "Pendiente", guestId, personas, habitaciones, observaciones.Trim());
        if (person is not null) EsperaRepositorio.InsertarEspera(request, person.Id);
        SampleData.ListaEspera.Insert(0, request); return request;
    }
    public void RegistrarContacto(int id, string medio, string resultado, string observaciones)
    {
        var request = SampleData.ListaEspera.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("No se encontró la solicitud de espera.");
        var contact = new ContactoListaEsperaModel(SampleData.ContactosListaEspera.Count + 1, id, DateTime.Now, medio, resultado, observaciones.Trim(), SesionActual.Usuario?.Usuario ?? "demo");
        var contactId = EsperaRepositorio.InsertarContacto(contact);
        if (contactId > 0) contact = contact with { Id = contactId };
        SampleData.ContactosListaEspera.Insert(0, contact);
        var index = SampleData.ListaEspera.FindIndex(item => item.Id == id);
        var estado = resultado == "Sin respuesta" ? "Contactar" : resultado == "Interesado" ? "Interesado" : request.Estado;
        EsperaRepositorio.CambiarEstadoEspera(id, estado);
        SampleData.ListaEspera[index] = request with { Estado = estado };
    }
    public void Cerrar(int id)
    {
        var index = SampleData.ListaEspera.FindIndex(item => item.Id == id);
        if (index < 0) throw new InvalidOperationException("No se encontró la solicitud.");
        if (SampleData.ListaEspera[index].Estado == "Convertida") throw new InvalidOperationException("Una solicitud convertida no se puede cerrar desde la lista de espera.");
        EsperaRepositorio.CambiarEstadoEspera(id, "Cerrada");
        SampleData.ListaEspera[index] = SampleData.ListaEspera[index] with { Estado = "Cerrada" };
    }
    public void MarcarConvertida(int id) { var index = SampleData.ListaEspera.FindIndex(item => item.Id == id); if (index < 0) throw new InvalidOperationException("No se encontró la solicitud."); EsperaRepositorio.CambiarEstadoEspera(id, "Convertida"); SampleData.ListaEspera[index] = SampleData.ListaEspera[index] with { Estado = "Convertida" }; }
    private static int SiguienteSolicitudId() => Persistencia.SiguienteId("lista_espera", "lista_espera_id", () => SampleData.ListaEspera.Count == 0 ? 1 : SampleData.ListaEspera.Max(item => item.Id) + 1);
}
