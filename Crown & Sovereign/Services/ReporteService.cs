using System.Data;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Generates temporary reports from the same domain collections and calculations used by the hotel workspaces.</summary>
public sealed class ReporteService
{
    private static readonly List<ResultadoReporte> _recientes = [];
    public static IReadOnlyList<DefinicionReporte> Catalogo { get; } =
    [
        new("ocupacion", "Ocupación hotelera", "Alojamiento y reservas", "Noches ocupadas y capacidad instalada por periodo.", "Estancias registradas; no reconstruye estados de habitación inexistentes."),
        new("reservas", "Reservas y estancias", "Alojamiento y reservas", "Altas, llegadas, salidas, estancias y cancelaciones.", "Cada indicador usa su fecha de evento correspondiente."),
        new("disponibilidad", "Disponibilidad y bloqueos", "Alojamiento y reservas", "Estado actual de habitaciones y bloqueos vigentes.", "Fotografía actual; no constituye un historial diario de bloqueos."),
        new("cambios", "Cambios de habitación", "Alojamiento y reservas", "Traslados de habitación registrados.", "El prototipo no conserva historial persistente completo de cambios."),
        new("ingresos", "Ingresos y cobros", "Finanzas", "Cargos registrados, pagos y correcciones ejecutadas.", "Cargos, cobros e invoices permanecen separados."),
        new("saldos", "Saldos pendientes", "Finanzas", "Saldo actual por folio, sin antigüedad inventada.", "Es estado actual, no saldo histórico del periodo."),
        new("facturacion", "Facturación y notas de crédito", "Finanzas", "Documentos emitidos y notas de crédito.", "Usa importes de documentos conservados."),
        new("correcciones", "Devoluciones y anulaciones", "Finanzas", "Solicitudes, autorizaciones y ejecuciones financieras.", "Solo lo ejecutado afecta cobros netos."),
        new("caja", "Cierres de caja", "Finanzas", "Turnos, arqueos y diferencias físicas.", "Apertura y medios no efectivo no son ingresos físicos."),
        new("tarifas", "Rendimiento de tarifas y canales", "Comercial", "Reservas, noches y precios acordados por canal.", "No recalcula reservas antiguas con precios actuales."),
        new("paquetes", "Paquetes y promociones", "Comercial", "Paquetes comprados y uso de beneficios registrados.", "Descuentos históricos solo si se preservaron."),
        new("servicios", "Utilización de servicios", "Comercial", "Solicitudes, citas, adquisiciones y usos.", "Una adquisición y su cumplimiento no son dos ventas."),
        new("convenios", "Convenios y proveedores", "Comercial", "Uso externo, costos acordados y vencimientos.", "Costo aplicado no equivale a pago al proveedor."),
        new("limpieza", "Limpieza e inspecciones", "Operaciones", "Tareas, estados e inspecciones registradas.", "Sin marcas reales de inicio/fin no se calcula duración."),
        new("mantenimiento", "Mantenimiento e incidencias", "Operaciones", "Incidencias, órdenes y bloqueos actuales.", "No se calculan gastos ni duración de bloqueo sin historia."),
        new("inventario", "Inventario y consumos", "Operaciones", "Existencias actuales, movimientos y consumos.", "No reconstruye saldo histórico sin balance inicial verificable."),
        new("recursos", "Recursos adicionales", "Operaciones", "Asignaciones, disponibilidad y devoluciones pendientes.", "Las asignaciones actuales son por reserva."),
        new("olvidados", "Objetos olvidados", "Operaciones", "Objetos registrados y estado actual.", "La custodia histórica completa no está disponible.")
    ];

    public static IReadOnlyList<ResultadoReporte> Recientes => _recientes.ToList();
    public static IReadOnlyList<string> Periodos { get; } = ["Hoy", "Ayer", "Últimos 7 días", "Últimos 30 días", "Últimos 90 días", "Este mes", "Mes anterior", "Este trimestre", "Este año", "Próximos 7 días", "Próximos 30 días", "Periodo personalizado"];

    public IReadOnlyList<ResultadoReporte> Generar(ParametrosReporte parametros)
    {
        Autorizar();
        if (parametros.Hasta.Date < parametros.Desde.Date) throw new InvalidOperationException("La fecha final debe ser igual o posterior a la fecha inicial.");
        if (parametros.Hasta.Date > DateTime.Today && parametros.Id is "ingresos" or "facturacion" or "correcciones" or "caja" or "limpieza" or "mantenimiento")
            throw new InvalidOperationException("Los reportes de actividad histórica no admiten fechas futuras.");
        var def = Catalogo.FirstOrDefault(item => item.Id == parametros.Id) ?? throw new InvalidOperationException("Selecciona un reporte válido.");
        var result = Construir(def, parametros);
        if (parametros.Comparar)
        {
            var length = (parametros.Hasta.Date - parametros.Desde.Date).Days + 1;
            var previousEnd = parametros.Desde.Date.AddDays(-1);
            var previousStart = previousEnd.AddDays(-length + 1);
            var priorParams = parametros with { Desde = previousStart, Hasta = previousEnd, Comparar = false, Periodo = "Periodo anterior" };
            var prior = Construir(def, priorParams);
            var previousKpis = prior.Indicadores.ToDictionary(item => item.Nombre, StringComparer.OrdinalIgnoreCase);
            var comparison = result.Indicadores.Select(current =>
            {
                previousKpis.TryGetValue(current.Nombre, out var old);
                if (current.ValorNumerico is decimal now && old?.ValorNumerico is decimal before)
                {
                    var delta = now - before;
                    var pct = before == 0 ? "No aplicable" : (delta / Math.Abs(before)).ToString("+0.0%;-0.0%;0.0%", System.Globalization.CultureInfo.GetCultureInfo("es-BO"));
                    return new IndicadorReporte(current.Nombre, old.Valor, $"Periodo anterior · variación {delta:+0.##;-0.##;0} · {pct}") { ValorNumerico = before };
                }
                return new IndicadorReporte(current.Nombre, old?.Valor ?? "No disponible", "Comparación no disponible para este indicador.");
            }).ToList();
            result = result with { PeriodoComparacion = $"{previousStart:dd/MM/yyyy} – {previousEnd:dd/MM/yyyy}", Comparacion = comparison };
        }
        _recientes.Insert(0, result);
        if (_recientes.Count > 20) _recientes.RemoveAt(_recientes.Count - 1);
        return _recientes.ToList();
    }

