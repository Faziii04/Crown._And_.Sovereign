using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed record PagoParte(string Metodo, decimal Monto);

public sealed class CajaService
{
    public TurnoCajaModel? ObtenerTurnoActual(int? usuarioId = null)
    {
        var id = usuarioId ?? SesionActual.Usuario?.Id;
        if (id is null) return null;
        return SampleData.TurnosCaja.Where(item => item.UsuarioId == id && item.Estado == "Abierto")
            .OrderByDescending(item => item.Apertura).FirstOrDefault();
    }

    public CashDeskSummary ObtenerResumen()
    {
        var shift = ObtenerTurnoActual();
        if (shift is null) return new(false, 0, 0, 0, 0, 0);
        var summary = ObtenerResumenTurno(shift.Id);
        return new(true, shift.MontoInicial, summary.EfectivoCobros, summary.NoEfectivo, summary.EfectivoDevoluciones, summary.EfectivoEsperado);
    }

    public CashDeskShiftSummary ObtenerResumenTurno(int turnoId)
    {
        var shift = SampleData.TurnosCaja.FirstOrDefault(item => item.Id == turnoId)
            ?? throw new InvalidOperationException("No se encontró el turno de caja.");
        var movements = SampleData.MovimientosCaja.Where(item => item.TurnoId == turnoId).ToList();
        var cashMethods = SampleData.MetodosPago.Where(item => item.EsEfectivo).Select(item => item.Nombre).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool IsCash(string method) => SampleData.MetodosPago.FirstOrDefault(item => item.Nombre.Equals(method, StringComparison.OrdinalIgnoreCase))?.EsEfectivo ?? cashMethods.Contains(method);

        var cashIn = movements.Where(item => item.Tipo == "Cobro" && IsCash(item.Metodo)).Sum(item => item.Monto);
        var cashOut = movements.Where(item => item.Tipo == "Devolución" && IsCash(item.Metodo)).Sum(item => item.Monto);
        var nonCash = movements.Where(item => item.Tipo == "Cobro" && !IsCash(item.Metodo)).Sum(item => item.Monto);
        var payments = movements.Where(item => item.Tipo == "Cobro").Sum(item => item.Monto);
        var methods = SampleData.MetodosPago.Where(item => item.Activo).Select(method => new MetodoPagoResumen(
            method.Nombre, movements.Where(item => item.Tipo == "Cobro" && item.Metodo == method.Nombre).Sum(item => item.Monto), method.EsEfectivo)).ToList();
        return new(shift, cashIn, cashOut, nonCash, payments, shift.MontoInicial + cashIn - cashOut, methods);
    }

