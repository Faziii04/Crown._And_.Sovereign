using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>One calculation and consultation surface for Gerente financial supervision.</summary>
public sealed class FinancialManagementService
{
    public static readonly string[] PeriodOptions = ["Hoy", "Ayer", "Últimos 7 días", "Este mes", "Mes anterior", "Últimos 30 días", "Últimos 90 días", "Este año", "Periodo personalizado"];

    public FinancialPeriod ResolvePeriod(string option, DateTime? customStart = null, DateTime? customEnd = null, DateTime? now = null)
    {
        var today = (now ?? DateTime.Today).Date;
        DateTime start, end;
        switch (option)
        {
            case "Hoy": start = today; end = today.AddDays(1); break;
            case "Ayer": start = today.AddDays(-1); end = today; break;
            case "Últimos 7 días": start = today.AddDays(-6); end = today.AddDays(1); break;
            case "Este mes": start = new(today.Year, today.Month, 1); end = today.AddDays(1); break;
            case "Mes anterior": start = new DateTime(today.Year, today.Month, 1).AddMonths(-1); end = new DateTime(today.Year, today.Month, 1); break;
            case "Últimos 30 días": start = today.AddDays(-29); end = today.AddDays(1); break;
            case "Últimos 90 días": start = today.AddDays(-89); end = today.AddDays(1); break;
            case "Este año": start = new(today.Year, 1, 1); end = today.AddDays(1); break;
            case "Periodo personalizado":
                if (customStart is null || customEnd is null) throw new InvalidOperationException("Selecciona las fechas inicial y final del periodo.");
                start = customStart.Value.Date; end = customEnd.Value.Date.AddDays(1);
                if (end <= start) throw new InvalidOperationException("La fecha final debe ser igual o posterior a la fecha inicial.");
                break;
            default: start = new(today.Year, today.Month, 1); end = today.AddDays(1); break;
        }
        var length = end - start;
        return new(option, start, end, start - length, start);
    }

    public FinancialPeriod FromDashboardPeriod(DashboardPeriod period) => new(period.Nombre, period.Desde.Date, period.Hasta.Date.AddDays(1),
        period.ComparacionDesde.Date, period.ComparacionHasta.Date.AddDays(1), period.IncluirComparacion);