    public ResultadoReporte Construir(DefinicionReporte def, ParametrosReporte p)
    {
        var table = CreateTable();
        var kpis = new List<IndicadorReporte>(); var chart = new List<PuntoReporte>(); var warnings = new List<string>();
        var start = p.Desde.Date; var end = p.Hasta.Date.AddDays(1);
        bool InRange(DateTime value) => value >= start && value < end;
        string method = "Filas consultadas en las colecciones de dominio existentes; el reporte no modifica registros.";
        switch (def.Id)
        {
            case "ocupacion":
            {
                var rooms = FilterRooms(p).ToList(); var physical = (decimal)rooms.Count * ((p.Hasta.Date - p.Desde.Date).Days + 1);
                decimal occupied = 0;
                var rowsByType = new Dictionary<string, (decimal occupied, decimal available)>();
                foreach (var room in rooms)
                {
                    var intervals = new List<(DateTime Start, DateTime End)>();
                    var assignments = SampleData.ReservaHabitaciones.Where(x => x.HabitacionId == room.Id).ToList();
                    var stays = SampleData.Estancias.Where(stay => stay.Estado is "Activa" or "Finalizada")
                        .Where(stay => assignments.Count == 0 ? SampleData.Reservas.FirstOrDefault(r => r.Id == stay.ReservaId)?.Habitacion == room.Id : assignments.Any(a => a.ReservaId == stay.ReservaId));
                    foreach (var stay in stays)
                    {
                        var from = stay.CheckIn.Date > start ? stay.CheckIn.Date : start;
                        var stayEnd = (stay.CheckOutReal ?? (stay.Estado == "Activa" ? DateTime.Today.AddDays(1) : stay.CheckOut)).Date;
                        var to = stayEnd < end.Date ? stayEnd : end.Date;
                        if (to > from) intervals.Add((from, to));
                    }
                    decimal nights = 0;
                    DateTime? mergedStart = null, mergedEnd = null;
                    foreach (var interval in intervals.OrderBy(x => x.Start).ThenBy(x => x.End))
                    {
                        if (mergedStart is null) { mergedStart = interval.Start; mergedEnd = interval.End; continue; }
                        if (interval.Start <= mergedEnd)
                        {
                            if (interval.End > mergedEnd) mergedEnd = interval.End;
                        }
                        else
                        {
                            nights += (mergedEnd!.Value - mergedStart.Value).Days;
                            mergedStart = interval.Start; mergedEnd = interval.End;
                        }
                    }
                    if (mergedStart is not null) nights += (mergedEnd!.Value - mergedStart.Value).Days;
                    occupied += nights;
                    var old = rowsByType.GetValueOrDefault(room.Tipo);
                    rowsByType[room.Tipo] = (old.occupied + nights, old.available + (end.Date - start).Days);
                }
                foreach (var pair in rowsByType.OrderBy(x => x.Key)) Add(table, pair.Key, pair.Value.occupied, pair.Value.available, Percent(pair.Value.occupied, pair.Value.available));
                var pct = Percent(occupied, physical); kpis.Add(Kpi("Noches ocupadas", occupied)); kpis.Add(Kpi("Noches habitación instaladas", physical)); kpis.Add(Kpi("Ocupación registrada", pct, "%"));
                kpis.Add(Kpi("Reservas del periodo", SampleData.Reservas.Count(r => r.Estado != "Cancelada" && r.Llegada.Date < end.Date && r.Salida.Date > start), "reservas; indicador distinto de noches ocupadas"));
                foreach (var pair in rowsByType) chart.Add(new(pair.Key, pair.Value.occupied, "Noches ocupadas"));
                method = "Ocupación registrada derivada de estancias activas/finalizadas y asignaciones de habitación: noches de estancia que intersectan el periodo ÷ habitaciones filtradas × noches calendario. Intervalo de alojamiento [check-in, check-out); no cuenta salida como noche. El sistema no dispone de estados históricos diarios de habitación.";
                warnings.Add("Noches bloqueadas históricas no disponibles; se usa capacidad física instalada y no se descuentan bloqueos históricos.");
                warnings.Add("Las estancias de demostración o sin asignación habitación-reserva verificable pueden quedar excluidas.");
                if (physical == 0) warnings.Add("No aplicable: el denominador del periodo es cero.");
                break;
            }
            case "reservas":
            {
                var rows = SampleData.Reservas.Where(r => (string.IsNullOrWhiteSpace(p.Canal) || p.Canal == "Todos" || r.Canal == p.Canal)
                    && (r.Habitacion == 0 || RoomMatches(r.Habitacion, p)));
                var created = rows.Where(r => r.FechaCreacion is DateTime createdAt && InRange(createdAt)).ToList();
                var arrivals = rows.Where(r => r.Estado != "Cancelada" && InRange(r.Llegada)).ToList();
                var departures = rows.Where(r => r.Estado != "Cancelada" && InRange(r.Salida)).ToList();
                var checkedIn = SampleData.Estancias.Where(x => InRange(x.CheckIn)).ToList();
                var checkedOut = SampleData.Estancias.Where(x => x.CheckOutReal is DateTime done && InRange(done)).ToList();
                foreach (var r in created) Add(table, $"Reserva #{r.Id}", r.Huesped, r.Estado, r.Canal, r.Llegada, r.Salida, r.FechaCreacion?.ToString("dd/MM/yyyy") ?? "Sin fecha");
                kpis.Add(Kpi("Reservas creadas", created.Count)); kpis.Add(Kpi("Ingresos previstos", arrivals.Count)); kpis.Add(Kpi("Salidas previstas", departures.Count));
                kpis.Add(Kpi("Check-in real", checkedIn.Count)); kpis.Add(Kpi("Check-out real", checkedOut.Count)); kpis.Add(Kpi("Estancias activas", SampleData.Estancias.Count(x => x.Estado == "Activa")));
                kpis.Add(Kpi("Canceladas", rows.Count(x => x.Estado == "Cancelada" && InRange(x.FechaCreacion ?? x.Llegada)))); kpis.Add(Kpi("Grupos", created.Count(x => x.GrupoId.HasValue)));
                method = "Creación, llegada, salida y movimientos reales usan fechas distintas. Check-out real usa CheckOutReal. No se deduce inasistencia de una reserva pasada.";
                warnings.Add("Cancelaciones sin fecha de evento no se atribuyen con certeza; las mostradas usan fecha de creación como referencia declarada.");
                break;
            }
            case "disponibilidad":
                foreach (var room in FilterRooms(p)) Add(table, room.Numero, room.Piso, room.Tipo, room.Estado, room.Limpieza, room.Bloqueada ? "Sí" : "No", room.Huesped ?? "—");
                kpis.Add(Kpi("Habitaciones físicas", FilterRooms(p).Count())); kpis.Add(Kpi("Ocupadas ahora", FilterRooms(p).Count(r => r.Estado.Equals("Ocupada", StringComparison.OrdinalIgnoreCase))));
                kpis.Add(Kpi("Bloqueadas ahora", FilterRooms(p).Count(r => r.Bloqueada))); kpis.Add(Kpi("Limpias ahora", FilterRooms(p).Count(r => r.Limpieza.Equals("Limpia", StringComparison.OrdinalIgnoreCase))));
                method = $"Estado actual observado {DateTime.Now:dd/MM/yyyy HH:mm}; las fechas del filtro no recrean el estado histórico."; warnings.Add("No hay intervalos históricos completos de bloqueo para este reporte."); break;
            case "cambios":
                foreach (var change in SampleData.CambiosHabitacion.Where(x => InRange(x.Fecha))) Add(table, change.Id, change.Fecha, change.ReservaId, change.EstanciaId,
                    RoomNumber(change.HabitacionOrigenId), RoomNumber(change.HabitacionDestinoId), change.Motivo);
                kpis.Add(Kpi("Cambios registrados", table.Rows.Count));
                method = "Consulta los registros disponibles en memoria.";
                if (table.Rows.Count == 0) warnings.Add("No hay historial de cambios disponible; no se interpreta como que no ocurrieron cambios.");
                break;
            case "ingresos":
            {
                var finance = new FinancialManagementService(); var period = new FinancialPeriod(p.Periodo, start, end, start, end);
                var details = finance.Movimientos(period).Where(x => p.MetodoPago is "" or "Todos" || x.Method == p.MetodoPago).OrderBy(x => x.Date).ToList();
                foreach (var x in details) Add(table, x.Date, x.Kind, x.Guest, x.FolioId, x.Method, x.Amount, x.Concept, x.Status);
                var charges = SampleData.Cargos.Where(x => !x.Anulado && x.FolioId is int id && SampleData.Folios.Any(f => f.Id == id) && InRange(x.Fecha)).Sum(x => x.Monto);
                var gross = SampleData.Pagos.Where(x => InRange(x.Fecha) && (p.MetodoPago is "" or "Todos" || x.Metodo == p.MetodoPago)).Sum(x => x.Monto);
                var refunds = SampleData.Devoluciones.Where(x => x.Estado == "Ejecutada" && x.Ejecutada is DateTime d && InRange(d) && (p.MetodoPago is "" or "Todos" || x.MetodoEjecucion == p.MetodoPago)).Sum(x => x.Monto);
                var voids = SampleData.Anulaciones.Where(x => x.Estado == "Ejecutada" && x.Ejecutada is DateTime d && InRange(d)).Sum(x => SampleData.Pagos.FirstOrDefault(pay => pay.Id == x.PagoId)?.Monto ?? 0m);
                kpis.Add(Kpi("Cargos registrados", charges, "Bs")); kpis.Add(Kpi("Cobros brutos", gross, "Bs")); kpis.Add(Kpi("Devoluciones ejecutadas", refunds, "Bs")); kpis.Add(Kpi("Anulaciones ejecutadas", voids, "Bs"));
                kpis.Add(Kpi("Cobros netos", gross - refunds - voids, "Bs")); method = "Los cargos usan Cargo.Fecha; pagos usan Pago.Fecha; devoluciones y anulaciones se atribuyen a su fecha de ejecución. Solo se descuentan correcciones ejecutadas. Cargos, cobros y documentos son magnitudes distintas.";
                if (p.MetodoPago is not ("" or "Todos")) warnings.Add("El filtro por método se aplica a cobros y devoluciones; los cargos no tienen método de pago."); break;
            }
            case "saldos":
            {
                var rows = new FinancialManagementService().Saldos(state: "Todos").Where(x => string.IsNullOrWhiteSpace(p.Canal) || p.Canal == "Todos" || (x.ReservationId is int id && SampleData.Reservas.FirstOrDefault(r => r.Id == id)?.Canal == p.Canal)).ToList();
                foreach (var x in rows) Add(table, x.FolioId, x.Guest, x.Context, x.Status, x.Charges, x.Collections, x.Refunds, x.Balance);
                kpis.Add(Kpi("Saldo positivo", rows.Where(x => x.Balance > 0).Sum(x => x.Balance), "Bs; estado actual")); kpis.Add(Kpi("Folios con saldo positivo", rows.Count(x => x.Balance > 0)));
                kpis.Add(Kpi("Créditos a favor", rows.Where(x => x.Balance < 0).Sum(x => Math.Abs(x.Balance)), "Bs; estado actual")); kpis.Add(Kpi("Folios cerrados con saldo", rows.Count(x => x.Status != "Abierto" && x.Balance != 0)));
                method = "Saldo vigente calculado mediante FolioService; no es una fotografía histórica del periodo. Un saldo positivo no se etiqueta como deuda vencida sin vencimiento configurado."; break;
            }
            case "facturacion":
                foreach (var f in SampleData.Facturas.Where(x => InRange(x.Fecha)))
                {
                    var credits = SampleData.NotasCredito.Where(x => x.FacturaId == f.Id && InRange(x.Fecha)).Sum(x => x.Monto);
                    Add(table, f.Numero, f.Fecha, f.Estado, f.Cliente, f.Subtotal, f.Impuesto, f.Total, credits, f.Total - credits);
                }
                kpis.Add(Kpi("Facturas emitidas", SampleData.Facturas.Count(x => InRange(x.Fecha)))); kpis.Add(Kpi("Total facturado", SampleData.Facturas.Where(x => InRange(x.Fecha)).Sum(x => x.Total), "Bs"));
                kpis.Add(Kpi("Notas de crédito del periodo", SampleData.NotasCredito.Where(x => InRange(x.Fecha)).Sum(x => x.Monto), "Bs"));
                kpis.Add(Kpi("Netos documentados", SampleData.Facturas.Where(x => InRange(x.Fecha)).Sum(x => x.Total) - SampleData.NotasCredito.Where(x => InRange(x.Fecha)).Sum(x => x.Monto), "Bs"));
                method = "La actividad de notas de crédito se atribuye a la fecha de emisión. Se conservan los importes guardados en cada factura."; break;
            case "correcciones":
                foreach (var x in SampleData.Devoluciones.Where(x => InRange(x.Ejecutada ?? x.Solicitada))) Add(table, "Devolución", x.Id, x.PagoId, x.Monto, x.Estado, x.Solicitada, x.Ejecutada, x.Motivo);
                foreach (var x in SampleData.Anulaciones.Where(x => InRange(x.Ejecutada ?? x.Solicitada))) Add(table, "Anulación", x.Id, x.PagoId, SampleData.Pagos.FirstOrDefault(pay => pay.Id == x.PagoId)?.Monto ?? 0m, x.Estado, x.Solicitada, x.Ejecutada, x.Motivo);
                kpis.Add(Kpi("Solicitudes listadas", table.Rows.Count)); kpis.Add(Kpi("Ejecutadas", SampleData.Devoluciones.Count(x => x.Estado == "Ejecutada" && x.Ejecutada is DateTime d && InRange(d)) + SampleData.Anulaciones.Count(x => x.Estado == "Ejecutada" && x.Ejecutada is DateTime d && InRange(d))));
                kpis.Add(Kpi("Pendientes actuales", SampleData.Devoluciones.Count(x => x.Estado == "Pendiente") + SampleData.Anulaciones.Count(x => x.Estado == "Pendiente"), "estado actual"));
                method = "Actividad histórica por fecha de ejecución; solicitudes no ejecutadas se incluyen por su fecha de solicitud y se muestran con estado explícito."; break;
            case "caja":
                foreach (var shift in new FinancialManagementService().Turnos(state: "Todos").Where(x => InRange(x.Shift.Apertura)))
                    Add(table, shift.Shift.Id, shift.Cashier, shift.Shift.Apertura, shift.Shift.Cierre, shift.Shift.Estado, shift.Shift.MontoInicial,
                        shift.Closure?.Esperado ?? shift.CashExpected, shift.Closure?.Contado, shift.Closure?.Diferencia, shift.Closure?.FechaRevision is null ? "Pendiente de revisión" : "Revisado");
                kpis.Add(Kpi("Turnos iniciados", table.Rows.Count)); kpis.Add(Kpi("Cierres sin revisar", SampleData.CierresCaja.Count(x => x.FechaRevision is null)));
                kpis.Add(Kpi("Faltantes", SampleData.CierresCaja.Where(x => x.Diferencia < 0 && InRange(x.Fecha)).Sum(x => Math.Abs(x.Diferencia)), "Bs"));
                kpis.Add(Kpi("Sobrantes", SampleData.CierresCaja.Where(x => x.Diferencia > 0 && InRange(x.Fecha)).Sum(x => x.Diferencia), "Bs"));
                kpis.Add(Kpi("Diferencia neta", SampleData.CierresCaja.Where(x => InRange(x.Fecha)).Sum(x => x.Diferencia), "Bs"));
                method = "Reconciliación por turno. El efectivo esperado procede de CajaService; monto de apertura y cobros no efectivo se muestran separados de ingresos físicos."; break;
            case "tarifas":
                foreach (var group in SampleData.Reservas.Where(x => x.Estado != "Cancelada" && x.Llegada.Date < end.Date && x.Salida.Date > start).GroupBy(x => x.Canal))
                {
                    var nights = group.Sum(x => Math.Max(0, ((x.Salida.Date < end.Date ? x.Salida.Date : end.Date) - (x.Llegada.Date > start ? x.Llegada.Date : start)).Days));
                    var agreed = group.SelectMany(x => x.AlojamientoNochesAcordadas).Where(x => x.Noche.Date >= start && x.Noche.Date < end.Date).Sum(x => x.Precio);
                    Add(table, group.Key, group.Count(), nights, agreed, nights == 0 ? null : agreed / nights);
                    chart.Add(new(group.Key, nights, "Noches"));
                }
                kpis.Add(Kpi("Reservas", table.Rows.Cast<DataRow>().Sum(x => Convert.ToInt32(x[1]))));
                kpis.Add(Kpi("Noches de alojamiento", table.Rows.Cast<DataRow>().Sum(x => Convert.ToDecimal(x[2]))));
                kpis.Add(Kpi("Importe acordado", table.Rows.Cast<DataRow>().Sum(x => x[3] == DBNull.Value ? 0m : Convert.ToDecimal(x[3])), "Bs"));
                method = "Noches de reserva intersectadas con el periodo; importes solo desde snapshots de precio por noche conservados. Comisión es estimada según porcentaje configurado, nunca pago ejecutado.";
                warnings.Add("Si faltan snapshots de precio histórico, el importe acordado no está disponible; no se recalcula con tarifas actuales."); break;
            case "paquetes":
                foreach (var row in SampleData.ReservaPaquetes.Where(x => SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId) is { } r && InRange(r.FechaCreacion ?? r.Llegada)))
                {
                    var package = SampleData.Paquetes.FirstOrDefault(x => x.Id == row.PaqueteId);
                    Add(table, row.ReservaId, package?.Nombre ?? $"Paquete #{row.PaqueteId}", row.Cantidad, row.PrecioCompra, row.PrecioCompra * row.Cantidad);
                }
                kpis.Add(Kpi("Unidades adquiridas", SampleData.ReservaPaquetes.Where(x => SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId) is { } r && InRange(r.FechaCreacion ?? r.Llegada)).Sum(x => x.Cantidad)));
                kpis.Add(Kpi("Ingreso acordado", SampleData.ReservaPaquetes.Where(x => SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId) is { } r && InRange(r.FechaCreacion ?? r.Llegada)).Sum(x => x.Cantidad * x.PrecioCompra), "Bs"));
                kpis.Add(Kpi("Usos incluidos", SampleData.ReservaPaqueteServicios.Count(x => SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId) is { } r && InRange(r.FechaCreacion ?? r.Llegada))));
                kpis.Add(Kpi("Promociones asociadas", SampleData.Reservas.Count(x => x.PromocionId.HasValue && InRange(x.FechaCreacion ?? x.Llegada))));
                method = "Compras y precios acordados de ReservaPaquete; se excluyen usos del beneficio como venta adicional. La asociación de una promoción no demuestra el descuento monetario aplicado.";
                warnings.Add("No se estima el importe histórico de descuentos si la reserva no conservó ese valor."); break;
            case "servicios":
                foreach (var use in SampleData.UsosServicio.Where(x => InRange(x.Fecha) && (p.Categoria is "Todas" or "" || SampleData.Servicios.FirstOrDefault(s => s.Id == x.ServicioId)?.Categoria == p.Categoria)))
                    Add(table, use.Id, SampleData.Servicios.FirstOrDefault(s => s.Id == use.ServicioId)?.Nombre ?? "Servicio no disponible", use.Fecha, use.Estado, use.Incluido ? "Incluido" : "Separado", use.Cargo, use.CostoProveedorAplicado);
                kpis.Add(Kpi("Usos completados", SampleData.UsosServicio.Count(x => InRange(x.Fecha) && x.Estado == "Completado")));
                kpis.Add(Kpi("Solicitudes registradas", SampleData.SolicitudesServicio.Count(x => InRange(x.Solicitada)))); kpis.Add(Kpi("Citas programadas", SampleData.Citas.Count(x => InRange(x.Inicio) && x.Estado is not "Cancelada" and not "No presentado")));
                kpis.Add(Kpi("Citas canceladas", SampleData.Citas.Count(x => x.Estado == "Cancelada" && InRange(x.Inicio))));
                kpis.Add(Kpi("Compras de servicio", SampleData.ServiciosAdquiridos.Where(x => InRange(x.FechaRegistro)).Sum(x => x.Cantidad), "unidades adquiridas"));
                kpis.Add(Kpi("Cargos asociados a usos", SampleData.UsosServicio.Where(x => InRange(x.Fecha) && !x.Incluido).Sum(x => x.Cargo), "Bs; no suma la adquisición otra vez"));
                method = "Solicitudes por Solicitada, citas por Inicio, usos por Fecha y adquisiciones por FechaRegistro. Compra y cumplimiento se muestran en métricas separadas.";
                warnings.Add("Beneficios ilimitados no tienen un balance numérico restante. Costos del proveedor son registrados, no pagos liquidados."); break;
            case "convenios":
                foreach (var use in SampleData.UsosServicio.Where(x => InRange(x.Fecha) && x.ConvenioId is not null))
                {
                    var agreement = SampleData.Convenios.FirstOrDefault(c => c.Id == use.ConvenioId);
                    Add(table, use.Fecha, agreement?.Empresa ?? "Proveedor no identificado", SampleData.Servicios.FirstOrDefault(s => s.Id == use.ServicioId)?.Nombre ?? "—", use.PrecioAplicado, use.CostoProveedorAplicado, use.PrecioAplicado is decimal price && use.CostoProveedorAplicado is decimal cost ? price - cost : null);
                }
                kpis.Add(Kpi("Usos externos", SampleData.UsosServicio.Count(x => InRange(x.Fecha) && x.ConvenioId.HasValue)));
                kpis.Add(Kpi("Costo aplicado registrado", SampleData.UsosServicio.Where(x => InRange(x.Fecha) && x.ConvenioId.HasValue).Sum(x => x.CostoProveedorAplicado ?? 0m), "Bs; no implica pago"));
                kpis.Add(Kpi("Convenios próximos a vencer", SampleData.Convenios.Count(x => x.Activo && x.VigenciaHasta.Date >= DateTime.Today && x.VigenciaHasta.Date <= DateTime.Today.AddDays(90)), "estado actual"));
                method = "Usos y costos aplicados registrados; contribución directa se calcula solo cuando precio y costo aplicados existen. No se infieren pagos a proveedores.";
                warnings.Add("La base no registra de forma integral pagos a proveedores ni su liquidación."); break;
            case "limpieza":
                foreach (var task in SampleData.Tareas.Where(x => InRange(x.Hora) && (p.Empleado is "Todos" or "" || x.Empleado == p.Empleado)))
                    Add(table, task.Id, task.Habitacion, task.Piso, task.Tipo, task.Hora, task.Empleado, task.Estado, task.Prioridad, task.Observaciones);
                kpis.Add(Kpi("Tareas programadas", table.Rows.Count)); kpis.Add(Kpi("Completadas", SampleData.Tareas.Count(x => InRange(x.Hora) && x.Estado == "Completada")));
                kpis.Add(Kpi("Pendientes", SampleData.Tareas.Count(x => InRange(x.Hora) && x.Estado == "Pendiente"))); kpis.Add(Kpi("Sin asignación", SampleData.Tareas.Count(x => InRange(x.Hora) && string.IsNullOrWhiteSpace(x.Empleado))));
                kpis.Add(Kpi("Inspecciones aprobadas", SampleData.Inspecciones.Count(x => InRange(x.Fecha) && x.Aprobada))); kpis.Add(Kpi("Inspecciones rechazadas", SampleData.Inspecciones.Count(x => InRange(x.Fecha) && !x.Aprobada)));
                method = "Tareas por hora programada y estado registrado; inspecciones por fecha de inspección. La hora programada no se interpreta como vencimiento.";
                warnings.Add("No existen marcas reales de inicio/fin por tarea en el modelo; no se calcula duración media ni productividad."); break;
            case "mantenimiento":
                foreach (var incident in SampleData.Incidencias.Where(x => InRange(x.FechaReporte) || x.Estado != "Resuelta"))
                    Add(table, incident.Id, incident.Habitacion, incident.Categoria, incident.Prioridad, incident.FechaReporte, incident.Estado,
                        SampleData.Habitaciones.FirstOrDefault(r => r.Id == incident.Habitacion)?.Bloqueada == true ? "Bloqueo activo" : "Sin bloqueo activo", incident.Descripcion);
                kpis.Add(Kpi("Incidencias registradas", SampleData.Incidencias.Count(x => InRange(x.FechaReporte)))); kpis.Add(Kpi("Abiertas actualmente", SampleData.Incidencias.Count(x => x.Estado != "Resuelta")));
                kpis.Add(Kpi("Órdenes completas", SampleData.OrdenesMantenimiento.Count(x => x.Estado is "Completada" or "Completado")));
                kpis.Add(Kpi("Habitaciones bloqueadas ahora", SampleData.Habitaciones.Count(x => x.Bloqueada)));
                method = "Incidencias por fecha de reporte; las abiertas se conservan como estado actual. Bloqueo solo cuando Habitacion.Bloqueada es verdadero.";
                warnings.Add("Sin historial completo de resolución y bloqueo no se calculan duración de resolución, recurrencia histórica ni impacto comercial acumulado.");
                warnings.Add("Sin costos de mantenimiento estructurados; no se estima gasto."); break;
            case "inventario":
                foreach (var product in SampleData.Productos.Where(x => p.Categoria is "Todas" or "" || x.Categoria == p.Categoria))
                {
                    var movements = SampleData.MovimientosInventario.Where(x => x.ProductoId == product.Id && InRange(x.Fecha)).ToList();
                    var consumption = SampleData.Consumos.Where(x => x.ProductoId == product.Id && InRange(x.Fecha)).ToList();
                    Add(table, product.Id, product.Nombre, product.Categoria, product.Stock, product.StockMinimo, product.Stock <= 0 ? "Agotado" : product.Stock < product.StockMinimo ? "Bajo mínimo" : "Disponible",
                        movements.Where(x => x.Cantidad > 0).Sum(x => x.Cantidad), movements.Where(x => x.Cantidad < 0).Sum(x => Math.Abs(x.Cantidad)), consumption.Sum(x => x.Cantidad));
                }
                kpis.Add(Kpi("Productos bajo mínimo", SampleData.Productos.Count(x => x.Stock > 0 && x.Stock < x.StockMinimo)));
                kpis.Add(Kpi("Productos agotados", SampleData.Productos.Count(x => x.Stock <= 0)));
                kpis.Add(Kpi("Consumos registrados", SampleData.Consumos.Where(x => InRange(x.Fecha)).Sum(x => x.Cantidad), "unidades"));
                method = "Existencias mostradas como condición actual; movimientos y consumos se filtran por sus fechas de registro.";
                warnings.Add("No se presenta inventario histórico ni valoración al costo: el precio disponible es precio de venta y no hay balance inicial fiable."); break;
            case "recursos":
                foreach (var allocation in SampleData.ReservaRecursos.Where(x => x.Estado is "ENTREGADO" or "PENDIENTE" or "SIN_VERIFICAR" || SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId) is { } r && InRange(r.FechaCreacion ?? r.Llegada)))
                {
                    var reservation = SampleData.Reservas.FirstOrDefault(r => r.Id == allocation.ReservaId);
                    var resource = SampleData.RecursosAdicionales.FirstOrDefault(x => x.RecursoId == allocation.RecursoId);
                    Add(table, allocation.ReservaId, resource?.Nombre ?? "Recurso retirado", allocation.Cantidad, allocation.Estado, reservation?.Llegada, reservation?.Salida,
                        allocation.Estado == "ENTREGADO" && reservation?.Estado == "Finalizada" ? "Pendiente de devolución" : "—", allocation.PrecioUnitarioAcordado * allocation.Cantidad);
                }
                kpis.Add(Kpi("Entregas vigentes", SampleData.ReservaRecursos.Count(x => x.Estado == "ENTREGADO" && SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId)?.Estado != "Finalizada")));
                kpis.Add(Kpi("Pendientes de devolución", SampleData.ReservaRecursos.Count(x => x.Estado == "ENTREGADO" && SampleData.Reservas.FirstOrDefault(r => r.Id == x.ReservaId)?.Estado == "Finalizada")));
                kpis.Add(Kpi("Asignaciones", table.Rows.Count)); method = "Asignaciones por reserva; un recurso entregado no se libera por superar la salida prevista. Se marca pendiente al constar la estancia finalizada.";
                warnings.Add("No hay marcas confiables de entrega/devolución ni asignación por habitación; no se inventa disponibilidad por activo físico."); break;
            case "olvidados":
                foreach (var item in SampleData.ObjetosOlvidados.Where(x => InRange(x.Encontrado) || x.Estado is not ("Entregado" or "Devuelto")))
                    Add(table, item.Id, item.Encontrado, item.HabitacionId is int roomId ? RoomNumber(roomId) : "—", item.Descripcion, item.Estado, item.Observaciones);
                kpis.Add(Kpi("Encontrados en periodo", SampleData.ObjetosOlvidados.Count(x => InRange(x.Encontrado))));
                kpis.Add(Kpi("En custodia ahora", SampleData.ObjetosOlvidados.Count(x => x.Estado is not ("Entregado" or "Devuelto")), "estado actual"));
                method = "Registro actual y fechas encontradas que conserva el modelo."; warnings.Add("No se dispone de historial completo de custodia y transferencias."); break;
        }
        ApplyDetailHeaders(def.Id, table);
        if (table.Rows.Count == 0 && kpis.All(x => x.ValorNumerico.GetValueOrDefault() == 0)) warnings.Add("No hay registros para los filtros y el periodo seleccionados. Cero medido no sustituye datos históricos ausentes.");
        NormalizeSeries(chart);
        var user = SesionActual.Usuario?.Nombre ?? "Usuario autenticado";
        var filters = $"Periodo: {p.Desde:dd/MM/yyyy} – {p.Hasta:dd/MM/yyyy}" + FilterText(p);
        return new(p.Id, def.Nombre, def.Categoria, DateTime.Now, user, p.Desde.Date, p.Hasta.Date, filters, kpis, chart, table, method, warnings);
    }

    public static IReadOnlyList<DefinicionReporte> FiltrarCatalogo(string query, string category) => Catalogo
        .Where(x => category is "Todas" or "" || x.Categoria == category)
        .Where(x => string.IsNullOrWhiteSpace(query) || $"{x.Nombre} {x.Categoria} {x.Descripcion}".Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public static ParametrosReporte ResolverPeriodo(string reportId, string period, DateTime? customStart = null, DateTime? customEnd = null)
    {
        var today = DateTime.Today; DateTime start, end;
        switch (period)
        {
            case "Hoy": start = today; end = today; break;
            case "Ayer": start = today.AddDays(-1); end = start; break;
            case "Últimos 7 días": start = today.AddDays(-6); end = today; break;
            case "Últimos 30 días": start = today.AddDays(-29); end = today; break;
            case "Últimos 90 días": start = today.AddDays(-89); end = today; break;
            case "Este mes": start = new(today.Year, today.Month, 1); end = today; break;
            case "Mes anterior": start = new DateTime(today.Year, today.Month, 1).AddMonths(-1); end = start.AddMonths(1).AddDays(-1); break;
            case "Este trimestre": start = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1); end = today; break;
            case "Este año": start = new(today.Year, 1, 1); end = today; break;
            case "Próximos 7 días": start = today; end = today.AddDays(6); break;
            case "Próximos 30 días": start = today; end = today.AddDays(29); break;
            case "Periodo personalizado":
                if (customStart is null || customEnd is null) throw new InvalidOperationException("Indica las fechas inicial y final.");
                start = customStart.Value.Date; end = customEnd.Value.Date; break;
            default: start = new(today.Year, today.Month, 1); end = today; break;
        }
        return new(reportId, period, start, end);
    }

    public static DataTable CreateTable()
    {
        var dt = new DataTable("Detalle");
        // Rows use object values; columns are assigned by Add on first row to retain a compact reusable table model.
        return dt;
    }
    public static void Add(DataTable dt, params object?[] values)
    {
        if (dt.Columns.Count == 0)
            for (var i = 0; i < values.Length; i++) dt.Columns.Add($"Dato {i + 1}", typeof(object));
        if (values.Length > dt.Columns.Count)
            for (var i = dt.Columns.Count; i < values.Length; i++) dt.Columns.Add($"Dato {i + 1}", typeof(object));
        var row = dt.NewRow();
        for (var i = 0; i < dt.Columns.Count; i++) row[i] = i < values.Length ? values[i] ?? DBNull.Value : DBNull.Value;
        dt.Rows.Add(row);
    }

    private static void ApplyDetailHeaders(string reportId, DataTable table)
    {
        string[]? labels = reportId switch
        {
            "ocupacion" => ["Tipo de habitación", "Noches ocupadas", "Noches instaladas", "Ocupación registrada %"],
            "reservas" => ["Reserva", "Huésped", "Estado", "Canal", "Llegada", "Salida", "Creada"],
            "disponibilidad" => ["Habitación", "Piso", "Tipo", "Estado", "Limpieza", "Bloqueada", "Huésped"],
            "cambios" => ["Cambio", "Fecha", "Reserva", "Estancia", "Habitación origen", "Habitación destino", "Motivo"],
            "ingresos" => ["Fecha", "Movimiento", "Huésped", "Folio", "Método", "Importe", "Concepto", "Estado"],
            "saldos" => ["Folio", "Huésped", "Contexto", "Estado", "Cargos Bs", "Cobros Bs", "Devoluciones Bs", "Saldo Bs"],
            "facturacion" => ["Factura", "Fecha", "Estado", "Cliente", "Subtotal Bs", "Impuesto Bs", "Total Bs", "Notas de crédito Bs", "Neto documentado Bs"],
            "correcciones" => ["Tipo", "Solicitud", "Pago", "Importe Bs", "Estado", "Solicitada", "Ejecutada", "Motivo"],
            "caja" => ["Turno", "Cajero", "Apertura", "Cierre", "Estado", "Fondo inicial Bs", "Esperado Bs", "Contado Bs", "Diferencia Bs", "Revisión"],
            "tarifas" => ["Canal", "Reservas", "Noches", "Precio acordado Bs", "Promedio por noche Bs"],
            "paquetes" => ["Reserva", "Paquete", "Cantidad", "Precio unitario Bs", "Total acordado Bs"],
            "servicios" => ["Uso", "Servicio", "Fecha", "Estado", "Cobertura", "Cargo Bs", "Costo aplicado Bs"],
            "convenios" => ["Fecha", "Proveedor", "Servicio", "Precio aplicado Bs", "Costo aplicado Bs", "Margen directo Bs"],
            "limpieza" => ["Tarea", "Habitación", "Piso", "Tipo", "Programada", "Colaborador", "Estado", "Prioridad", "Observaciones"],
            "mantenimiento" => ["Incidencia", "Habitación", "Categoría", "Prioridad", "Reportada", "Estado", "Bloqueo actual", "Descripción"],
            "inventario" => ["Producto", "Nombre", "Categoría", "Stock actual", "Stock mínimo", "Estado", "Entradas en periodo", "Salidas en periodo", "Consumos en periodo"],
            "recursos" => ["Reserva", "Recurso", "Cantidad", "Estado", "Llegada", "Salida", "Devolución", "Importe acordado Bs"],
            "olvidados" => ["Objeto", "Encontrado", "Habitación", "Descripción", "Estado", "Observaciones"],
            _ => null
        };
        if (labels is null || labels.Length != table.Columns.Count) return;
        for (var i = 0; i < labels.Length; i++) table.Columns[i].ColumnName = labels[i];
    }

    private static void Autorizar() { if (!NavigationService.Puede("Reportes")) throw new UnauthorizedAccessException("Solo Gerencia puede generar reportes."); }
    private static IndicadorReporte Kpi(string name, decimal value, string unit = "") => new(name, unit == "Bs" ? $"Bs {value:N2}" : unit == "%" ? $"{value:N1}%" : value.ToString("N0"), unit) { ValorNumerico = value };
    private static IndicadorReporte Kpi(string name, int value, string context = "") => new(name, value.ToString("N0"), context) { ValorNumerico = value };
    private static decimal Percent(decimal value, decimal total) => total == 0 ? 0 : decimal.Round(value * 100m / total, 2);
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static IEnumerable<HabitacionModel> FilterRooms(ParametrosReporte p) => SampleData.Habitaciones.Where(room => RoomMatches(room.Id, p));
    private static bool RoomMatches(int roomId, ParametrosReporte p)
    {
        var room = SampleData.Habitaciones.FirstOrDefault(x => x.Id == roomId);
        return room is not null && (p.TipoHabitacion is "Todos" or "" || room.Tipo == p.TipoHabitacion) && (p.Piso is "Todos" or "" || room.Piso.ToString() == p.Piso);
    }
    private static string RoomNumber(int roomId) => SampleData.Habitaciones.FirstOrDefault(x => x.Id == roomId)?.Numero ?? $"#{roomId}";
    private static string FilterText(ParametrosReporte p)
    {
        var values = new List<string>();
        if (p.TipoHabitacion is not ("Todos" or "")) values.Add($"Tipo habitación: {p.TipoHabitacion}");
        if (p.Piso is not ("Todos" or "")) values.Add($"Piso: {p.Piso}");
        if (p.Canal is not ("Todos" or "")) values.Add($"Canal/estado: {p.Canal}");
        if (p.MetodoPago is not ("Todos" or "")) values.Add($"Método de pago: {p.MetodoPago}");
        if (p.Categoria is not ("Todas" or "")) values.Add($"Categoría: {p.Categoria}");
        if (p.Empleado is not ("Todos" or "")) values.Add($"Empleado: {p.Empleado}");
        return values.Count == 0 ? " · Sin filtros adicionales" : " · " + string.Join(" · ", values);
    }
    private static void NormalizeSeries(List<PuntoReporte> points)
    {
        var max = points.Select(x => x.Valor).DefaultIfEmpty(0).Max();
        for (var i = 0; i < points.Count; i++) points[i] = points[i] with { Proporcion = max <= 0 ? 0 : (double)(points[i].Valor / max) };
    }
}