    public IReadOnlyList<CashMovementRow> ListarMovimientos(int turnoId, string tipo = "Todos", string busqueda = "", string? metodo = null)
    {
        return SampleData.MovimientosCaja.Where(item => item.TurnoId == turnoId)
            .Where(item => tipo is "Todos" or "Todos los movimientos" || item.Tipo == tipo)
            .Where(item => string.IsNullOrWhiteSpace(metodo) || metodo == "Todos los medios" || item.Metodo == metodo)
            .Select(item =>
            {
                var folio = item.FolioId is int folioId ? SampleData.Folios.FirstOrDefault(value => value.Id == folioId) : null;
                var guestId = folio?.HuespedPrincipalId ?? (folio?.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(value => value.Id == stayId)?.HuespedId : null);
                var guest = guestId is int id ? SampleData.Huespedes.FirstOrDefault(value => value.Id == id) : null;
                var payment = item.PagoId is int paymentId ? SampleData.Pagos.FirstOrDefault(value => value.Id == paymentId) : null;
                var status = item.Tipo switch
                {
                    "Devolución" => SampleData.Devoluciones.FirstOrDefault(value => value.Id == item.CorreccionId)?.Estado ?? "Ejecutada",
                    "Anulación" => SampleData.Anulaciones.FirstOrDefault(value => value.Id == item.CorreccionId)?.Estado ?? "Ejecutada",
                    _ => payment?.Estado ?? "Registrado"
                };
                return new CashMovementRow(item.Id, item.Fecha, item.Tipo, guest?.Nombre ?? "—", item.FolioId ?? 0, item.Metodo, item.Monto, item.Concepto, item.PagoId, status);
            })
            .Where(item => string.IsNullOrWhiteSpace(busqueda) || $"{item.Id} {item.Huesped} {item.FolioId} {item.Metodo} {item.Concepto} {item.PagoId}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Fecha).ToList();
    }

    public IReadOnlyList<TurnoCajaModel> ListarTurnos(string busqueda = "", string filtro = "Todos")
    {
        var user = SesionActual.Usuario;
        var shifts = SampleData.TurnosCaja.AsEnumerable();
        if (user?.Rol != "Gerente") shifts = shifts.Where(item => item.UsuarioId == user?.Id);
        if (filtro == "Cerrados") shifts = shifts.Where(item => item.Estado == "Cerrado");
        return shifts.Where(item => string.IsNullOrWhiteSpace(busqueda) || item.Id.ToString().Contains(busqueda, StringComparison.OrdinalIgnoreCase) || SampleData.Usuarios.FirstOrDefault(userItem => userItem.Id == item.UsuarioId)?.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(item => item.Apertura).ToList();
    }

    public CierreCajaModel? ObtenerCierre(int turnoId) => SampleData.CierresCaja.FirstOrDefault(item => item.TurnoCajaId == turnoId);

    public TurnoCajaModel Abrir(decimal apertura, string observaciones = "")
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException("Inicia sesión para abrir un turno de caja.");
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede abrir caja.");
        if (ObtenerTurnoActual(user.Id) is not null) throw new InvalidOperationException("Ya tienes un turno de caja abierto.");
        if (apertura < 0 || decimal.Round(apertura, 2) != apertura) throw new InvalidOperationException("El fondo inicial debe ser un importe válido y no negativo.");
        var shift = new TurnoCajaModel(SampleData.TurnosCaja.Count == 0 ? 1 : SampleData.TurnosCaja.Max(item => item.Id) + 1, user.Id, DateTime.Now, apertura, "Abierto")
        { ObservacionesCierre = observaciones.Trim() };
        SampleData.TurnosCaja.Add(shift);
        SampleData.MovimientosCaja.Add(new(SampleData.MovimientosCaja.Count + 1, shift.Id, shift.Apertura, "Apertura", apertura, observaciones.Trim()) { Metodo = "Efectivo", UsuarioId = user.Id });
        SampleData.AperturaCaja = apertura;
        SampleData.CajaAbierta = true;
        SampleData.Auditoria.Insert(0, new(DateTime.Now, user.Usuario, "Apertura de caja", "Cerrada", $"Turno #{shift.Id} · Bs {apertura:N2}"));
        return shift;
    }

    public CierreCajaModel Cerrar(int turnoId, decimal contado, string observaciones, decimal? esperadoRevisado = null)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede cerrar caja.");
        var shift = SampleData.TurnosCaja.FirstOrDefault(item => item.Id == turnoId) ?? throw new InvalidOperationException("No se encontró el turno.");
        if (shift.Estado != "Abierto") throw new InvalidOperationException("Este turno ya está cerrado.");
        if (shift.UsuarioId != user.Id) throw new UnauthorizedAccessException("Solo puedes cerrar tu propio turno.");
        if (contado < 0 || decimal.Round(contado, 2) != contado) throw new InvalidOperationException("El efectivo contado debe ser válido y no negativo.");
        var summary = ObtenerResumenTurno(turnoId);
        if (esperadoRevisado is decimal viewed && viewed != summary.EfectivoEsperado) throw new InvalidOperationException("Se registraron movimientos después de abrir el cierre. Revisa el nuevo total antes de confirmar.");
        var difference = contado - summary.EfectivoEsperado;
        if (difference != 0 && string.IsNullOrWhiteSpace(observaciones)) throw new InvalidOperationException("Describe la diferencia antes de cerrar el turno.");
        var closureId = SampleData.CierresCaja.Count == 0 ? 1 : SampleData.CierresCaja.Max(item => item.Id) + 1;
        var closure = new CierreCajaModel(closureId, DateTime.Now, summary.EfectivoEsperado, contado, difference, observaciones.Trim())
        {
            TurnoCajaId = turnoId, UsuarioId = user.Id, Apertura = shift.Apertura, AperturaMonto = shift.MontoInicial,
            EfectivoCobros = summary.EfectivoCobros, EfectivoDevoluciones = summary.EfectivoDevoluciones, NoEfectivo = summary.NoEfectivo
        };
        var index = SampleData.TurnosCaja.FindIndex(item => item.Id == turnoId);
        SampleData.CierresCaja.Add(closure);
        SampleData.TurnosCaja[index] = shift with { Estado = "Cerrado", Cierre = closure.Fecha, EfectivoContado = contado, CierreCajaId = closure.Id, ObservacionesCierre = observaciones.Trim() };
        SampleData.CajaAbierta = SampleData.TurnosCaja.Any(item => item.Estado == "Abierto");
        SampleData.Auditoria.Insert(0, new(DateTime.Now, user.Usuario, "Cierre de caja", $"Turno #{turnoId} · esperado Bs {closure.Esperado:N2}", $"Contado Bs {contado:N2} · diferencia Bs {difference:N2}"));
        return closure;
    }
}