    public FinancialSummary Resumen(FinancialPeriod period)
    {
        var charges = SampleData.Cargos.Where(item => !item.Anulado && item.FolioId is int folioId && SampleData.Folios.Any(folio => folio.Id == folioId) && InRange(item.Fecha, period.Start, period.End)).ToList();
        var payments = SampleData.Pagos.Where(item => InRange(item.Fecha, period.Start, period.End)).ToList();
        var refunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.Start, period.End)).ToList();
        var cancellations = SampleData.Anulaciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.Start, period.End)).ToList();
        decimal AmountOfCancellation(AnulacionModel item) => SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId)?.Monto ?? 0m;
        var gross = payments.Sum(item => item.Monto);
        var refundTotal = refunds.Sum(item => item.Monto);
        var cancellationTotal = cancellations.Sum(AmountOfCancellation);
        var priorCharges = SampleData.Cargos.Where(item => !item.Anulado && item.FolioId is int id && SampleData.Folios.Any(folio => folio.Id == id) && InRange(item.Fecha, period.PreviousStart, period.PreviousEnd)).Sum(item => item.Monto);
        var priorGross = SampleData.Pagos.Where(item => InRange(item.Fecha, period.PreviousStart, period.PreviousEnd)).Sum(item => item.Monto);
        var priorRefunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.PreviousStart, period.PreviousEnd)).Sum(item => item.Monto);
        var priorCancellations = SampleData.Anulaciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.PreviousStart, period.PreviousEnd)).Sum(AmountOfCancellation);
        var balances = new FolioService().CalcularSaldo();
        var corrections = new CorreccionFinancieraService().Listar();
        var methods = SampleData.MetodosPago.Select(method =>
        {
            var amount = payments.Where(payment => payment.Metodo == method.Nombre).Sum(payment => payment.Monto);
            var total = gross == 0 ? 0 : (double)(amount / gross * 100m);
            return new FinancialBreakdownRow(method.Nombre, amount, total, method.EsEfectivo);
        }).Where(item => item.Amount != 0).OrderByDescending(item => item.Amount).ToList();
        var trend = BuildTrend(period, charges.Sum(item => item.Monto), gross, refundTotal, cancellationTotal);
        var alerts = new List<FinancialAlertRow>();
        if (corrections.Count(item => item.Estado == "Pendiente") > 0) alerts.Add(new("Autorizaciones pendientes", $"{corrections.Count(item => item.Estado == "Pendiente")} solicitudes por revisar", "Atención", "Autorizaciones"));
        if (corrections.Count(item => item.Estado == "Autorizada") > 0) alerts.Add(new("Correcciones por ejecutar", $"{corrections.Count(item => item.Estado == "Autorizada")} solicitudes aprobadas por ejecutar en Caja", "Información", "Autorizaciones"));
        if (SampleData.CierresCaja.Any(item => item.FechaRevision is null)) alerts.Add(new("Cierres por revisar", $"{SampleData.CierresCaja.Count(item => item.FechaRevision is null)} cierres históricos", "Atención", "Cierres"));
        if (SampleData.TurnosCaja.Any(item => item.Estado == "Abierto")) alerts.Add(new("Turnos abiertos", $"{SampleData.TurnosCaja.Count(item => item.Estado == "Abierto")} cajas abiertas actualmente", "Información", "Cierres"));
        var creditNotes = SampleData.NotasCredito.Where(item => InRange(item.Fecha, period.Start, period.End)).Sum(item => item.Monto);
        return new(charges.Sum(item => item.Monto), gross, refundTotal, cancellationTotal, gross - refundTotal - cancellationTotal,
            balances, corrections.Count(item => item.Estado == "Pendiente"), corrections.Count(item => item.Estado == "Autorizada"),
            SampleData.TurnosCaja.Count(item => item.Estado == "Abierto"), SampleData.CierresCaja.Count(item => item.FechaRevision is null),
            SampleData.Facturas.Count(item => InRange(item.Fecha, period.Start, period.End)), creditNotes,
            priorCharges, priorGross - priorRefunds - priorCancellations, methods, trend, alerts);
    }

    public IReadOnlyList<FinancialMovementRow> Movimientos(FinancialPeriod period, string kind = "Todos", string query = "")
    {
        var rows = new List<FinancialMovementRow>();
        foreach (var item in SampleData.Cargos.Where(item => !item.Anulado && item.FolioId is not null && InRange(item.Fecha, period.Start, period.End)))
            rows.Add(new(item.Fecha, "Cargo", GuestForFolio(item.FolioId!.Value), item.FolioId.Value, item.Concepto, item.Monto, "—", "Registrado", item.Id, ContextForFolio(item.FolioId.Value)));
        foreach (var item in SampleData.Pagos.Where(item => InRange(item.Fecha, period.Start, period.End)))
            rows.Add(new(item.Fecha, "Cobro", GuestForFolio(item.FolioId), item.FolioId, item.Concepto, item.Monto, item.Metodo, item.Estado, item.Id, ContextForFolio(item.FolioId)));
        foreach (var item in SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.Start, period.End)))
            if (SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId) is { } payment)
                rows.Add(new(item.Ejecutada ?? item.Solicitada, "Devolución", GuestForFolio(payment.FolioId), payment.FolioId, item.Motivo, -item.Monto, item.MetodoEjecucion, "Ejecutada", item.Id, $"Pago #{payment.Id}"));
        foreach (var item in SampleData.Anulaciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, period.Start, period.End)))
            if (SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId) is { } payment)
                rows.Add(new(item.Ejecutada ?? item.Solicitada, "Anulación", GuestForFolio(payment.FolioId), payment.FolioId, item.Motivo, -payment.Monto, payment.Metodo, "Ejecutada", item.Id, $"Pago #{payment.Id}"));
        return rows.Where(item => kind is "Todos" or "Todos los movimientos" || item.Kind == kind)
            .Where(item => string.IsNullOrWhiteSpace(query) || $"{item.Guest} {item.FolioId} {item.Concept} {item.Method} {item.Status} {item.SourceId}".Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Date).ToList();
    }

    public IReadOnlyList<FinancialBalanceRow> Saldos(string query = "", string state = "Abiertos")
    {
        var result = SampleData.Folios.Select(folio =>
        {
            var charges = SampleData.Cargos.Where(item => item.FolioId == folio.Id && !item.Anulado).Sum(item => item.Monto);
            var payments = SampleData.Pagos.Where(item => item.FolioId == folio.Id).Sum(item => item.Monto);
            var refunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && SampleData.Pagos.Any(payment => payment.Id == item.PagoId && payment.FolioId == folio.Id)).Sum(item => item.Monto);
            var reservationId = folio.ReservaId ?? (folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.ReservaId : null);
            var guest = GuestForFolio(folio.Id);
            return new FinancialBalanceRow(folio.Id, guest, ContextForFolio(folio.Id), folio.Tipo, folio.Estado, charges, payments, refunds,
                new FolioService().CalcularSaldoFolio(folio.Id), reservationId, folio.EstanciaId);
        });
        return result.Where(item => state == "Todos" || state == "Abiertos" && item.Status == "Abierto" || state == "Con saldo" && item.Balance != 0 || state == "Crédito a favor" && item.Balance < 0)
            .Where(item => string.IsNullOrWhiteSpace(query) || $"{item.FolioId} {item.Guest} {item.Context} {item.ReservationId} {item.StayId}".Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => Math.Abs(item.Balance)).ToList();
    }

    public IReadOnlyList<FinancialInvoiceRow> Facturas(FinancialPeriod period, string query = "") => SampleData.Facturas
        .Where(item => InRange(item.Fecha, period.Start, period.End))
        .Select(invoice =>
        {
            var credits = SampleData.NotasCredito.Where(item => item.FacturaId == invoice.Id).Sum(item => item.Monto);
            var guest = invoice.Cliente;
            var context = invoice.EstanciaId is int stay ? $"Estancia #{stay}" : invoice.ReservaId is int reservation ? $"Reserva #{reservation}" : $"Folio #{invoice.FolioId}";
            return new FinancialInvoiceRow(invoice, string.IsNullOrWhiteSpace(guest) ? GuestForFolio(invoice.FolioId) : guest, $"#{invoice.FolioId}", context, credits, invoice.Total - credits);
        })
        .Where(row => string.IsNullOrWhiteSpace(query) || $"{row.Number} {row.Guest} {row.Folio} {row.Context} {row.Status}".Contains(query, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(row => row.Date).ToList();

    public IReadOnlyList<FinancialCreditNoteRow> NotasCredito(int invoiceId) => SampleData.NotasCredito.Where(item => item.FacturaId == invoiceId)
        .Select(item => new FinancialCreditNoteRow(item.Id, string.IsNullOrWhiteSpace(item.Numero) ? $"NC-{item.Id:0000}" : item.Numero, item.FacturaId,
            SampleData.Facturas.FirstOrDefault(invoice => invoice.Id == item.FacturaId)?.Numero ?? "Factura no disponible", item.Fecha, item.Monto, item.Motivo)).ToList();

    public IReadOnlyList<CorrectionDirectoryRow> Autorizaciones(string state = "Todos") => new CorreccionFinancieraService().Listar(state);

    public IReadOnlyList<FinancialCashShiftRow> Turnos(string query = "", string state = "Todos")
    {
        var service = new CajaService();
        return service.ListarTurnos(query, state).Select(shift =>
        {
            var summary = service.ObtenerResumenTurno(shift.Id);
            return new FinancialCashShiftRow(shift, SampleData.Usuarios.FirstOrDefault(user => user.Id == shift.UsuarioId)?.Nombre ?? "—", summary.EfectivoEsperado, summary.NoEfectivo, summary.EfectivoDevoluciones, summary.EfectivoAnulaciones, service.ObtenerCierre(shift.Id));
        }).ToList();
    }

    public IReadOnlyList<FinancialDiagnosticRow> Diagnosticos()
    {
        var result = new List<FinancialDiagnosticRow>();
        var folioService = new FolioService();
        foreach (var folio in SampleData.Folios)
        {
            var calculated = folioService.CalcularSaldoFolio(folio.Id);
            if (Math.Abs(folio.Saldo - calculated) > 0.01m)
                result.Add(new("Revisar", $"Folio #{folio.Id}", $"Saldo almacenado Bs {folio.Saldo:N2} y saldo calculado Bs {calculated:N2}. No se modificó ningún registro.", folio.Id));
        }
        foreach (var invoice in SampleData.Facturas.Where(item => (item.Subtotal != 0 || item.Impuesto != 0 || item.Lineas.Count > 0) && item.Total != item.Subtotal + item.Impuesto))
            result.Add(new("Revisar", invoice.Numero, "El total guardado no coincide con subtotal más impuesto; se conserva el valor histórico.", invoice.FolioId));
        foreach (var closure in SampleData.CierresCaja.Where(item => decimal.Round(item.Contado - item.Esperado, 2) != item.Diferencia))
            result.Add(new("Revisar", $"Cierre #{closure.Id}", "La diferencia guardada no coincide con efectivo contado menos esperado.", null));
        return result;
    }

    public void RevisarCierre(int cierreId)
    {
        if (!NavigationService.Puede("Finanzas")) throw new UnauthorizedAccessException("Solo Gerencia puede revisar cierres de caja.");
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        var index = SampleData.CierresCaja.FindIndex(item => item.Id == cierreId);
        if (index < 0) throw new InvalidOperationException("No se encontró el cierre.");
        var closure = SampleData.CierresCaja[index];
        if (closure.FechaRevision is not null) throw new InvalidOperationException("Este cierre ya fue revisado.");
        SampleData.CierresCaja[index] = closure with { UsuarioRevisorId = user.Id, FechaRevision = DateTime.Now };
    }

    public static bool InRange(DateTime value, DateTime start, DateTime endExclusive) => value >= start && value < endExclusive;
    private static IReadOnlyList<FinancialTrendRow> BuildTrend(FinancialPeriod period, decimal totalCharges, decimal totalCollections, decimal totalRefunds, decimal totalCancellations)
    {
        var span = period.End - period.Start;
        var bucketCount = span.TotalDays <= 1 ? 6 : Math.Clamp((int)Math.Ceiling(span.TotalDays / 7), 4, 12);
        var rows = new List<FinancialTrendRow>();
        for (var i = 0; i < bucketCount; i++)
        {
            var start = period.Start.AddTicks(span.Ticks * i / bucketCount);
            var end = i == bucketCount - 1 ? period.End : period.Start.AddTicks(span.Ticks * (i + 1) / bucketCount);
            var charges = SampleData.Cargos.Where(item => !item.Anulado && item.FolioId is int folioId && SampleData.Folios.Any(folio => folio.Id == folioId) && InRange(item.Fecha, start, end)).Sum(item => item.Monto);
            var collections = SampleData.Pagos.Where(item => InRange(item.Fecha, start, end)).Sum(item => item.Monto);
            var refunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, start, end)).Sum(item => item.Monto);
            var cancels = SampleData.Anulaciones.Where(item => item.Estado == "Ejecutada" && InRange(item.Ejecutada ?? item.Solicitada, start, end)).Sum(item => SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId)?.Monto ?? 0m);
            var label = span.TotalDays <= 1 ? start.ToString("HH:mm") : start.ToString("dd/MM");
            rows.Add(new(label, charges, collections, refunds, cancels));
        }
        _ = (totalCharges, totalCollections, totalRefunds, totalCancellations);
        return rows;
    }
    private static string GuestForFolio(int folioId)
    {
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == folioId);
        var guestId = folio?.HuespedPrincipalId ?? (folio?.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null)
            ?? (folio?.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null);
        return SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId)?.Nombre ?? "Huésped por identificar";
    }
    private static string ContextForFolio(int folioId)
    {
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == folioId);
        if (folio is null) return "Folio sin atribución";
        if (folio.EstanciaId is int stayId) return $"Estancia #{stayId}";
        if (folio.ReservaId is int reservationId) return $"Reserva #{reservationId}";
        return folio.Tipo;
    }
}
