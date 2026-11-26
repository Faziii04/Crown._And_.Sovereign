using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class ReservaDialog : Window
{
    private readonly ReservaService _reservas = new();
    private readonly HabitacionDisponibilidadService _disponibilidad = new();
    private readonly ReservaModel? _existente;
    private readonly BorradorReserva _borrador;
    public ReservaModel? ReservaCreada { get; private set; }

    public ReservaDialog(DateTime? llegada = null, string? tipoHabitacion = null, ReservaModel? existente = null, int? habitacionId = null, BorradorReserva? borrador = null)
    {
        InitializeComponent();
        var workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(900, workArea.Width); MinHeight = Math.Min(560, workArea.Height);
        Width = Math.Min(1140, workArea.Width * .96); Height = Math.Min(780, workArea.Height * .92);
        _existente = existente;
        if (existente is not null && borrador is null)
        {
            borrador = new BorradorReserva
            {
                ReservaOrigenId = existente.Id, Llegada = existente.Llegada.Date, Salida = existente.Salida.Date,
                Personas = Math.Max(1, existente.Personas), Canal = existente.Canal, TipoPreferido = SampleData.Habitaciones.First(item => item.Id == existente.Habitacion).Tipo,
                HuespedPrincipalId = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == existente.Id && item.Principal)?.HuespedId ?? SampleData.Huespedes.FirstOrDefault(item => item.Nombre == existente.Huesped)?.Id,
                PromocionId = existente.PromocionId, GrupoId = existente.GrupoId
            };
            borrador.HabitacionesSeleccionadas.AddRange(SampleData.ReservaHabitaciones.Where(item => item.ReservaId == existente.Id).Select(item => item.HabitacionId).Append(existente.Habitacion).Distinct());
            borrador.HuespedesAcompanantes.AddRange(SampleData.ReservaHuespedes.Where(item => item.ReservaId == existente.Id && !item.Principal).Select(item => item.HuespedId));
            borrador.Personas = Math.Max(borrador.Personas, 1 + borrador.HuespedesAcompanantes.Count);
            var purchasedPackage = SampleData.ReservaPaquetes.FirstOrDefault(item => item.ReservaId == existente.Id);
            borrador.PaqueteId = purchasedPackage?.PaqueteId; borrador.CantidadPaquete = purchasedPackage?.Cantidad ?? 1; borrador.PrecioPaqueteAcordado = purchasedPackage?.PrecioCompra;
            borrador.RecursosAdicionales.AddRange(SampleData.ReservaRecursos.Where(item => item.ReservaId == existente.Id).Select(item => new ReservaRecursoBorrador
            { RecursoId = item.RecursoId, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, TipoCobroAplicado = item.TipoCobroAplicado, Estado = item.Estado, Observaciones = item.Observaciones, CargoId = item.CargoId }));
            borrador.ServiciosAdquiridos.AddRange(SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == existente.Id).Select(item => new ServicioAdquiridoBorrador
            { ServicioId = item.ServicioId, HuespedId = item.HuespedId, ReservaHabitacionId = item.ReservaHabitacionId, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, Estado = item.Estado, CargoId = item.CargoId, Observaciones = item.Observaciones }));
            borrador.SolicitudesEspeciales = SampleData.SolicitudesEspeciales.FirstOrDefault(item => item.ReservaId == existente.Id)?.Descripcion ?? "";
        }
        _borrador = borrador ?? new BorradorReserva();
        _borrador.ReservaOrigenId ??= existente?.Id;

        if (existente is not null) { Title = $"Editar reserva #{existente.Id}"; DialogTitle.Text = Title; SaveButton.Content = "Guardar cambios"; SavePendingButton.Visibility = Visibility.Collapsed; }
        GuestsStep.ConfigurarHuespedes(SampleData.Huespedes);
        GuestsStep.HuespedPrincipal.SelectedItem = SampleData.Huespedes.FirstOrDefault(g => g.Id == _borrador.HuespedPrincipalId) ?? SampleData.Huespedes.FirstOrDefault(g => g.Nombre == existente?.Huesped) ?? SampleData.Huespedes.FirstOrDefault();
        StayStep.GuestCount.Text = Math.Max(1, _borrador.Personas).ToString();
        StayStep.Arrival.SelectedDate = existente?.Llegada.Date ?? _borrador.Llegada.Date;
        StayStep.Departure.SelectedDate = existente?.Salida.Date ?? _borrador.Salida.Date;
        StayStep.Channel.ItemsSource = new[] { "Directa", "Agencia", "Teléfono", "Sitio web" };
        StayStep.Channel.SelectedItem = _borrador.Canal;
        ExtrasStep.Paquete.ItemsSource = new[] { "Sin paquete" }.Concat(SampleData.Paquetes.Where(item => item.Activo).Select(item => item.Nombre)).ToList();
        ExtrasStep.Paquete.SelectedItem = SampleData.Paquetes.FirstOrDefault(item => item.Id == _borrador.PaqueteId)?.Nombre ?? "Sin paquete";
        ExtrasStep.CantidadPaquete.SelectedItem = Math.Clamp(_borrador.CantidadPaquete, 1, 5);
        ExtrasStep.Promocion.ItemsSource = SampleData.Promociones.Where(item => item.Hasta.Date >= DateTime.Today).ToList();
        ExtrasStep.Promocion.SelectedItem = SampleData.Promociones.FirstOrDefault(item => item.Id == _borrador.PromocionId);
        ExtrasStep.Solicitud.Text = _borrador.SolicitudesEspeciales;

        var selectedGuests = SelectedGuests();
        var start = StayStep.Arrival.SelectedDate ?? _borrador.Llegada; var end = StayStep.Departure.SelectedDate ?? _borrador.Salida;
        ExtrasStep.Configurar(_borrador, selectedGuests, start, end, existente?.Id);
        ExtrasStep.Recursos.DraftChanged += (_, _) => ActualizarResumen();
        ExtrasStep.ServiciosAdquiridos.DraftChanged += (_, _) => ActualizarResumen();

        StayStep.Arrival.SelectedDateChanged += (_, _) => { if (IsLoaded) { CargarHabitaciones(RoomsStep.HabitacionesSeleccionadas); ActualizarAdicionalesPorFechas(); ActualizarResumen(); } };
        StayStep.Departure.SelectedDateChanged += (_, _) => { if (IsLoaded) { CargarHabitaciones(RoomsStep.HabitacionesSeleccionadas); ActualizarAdicionalesPorFechas(); ActualizarResumen(); } };
        RoomsStep.SeleccionCambiada += ActualizarResumen;
        StayStep.GuestCount.TextChanged += (_, _) => { if (IsLoaded) ActualizarResumen(); };
        GuestsStep.HuespedPrincipal.SelectionChanged += (_, _) => { GuestsStep.RefrescarListadoAcompanantes(); RefreshServiceCoverage(); ActualizarResumen(); };
        GuestsStep.Acompanantes.SelectionChanged += (_, _) => { RefreshServiceCoverage(); ActualizarResumen(); };
        ExtrasStep.Paquete.SelectionChanged += (_, _) => { if (IsLoaded) { SyncPackageSelection(); RefreshServiceCoverage(); ActualizarResumen(); } };
        ExtrasStep.CantidadPaquete.SelectionChanged += (_, _) => { if (IsLoaded) { SyncPackageSelection(); RefreshServiceCoverage(); ActualizarResumen(); } };
        ExtrasStep.Promocion.SelectionChanged += (_, _) => { if (IsLoaded) ActualizarResumen(); };
        ExtrasStep.Solicitud.TextChanged += (_, _) => { if (IsLoaded) ActualizarResumen(); };

        var existingRoomIds = existente is null ? [] : SampleData.ReservaHabitaciones.Where(item => item.ReservaId == existente.Id).Select(item => item.HabitacionId).Append(existente.Habitacion).Distinct().ToList();
        var initialRoomIds = _borrador.HabitacionesSeleccionadas.Count > 0 ? _borrador.HabitacionesSeleccionadas.ToList() : existingRoomIds;
        if (initialRoomIds.Count == 0 && habitacionId is int preferredRoom) initialRoomIds.Add(preferredRoom);
        foreach (var companion in GuestsStep.Acompanantes.Items.Cast<HuespedModel>().Where(item => _borrador.HuespedesAcompanantes.Contains(item.Id))) GuestsStep.Acompanantes.SelectedItems.Add(companion);
        CargarHabitaciones(initialRoomIds, tipoHabitacion ?? _borrador.TipoPreferido, initialRoomIds.Count == 0);
        SyncPackageSelection();
        ActualizarResumen();
        WizardTabs.SelectedIndex = Math.Clamp(_borrador.PasoInicial, 0, 4); ActualizarPaso();
    }

    private DateTime FechaEntrada => StayStep.Arrival.SelectedDate ?? DateTime.Today.AddDays(1);
    private DateTime FechaSalida => StayStep.Departure.SelectedDate ?? FechaEntrada.AddDays(1);
    private IReadOnlyList<HuespedModel> SelectedGuests()
    {
        var selected = new List<HuespedModel>();
        if (GuestsStep.HuespedPrincipal.SelectedItem is HuespedModel primary) selected.Add(primary);
        selected.AddRange(GuestsStep.Acompanantes.SelectedItems.Cast<HuespedModel>());
        return selected.DistinctBy(item => item.Id).ToList();
    }
    private void SyncPackageSelection()
    {
        var previousPackageId = _borrador.PaqueteId;
        var selectedPackageId = SampleData.Paquetes.FirstOrDefault(item => item.Nombre == ExtrasStep.Paquete.SelectedItem?.ToString())?.Id;
        if (selectedPackageId != previousPackageId) _borrador.PrecioPaqueteAcordado = selectedPackageId is int selectedId ? SampleData.Paquetes.First(item => item.Id == selectedId).Precio : null;
        else if (selectedPackageId is int unchangedId && _borrador.PrecioPaqueteAcordado is null) _borrador.PrecioPaqueteAcordado = SampleData.Paquetes.First(item => item.Id == unchangedId).Precio;
        _borrador.PaqueteId = selectedPackageId;
        _borrador.CantidadPaquete = _borrador.PaqueteId.HasValue && int.TryParse(ExtrasStep.CantidadPaquete.SelectedItem?.ToString(), out var quantity) ? quantity : 1;
        _borrador.PromocionId = (ExtrasStep.Promocion.SelectedItem as PromocionModel)?.Id;
        ExtrasStep.MostrarPrecioPaquete(_borrador.PrecioPaqueteAcordado);
    }
    private void RefreshServiceCoverage()
    {
        SyncPackageSelection(); ExtrasStep.ServiciosAdquiridos.ActualizarCobertura(_borrador.PaqueteId, SelectedGuests());
    }
    private void ActualizarAdicionalesPorFechas()
    {
        _borrador.Llegada = FechaEntrada.Date; _borrador.Salida = FechaSalida.Date;
        ExtrasStep.Recursos.ActualizarFechas(FechaEntrada, FechaSalida);
        ExtrasStep.ServiciosAdquiridos.Configurar(_borrador, SelectedGuests());
    }
    private bool _reservasRoomAvailable(int roomId)
    {
        return _existente is null ? _disponibilidad.EsDisponible(roomId, FechaEntrada, FechaSalida) : _disponibilidad.EsDisponible(roomId, FechaEntrada, FechaSalida, _existente.Id);
    }
    private int PersonasSolicitadas => int.TryParse(StayStep.GuestCount.Text, out var people) ? Math.Clamp(people, 1, 30) : 1;

    private void CargarHabitaciones(IEnumerable<int>? selectedIds = null, string? preferredType = null, bool suggestInitialRoom = false)
    {
        var arrival = FechaEntrada; var departure = FechaSalida;
        if (departure <= arrival) departure = arrival.AddDays(1);
        var criteria = new ReservationSearchCriteria { Llegada = arrival.Date, Salida = departure.Date, Personas = PersonasSolicitadas, Habitaciones = 1 };
        var available = _disponibilidad.BuscarHabitaciones(criteria, excluirReservaId: _existente?.Id)
            .Select(item => (Habitacion: SampleData.Habitaciones.First(room => room.Id == item.Id), item.TotalEstancia)).ToList();
        var ids = selectedIds?.Where(id => available.Any(item => item.Habitacion.Id == id)).Distinct().ToList() ?? [];
        if (suggestInitialRoom && ids.Count == 0 && available.Count > 0)
        {
            var suggestion = available.OrderByDescending(item => item.Habitacion.Capacidad).ThenBy(item => item.TotalEstancia).First(); ids.Add(suggestion.Habitacion.Id);
        }
        RoomsStep.Mostrar(available, ids); RoomsStep.EstablecerTipoPreferido(preferredType);
    }

    private void PreviousStep_Click(object sender, RoutedEventArgs e) { WizardTabs.SelectedIndex = Math.Max(0, WizardTabs.SelectedIndex - 1); ActualizarPaso(); }
    private void NextStep_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidarPaso(WizardTabs.SelectedIndex)) return;
        WizardTabs.SelectedIndex = Math.Min(4, WizardTabs.SelectedIndex + 1); ActualizarPaso(); ActualizarResumen();
    }
    private void ActualizarPaso()
    {
        var last = WizardTabs.SelectedIndex == 4; PreviousStepButton.IsEnabled = WizardTabs.SelectedIndex > 0;
        NextStepButton.Visibility = last ? Visibility.Collapsed : Visibility.Visible; SavePendingButton.Visibility = last ? Visibility.Visible : Visibility.Collapsed; SaveButton.Visibility = last ? Visibility.Visible : Visibility.Collapsed;
    }
    private bool ValidarPaso(int step)
    {
        ValidationText.Text = "";
        if (step == 0)
        {
            if (FechaEntrada >= FechaSalida) { ValidationText.Text = "La salida debe ser posterior a la llegada."; return false; }
            if (!int.TryParse(StayStep.GuestCount.Text, out var peopleCount)) { ValidationText.Text = "Indica cuántas personas se alojarán."; return false; }
            if (peopleCount is < 1 or > 30) { ValidationText.Text = "La reserva admite entre 1 y 30 personas."; return false; }
        }
        if (step == 1 && RoomsStep.HabitacionesSeleccionadas.Count == 0) { ValidationText.Text = "Selecciona al menos una habitación."; return false; }
        if (step == 1 && RoomsStep.CapacidadSeleccionada < PersonasSolicitadas) { ValidationText.Text = $"La capacidad marcada es de {RoomsStep.CapacidadSeleccionada} plaza(s) para {PersonasSolicitadas} personas. Añade otra habitación."; return false; }
        if (step == 1 && RoomsStep.RequiereMismoPiso && !RoomsStep.SeleccionEnMismoPiso) { ValidationText.Text = "Mantén todas las habitaciones seleccionadas en el mismo piso o quita esa preferencia."; return false; }
        if (step == 2 && GuestsStep.HuespedPrincipal.SelectedItem is not HuespedModel) { ValidationText.Text = "Selecciona el huésped principal."; return false; }
        return true;
    }

    private void ActualizarResumen()
    {
        var selectedRooms = RoomsStep.HabitacionesSeleccionadasModels; var room = selectedRooms.FirstOrDefault();
        var start = FechaEntrada; var end = FechaSalida; var nights = Math.Max(0, (end.Date - start.Date).Days);
        _borrador.Llegada = start.Date; _borrador.Salida = end.Date; _borrador.HabitacionesSeleccionadas.Clear(); _borrador.HabitacionesSeleccionadas.AddRange(selectedRooms.Select(item => item.Id));
        _borrador.Personas = PersonasSolicitadas; SyncPackageSelection();
        var summary = new TarifaService().CalcularResumenEconomico(_borrador);
        RoomsStep.MostrarCapacidad(PersonasSolicitadas);
        if (RoomsStep.Asignacion is not null) RoomsStep.Asignacion.Text = $"Huésped principal: {(GuestsStep.HuespedPrincipal?.SelectedItem as HuespedModel)?.Nombre ?? "Sin seleccionar"}\nAcompañantes identificados: {GuestsStep.Acompanantes?.SelectedItems.Count ?? 0} de {PersonasSolicitadas - 1} posible(s)";
        if (StayStep.Duration is not null) StayStep.Duration.Text = $"{nights} noches · {start:dd/MM/yyyy} a {end:dd/MM/yyyy}";
        StayDurationAside.Text = $"{nights} noches · {start:dd/MM}–{end:dd/MM/yyyy} · {PersonasSolicitadas} huésped(es) · {selectedRooms.Count} habitación(es)";
        RateSummary.Text = room is null ? "Tarifa no disponible" : $"{selectedRooms.Count} habitación(es) · {selectedRooms.Sum(item => item.Capacidad)} plazas\n{summary.Alojamiento:N2} Bs alojamiento";
        PackageSummary.Text = _borrador.PaqueteId is int packageId ? $"{SampleData.Paquetes.First(item => item.Id == packageId).Nombre} × {_borrador.CantidadPaquete}" : "Sin paquete";
        LodgingAmount.Text = $"Bs {summary.Alojamiento:N2}"; PackageAmount.Text = $"Bs {summary.Paquetes:N2}";
        ResourceAmount.Text = $"Bs {summary.Recursos:N2} · {_borrador.RecursosAdicionales.Count} tipo(s)";
        ServiceAmount.Text = $"Bs {summary.Servicios:N2} · {_borrador.ServiciosAdquiridos.Sum(item => item.Cantidad)} sesión(es)";
        DiscountAmount.Text = $"− Bs {summary.Descuentos:N2}"; TaxAmount.Text = $"Bs {summary.Impuestos:N2}";
        PricingWarning.Text = summary.PreciosPendientes ? "Hay precios acordados sin verificar. Revísalos antes de confirmar." : "";
        EstimatedTotal.Text = $"Bs {summary.Total:N2}";
        ReviewStep.Resumen.Text = $"ALOJAMIENTO\n{start:dd/MM/yyyy} — {end:dd/MM/yyyy} · {nights} noche(s) · {PersonasSolicitadas} huésped(es)\nHabitaciones: {(selectedRooms.Count > 0 ? string.Join(", ", selectedRooms.Select(item => item.Numero)) : "Sin seleccionar")} · Bs {summary.Alojamiento:N2}\n\nPAQUETES ADQUIRIDOS\n{PackageSummary.Text} · Bs {summary.Paquetes:N2}\n\nRECURSOS ADICIONALES\n{string.Join("\n", _borrador.RecursosAdicionales.Select(item => { var resource = SampleData.RecursosAdicionales.FirstOrDefault(value => value.RecursoId == item.RecursoId); var amount = item.PrecioUnitarioAcordado is decimal price ? new RecursoAdicionalService().CalcularImporte(price, item.TipoCobroAplicado ?? "POR_ESTANCIA", item.Cantidad, nights) : 0m; return $"{resource?.Nombre ?? "Recurso"} × {item.Cantidad} · Bs {amount:N2}"; }).DefaultIfEmpty("Ninguno"))}\n\nSERVICIOS ADICIONALES ADQUIRIDOS\n{string.Join("\n", _borrador.ServiciosAdquiridos.Select(item => $"{SampleData.Servicios.FirstOrDefault(value => value.Id == item.ServicioId)?.Nombre ?? "Servicio"} × {item.Cantidad} · Bs {item.Cantidad * item.PrecioUnitarioAcordado.GetValueOrDefault():N2} · {(item.HuespedId is int guestId ? SampleData.Huespedes.FirstOrDefault(value => value.Id == guestId)?.Nombre : "Compartido / sin destinatario")}").DefaultIfEmpty("Ninguno; citas no confirmadas"))}\n\nSOLICITUDES ESPECIALES\n{(string.IsNullOrWhiteSpace(ExtrasStep.Solicitud.Text) ? "Ninguna" : ExtrasStep.Solicitud.Text.Trim())}\n\nDescuento: Bs {summary.Descuentos:N2} · Impuestos: Bs {summary.Impuestos:N2}\nTOTAL DE RESERVA: Bs {summary.Total:N2}";
    }

    private void ShowBreakdown_Click(object sender, RoutedEventArgs e) { WizardTabs.SelectedIndex = 4; ActualizarPaso(); }
    private void Save_Click(object sender, RoutedEventArgs e) => Guardar(pendiente: false);
    private void SavePending_Click(object sender, RoutedEventArgs e) => Guardar(pendiente: true);
    private void Guardar(bool pendiente)
    {
        ValidationText.Text = "";
        if (GuestsStep.HuespedPrincipal.SelectedItem is not HuespedModel guest || RoomsStep.HabitacionesSeleccionadas.Count == 0) { ValidationText.Text = "Selecciona el huésped principal y al menos una habitación."; return; }
        if (!int.TryParse(StayStep.GuestCount.Text, out var requestedPeople) || requestedPeople is < 1 or > 30) { ValidationText.Text = "Indica una cantidad válida de personas (1–30)."; return; }
        if (RoomsStep.CapacidadSeleccionada < requestedPeople) { ValidationText.Text = $"La capacidad seleccionada es de {RoomsStep.CapacidadSeleccionada} plaza(s) para {requestedPeople} personas. Añade otra habitación."; WizardTabs.SelectedIndex = 1; ActualizarPaso(); return; }
        if (FechaSalida <= FechaEntrada) { ValidationText.Text = "La salida debe ser posterior a la llegada."; return; }
        try
        {
            _borrador.Llegada = FechaEntrada.Date; _borrador.Salida = FechaSalida.Date; _borrador.Personas = requestedPeople;
            _borrador.HabitacionesSeleccionadas.Clear(); _borrador.HabitacionesSeleccionadas.AddRange(RoomsStep.HabitacionesSeleccionadas);
            _borrador.HabitacionesRequeridas = _borrador.HabitacionesSeleccionadas.Count; _borrador.Canal = StayStep.Channel.SelectedItem?.ToString() ?? "Directa";
            _borrador.HuespedPrincipalId = guest.Id; _borrador.HuespedesAcompanantes.Clear(); _borrador.HuespedesAcompanantes.AddRange(GuestsStep.Acompanantes.SelectedItems.Cast<HuespedModel>().Select(item => item.Id));
            _borrador.TipoPreferido = RoomsStep.HabitacionesSeleccionadasModels.FirstOrDefault()?.Tipo;
            SyncPackageSelection(); _borrador.SolicitudesEspeciales = ExtrasStep.Solicitud.Text.Trim();
            if (_existente is null) ReservaCreada = _reservas.CrearReserva(_borrador, pendiente);
            else { _reservas.ActualizarReserva(_existente, _borrador); ReservaCreada = _existente; }
            if (!string.IsNullOrWhiteSpace(_borrador.SolicitudesEspeciales)) SampleData.Auditoria.Insert(0, new(DateTime.Now, guest.Nombre, "Solicitud especial de reserva", "—", _borrador.SolicitudesEspeciales));
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