public sealed class FolioService
{
    public decimal CalcularSaldo(int? guestId = null)
    {
        var folios = SampleData.Folios.Where(item => guestId is null || ObtenerGuestId(item) == guestId).ToList();
        return folios.Sum(item => CalcularSaldoFolio(item.Id));
    }
    public decimal CalcularSaldoPorEstancia(int estanciaId)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId) ?? throw new InvalidOperationException("No se encontró la estancia.");
        var folio = stay.FolioId is int id ? SampleData.Folios.FirstOrDefault(item => item.Id == id) : SampleData.Folios.FirstOrDefault(item => item.EstanciaId == stay.Id);
        return folio is null ? 0 : CalcularSaldoFolio(folio.Id);
    }
    public decimal CalcularSaldoFolio(int folioId)
    {
        if (!SampleData.Folios.Any(item => item.Id == folioId)) throw new InvalidOperationException("No se encontró el folio.");
        var charges = SampleData.Cargos.Where(item => item.FolioId == folioId && !item.Anulado).Sum(item => item.Monto);
        var payments = SampleData.Pagos.Where(item => item.FolioId == folioId && item.Estado != "Anulado").Sum(item => item.Monto);
        var refunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId)?.FolioId == folioId).Sum(item => item.Monto);
        return Math.Max(0, charges - payments + refunds);
    }
    public IReadOnlyList<FolioLine> ConsultarMovimientos(int guestId) => SampleData.Folios.Where(item => ObtenerGuestId(item) == guestId).SelectMany(item => ConsultarMovimientosPorFolio(item.Id)).OrderByDescending(item => item.Fecha).ToList();
    public IReadOnlyList<FolioLine> ConsultarMovimientosPorEstancia(int estanciaId)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId) ?? throw new InvalidOperationException("No se encontró la estancia.");
        var folioId = stay.FolioId ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == estanciaId)?.Id;
        return folioId is int id ? ConsultarMovimientosPorFolio(id) : [];
    }
    public IReadOnlyList<FolioLine> ConsultarMovimientosPorFolio(int folioId)
    {
        var charges = SampleData.Cargos.Where(item => item.FolioId == folioId).Select(item => new FolioLine(item.Fecha, item.Anulado ? "Anulación" : "Cargo", item.Concepto, item.Anulado ? 0m : item.Monto) { Origen = OrigenCargo(item.Concepto), Estado = item.Anulado ? "Anulado" : "Aplicado" });
        var payments = SampleData.Pagos.Where(item => item.FolioId == folioId).Select(item => new FolioLine(item.Fecha, item.Estado == "Anulado" ? "Anulación" : "Pago", $"{item.Concepto} · {item.Metodo}", item.Estado == "Anulado" ? 0m : -item.Monto) { Origen = "Caja", Estado = item.Estado });
        var refunds = SampleData.Devoluciones.Where(item => item.Estado == "Ejecutada" && SampleData.Pagos.FirstOrDefault(payment => payment.Id == item.PagoId)?.FolioId == folioId)
            .Select(item => new FolioLine(item.Ejecutada ?? item.Solicitada, "Devolución", item.Motivo, item.Monto) { Origen = "Corrección financiera", Estado = "Ejecutada" });
        return charges.Concat(payments).Concat(refunds).OrderByDescending(item => item.Fecha).ToList();
    }
    private static string OrigenCargo(string concept)
        => concept.StartsWith("Alojamiento", StringComparison.OrdinalIgnoreCase) ? "Alojamiento"
            : concept.StartsWith("Recurso", StringComparison.OrdinalIgnoreCase) ? "Recurso adicional"
            : concept.StartsWith("Servicio adquirido", StringComparison.OrdinalIgnoreCase) ? "Servicio adquirido"
            : concept.StartsWith("Consumo", StringComparison.OrdinalIgnoreCase) ? "Consumo"
            : concept.StartsWith("Servicio", StringComparison.OrdinalIgnoreCase) || concept.StartsWith("Masaje", StringComparison.OrdinalIgnoreCase) ? "Servicio utilizado"
            : "Cargo adicional";
    public int ContarMovimientosSinEstancia(int estanciaId)
    {
        var stay = SampleData.Estancias.First(item => item.Id == estanciaId);
        return SampleData.Cargos.Count(item => item.HuespedId == stay.HuespedId && item.EstanciaId is null && item.FolioId is null)
             + SampleData.Movimientos.Count(item => item.HuespedId == stay.HuespedId && item.Tipo == "Pago" && item.EstanciaId is null && item.FolioId is null);
    }
    public void RegistrarPago(decimal monto, string medio, int? guestId = null)
    {
        if (guestId is not int id) throw new InvalidOperationException("Selecciona el folio que recibirá el pago.");
        var stay = SampleData.Estancias.FirstOrDefault(item => item.HuespedId == id && item.Estado == "Activa");
        var folio = stay?.FolioId is int folioId ? folioId : SampleData.Folios.FirstOrDefault(item => ObtenerGuestId(item) == id)?.Id;
        if (folio is null) throw new InvalidOperationException("El huésped no tiene un folio elegible.");
        new PagoService().Registrar(folio.Value, [new(medio, monto)]);
    }
    public void RegistrarPagoPorEstancia(decimal monto, string medio, int estanciaId)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa") ?? throw new InvalidOperationException("La estancia ya no está activa.");
        var folio = stay.FolioId ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == estanciaId)?.Id ?? throw new InvalidOperationException("No se encontró el folio de la estancia.");
        new PagoService().Registrar(folio, [new(medio, monto)]);
    }
    public void RegistrarCargo(string concepto, decimal importe, int guestId = 0)
    {
        if (!NavigationService.Puede("Estancias") && !NavigationService.Puede("Caja y facturación") && !NavigationService.Puede("Consumos") && !NavigationService.Puede("Atención de servicios")) throw new UnauthorizedAccessException("Tu rol no puede agregar cargos a folios.");
        var stay = SampleData.Estancias.FirstOrDefault(item => item.HuespedId == guestId && item.Estado == "Activa");
        if (stay is null) throw new InvalidOperationException("Selecciona un folio o una estancia activa para agregar el cargo.");
        RegistrarCargoPorEstancia(concepto, importe, stay.Id);
    }
    public void RegistrarCargoPorEstancia(string concepto, decimal importe, int estanciaId)
    {
        if (!NavigationService.Puede("Estancias") && !NavigationService.Puede("Caja y facturación") && !NavigationService.Puede("Consumos") && !NavigationService.Puede("Atención de servicios")) throw new UnauthorizedAccessException("Tu rol no puede agregar cargos a folios.");
        if (importe <= 0 || string.IsNullOrWhiteSpace(concepto)) throw new InvalidOperationException("Indica un concepto y un importe positivo.");
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa") ?? throw new InvalidOperationException("La estancia ya no está activa.");
        var folioId = stay.FolioId ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == estanciaId)?.Id ?? throw new InvalidOperationException("No se encontró el folio.");
        SampleData.Cargos.Add(new CargoModel(SampleData.Cargos.Count == 0 ? 1 : SampleData.Cargos.Max(item => item.Id) + 1, concepto.Trim(), importe, DateTime.Now, stay.HuespedId, stay.Id) { FolioId = folioId });
    }
    public void RegistrarCargosPorEstancia(IEnumerable<(string Concepto, decimal Importe)> movimientos, int estanciaId)
    {
        var entries = movimientos.ToList();
        ValidarCargosPorEstancia(entries, estanciaId);
        var stay = SampleData.Estancias.First(item => item.Id == estanciaId && item.Estado == "Activa");
        var folioId = stay.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == estanciaId).Id;
        var nextId = SampleData.Cargos.Count == 0 ? 1 : SampleData.Cargos.Max(item => item.Id) + 1;
        var now = DateTime.Now;
        SampleData.Cargos.AddRange(entries.Select((item, index) => new CargoModel(nextId + index, item.Concepto.Trim(), item.Importe, now, stay.HuespedId, stay.Id) { FolioId = folioId }));
    }
    public void ValidarCargosPorEstancia(IEnumerable<(string Concepto, decimal Importe)> movimientos, int estanciaId)
    {
        if (!NavigationService.Puede("Estancias") && !NavigationService.Puede("Caja y facturación") && !NavigationService.Puede("Consumos") && !NavigationService.Puede("Atención de servicios")) throw new UnauthorizedAccessException("Tu rol no puede agregar cargos a folios.");
        var entries = movimientos.ToList();
        if (entries.Any(item => string.IsNullOrWhiteSpace(item.Concepto) || item.Importe <= 0)) throw new InvalidOperationException("Cada movimiento requiere un concepto y un importe positivo.");
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa") ?? throw new InvalidOperationException("La estancia no está activa.");
        if (stay.FolioId is null && !SampleData.Folios.Any(item => item.EstanciaId == estanciaId)) throw new InvalidOperationException("No se encontró el folio de la estancia.");
    }
    private static int? ObtenerGuestId(FolioModel folio)
    {
        if (folio.HuespedPrincipalId is int guestId) return guestId;
        if (folio.EstanciaId is int stayId) return SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId;
        return folio.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null;
    }
}

