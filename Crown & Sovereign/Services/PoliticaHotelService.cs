using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed class PoliticaHotelService
{
    private static readonly IReadOnlyList<DefinicionPolitica> Specs =
    [
        new("checkin", "Hora de check-in", "Alojamiento", "Hora estándar diaria de ingreso; se muestra en los flujos compatibles de recepción.", "15:00", "hora", true, "Recepción muestra esta hora como referencia; no fuerza ni aprueba una habitación no preparada."),
        new("checkout", "Hora de check-out", "Alojamiento", "Hora estándar diaria de salida.", "11:00", "hora", true, "Recepción muestra la hora estándar; no extiende ni finaliza estancias automáticamente."),
        new("early", "Ingreso anticipado", "Alojamiento", "Condición para una llegada antes del horario estándar.", "Sujeto a disponibilidad y preparación de la habitación.", "texto", false, "Informativa; no calcula cargos ni permite omitir readiness."),
        new("late", "Salida tardía", "Alojamiento", "Condición para solicitar una salida posterior al horario estándar.", "Sujeta a disponibilidad y evaluación de recepción.", "texto", false, "Informativa; no calcula cargos ni reserva disponibilidad."),
        new("pets", "Admisión de mascotas", "Alojamiento", "Regla general del establecimiento y condiciones adicionales.", "Consultar", "mascotas", false, "Informativa; no existe una asignación confiable de habitaciones pet-friendly."),
        new("reservation_cancel_hours", "Cancelación gratuita", "Reservas y cancelaciones", "Horas antes de la hora estándar de ingreso.", "48", "horas", false, "La simulación es orientativa; el sistema no calcula ni cobra una penalización de alojamiento."),
        new("reservation_cancel_conditions", "Cancelación fuera del plazo", "Reservas y cancelaciones", "Condiciones a revisar fuera de la ventana gratuita.", "Consultar condiciones comerciales aplicables.", "texto", false, "Informativa; no se atribuye retroactivamente a reservas sin snapshot."),
        new("reservation_noshow", "No presentación", "Reservas y cancelaciones", "Condiciones generales para revisar una reserva no presentada.", "Consultar condiciones comerciales aplicables.", "texto", false, "Informativa; el paso de la fecha no confirma una inasistencia."),
        new("advance", "Anticipos", "Pagos y garantías", "Condiciones comerciales de anticipos.", "Según las condiciones comerciales aplicables.", "texto", false, "Informativa; no calcula ni registra anticipos automáticamente."),
        new("guarantee", "Garantías de reserva", "Pagos y garantías", "Condiciones para garantías y respaldo de reservas.", "No configurado.", "texto", false, "No hay motor de garantías/depositos con base de cálculo definida."),
        new("settlement", "Liquidación de saldos", "Pagos y garantías", "Condiciones informativas de liquidación.", "Liquidar el saldo durante el checkout.", "texto", false, "El check-out aplica el saldo calculado por FolioService."),
        new("refunds", "Devoluciones y anulaciones", "Pagos y garantías", "Control de correcciones financieras.", "Requieren autorización de Gerencia y ejecución posterior en Caja.", "inmutable", true, "Invariante financiera protegida; no editable desde políticas.")
    ];

    public IReadOnlyList<DefinicionPolitica> Definiciones => Specs;
    public IReadOnlyList<PoliticaModel> Politicas() => SampleData.Politicas.ToList();
    public PoliticaServicioModel? PoliticaServicioActual => SampleData.PoliticasServicio.FirstOrDefault();
    public bool PersistenciaDisponible => Persistencia.Activa;

    public IReadOnlyList<ConfiguracionAuditModel> Historial(string query = "", string category = "Todas", int? userId = null, DateTime? from = null, DateTime? to = null)
        => SampleData.HistorialPoliticas.Where(x => (string.IsNullOrWhiteSpace(query) || $"{x.Cambio} {x.ValorAnterior} {x.ValorNuevo}".Contains(query, StringComparison.OrdinalIgnoreCase))
            && (from is null || x.Fecha.Date >= from.Value.Date) && (to is null || x.Fecha.Date <= to.Value.Date)
            && (userId is null || x.UsuarioId == userId) && (category == "Todas" || CategoryFromKey(x.Cambio) == category))
            .OrderByDescending(x => x.Fecha).ToList();

    public IReadOnlyList<string> Diagnosticos()
    {
        var result = new List<string>();
        foreach (var spec in Specs.Where(x => x.TipoEditor != "inmutable"))
        {
            var matches = Find(spec.Codigo);
            if (matches.Count > 1) result.Add($"Configuración duplicada: {spec.Nombre}. Hay {matches.Count} filas con identificador equivalente.");
            else if (matches.Count == 0) result.Add($"Sin configurar: {spec.Nombre}. Se conserva como valor no guardado hasta que Gerencia lo configure.");
            else if (!TryNormalize(spec, matches[0].Valor, out _)) result.Add($"Valor inválido: {spec.Nombre} no coincide con el formato esperado.");
        }
        if (PoliticaServicioActual is null) result.Add("No existe una política estructurada de servicios para el hotel.");
        else try { ValidarPoliticaServicio(PoliticaServicioActual); } catch (InvalidOperationException ex) { result.Add("Política de servicios inválida: " + ex.Message); }
        var legacy = SampleData.Citas.Count(x => string.IsNullOrWhiteSpace(x.PoliticaAplicadaJson));
        if (legacy > 0) result.Add($"{legacy} citas no conservan una política histórica verificable.");
        return result;
    }

    public string Valor(string code)
    {
        var spec = Specs.First(x => x.Codigo == code);
        var matches = Find(code);
        if (matches.Count > 1) throw new InvalidOperationException($"Hay definiciones duplicadas para «{spec.Nombre}». No se eligió una arbitrariamente.");
        var stored = matches.FirstOrDefault()?.Valor;
        if (stored is null) return "Sin configurar";
        if (!TryNormalize(spec, stored, out var normalized)) return $"Valor inválido · {stored}";
        return code == "reservation_cancel_hours" ? $"{normalized} horas" : normalized;
    }

    public bool TryGetTime(string code, out TimeOnly time)
    {
        time = default; var spec = Specs.First(x => x.Codigo == code); var items = Find(code);
        return items.Count == 1 && TimeOnly.TryParse(items[0].Valor, CultureInfo.InvariantCulture, out time);
    }
    public TimeOnly HoraCheckIn() => TryGetTime("checkin", out var time) ? time : TimeOnly.Parse("15:00", CultureInfo.InvariantCulture);
    public TimeOnly HoraCheckOut() => TryGetTime("checkout", out var time) ? time : TimeOnly.Parse("11:00", CultureInfo.InvariantCulture);

    public PoliticaModel Guardar(string code, string value)
    {
        Authorize();
        var spec = Specs.FirstOrDefault(x => x.Codigo == code) ?? throw new InvalidOperationException("No se encontró la política seleccionada.");
        if (spec.TipoEditor == "inmutable") throw new UnauthorizedAccessException("Esta regla es un control financiero obligatorio y no se puede desactivar.");
        if (!TryNormalize(spec, value, out var normalized)) throw new InvalidOperationException("El valor no coincide con el formato esperado.");
        var matches = Find(code);
        if (matches.Count > 1) throw new InvalidOperationException($"Hay varias políticas para «{spec.Nombre}». Resuelve la duplicidad antes de editar.");
        var old = matches.SingleOrDefault();
        if (old?.Valor == normalized) return old;
        var user = SesionActual.Usuario!.Id;
        var hotelId = old is null && Persistencia.Activa ? CurrentHotelId() : (SampleData.PoliticasServicio.FirstOrDefault()?.HotelId ?? 1);
        var updated = Persistencia.Activa
            ? PoliticaRepositorio.GuardarPolitica(hotelId, old, spec.Nombre, spec.Descripcion, normalized, user, "POLITICA." + code.ToUpperInvariant())
            : new PoliticaModel(old?.Id ?? (SampleData.Politicas.Count == 0 ? 1 : SampleData.Politicas.Max(x => x.Id) + 1), spec.Nombre, spec.Descripcion, normalized);
        if (old is not null) SampleData.Politicas.Remove(old);
        SampleData.Politicas.Add(updated);
        SampleData.HistorialPoliticas.Insert(0, new(SampleData.HistorialPoliticas.Count == 0 ? 1 : SampleData.HistorialPoliticas.Max(x => x.Id) + 1, hotelId, user,
            "POLITICA." + code.ToUpperInvariant(), old?.Valor ?? "(sin configurar)", normalized, DateTime.Now));
        return updated;
    }

    public ConfiguracionAuditModel Restaurar(ConfiguracionAuditModel item)
    {
        Authorize();
        var code = item.Cambio.StartsWith("POLITICA.", StringComparison.OrdinalIgnoreCase) ? item.Cambio["POLITICA.".Length..].ToLowerInvariant() : "";
        if (Specs.All(x => x.Codigo != code)) throw new InvalidOperationException("No es posible interpretar este valor histórico con seguridad.");
        var before = SampleData.HistorialPoliticas.Count;
        Guardar(code, item.ValorAnterior);
        if (SampleData.HistorialPoliticas.Count == before) throw new InvalidOperationException("La política ya tiene ese valor; no se realizó una restauración ni se creó auditoría.");
        return SampleData.HistorialPoliticas[0];
    }

    public void GuardarServicio(PoliticaServicioModel next)
    {
        Authorize(); ValidarPoliticaServicio(next);
        var current = PoliticaServicioActual;
        var user = SesionActual.Usuario!.Id;
        var hotelId = current?.HotelId ?? (Persistencia.Activa ? CurrentHotelId() : 1);
        next = next with { HotelId = hotelId };
        if (current == next) return;
        if (Persistencia.Activa) PoliticaRepositorio.GuardarPoliticaServicio(current, next, user);
        if (current is null) SampleData.PoliticasServicio.Add(next);
        else SampleData.PoliticasServicio[SampleData.PoliticasServicio.IndexOf(current)] = next;
        RegisterServiceAudit(current, next, hotelId, user);
    }

    public SimulacionPoliticaResultado SimularCancelacionReserva(DateTime llegada, DateTime cancelada)
    {
        var text = Find("reservation_cancel_hours").SingleOrDefault()?.Valor;
        if (!int.TryParse(StripHours(text ?? ""), out var hours) || hours < 0)
            return new("No disponible", "El plazo de cancelación no está configurado correctamente.", 0, "Sin base de cálculo", false, "No modifica la reserva ni registra un cargo.");
        var checkin = llegada.Date.Add(HoraCheckIn().ToTimeSpan()); var boundary = checkin.AddHours(-hours);
        return cancelada <= boundary
            ? new("Dentro del plazo gratuito", $"La cancelación simulada ocurre antes del límite {boundary:dd/MM/yyyy HH:mm}.", 0, "No se calcula penalización", false, "La reserva no conserva un snapshot confiable; revisar antes de aplicar a una reserva existente.")
            : new("Fuera del plazo gratuito", $"La cancelación simulada ocurre después del límite {boundary:dd/MM/yyyy HH:mm}.", 0, "No hay penalización monetaria estructurada", false, "La simulación no cambia reservas, cargos ni pagos.");
    }

    public static void ValidarPoliticaServicio(PoliticaServicioModel model)
    {
        if (model.HorasCancelacionGratuita < 0) throw new InvalidOperationException("Las horas de cancelación gratuita no pueden ser negativas.");
        ValidatePenalty(model.TipoPenalizacionCancelacion, model.ValorPenalizacionCancelacion);
        ValidatePenalty(model.TipoPenalizacionInasistencia, model.ValorPenalizacionInasistencia);
    }
    public static decimal CalcularPenalizacion(string type, decimal value, decimal? baseAmount)
    {
        ValidatePenalty(type, value);
        var normalized = PenaltyCode(type);
        if (normalized == "NINGUNA") return 0m;
        if (baseAmount is null || baseAmount < 0) throw new InvalidOperationException("No existe un importe histórico confiable para calcular la penalización.");
        return decimal.Round(normalized == "MONTO_FIJO" ? value : baseAmount.Value * value / 100m, 2, MidpointRounding.AwayFromZero);
    }
    public SimulacionPoliticaResultado SimularServicio(DateTime appointmentAt, DateTime simulatedAt, bool noShow, decimal? historicalPrice, bool packageCovered, bool unlimitedBenefit, PoliticaServicioModel? policyOverride = null)
    {
        var policy = policyOverride ?? PoliticaServicioActual ?? throw new InvalidOperationException("No existe una política de servicios configurada.");
        var snapshotType = noShow ? policy.TipoPenalizacionInasistencia : policy.TipoPenalizacionCancelacion;
        var snapshotAmount = noShow ? policy.ValorPenalizacionInasistencia : policy.ValorPenalizacionCancelacion;
        var consume = packageCovered && (noShow ? policy.ConsumirBeneficioInasistencia : policy.ConsumirBeneficioCancelacionTardia);
        if (!noShow && simulatedAt <= appointmentAt.AddHours(-policy.HorasCancelacionGratuita))
            return new("Cancelación gratuita", "La cancelación ocurre en o antes del límite gratuito.", 0, "Sin penalización", false, "Sólo simulación; ningún beneficio fue consumido.");
        if (packageCovered)
            return new(noShow ? "Inasistencia" : "Cancelación tardía", consume ? (unlimitedBenefit ? "La política dispone consumir el beneficio; la cobertura es ilimitada y no se calcula un saldo numérico." : "La política dispone consumir una utilización del beneficio.") : "No se consume el beneficio según la política actual.", 0, "Servicio cubierto por paquete; no se infiere precio cobrado al huésped", consume, "Ningún beneficio fue consumido durante la simulación.");
        try
        {
            var amount = CalcularPenalizacion(snapshotType, snapshotAmount, historicalPrice);
            return new(noShow ? "Inasistencia" : "Cancelación tardía", $"Penalización simulada: Bs {amount:N2}.", amount,
                snapshotType == "PORCENTAJE" || snapshotType == "Porcentaje" ? $"{snapshotAmount:N2}% sobre Bs {historicalPrice:N2} preservados" : "Monto fijo configurado", false, "No se crea cargo, cita ni consumo.");
        }
        catch (InvalidOperationException ex) { return new("Requiere revisión", ex.Message, 0, "Base de cálculo no disponible", false, "No se modifica ningún registro."); }
    }
    public static string SnapshotActualServicio() => SampleData.PoliticasServicio.Count == 0 ? "" : JsonSerializer.Serialize(SampleData.PoliticasServicio[0]);
    public static PoliticaServicioModel? LeerSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<PoliticaServicioModel>(json); } catch (JsonException) { return null; }
    }

    private List<PoliticaModel> Find(string code)
    {
        var names = code switch
        {
            "checkin" => new[] { "checkin", "horariodecheckin", "horaingreso" },
            "checkout" => new[] { "checkout", "horariodecheckout", "horadesalida" },
            "reservation_cancel_hours" => new[] { "cancelacion", "cancelacionsinpenalizacion", "plazocancelacion" },
            "pets" => new[] { "mascotas", "politicamascotas" },
            _ => new[] { "policycode:" + code }
        };
        var canonical = Specs.FirstOrDefault(x => x.Codigo == code);
        var allNames = canonical is null ? names : names.Concat([Normalize(canonical.Nombre)]).ToArray();
        return SampleData.Politicas.Where(x => allNames.Contains(Normalize(x.Nombre))).ToList();
    }
    private static bool TryNormalize(DefinicionPolitica spec, string value, out string normalized)
    {
        normalized = value.Trim();
        switch (spec.TipoEditor)
        {
            case "hora": if (TimeOnly.TryParse(value, CultureInfo.CurrentCulture, out var time) || TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out time)) { normalized = time.ToString("HH:mm", CultureInfo.InvariantCulture); return true; } return false;
            case "horas":
                var stripped = StripHours(value);
                if (int.TryParse(stripped, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours) && hours >= 0) { normalized = hours.ToString(CultureInfo.InvariantCulture); return true; }
                return false;
            case "mascotas": return new[] { "No se admiten mascotas.", "Se admiten mascotas.", "Admisión sujeta a condiciones." }.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
            case "inmutable": return false;
            default: return !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 1000;
        }
    }
    private static string StripHours(string text) => Regex.Replace(text.Trim(), @"\s*horas?\s*$", "", RegexOptions.IgnoreCase);
    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
    }
    private static void Authorize() { if (SesionActual.Usuario?.Rol != "Gerente" || !NavigationService.Puede("Políticas del hotel")) throw new UnauthorizedAccessException("Solo Gerencia puede modificar políticas empresariales."); }
    private static int CurrentHotelId() => Convert.ToInt32(DatabaseConnection.Escalar("SELECT hotel_id FROM public.hotel ORDER BY hotel_id LIMIT 1") ?? throw new InvalidOperationException("No se encontró el hotel."));
    private static string CategoryFromKey(string change)
    {
        if (change.StartsWith("POLITICA_SERVICIO", StringComparison.OrdinalIgnoreCase)) return "Servicios";
        var code = change.StartsWith("POLITICA.", StringComparison.OrdinalIgnoreCase) ? change["POLITICA.".Length..].ToLowerInvariant() : "";
        return Specs.FirstOrDefault(x => x.Codigo == code)?.Categoria ?? "Todas";
    }
    private static void ValidatePenalty(string type, decimal value)
    {
        var normalized = PenaltyCode(type);
        if (normalized is not ("NINGUNA" or "MONTO_FIJO" or "PORCENTAJE")) throw new InvalidOperationException("El tipo de penalización no está soportado.");
        if (value < 0 || normalized == "PORCENTAJE" && value > 100) throw new InvalidOperationException("El importe debe ser no negativo y el porcentaje no puede superar 100%.");
        if (normalized == "NINGUNA" && value != 0) throw new InvalidOperationException("Una política sin penalización debe tener importe cero.");
    }
    private static string PenaltyCode(string value) => value.Trim().ToUpperInvariant() switch { "NINGUNA" or "NINGUNO" or "SIN PENALIZACIÓN" or "SIN PENALIZACION" => "NINGUNA", "MONTO_FIJO" or "MONTO FIJO" or "FIJO" => "MONTO_FIJO", "PORCENTAJE" or "PERCENTAGE" => "PORCENTAJE", _ => value.Trim().ToUpperInvariant() };
    private static void RegisterServiceAudit(PoliticaServicioModel? old, PoliticaServicioModel next, int hotel, int user)
    {
        void Add(string key, string before, string after) { if (before == after) return; SampleData.HistorialPoliticas.Insert(0, new(SampleData.HistorialPoliticas.Count == 0 ? 1 : SampleData.HistorialPoliticas.Max(x => x.Id) + 1, hotel, user, key, before, after, DateTime.Now)); }
        Add("POLITICA_SERVICIO.HORAS_CANCELACION_GRATUITA", old?.HorasCancelacionGratuita.ToString() ?? "(sin configurar)", next.HorasCancelacionGratuita.ToString());
        Add("POLITICA_SERVICIO.PENALIZACION_CANCELACION", old is null ? "(sin configurar)" : $"{old.TipoPenalizacionCancelacion}:{old.ValorPenalizacionCancelacion}", $"{next.TipoPenalizacionCancelacion}:{next.ValorPenalizacionCancelacion}");
        Add("POLITICA_SERVICIO.CONSUMIR_BENEFICIO_CANCELACION_TARDIA", old?.ConsumirBeneficioCancelacionTardia.ToString() ?? "(sin configurar)", next.ConsumirBeneficioCancelacionTardia.ToString());
        Add("POLITICA_SERVICIO.PENALIZACION_INASISTENCIA", old is null ? "(sin configurar)" : $"{old.TipoPenalizacionInasistencia}:{old.ValorPenalizacionInasistencia}", $"{next.TipoPenalizacionInasistencia}:{next.ValorPenalizacionInasistencia}");
        Add("POLITICA_SERVICIO.CONSUMIR_BENEFICIO_INASISTENCIA", old?.ConsumirBeneficioInasistencia.ToString() ?? "(sin configurar)", next.ConsumirBeneficioInasistencia.ToString());
    }
}
