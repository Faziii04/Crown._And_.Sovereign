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
    private readonly BorradorReserva? _borrador;
    public ReservaModel? ReservaCreada { get; private set; }
    public ReservaDialog(DateTime? llegada = null, string? tipoHabitacion = null, ReservaModel? existente = null, int? habitacionId = null, BorradorReserva? borrador = null)
    {
        InitializeComponent();
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
            borrador.PaqueteId = SampleData.ReservaPaquetes.FirstOrDefault(item => item.ReservaId == existente.Id)?.PaqueteId;
            borrador.ServiciosAdicionales.AddRange(SampleData.ServiciosReserva.Where(item => item.ReservaId == existente.Id).Select(item => item.ServicioId));
            borrador.SolicitudesEspeciales = SampleData.SolicitudesEspeciales.FirstOrDefault(item => item.ReservaId == existente.Id)?.Descripcion ?? "";
        }
        _borrador = borrador;
        StayStep.Arrival.SelectedDateChanged += (_, _) => { if (IsLoaded) { CargarHabitaciones(RoomsStep.HabitacionesSeleccionadas); ActualizarResumen(); } };
        StayStep.Departure.SelectedDateChanged += (_, _) => { if (IsLoaded) { CargarHabitaciones(RoomsStep.HabitacionesSeleccionadas); ActualizarResumen(); } };
        RoomsStep.SeleccionCambiada += ActualizarResumen;
        StayStep.GuestCount.TextChanged += (_, _) => { if (IsLoaded) ActualizarResumen(); };
        GuestsStep.HuespedPrincipal.SelectionChanged += (_, _) => { GuestsStep.RefrescarListadoAcompanantes(); ActualizarResumen(); };
        GuestsStep.Acompanantes.SelectionChanged += (_, _) => ActualizarResumen();
        ExtrasStep.Paquete.SelectionChanged += (_, _) => ActualizarResumen();
        ExtrasStep.Promocion.SelectionChanged += (_, _) => ActualizarResumen();
        ExtrasStep.Servicios.SelectionChanged += (_, _) => ActualizarResumen();
        ExtrasStep.Solicitud.TextChanged += (_, _) => ActualizarResumen();
        if (existente is not null) { Title = $"Editar reserva #{existente.Id}"; DialogTitle.Text = Title; SaveButton.Content = "Guardar cambios"; SavePendingButton.Visibility = Visibility.Collapsed; }
        GuestsStep.ConfigurarHuespedes(SampleData.Huespedes);
        GuestsStep.HuespedPrincipal.SelectedItem = SampleData.Huespedes.FirstOrDefault(g => g.Nombre == existente?.Huesped) ?? SampleData.Huespedes.FirstOrDefault();
        StayStep.GuestCount.Text = Math.Max(1, borrador?.Personas ?? existente?.Personas ?? 1).ToString();
        StayStep.Arrival.SelectedDate = existente?.Llegada.Date ?? borrador?.Llegada.Date ?? llegada?.Date ?? DateTime.Today.AddDays(1);
        StayStep.Departure.SelectedDate = existente?.Salida.Date ?? borrador?.Salida.Date ?? (llegada?.Date ?? DateTime.Today.AddDays(1)).AddDays(2);
        StayStep.Channel.ItemsSource = new[] { "Directa", "Agencia", "Teléfono", "Sitio web" };
        StayStep.Channel.SelectedItem = borrador?.Canal ?? existente?.Canal ?? "Directa";
        ExtrasStep.Paquete.ItemsSource = new[] { "Sin paquete", "Bienestar", "Desayuno diario", "Escapada romántica", "Familiar" };
        ExtrasStep.Paquete.SelectedItem = existente?.Paquete ?? "Sin paquete";
        ExtrasStep.Promocion.ItemsSource = SampleData.Promociones.Where(item => item.Hasta.Date >= DateTime.Today).ToList();
        ExtrasStep.Servicios.ItemsSource = SampleData.Servicios.Where(item => item.Activo).ToList();
        StayStep.Channel.SelectedItem = existente?.Canal ?? "Directa";
        List<int> existingRoomIds = existente is null ? [] : SampleData.ReservaHabitaciones.Where(item => item.ReservaId == existente.Id).Select(item => item.HabitacionId).Append(existente.Habitacion).Distinct().ToList();
        var initialRoomIds = borrador?.HabitacionesSeleccionadas.Count > 0 ? borrador.HabitacionesSeleccionadas.ToList() : existingRoomIds;
        if (initialRoomIds.Count == 0 && habitacionId is int preferredRoom) initialRoomIds.Add(preferredRoom);
        if (existente is not null)
        {
            var selectedGuests = SampleData.ReservaHuespedes.Where(item => item.ReservaId == existente.Id && !item.Principal).Select(item => item.HuespedId).ToHashSet();
            foreach (var companion in GuestsStep.Acompanantes.Items.Cast<HuespedModel>().Where(item => selectedGuests.Contains(item.Id))) GuestsStep.Acompanantes.SelectedItems.Add(companion);
        }
        if (borrador is not null)
        {
            GuestsStep.HuespedPrincipal.SelectedItem = SampleData.Huespedes.FirstOrDefault(g => g.Id == borrador.HuespedPrincipalId) ?? GuestsStep.HuespedPrincipal.SelectedItem;
            foreach (var companion in GuestsStep.Acompanantes.Items.Cast<HuespedModel>().Where(item => borrador.HuespedesAcompanantes.Contains(item.Id))) GuestsStep.Acompanantes.SelectedItems.Add(companion);
            if (borrador.PaqueteId is int packageId) ExtrasStep.Paquete.SelectedItem = SampleData.Paquetes.FirstOrDefault(p => p.Id == packageId)?.Nombre;
            ExtrasStep.Promocion.SelectedItem = SampleData.Promociones.FirstOrDefault(item => item.Id == borrador.PromocionId);
            foreach (var service in ExtrasStep.Servicios.Items.Cast<ServicioModel>().Where(item => borrador.ServiciosAdicionales.Contains(item.Id))) ExtrasStep.Servicios.SelectedItems.Add(service);
            ExtrasStep.Solicitud.Text = borrador.SolicitudesEspeciales;
        }
        CargarHabitaciones(initialRoomIds, tipoHabitacion ?? borrador?.TipoPreferido, initialRoomIds.Count == 0);
        ActualizarResumen();
        WizardTabs.SelectedIndex = Math.Clamp(borrador?.PasoInicial ?? 0, 0, 4);
        ActualizarPaso();
    }

    private bool _reservasRoomAvailable(int roomId)
    {
        var start = StayStep.Arrival.SelectedDate ?? DateTime.Today.AddDays(1);
        var end = StayStep.Departure.SelectedDate ?? start.AddDays(2);
        var habitaciones = new HabitacionService();
        return _existente is null ? habitaciones.ConsultarDisponibilidad(roomId, start, end) : habitaciones.ConsultarDisponibilidad(roomId, start, end, _existente.Id);
    }

    private int PersonasSolicitadas => int.TryParse(StayStep.GuestCount.Text, out var people) ? Math.Clamp(people, 1, 30) : 1;

    private void CargarHabitaciones(IEnumerable<int>? selectedIds = null, string? preferredType = null, bool suggestInitialRoom = false)
    {
        var arrival = StayStep.Arrival.SelectedDate ?? DateTime.Today.AddDays(1);
        var departure = StayStep.Departure.SelectedDate ?? arrival.AddDays(2);
        if (departure <= arrival) departure = arrival.AddDays(1);
        var criteria = new ReservationSearchCriteria { Llegada = arrival.Date, Salida = departure.Date, Personas = PersonasSolicitadas, Habitaciones = 1 };
        var available = _disponibilidad.BuscarHabitaciones(criteria, excluirReservaId: _existente?.Id)
            .Select(item => (Habitacion: SampleData.Habitaciones.First(room => room.Id == item.Id), item.TotalEstancia)).ToList();
        var ids = selectedIds?.Where(id => available.Any(item => item.Habitacion.Id == id)).Distinct().ToList() ?? [];
        if (suggestInitialRoom && ids.Count == 0 && available.Count > 0)
        {
            var suggestion = available.OrderByDescending(item => item.Habitacion.Capacidad).ThenBy(item => item.TotalEstancia).First();
            ids.Add(suggestion.Habitacion.Id);
        }
        RoomsStep.Mostrar(available, ids);
        RoomsStep.EstablecerTipoPreferido(preferredType);
    }

    private void PreviousStep_Click(object sender, RoutedEventArgs e) { WizardTabs.SelectedIndex = Math.Max(0, WizardTabs.SelectedIndex - 1); ActualizarPaso(); }
    private void NextStep_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidarPaso(WizardTabs.SelectedIndex)) return;
        WizardTabs.SelectedIndex = Math.Min(4, WizardTabs.SelectedIndex + 1); ActualizarPaso(); ActualizarResumen();
    }
    private void ActualizarPaso()
    {
        var last = WizardTabs.SelectedIndex == 4;
        PreviousStepButton.IsEnabled = WizardTabs.SelectedIndex > 0;
        NextStepButton.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
        SavePendingButton.Visibility = last ? Visibility.Visible : Visibility.Collapsed;
        SaveButton.Visibility = last ? Visibility.Visible : Visibility.Collapsed;
    }
    private bool ValidarPaso(int step)
    {
        ValidationText.Text = "";
        if (step == 0)
        {
            if ((StayStep.Arrival.SelectedDate ?? DateTime.MinValue) >= (StayStep.Departure.SelectedDate ?? DateTime.MinValue)) { ValidationText.Text = "La salida debe ser posterior a la llegada."; return false; }
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
        var selectedRooms = RoomsStep.HabitacionesSeleccionadasModels;
        var room = selectedRooms.FirstOrDefault();
        var start = StayStep.Arrival?.SelectedDate ?? DateTime.Today.AddDays(1);
        var end = StayStep.Departure?.SelectedDate ?? start.AddDays(2);
        var nights = Math.Max(0, (end.Date - start.Date).Days);
        var amount = 0m;
        if (selectedRooms.Count > 0 && end.Date > start.Date)
        {
            var estimate = _borrador ?? new BorradorReserva();
            estimate.Llegada = start.Date; estimate.Salida = end.Date;
            estimate.HabitacionesSeleccionadas.Clear();
            estimate.HabitacionesSeleccionadas.AddRange(selectedRooms.Select(item => item.Id));
            estimate.Personas = PersonasSolicitadas;
            estimate.PaqueteId = SampleData.Paquetes.FirstOrDefault(item => item.Nombre == ExtrasStep.Paquete?.SelectedItem?.ToString())?.Id;
            estimate.PromocionId = (ExtrasStep.Promocion?.SelectedItem as PromocionModel)?.Id;
            estimate.ServiciosAdicionales.Clear(); estimate.ServiciosAdicionales.AddRange(ExtrasStep.Servicios?.SelectedItems.Cast<ServicioModel>().Select(item => item.Id) ?? []);
            amount = new TarifaService().CalcularTotal(estimate);
        }
        RoomsStep.MostrarCapacidad(PersonasSolicitadas);
        if (RoomsStep.Asignacion is not null) RoomsStep.Asignacion.Text = $"Huésped principal: {(GuestsStep.HuespedPrincipal?.SelectedItem as HuespedModel)?.Nombre ?? "Sin seleccionar"}\nAcompañantes identificados: {GuestsStep.Acompanantes?.SelectedItems.Count ?? 0} de {PersonasSolicitadas - 1} posible(s)";
        if (StayStep.Duration is not null) StayStep.Duration.Text = $"{nights} noches · {start:dd/MM/yyyy} a {end:dd/MM/yyyy}";
        if (StayDurationAside is not null) StayDurationAside.Text = $"{nights} noches · {start:dd/MM}–{end:dd/MM/yyyy}";
        if (RateSummary is not null) RateSummary.Text = room is null ? "Tarifa no disponible" : $"{selectedRooms.Count} habitación(es) · {selectedRooms.Sum(item => item.Capacidad)} plazas\n{selectedRooms.Sum(item => SampleData.Tarifas.GetValueOrDefault(item.Id)):N2} Bs por noche";
        if (PackageSummary is not null) PackageSummary.Text = $"{ExtrasStep.Paquete?.SelectedItem ?? "Sin paquete"}{(ExtrasStep.Promocion?.SelectedItem is PromocionModel promo ? $" · {promo.Nombre}" : "")}";
        if (EstimatedTotal is not null) EstimatedTotal.Text = $"Bs {amount:N2}";
        if (ReviewStep.Resumen is not null) ReviewStep.Resumen.Text = $"Huésped principal: {(GuestsStep.HuespedPrincipal?.SelectedItem as HuespedModel)?.Nombre ?? "Sin seleccionar"}\nPersonas: {PersonasSolicitadas} · acompañantes identificados: {GuestsStep.Acompanantes?.SelectedItems.Count ?? 0}\nFechas: {start:dd/MM/yyyy} – {end:dd/MM/yyyy} ({nights} noches)\nHabitación(es): {(selectedRooms.Count > 0 ? string.Join(", ", selectedRooms.Select(item => item.Numero)) : "Sin seleccionar")} · capacidad total {selectedRooms.Sum(item => item.Capacidad)}\nCanal: {StayStep.Channel?.SelectedItem}\nPaquete: {ExtrasStep.Paquete?.SelectedItem}\nPromoción: {(ExtrasStep.Promocion?.SelectedItem as PromocionModel)?.Nombre ?? "Ninguna"}\nServicios adicionales: {string.Join(", ", ExtrasStep.Servicios?.SelectedItems.Cast<ServicioModel>().Select(item => item.Nombre) ?? [])}\nSolicitud especial: {(string.IsNullOrWhiteSpace(ExtrasStep.Solicitud?.Text) ? "Ninguna" : ExtrasStep.Solicitud.Text.Trim())}";
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Guardar(pendiente: false);
    private void SavePending_Click(object sender, RoutedEventArgs e) => Guardar(pendiente: true);

    private void Guardar(bool pendiente)
    {
        ValidationText.Text = "";
        if (GuestsStep.HuespedPrincipal.SelectedItem is not HuespedModel guest || RoomsStep.HabitacionesSeleccionadas.Count == 0)
        { ValidationText.Text = "Selecciona el huésped principal y al menos una habitación."; return; }
        if (!int.TryParse(StayStep.GuestCount.Text, out var requestedPeople) || requestedPeople is < 1 or > 30)
        { ValidationText.Text = "Indica una cantidad válida de personas (1–30)."; return; }
        if (RoomsStep.CapacidadSeleccionada < requestedPeople)
        { ValidationText.Text = $"La capacidad seleccionada es de {RoomsStep.CapacidadSeleccionada} plaza(s) para {requestedPeople} personas. Añade otra habitación."; WizardTabs.SelectedIndex = 1; ActualizarPaso(); return; }
        var arrival = StayStep.Arrival.SelectedDate ?? DateTime.MinValue;
        var departure = StayStep.Departure.SelectedDate ?? DateTime.MinValue;
        if (departure <= arrival) { ValidationText.Text = "La salida debe ser posterior a la llegada."; return; }
        try
        {
            var selectedRoomIds = RoomsStep.HabitacionesSeleccionadas.ToList();
            if (selectedRoomIds.Any(roomId => !_reservasRoomAvailable(roomId))) { ValidationText.Text = "Una habitación ya no está disponible para esas fechas."; return; }
            var draft = _borrador ?? new BorradorReserva();
            draft.Llegada = arrival.Date; draft.Salida = departure.Date; draft.Personas = requestedPeople; draft.HabitacionesSeleccionadas.Clear(); draft.HabitacionesSeleccionadas.AddRange(selectedRoomIds); draft.HabitacionesRequeridas = selectedRoomIds.Count;
            draft.Canal = StayStep.Channel.SelectedItem?.ToString() ?? "Directa"; draft.HuespedPrincipalId = guest.Id;
            draft.HuespedesAcompanantes.Clear(); draft.HuespedesAcompanantes.AddRange(GuestsStep.Acompanantes.SelectedItems.Cast<HuespedModel>().Select(item => item.Id));
            draft.PaqueteId = SampleData.Paquetes.FirstOrDefault(item => item.Nombre == ExtrasStep.Paquete.SelectedItem?.ToString())?.Id;
            draft.PromocionId = (ExtrasStep.Promocion.SelectedItem as PromocionModel)?.Id;
            draft.ServiciosAdicionales.Clear(); draft.ServiciosAdicionales.AddRange(ExtrasStep.Servicios.SelectedItems.Cast<ServicioModel>().Select(item => item.Id));
            draft.SolicitudesEspeciales = ExtrasStep.Solicitud.Text.Trim();
            if (_existente is null)
            {
                ReservaCreada = _reservas.CrearReserva(draft, pendiente);
            }
            else
            {
                _reservas.ActualizarReserva(_existente, draft);
                ReservaCreada = _existente;
            }
            if (!string.IsNullOrWhiteSpace(ExtrasStep.Solicitud.Text)) SampleData.Auditoria.Insert(0, new(DateTime.Now, guest.Nombre, "Solicitud especial de reserva", "—", ExtrasStep.Solicitud.Text.Trim()));
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