public sealed class PagoService
{
    public IReadOnlyList<FolioSelectorRow> BuscarFolios(string busqueda = "", string filtro = "Todos", bool soloConSaldo = true)
    {
        return SampleData.Folios.Where(item => item.Estado == "Abierto")
            .Where(item => filtro is "Todos" or "Todos los folios" || filtro == "Estancias activas" && item.EstanciaId is int stayId && SampleData.Estancias.Any(stay => stay.Id == stayId && stay.Estado == "Activa") || filtro == "Anticipos de reservas" && item.ReservaId is int)
            .Select(item =>
            {
                var guestId = item.HuespedPrincipalId ?? (item.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(stay => stay.Id == stayId)?.HuespedId : null) ?? (item.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(link => link.ReservaId == reservationId && link.Principal)?.HuespedId : null) ?? 0;
                var guest = SampleData.Huespedes.FirstOrDefault(person => person.Id == guestId);
                var reservation = item.ReservaId is int rid ? SampleData.Reservas.FirstOrDefault(value => value.Id == rid) : item.EstanciaId is int sid ? SampleData.Estancias.FirstOrDefault(value => value.Id == sid) is { } stay ? SampleData.Reservas.FirstOrDefault(value => value.Id == stay.ReservaId) : null : null;
                var roomId = item.EstanciaId is int estId ? SampleData.Estancias.FirstOrDefault(value => value.Id == estId) is { } est ? SampleData.Huespedes.FirstOrDefault(person => person.Id == est.HuespedId)?.Habitacion : null : reservation?.Habitacion;
                var room = roomId is int id ? SampleData.Habitaciones.FirstOrDefault(value => value.Id == id)?.Numero : null;
                var context = item.EstanciaId is int currentStay ? $"Estancia #{currentStay}" : reservation is not null ? $"Reserva #{reservation.Id} · {reservation.Llegada:dd/MM}" : "Cuenta compartida";
                var balance = new FolioService().CalcularSaldoFolio(item.Id);
                return new FolioSelectorRow(item.Id, guest?.Nombre ?? "Huésped", context, room is null ? "—" : $"Habitación {room}", item.Tipo, item.Estado, balance, guestId, item.ReservaId, item.EstanciaId);
            })
            .Where(item => !soloConSaldo || item.Saldo > 0)
            .Where(item => string.IsNullOrWhiteSpace(busqueda) || $"{item.FolioId} {item.Huésped} {item.Habitación} {item.Contexto} {SampleData.Huespedes.FirstOrDefault(guest => guest.Id == item.GuestId)?.Documento}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Huésped).ToList();
    }

    public IReadOnlyList<PaymentDirectoryRow> Listar(string busqueda = "", string alcance = "Todos los pagos", string? metodo = null, string? estado = null)
    {
        var user = SesionActual.Usuario;
        var payments = SampleData.Pagos.AsEnumerable();
        if (alcance == "Turno actual")
        {
            var shift = new CajaService().ObtenerTurnoActual();
            payments = shift is null ? [] : payments.Where(item => item.TurnoCajaId == shift.Id);
        }
        else if (alcance == "Hoy") payments = payments.Where(item => item.Fecha.Date == DateTime.Today);
        if (!string.IsNullOrWhiteSpace(metodo) && metodo != "Todos los medios") payments = payments.Where(item => item.Metodo == metodo);
        var rows = payments.Select(payment =>
        {
            var folio = SampleData.Folios.FirstOrDefault(item => item.Id == payment.FolioId);
            var guestId = folio is null ? 0 : folio.HuespedPrincipalId ?? (folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null) ?? (folio.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null) ?? 0;
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId);
            var refundTotal = SampleData.Devoluciones.Where(item => item.PagoId == payment.Id && item.Estado == "Ejecutada").Sum(item => item.Monto);
            var viewState = payment.Estado == "Anulado" ? "Anulado" : refundTotal >= payment.Monto ? "Devuelto" : refundTotal > 0 ? "Parcialmente devuelto" : payment.Estado;
            var stay = folio?.EstanciaId is int stayId2 ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId2) : null;
            var reservation = folio?.ReservaId is int reservationId2 ? SampleData.Reservas.FirstOrDefault(item => item.Id == reservationId2) : stay is not null ? SampleData.Reservas.FirstOrDefault(item => item.Id == stay.ReservaId) : null;
            var roomId = reservation?.Habitacion ?? guest?.Habitacion ?? 0;
            var room = SampleData.Habitaciones.FirstOrDefault(item => item.Id == roomId)?.Numero ?? "—";
            return new PaymentDirectoryRow(payment, guest?.Nombre ?? "—", guest?.Documento ?? "—", reservation?.Id, stay?.Id, $"#{payment.FolioId}", room, viewState, refundTotal);
        });
        if (user?.Rol == "Recepcionista" && alcance == "Mis pagos") rows = rows.Where(row => row.Pago.UsuarioId == user.Id);
        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos los estados") rows = rows.Where(row => row.Estado == estado);
        return rows.Where(row => string.IsNullOrWhiteSpace(busqueda) || $"{row.Id} {row.Pago.FolioId} {row.Huesped} {row.Documento} {row.ReservaId} {row.EstanciaId} {row.Concepto}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(row => row.Fecha).ToList();
    }

