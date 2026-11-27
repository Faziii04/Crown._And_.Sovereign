using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed class DashboardService
{
    public IReadOnlyList<DashboardMetric> Recepcion()
    {
        var arrivals = SampleData.Reservas.Count(r => r.Estado != "Cancelada" && r.Llegada.Date == DateTime.Today);
        var departures = SampleData.Reservas.Count(r => r.Estado != "Cancelada" && r.Salida.Date == DateTime.Today);
        var occupied = SampleData.Habitaciones.Count(r => r.Estado == "Ocupada" && !r.Bloqueada);
        var available = SampleData.Habitaciones.Count(r => r.Estado == "Disponible" && !r.Bloqueada);
        return [new("Llegadas de hoy", arrivals.ToString(), "Reservas previstas", "Information"), new("Salidas de hoy", departures.ToString(), "Revisar folios", "Warning"), new("Ocupación actual", $"{occupied * 100 / Math.Max(1, SampleData.Habitaciones.Count)}%", $"{occupied} habitaciones ocupadas", "Success"), new("Habitaciones disponibles", available.ToString(), "Operativas y listas", "Neutral")];
    }

    public IReadOnlyList<DashboardArrival> LlegadasYSalidas()
    {
        var today = SampleData.Reservas.Where(r => r.Estado != "Cancelada" && (r.Llegada.Date == DateTime.Today || r.Salida.Date == DateTime.Today)).OrderBy(r => r.Llegada).Take(12);
        return today.Select(r => new DashboardArrival(r.Llegada.Date == DateTime.Today ? "15:00" : "11:00", r.Huesped, SampleData.Habitaciones.First(h => h.Id == r.Habitacion).Numero, r.Llegada.Date == DateTime.Today ? "Llegada" : "Salida", r.Llegada.Date == DateTime.Today ? "Check-in" : "Check-out", r.Id)).ToList();
    }

    public IReadOnlyList<DashboardMetric> Operaciones()
    {
        return [new("Citas próximas", SampleData.Citas.Count(c => c.Inicio >= DateTime.Now && c.Estado != "Cancelada").ToString(), "Agenda de hoy", "Information"), new("Solicitudes pendientes", SampleData.Citas.Count(c => c.Estado == "Solicitada").ToString(), "Por asignar", "Warning"), new("Habitaciones por limpiar", SampleData.Tareas.Count(t => t.Estado == "Pendiente").ToString(), "Tareas pendientes", "Warning"), new("Incidencias abiertas", SampleData.Incidencias.Count(i => i.Estado != "Resuelta").ToString(), $"{SampleData.Habitaciones.Count(h => h.Bloqueada)} habitaciones bloqueadas", "Danger"), new("Stock bajo", SampleData.Productos.Count(p => p.Stock <= p.StockMinimo).ToString(), "Productos por reponer", "Warning")];
    }

    public IReadOnlyList<DashboardAlert> AlertasOperacion() =>
        [.. SampleData.Incidencias.Where(i => i.Prioridad == "Alta").Take(3).Select(i => { var room = SampleData.Habitaciones.First(r => r.Id == i.Habitacion); return new DashboardAlert(i.Descripcion, $"Habitación {room.Numero} · Piso {room.Piso}", i.Estado, "Revisar", "Mantenimiento"); }),
         .. SampleData.Productos.Where(p => p.Stock <= p.StockMinimo).Take(3).Select(p => new DashboardAlert(p.Nombre, $"Existencia: {p.Stock} · mínimo {p.StockMinimo}", "Reponer", "Ver inventario", "Productos e inventario"))];

    public IReadOnlyList<DashboardMetric> Administracion() =>
        [new("Empleados", SampleData.Empleados.Count.ToString(), "Personal registrado"), new("Cuentas activas", SampleData.Usuarios.Count(u => u.Activo).ToString(), "De cuatro roles"), new("Cuentas inactivas", SampleData.Usuarios.Count(u => !u.Activo).ToString(), "Revisión de acceso", "Warning"), new("Habitaciones", SampleData.Habitaciones.Count.ToString(), "En cinco pisos"), new("Módulos", "12", "Configuración del hotel")];

    public DashboardExecutiveSummary Gerencia(DashboardPeriod period)
    {
        var start = period.Desde.Date;
        var end = period.Hasta.Date.AddDays(1);
        var previousStart = period.ComparacionDesde.Date;
        var previousEnd = period.ComparacionHasta.Date.AddDays(1);
        bool InRange(DateTime value, DateTime from, DateTime to) => value >= from && value < to;

        var posted = SampleData.Cargos.Where(charge => !charge.Anulado && charge.FolioId is int folioId && SampleData.Folios.Any(folio => folio.Id == folioId));
        var currentCharges = posted.Where(charge => InRange(charge.Fecha, start, end)).ToList();
        var previousCharges = posted.Where(charge => InRange(charge.Fecha, previousStart, previousEnd)).ToList();
        var serviceChargeIds = SampleData.ServiciosAdquiridos.Where(item => item.CargoId.HasValue).Select(item => item.CargoId!.Value)
            .Concat(SampleData.SolicitudesServicio.Where(item => item.CargoId.HasValue).Select(item => item.CargoId!.Value)).ToHashSet();
        var resourceChargeIds = SampleData.ReservaRecursos.Where(item => item.CargoId.HasValue).Select(item => item.CargoId!.Value).ToHashSet();
        var income = currentCharges.Sum(charge => charge.Monto);
        var previousIncome = previousCharges.Sum(charge => charge.Monto);
        var payments = SampleData.Pagos.Where(payment => payment.Estado != "Anulado" && InRange(payment.Fecha, start, end)).Sum(payment => payment.Monto);
        var refunds = SampleData.Devoluciones.Where(item => item.Ejecutada is DateTime done && InRange(done, start, end)).Sum(item => item.Monto);
        var voided = SampleData.Anulaciones.Where(item => item.Ejecutada is DateTime done && InRange(done, start, end)).Sum(item => SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId)?.Monto ?? 0m);
        var roomNights = OccupiedRoomNights(start, end);
        var previousNights = OccupiedRoomNights(previousStart, previousEnd);
        var availableRoomNights = Math.Max(0, (end - start).Days * SampleData.Habitaciones.Count);
        var previousAvailable = Math.Max(0, (previousEnd - previousStart).Days * SampleData.Habitaciones.Count);
        var occupancy = availableRoomNights == 0 ? 0 : (decimal)roomNights * 100m / availableRoomNights;
        var previousOccupancy = previousAvailable == 0 ? 0 : (decimal)previousNights * 100m / previousAvailable;
        var reservations = SampleData.Reservas.Where(item => item.FechaCreacion is DateTime created && InRange(created, start, end) && item.Estado != "Cancelada").ToList();
        var previousReservations = SampleData.Reservas.Count(item => item.FechaCreacion is DateTime created && InRange(created, previousStart, previousEnd) && item.Estado != "Cancelada");
        var serviceRevenue = currentCharges.Where(charge => IsServiceCharge(charge) || serviceChargeIds.Contains(charge.Id)).Sum(charge => charge.Monto);
        var previousServiceRevenue = previousCharges.Where(charge => IsServiceCharge(charge) || serviceChargeIds.Contains(charge.Id)).Sum(charge => charge.Monto);

        string Compare(decimal current, decimal previous, string format = "N0") => !period.IncluirComparacion ? "Comparación desactivada" : previous == 0
            ? (current == 0 ? "Sin variación frente al período anterior" : "Sin base comparable en el período anterior")
            : $"{(current - previous) / Math.Abs(previous):+0.0%;-0.0%;0.0%} frente al período anterior";
        string CompareCount(int current, int previous) => !period.IncluirComparacion ? "Comparación desactivada" : previous == 0 ? (current == 0 ? "Sin variación frente al período anterior" : "Sin base comparable en el período anterior") : $"{(decimal)(current - previous) / previous:+0.0%;-0.0%;0.0%} frente al período anterior";

        var categories = new[] { "Alojamiento", "Paquetes", "Servicios", "Alimentos y bebidas", "Recursos", "Otros" }
            .Select(name =>
            {
                var items = currentCharges.Where(charge => CategorizeCharge(charge, serviceChargeIds, resourceChargeIds) == name).ToList();
                return new DashboardCategory(name, items.Sum(charge => charge.Monto), income == 0 ? 0 : (double)(items.Sum(charge => charge.Monto) * 100m / income), items.Count);
            }).ToList();
        var decisions = new List<DashboardDecision>();
        decisions.AddRange(SampleData.Devoluciones.Where(item => item.Estado == "Pendiente").Select(item =>
        {
            var payment = SampleData.Pagos.FirstOrDefault(value => value.Id == item.PagoId);
            var folio = payment is null ? null : SampleData.Folios.FirstOrDefault(value => value.Id == payment.FolioId);
            var guestId = folio?.HuespedPrincipalId ?? (folio?.EstanciaId is int stay ? SampleData.Estancias.FirstOrDefault(value => value.Id == stay)?.HuespedId : null);
            return new DashboardDecision(item.Id, "Devolución", SampleData.Huespedes.FirstOrDefault(value => value.Id == guestId)?.Nombre ?? "Huésped por identificar", item.Monto, item.Solicitada, item.Motivo, item.Estado);
        }));
        decisions.AddRange(SampleData.Anulaciones.Where(item => item.Estado == "Pendiente").Select(item =>
        {
            var payment = SampleData.Pagos.FirstOrDefault(value => value.Id == item.PagoId);
            var folio = payment is null ? null : SampleData.Folios.FirstOrDefault(value => value.Id == payment.FolioId);
            var guestId = folio?.HuespedPrincipalId ?? (folio?.EstanciaId is int stay ? SampleData.Estancias.FirstOrDefault(value => value.Id == stay)?.HuespedId : null);
            return new DashboardDecision(item.Id, "Anulación", SampleData.Huespedes.FirstOrDefault(value => value.Id == guestId)?.Nombre ?? "Huésped por identificar", payment?.Monto ?? 0m, item.Solicitada, item.Motivo, item.Estado);
        }));
        var arrivals = SampleData.Reservas.Count(item => item.Estado != "Cancelada" && item.Llegada.Date == DateTime.Today);
        var departures = SampleData.Reservas.Count(item => item.Estado != "Cancelada" && item.Salida.Date == DateTime.Today);
        var blockedRooms = SampleData.Habitaciones.Count(item => item.Bloqueada);
        var lowStock = SampleData.Productos.Count(item => item.Stock <= item.StockMinimo);
        var openTasks = SampleData.Tareas.Count(item => item.Estado is "Pendiente" or "En progreso");
        var openIncidents = SampleData.Incidencias.Count(item => item.Estado != "Resuelta");
        var serviceUses = SampleData.UsosServicio.Count(item => InRange(item.Fecha, start, end) && item.Estado == "Completado");
        var resourcesInPeriod = SampleData.ReservaRecursos.Where(item => SampleData.Reservas.FirstOrDefault(reservation => reservation.Id == item.ReservaId)?.FechaCreacion is DateTime created && InRange(created, start, end)).ToList();
        var resources = resourcesInPeriod.Count;
        var deliveredResources = resourcesInPeriod.Count(item => item.Estado == "ENTREGADO");
        var pendingResources = resourcesInPeriod.Count(item => item.Estado is "PENDIENTE" or "SIN_VERIFICAR");
        var resourceIncome = currentCharges.Where(item => resourceChargeIds.Contains(item.Id)).Sum(item => item.Monto);
        var packageSales = SampleData.ReservaPaquetes.Where(item => SampleData.Reservas.FirstOrDefault(reservation => reservation.Id == item.ReservaId)?.FechaCreacion is DateTime created && InRange(created, start, end)).Sum(item => item.Cantidad);
        var expiringAgreements = SampleData.Convenios.Count(item => item.Activo && item.VigenciaDesde.Date <= DateTime.Today && item.VigenciaHasta.Date >= DateTime.Today && item.VigenciaHasta.Date <= DateTime.Today.AddDays(90));
        var futureOccupancy = UpcomingOccupancy(DateTime.Today, 7);
        var currentFolioBalance = new FolioService().CalcularSaldo();
        var alerts = AlertasOperacion().Take(6).ToList();
        var operations = new List<DashboardMetric>
        {
            new("Llegadas hoy", arrivals.ToString(), "Reservas con llegada prevista"),
            new("Salidas hoy", departures.ToString(), "Reservas con salida prevista"),
            new("Ocupación próximos 7 días", $"{futureOccupancy:N1}%", "Asignaciones confirmadas y estancias activas", "Information"),
            new("Tareas de habitaciones", openTasks.ToString(), "Pendientes o en progreso", openTasks > 0 ? "Warning" : "Success"),
            new("Incidencias abiertas", openIncidents.ToString(), $"{blockedRooms} habitación(es) bloqueada(s)", openIncidents > 0 ? "Warning" : "Success"),
            new("Artículos bajo mínimo", lowStock.ToString(), "Revisión de inventario", lowStock > 0 ? "Warning" : "Success")
        };
        var commercial = new List<DashboardMetric>
        {
            new("Paquetes adquiridos", packageSales.ToString(), "Unidades registradas por reserva"),
            new("Servicios completados", serviceUses.ToString(), "Usos; no equivale a ingresos"),
            new("Recursos solicitados", resources.ToString(), $"{deliveredResources} entregados · {pendingResources} pendientes"),
            new("Ingresos por recursos", $"Bs {resourceIncome:N2}", "Cargos vinculados a su solicitud"),
            new("Convenios por vencer", expiringAgreements.ToString(), "Dentro de los próximos 90 días")
        };
        var metrics = new List<DashboardMetric>
        {
            new("Cargos netos registrados", $"Bs {income:N2}", Compare(income, previousIncome), "Information"),
            new("Ocupación promedio", $"{occupancy:N1}%", Compare(occupancy, previousOccupancy), "Success"),
            new("Reservas registradas", reservations.Count.ToString(), CompareCount(reservations.Count, previousReservations), "Neutral"),
            new("Ingresos por servicios", $"Bs {serviceRevenue:N2}", Compare(serviceRevenue, previousServiceRevenue), "Information")
        };
        return new DashboardExecutiveSummary(period, metrics, BuildTrend(currentCharges, start, end), categories, decisions.OrderBy(item => item.Fecha).ToList(), operations, alerts, commercial,
            reservations.GroupBy(item => string.IsNullOrWhiteSpace(item.Canal) ? "Sin canal" : item.Canal).Select(group => new DashboardBar(group.Key, group.Count(), 0, $"{group.Count()} reservas")).OrderByDescending(item => item.Valor).ToList(),
            income, payments, refunds, voided, reservations.Count, occupancy, arrivals, departures, blockedRooms, lowStock, packageSales, serviceUses, resources, deliveredResources, pendingResources, resourceIncome, expiringAgreements, currentFolioBalance,
            "Suma de cargos no anulados, registrados en folios existentes dentro de las fechas seleccionadas. No representa cobros recibidos.",
            "Cuenta reservas con fecha de creación registrada; se excluyen las canceladas.",
            "Noches de habitación asignadas ÷ noches de habitación instaladas. El historial de bloqueos no está disponible.");
    }

    public static DashboardPeriod PeriodoPredeterminado(DateTime? now = null)
    {
        var today = (now ?? DateTime.Today).Date;
        var start = new DateTime(today.Year, today.Month, 1);
        var previousStart = start.AddDays(-1).AddDays(-today.Day + 1);
        var previousEnd = start.AddDays(-1);
        return new DashboardPeriod("Este mes", start, today, previousStart, previousEnd);
    }

    private static bool IsServiceCharge(CargoModel charge) => charge.Concepto.StartsWith("Servicio adquirido", StringComparison.OrdinalIgnoreCase)
        || charge.Concepto.StartsWith("Servicio ·", StringComparison.OrdinalIgnoreCase)
        || charge.Concepto.StartsWith("Servicio solicitado", StringComparison.OrdinalIgnoreCase)
        || charge.Concepto.StartsWith("Servicio de habitación", StringComparison.OrdinalIgnoreCase);

    private static string CategorizeCharge(CargoModel charge, IReadOnlySet<int> serviceChargeIds, IReadOnlySet<int> resourceChargeIds)
    {
        var concept = charge.Concepto;
        if (concept.StartsWith("Alojamiento", StringComparison.OrdinalIgnoreCase) || concept.StartsWith("Estancia", StringComparison.OrdinalIgnoreCase)) return "Alojamiento";
        if (concept.StartsWith("Paquete", StringComparison.OrdinalIgnoreCase)) return "Paquetes";
        if (serviceChargeIds.Contains(charge.Id) || IsServiceCharge(charge)) return "Servicios";
        if (resourceChargeIds.Contains(charge.Id)) return "Recursos";
        if (concept.StartsWith("Consumo", StringComparison.OrdinalIgnoreCase) || concept.StartsWith("Alimento", StringComparison.OrdinalIgnoreCase) || concept.StartsWith("Bebida", StringComparison.OrdinalIgnoreCase)) return "Alimentos y bebidas";
        if (concept.StartsWith("Recurso", StringComparison.OrdinalIgnoreCase)) return "Recursos";
        return "Otros";
    }

    private static int OccupiedRoomNights(DateTime start, DateTime end)
    {
        var occupied = new HashSet<(DateTime Night, int Room)>();
        foreach (var link in SampleData.ReservaHabitaciones)
        {
            var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == link.ReservaId && item.Estado is "En estancia" or "Finalizada");
            if (reservation is null || !SampleData.Habitaciones.Any(item => item.Id == link.HabitacionId)) continue;
            var from = reservation.Llegada.Date > start.Date ? reservation.Llegada.Date : start.Date;
            var to = reservation.Salida.Date < end.Date ? reservation.Salida.Date : end.Date;
            for (var night = from; night < to; night = night.AddDays(1)) occupied.Add((night, link.HabitacionId));
        }
        return occupied.Count;
    }

    private static decimal UpcomingOccupancy(DateTime start, int nights)
    {
        var occupied = new HashSet<(DateTime Night, int Room)>();
        var end = start.Date.AddDays(nights);
        foreach (var link in SampleData.ReservaHabitaciones)
        {
            var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == link.ReservaId && item.Estado is "Confirmada" or "En estancia");
            if (reservation is null) continue;
            var from = reservation.Llegada.Date > start.Date ? reservation.Llegada.Date : start.Date;
            var to = reservation.Salida.Date < end ? reservation.Salida.Date : end;
            for (var night = from; night < to; night = night.AddDays(1)) occupied.Add((night, link.HabitacionId));
        }
        var denominator = Math.Max(1, nights * SampleData.Habitaciones.Count);
        return (decimal)occupied.Count * 100m / denominator;
    }

    private static IReadOnlyList<DashboardBar> BuildTrend(IReadOnlyList<CargoModel> charges, DateTime start, DateTime end)
    {
        var days = Math.Max(1, (end.Date - start.Date).Days);
        var bucketDays = days <= 7 ? 1 : 7;
        var buckets = days <= 31
            ? Enumerable.Range(0, (days + bucketDays - 1) / bucketDays).Select(offset =>
            {
                var from = start.Date.AddDays(offset * bucketDays);
                var to = from.AddDays(bucketDays) > end.Date ? end.Date : from.AddDays(bucketDays);
                var label = bucketDays == 1 ? from.ToString("dd MMM") : $"{from:dd}–{to.AddDays(-1):dd MMM}";
                return (From: from, To: to, Label: label);
            }).ToList()
            : Enumerable.Range(0, Math.Max(1, (end.Year - start.Year) * 12 + end.Month - start.Month + 1)).Select(offset =>
            {
                var from = new DateTime(start.Year, start.Month, 1).AddMonths(offset);
                return (From: from < start ? start : from, To: from.AddMonths(1) > end ? end : from.AddMonths(1), Label: from.ToString("MMM yy"));
            }).ToList();
        var amounts = buckets.Select(bucket => charges.Where(charge => charge.Fecha >= bucket.From && charge.Fecha < bucket.To).Sum(charge => charge.Monto)).ToList();
        var max = Math.Max(1m, amounts.DefaultIfEmpty(0m).Max());
        return buckets.Select((bucket, index) => new DashboardBar(bucket.Label, amounts[index], (double)(amounts[index] * 100m / max), $"Bs {amounts[index]:N2}")).ToList();
    }
}