    public IReadOnlyList<PagoModel> Registrar(int folioId, IReadOnlyList<PagoParte> partes, string referencia = "")
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException("Inicia sesión para registrar pagos.");
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede registrar pagos.");
        var shift = new CajaService().ObtenerTurnoActual(user.Id) ?? throw new InvalidOperationException("Abre tu turno de caja para registrar pagos.");
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == folioId && item.Estado == "Abierto") ?? throw new InvalidOperationException("Selecciona un folio abierto.");
        if (partes.Count == 0 || partes.Any(item => item.Monto <= 0 || decimal.Round(item.Monto, 2) != item.Monto)) throw new InvalidOperationException("Cada medio de pago debe tener un importe positivo con dos decimales.");
        var selectedMethods = partes.Select(part => (Part: part, Method: SampleData.MetodosPago.FirstOrDefault(method => method.Nombre == part.Metodo && method.Activo))).ToList();
        if (selectedMethods.Any(item => item.Method is null)) throw new InvalidOperationException("Selecciona medios de pago activos.");
        var total = partes.Sum(item => item.Monto);
        var due = new FolioService().CalcularSaldoFolio(folioId);
        if (total > due) throw new InvalidOperationException($"El pago supera el saldo del folio. Saldo actual: Bs {due:N2}.");
        if (SampleData.Folios.First(item => item.Id == folioId).Estado != "Abierto") throw new InvalidOperationException("El folio ya no está abierto.");

        var guestId = folio.HuespedPrincipalId ?? (folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null) ?? (folio.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null) ?? 0;
        var created = new List<PagoModel>();
        foreach (var (part, method) in selectedMethods)
        {
            var paymentId = SampleData.Pagos.Count == 0 ? 1 : SampleData.Pagos.Max(item => item.Id) + 1;
            var payment = new PagoModel(paymentId, folioId, part.Monto, method!.Nombre, DateTime.Now, "Aplicado")
            { TurnoCajaId = shift.Id, UsuarioId = user.Id, Referencia = referencia.Trim(), Concepto = folio.Tipo == "Reserva" ? "Anticipo de reserva" : "Pago de folio", EsAnticipo = folio.Tipo == "Reserva" };
            SampleData.Pagos.Add(payment);
            SampleData.MovimientosCaja.Add(new(SampleData.MovimientosCaja.Count == 0 ? 1 : SampleData.MovimientosCaja.Max(item => item.Id) + 1, shift.Id, payment.Fecha, "Cobro", payment.Monto, payment.Concepto)
            { PagoId = payment.Id, FolioId = folioId, Metodo = method.Nombre, UsuarioId = user.Id });
            var stay = folio.EstanciaId is int stayKey ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayKey) : null;
            SampleData.Movimientos.Insert(0, new(payment.Fecha, "Pago", payment.Concepto, payment.Monto, payment.Metodo, guestId)
            { EstanciaId = folio.EstanciaId, TurnoCajaId = shift.Id, PagoId = payment.Id, FolioId = folioId });
            created.Add(payment);
        }
        UpdateFolioBalance(folioId);
        return created;
    }

    public PagoModel Obtener(int pagoId) => SampleData.Pagos.FirstOrDefault(item => item.Id == pagoId) ?? throw new InvalidOperationException("No se encontró el pago.");
    private static void UpdateFolioBalance(int folioId)
    {
        var index = SampleData.Folios.FindIndex(item => item.Id == folioId);
        if (index >= 0) SampleData.Folios[index] = SampleData.Folios[index] with { Saldo = new FolioService().CalcularSaldoFolio(folioId) };
    }
}

public sealed class CorreccionFinancieraService
{
    public DevolucionModel SolicitarDevolucion(int pagoId, decimal monto, string motivo, string descripcion)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede solicitar devoluciones.");
        var payment = new PagoService().Obtener(pagoId);
        if (payment.Estado == "Anulado") throw new InvalidOperationException("Un pago anulado no admite devoluciones.");
        if (monto <= 0 || string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Indica un importe y un motivo válido.");
        var reserved = SampleData.Devoluciones.Where(item => item.PagoId == pagoId && item.Estado is "Solicitada" or "Pendiente" or "Autorizada" or "Ejecutada").Sum(item => item.Monto);
        if (monto + reserved > payment.Monto) throw new InvalidOperationException("El importe solicitado supera el saldo disponible para devolver.");
        if (SampleData.Devoluciones.Any(item => item.PagoId == pagoId && item.Estado is "Solicitada" or "Pendiente" && item.Motivo == motivo)) throw new InvalidOperationException("Ya existe una solicitud pendiente con el mismo motivo para este pago.");
        var request = new DevolucionModel(NextId(SampleData.Devoluciones.Select(item => item.Id)), pagoId, monto, motivo.Trim(), "Pendiente") { Solicitada = DateTime.Now, UsuarioSolicitaId = user.Id, Descripcion = descripcion.Trim() };
        SampleData.Devoluciones.Add(request);
        new NotificacionService().Crear($"Devolución pendiente de revisión · Pago #{pagoId} · Bs {monto:N2}", "Gerente", "Finanzas", "Devolución", request.Id);
        return request;
    }
    public AnulacionModel SolicitarAnulacion(int pagoId, string motivo, string descripcion)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede solicitar anulaciones.");
        var payment = new PagoService().Obtener(pagoId);
        if (payment.Estado == "Anulado" || SampleData.Devoluciones.Any(item => item.PagoId == pagoId && item.Estado is "Ejecutada" or "Autorizada")) throw new InvalidOperationException("El pago tiene una corrección incompatible o ya fue anulado.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Indica un motivo para solicitar la anulación.");
        if (SampleData.Anulaciones.Any(item => item.PagoId == pagoId && item.Estado is "Pendiente" or "Solicitada" or "Autorizada")) throw new InvalidOperationException("Ya existe una solicitud de anulación abierta para este pago.");
        var request = new AnulacionModel(NextId(SampleData.Anulaciones.Select(item => item.Id)), pagoId, motivo.Trim(), "Pendiente") { Solicitada = DateTime.Now, UsuarioSolicitaId = user.Id, Descripcion = descripcion.Trim() };
        SampleData.Anulaciones.Add(request);
        new NotificacionService().Crear($"Anulación pendiente de revisión · Pago #{pagoId}", "Gerente", "Finanzas", "Anulación", request.Id);
        return request;
    }
    public void AutorizarDevolucion(int id) => ActualizarDevolucion(id, true);
    public void RechazarDevolucion(int id) => ActualizarDevolucion(id, false);
    public void AutorizarAnulacion(int id) => ActualizarAnulacion(id, true);
    public void RechazarAnulacion(int id) => ActualizarAnulacion(id, false);
    public IReadOnlyList<CorrectionDirectoryRow> Listar(string estado = "Todos")
    {
        var result = new List<CorrectionDirectoryRow>();
        foreach (var item in SampleData.Devoluciones)
        {
            var payment = SampleData.Pagos.FirstOrDefault(value => value.Id == item.PagoId);
            var folio = payment is null ? null : SampleData.Folios.FirstOrDefault(value => value.Id == payment.FolioId);
            var guestId = folio is null ? 0 : folio.HuespedPrincipalId ?? (folio.EstanciaId is int sid ? SampleData.Estancias.FirstOrDefault(value => value.Id == sid)?.HuespedId : null) ?? 0;
            var requester = SampleData.Usuarios.FirstOrDefault(value => value.Id == item.UsuarioSolicitaId)?.Nombre ?? "—";
            var approver = item.UsuarioAutorizaId is int aid ? SampleData.Usuarios.FirstOrDefault(value => value.Id == aid)?.Nombre ?? "—" : "—";
            result.Add(new(item.Id, "Devolución", SampleData.Huespedes.FirstOrDefault(value => value.Id == guestId)?.Nombre ?? "—", item.PagoId, payment?.FolioId ?? 0, item.Monto, payment?.Metodo ?? "—", item.Motivo, item.Estado, requester, approver, item.Solicitada, item.Estado == "Autorizada"));
        }
        foreach (var item in SampleData.Anulaciones)
        {
            var payment = SampleData.Pagos.FirstOrDefault(value => value.Id == item.PagoId);
            var folio = payment is null ? null : SampleData.Folios.FirstOrDefault(value => value.Id == payment.FolioId);
            var guestId = folio is null ? 0 : folio.HuespedPrincipalId ?? (folio.EstanciaId is int sid ? SampleData.Estancias.FirstOrDefault(value => value.Id == sid)?.HuespedId : null) ?? 0;
            result.Add(new(item.Id, "Anulación", SampleData.Huespedes.FirstOrDefault(value => value.Id == guestId)?.Nombre ?? "—", item.PagoId, payment?.FolioId ?? 0, payment?.Monto ?? 0, payment?.Metodo ?? "—", item.Motivo, item.Estado, SampleData.Usuarios.FirstOrDefault(value => value.Id == item.UsuarioSolicitaId)?.Nombre ?? "—", item.UsuarioAutorizaId is int aid ? SampleData.Usuarios.FirstOrDefault(value => value.Id == aid)?.Nombre ?? "—" : "—", item.Solicitada, item.Estado == "Autorizada"));
        }
        return result.Where(item => estado is "Todos" or "Todos los estados" || item.Estado == estado).OrderByDescending(item => item.Fecha).ToList();
    }
    public DevolucionModel EjecutarDevolucion(int id, string observaciones)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede ejecutar devoluciones.");
        var index = SampleData.Devoluciones.FindIndex(item => item.Id == id);
        if (index < 0) throw new InvalidOperationException("No se encontró la solicitud.");
        var request = SampleData.Devoluciones[index];
        if (request.Estado != "Autorizada" || request.Ejecutada is not null) throw new InvalidOperationException("La solicitud no está autorizada o ya fue ejecutada.");
        var shift = new CajaService().ObtenerTurnoActual(user.Id) ?? throw new InvalidOperationException("Abre tu turno de caja para ejecutar la devolución.");
        var payment = new PagoService().Obtener(request.PagoId);
        var alreadyRefunded = SampleData.Devoluciones.Where(item => item.PagoId == payment.Id && item.Estado == "Ejecutada").Sum(item => item.Monto);
        if (alreadyRefunded + request.Monto > payment.Monto || payment.Estado == "Anulado") throw new InvalidOperationException("El pago ya no admite esta devolución.");
        var updated = request with { Estado = "Ejecutada", Ejecutada = DateTime.Now, TurnoEjecucionId = shift.Id, MetodoEjecucion = payment.Metodo, Descripcion = string.IsNullOrWhiteSpace(observaciones) ? request.Descripcion : $"{request.Descripcion} · Ejecución: {observaciones.Trim()}" };
        SampleData.Devoluciones[index] = updated;
        SampleData.MovimientosCaja.Add(new(NextId(SampleData.MovimientosCaja.Select(item => item.Id)), shift.Id, updated.Ejecutada.Value, "Devolución", updated.Monto, updated.Motivo)
        { PagoId = payment.Id, FolioId = payment.FolioId, Metodo = payment.Metodo, UsuarioId = user.Id, CorreccionId = updated.Id });
        var refunded = SampleData.Devoluciones.Where(item => item.PagoId == payment.Id && item.Estado == "Ejecutada").Sum(item => item.Monto);
        var paymentIndex = SampleData.Pagos.FindIndex(item => item.Id == payment.Id);
        SampleData.Pagos[paymentIndex] = payment with { Estado = refunded >= payment.Monto ? "Devuelto" : "Parcialmente devuelto" };
        SampleData.Movimientos.Insert(0, new(updated.Ejecutada.Value, "Devolución", updated.Motivo, updated.Monto, payment.Metodo, GuestFor(payment.FolioId)) { TurnoCajaId = shift.Id, PagoId = payment.Id, FolioId = payment.FolioId });
        return updated;
    }
    public AnulacionModel EjecutarAnulacion(int id, string observaciones)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede ejecutar anulaciones.");
        var index = SampleData.Anulaciones.FindIndex(item => item.Id == id);
        if (index < 0) throw new InvalidOperationException("No se encontró la solicitud.");
        var request = SampleData.Anulaciones[index];
        if (request.Estado != "Autorizada" || request.Ejecutada is not null) throw new InvalidOperationException("La solicitud no está autorizada o ya fue ejecutada.");
        var shift = new CajaService().ObtenerTurnoActual(user.Id) ?? throw new InvalidOperationException("Abre tu turno para ejecutar la anulación.");
        var payment = new PagoService().Obtener(request.PagoId);
        if (payment.Estado == "Anulado" || SampleData.Devoluciones.Any(item => item.PagoId == payment.Id && item.Estado == "Ejecutada")) throw new InvalidOperationException("El pago ya tiene una corrección ejecutada.");
        var updated = request with { Estado = "Ejecutada", Ejecutada = DateTime.Now, TurnoEjecucionId = shift.Id, Descripcion = string.IsNullOrWhiteSpace(observaciones) ? request.Descripcion : $"{request.Descripcion} · Ejecución: {observaciones.Trim()}" };
        SampleData.Anulaciones[index] = updated;
        var paymentIndex = SampleData.Pagos.FindIndex(item => item.Id == payment.Id);
        SampleData.Pagos[paymentIndex] = payment with { Estado = "Anulado" };
        SampleData.MovimientosCaja.Add(new(NextId(SampleData.MovimientosCaja.Select(item => item.Id)), shift.Id, updated.Ejecutada.Value, "Anulación", payment.Monto, updated.Motivo)
        { PagoId = payment.Id, FolioId = payment.FolioId, Metodo = payment.Metodo, UsuarioId = user.Id, CorreccionId = updated.Id });
        SampleData.Movimientos.Insert(0, new(updated.Ejecutada.Value, "Anulación", updated.Motivo, payment.Monto, payment.Metodo, GuestFor(payment.FolioId)) { TurnoCajaId = shift.Id, PagoId = payment.Id, FolioId = payment.FolioId });
        return updated;
    }
    private static void ActualizarDevolucion(int id, bool approve)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Finanzas")) throw new UnauthorizedAccessException("Solo Gerencia puede revisar solicitudes financieras.");
        var index = SampleData.Devoluciones.FindIndex(item => item.Id == id);
        if (index < 0) throw new InvalidOperationException("No se encontró la solicitud.");
        var request = SampleData.Devoluciones[index];
        if (request.Estado != "Pendiente") throw new InvalidOperationException("La solicitud ya fue revisada.");
        if (request.UsuarioSolicitaId == user.Id) throw new UnauthorizedAccessException("No puedes autorizar una solicitud que tú mismo presentaste.");
        SampleData.Devoluciones[index] = request with { Estado = approve ? "Autorizada" : "Rechazada", UsuarioAutorizaId = user.Id, FechaAutorizacion = DateTime.Now };
    }
    private static void ActualizarAnulacion(int id, bool approve)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Finanzas")) throw new UnauthorizedAccessException("Solo Gerencia puede revisar solicitudes financieras.");
        var index = SampleData.Anulaciones.FindIndex(item => item.Id == id);
        if (index < 0) throw new InvalidOperationException("No se encontró la solicitud.");
        var request = SampleData.Anulaciones[index];
        if (request.Estado != "Pendiente") throw new InvalidOperationException("La solicitud ya fue revisada.");
        if (request.UsuarioSolicitaId == user.Id) throw new UnauthorizedAccessException("No puedes autorizar una solicitud que tú mismo presentaste.");
        SampleData.Anulaciones[index] = request with { Estado = approve ? "Autorizada" : "Rechazada", UsuarioAutorizaId = user.Id, FechaAutorizacion = DateTime.Now };
    }
    private static int GuestFor(int folioId)
    {
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == folioId);
        return folio?.HuespedPrincipalId ?? (folio?.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null) ?? 0;
    }
    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty().Max() + 1;
}

public sealed class FacturaService
{
    public IReadOnlyList<CargoModel> CargosFacturables(int folioId)
    {
        if (!SampleData.Folios.Any(item => item.Id == folioId)) throw new InvalidOperationException("No se encontró el folio.");
        var invoicedChargeIds = SampleData.Facturas.SelectMany(item => item.Lineas).Select(item => item.CargoId).Where(item => item > 0).ToHashSet();
        return SampleData.Cargos.Where(item => item.FolioId == folioId && !item.Anulado && !invoicedChargeIds.Contains(item.Id)).OrderBy(item => item.Fecha).ToList();
    }
    public InvoiceDraftModel Preparar(int folioId, string? cliente = null, string? documento = null)
    {
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == folioId) ?? throw new InvalidOperationException("No se encontró el folio.");
        var guestId = folio.HuespedPrincipalId ?? (folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null) ?? (folio.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null) ?? 0;
        var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId);
        var charges = CargosFacturables(folioId);
        if (charges.Count == 0) throw new InvalidOperationException("El folio no tiene cargos disponibles para facturar.");
        var lines = charges.Select(item => new FacturaLineaModel(item.Id, item.Concepto, 1, item.Monto, item.Monto)).ToList();
        var subtotal = lines.Sum(item => item.Importe);
        var taxRate = SampleData.Impuestos.FirstOrDefault(item => item.Activo)?.Porcentaje ?? 0;
        var tax = decimal.Round(subtotal * taxRate / 100m, 2, MidpointRounding.AwayFromZero);
        return new(folioId, string.IsNullOrWhiteSpace(cliente) ? guest?.Nombre ?? "Cliente no registrado" : cliente.Trim(), string.IsNullOrWhiteSpace(documento) ? guest?.Documento ?? "" : documento.Trim(), subtotal, taxRate, tax, subtotal + tax, lines);
    }
    public FacturaModel Emitir(InvoiceDraftModel draft)
    {
        var user = SesionActual.Usuario ?? throw new UnauthorizedAccessException();
        if (!NavigationService.Puede("Caja y facturación")) throw new UnauthorizedAccessException("Tu rol no puede emitir facturas.");
        if (SampleData.Folios.FirstOrDefault(item => item.Id == draft.FolioId) is null) throw new InvalidOperationException("El folio ya no está disponible.");
        var current = Preparar(draft.FolioId, draft.Cliente, draft.Documento);
        if (!current.Lineas.Select(item => item.CargoId).SequenceEqual(draft.Lineas.Select(item => item.CargoId)) || current.Total != draft.Total) throw new InvalidOperationException("Los cargos del folio cambiaron. Revisa la factura antes de emitirla.");
        if (current.Lineas.Any(line => SampleData.Facturas.SelectMany(item => item.Lineas).Any(existing => existing.CargoId == line.CargoId))) throw new InvalidOperationException("Uno o más cargos ya fueron facturados.");
        var invoiceId = SampleData.Facturas.Count == 0 ? 1 : SampleData.Facturas.Max(item => item.Id) + 1;
        var year = DateTime.Today.Year;
        var sequence = SampleData.Facturas.Count(item => item.Numero.StartsWith($"FAC-{year}-", StringComparison.OrdinalIgnoreCase)) + 1;
        var folio = SampleData.Folios.First(item => item.Id == draft.FolioId);
        var stay = folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId) : null;
        var reservationId = folio.ReservaId ?? stay?.ReservaId;
        var roomId = reservationId is int rid ? SampleData.Reservas.FirstOrDefault(item => item.Id == rid)?.Habitacion : null;
        var invoice = new FacturaModel(invoiceId, draft.FolioId, $"FAC-{year}-{sequence:0000}", DateTime.Now, current.Total, "Emitida")
        {
            Subtotal = current.Subtotal, Impuesto = current.Impuesto, PorcentajeImpuesto = current.PorcentajeImpuesto,
            Cliente = current.Cliente, DocumentoCliente = current.Documento, Lineas = current.Lineas,
            UsuarioEmiteId = user.Id, ReservaId = reservationId, EstanciaId = stay?.Id, HabitacionId = roomId
        };
        SampleData.Facturas.Add(invoice);
        return invoice;
    }
    public IReadOnlyList<InvoiceDirectoryRow> Listar(string busqueda = "", string estado = "Todos")
    {
        return SampleData.Facturas.Select(invoice =>
        {
            var folio = SampleData.Folios.FirstOrDefault(item => item.Id == invoice.FolioId);
            var guestId = folio is null ? 0 : folio.HuespedPrincipalId ?? (folio.EstanciaId is int stayId ? SampleData.Estancias.FirstOrDefault(item => item.Id == stayId)?.HuespedId : null) ?? (folio.ReservaId is int reservationId ? SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservationId && item.Principal)?.HuespedId : null) ?? 0;
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == guestId);
            var attributed = folio is not null && invoice.Lineas.Count > 0;
            var context = invoice.EstanciaId is int estId ? $"Estancia #{estId}" : invoice.ReservaId is int resId ? $"Reserva #{resId}" : attributed ? folio!.Tipo : "Legado sin folio verificado";
            return new InvoiceDirectoryRow(invoice, invoice.Cliente.Length > 0 ? invoice.Cliente : guest?.Nombre ?? "Cliente histórico sin identificar", attributed ? $"#{invoice.FolioId}" : "Sin atribución", context, attributed);
        }).Where(row => estado is "Todos" or "Todas" || row.Estado == estado)
          .Where(row => string.IsNullOrWhiteSpace(busqueda) || $"{row.Numero} {row.Huesped} {row.Folio} {row.Contexto} {row.Factura.DocumentoCliente}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
          .OrderByDescending(row => row.Fecha).ToList();
    }
}
