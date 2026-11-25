using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Administracion;
using Crown___Sovereign.Views.Dialogs.Recepcion;
using Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Filtros;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Resumen;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;

public partial class HabitacionesControl : UserControl
{
    private readonly HabitacionService _habitaciones = new();
    private readonly bool _administracion;
    private HabitacionOperacionModel? _selectedRoom;
    private int? _selectedFloor;
    private double _scrollOffset;

    public event Action<string, int?>? NavegacionSolicitada;

    public HabitacionesControl(bool administracion = false)
    {
        InitializeComponent();
        _administracion = administracion;
        ReceptionPanel.Visibility = administracion ? Visibility.Collapsed : Visibility.Visible;
        AdminPanel.Visibility = administracion ? Visibility.Visible : Visibility.Collapsed;
        TodayText.Text = DateTime.Today.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

        if (administracion)
        {
            AdminFilters.FiltrosCambiados += RecargarAdministracion;
            AdminRoomBoard.HabitacionSeleccionada += room => { AdminRoomDetail.Mostrar(room, true); RoomTabs.SelectedIndex = 1; };
            AdminRoomDetail.ReservarSolicitada += room => CrearReserva(_habitaciones.ObtenerEstadoOperativo(room.Id));
            AdminRoomDetail.EditarSolicitado += EditarHabitacion;
            RecargarAdministracion();
            return;
        }

        Filters.FiltrosCambiados += Recargar;
        FloorNavigation.PisoSeleccionado += floor => { _selectedFloor = floor; Filters.SeleccionarPiso(floor); };
        Summary.FiltroSolicitado += filter => { Filters.SeleccionarFiltroRapido(filter); Recargar(); };
        Alerts.AlertaSeleccionada += alert =>
        {
            AlertsPopup.IsOpen = false;
            Filters.SeleccionarFiltroRapido(alert.Filtro);
            if (alert.HabitacionId is int roomId) AbrirDetalle(_habitaciones.ObtenerEstadoOperativo(roomId));
            else Recargar();
        };
        RoomBoard.DetalleSolicitado += AbrirDetalle;
        RoomBoard.AccionSolicitada += EjecutarAccion;
        RoomBoard.LimpiarFiltrosSolicitado += LimpiarFiltros;
        RoomBoard.VistaCambiada += _ => { var rooms = _habitaciones.ListarOperativas(Filters.Filtro); RoomBoard.Mostrar(rooms, _selectedFloor); RoomBoard.ScrollOffset = _scrollOffset; };
        RoomDetail.VolverSolicitado += VolverAlTablero;
        RoomDetail.AccionSolicitada += EjecutarAccion;
        RoomDetail.EstanciaSolicitada += id => { VolverAlTablero(); NavegacionSolicitada?.Invoke("Estancias", id); };
        RoomDetail.ReservaSolicitada += AbrirReservaExistente;
        RoomDetail.DatosActualizados += () => { Recargar(); MostrarDetalleSeleccionado(); };
        Recargar();
    }

    private void Recargar()
    {
        if (_administracion) return;
        var priorOffset = RoomBoard.ScrollOffset;
        var all = _habitaciones.ListarOperativas(new FiltroHabitacionesOperativas());
        var filtered = _habitaciones.ListarOperativas(Filters.Filtro);
        _selectedFloor = Filters.Piso;
        Summary.Mostrar(_habitaciones.ObtenerResumenOperativo(), Filters.Filtro.FiltroRapido == "Todas" ? null : Filters.Filtro.FiltroRapido);
        Alerts.Mostrar(all);
        ActualizarBotonesAlertas();
        FloorNavigation.Mostrar(all, _selectedFloor);
        Filters.MostrarCantidad(filtered.Count);
        RoomBoard.Mostrar(filtered, _selectedFloor);
        RoomBoard.ScrollOffset = priorOffset;
    }

    private void AbrirDetalle(HabitacionOperacionModel room)
    {
        _scrollOffset = RoomBoard.ScrollOffset;
        EstablecerTableroAmpliado(false);
        _selectedRoom = room;
        RoomDetail.Mostrar(_habitaciones.ObtenerEstadoOperativo(room.Habitacion.Id));
        RoomDetail.SelectedTab = 0;
        RoomBoard.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;
        ExpandBoardButton.Visibility = Visibility.Collapsed;
        Summary.Visibility = Filters.Visibility = FloorNavigation.Visibility = Visibility.Collapsed;
    }
    public void AbrirHabitacion(int roomId)
    {
        if (_administracion) return;
        VolverAlTablero();
        AbrirDetalle(_habitaciones.ObtenerEstadoOperativo(roomId));
    }

    private void MostrarDetalleSeleccionado()
    {
        if (_selectedRoom is null) return;
        RoomDetail.Mostrar(_habitaciones.ObtenerEstadoOperativo(_selectedRoom.Habitacion.Id));
    }

    private void VolverAlTablero()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        RoomBoard.Visibility = Visibility.Visible;
        ExpandBoardButton.Visibility = Visibility.Visible;
        Summary.Visibility = Filters.Visibility = FloorNavigation.Visibility = Visibility.Visible;
        EstablecerTableroAmpliado(false);
        Recargar();
        RoomBoard.ScrollOffset = _scrollOffset;
    }

    private void BackToBoard_Click(object sender, RoutedEventArgs e) => VolverAlTablero();
    private void Refresh_Click(object sender, RoutedEventArgs e) { Recargar(); MostrarDetalleSeleccionado(); }
    private void ExpandBoard_Click(object sender, RoutedEventArgs e) => EstablecerTableroAmpliado(true);
    private void RestoreBoard_Click(object sender, RoutedEventArgs e) => EstablecerTableroAmpliado(false);

    private void EstablecerTableroAmpliado(bool expandido)
    {
        if (expandido) AlertsPopup.IsOpen = false;
        ReceptionPanel.Margin = expandido ? new Thickness(10, 6, 10, 10) : new Thickness(24, 0, 24, 18);
        PageHeader.Visibility = expandido ? Visibility.Collapsed : Visibility.Visible;
        ExpandedToolbar.Visibility = expandido ? Visibility.Visible : Visibility.Collapsed;
        if (DetailPanel.Visibility != Visibility.Visible)
        {
            Summary.Visibility = expandido ? Visibility.Collapsed : Visibility.Visible;
            Filters.Visibility = expandido ? Visibility.Collapsed : Visibility.Visible;
            FloorNavigation.Visibility = expandido ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void Alerts_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement target || Alerts.CantidadAlertas == 0) return;
        AlertsPopup.PlacementTarget = target;
        AlertsPopup.IsOpen = !AlertsPopup.IsOpen;
    }

    private void ActualizarBotonesAlertas()
    {
        var count = Alerts.CantidadAlertas;
        var visible = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        AlertsButton.Content = ExpandedAlertsButton.Content = $"Atención requerida · {count}";
        AlertsButton.Visibility = ExpandedAlertsButton.Visibility = visible;
        if (count == 0) AlertsPopup.IsOpen = false;
    }

    private void LimpiarFiltros()
    {
        Filters.SearchBox.Text = "";
        Filters.TypeBox.SelectedIndex = 0;
        Filters.AdvancedFilters.Limpiar();
        Filters.SeleccionarPiso(null);
        Filters.SeleccionarFiltroRapido("Todas");
        Recargar();
    }

    private void EjecutarAccion(HabitacionOperacionModel room, string action)
    {
        try
        {
            switch (action)
            {
                case "Crear reserva": CrearReserva(room); break;
                case "Reserva":
                    if (room.ProximaReserva is not null) AbrirReservaExistente(room.ProximaReserva.Id); else CrearReserva(room);
                    break;
                case "Check-in":
                    if (room.ProximaReserva is null) throw new InvalidOperationException("No se encontró la reserva asignada a esta habitación.");
                    new EstanciaService().RealizarCheckIn(room.ProximaReserva.Id); NotificarActualizacion(); break;
                case "Checkout":
                    if (room.Huespedes.FirstOrDefault() is not { } guest) throw new InvalidOperationException("No se identificó al huésped para completar la salida.");
                    if (new CheckOutDialog(guest.Id) { Owner = Window.GetWindow(this) }.ShowDialog() == true) NotificarActualizacion();
                    break;
                case "Estancia":
                    if (room.EstanciaActual is null) throw new InvalidOperationException("No hay una estancia activa asociada.");
                    VolverAlTablero(); NavegacionSolicitada?.Invoke("Estancias", room.EstanciaActual.HuespedId); break;
                case "Solicitar limpieza":
                case "Preparación":
                    if (action == "Solicitar limpieza")
                    {
                        if (new SolicitarLimpiezaDialog(room) { Owner = Window.GetWindow(this) }.ShowDialog() == true) NotificarActualizacion();
                    }
                    else { AbrirDetalle(room); RoomDetail.SelectedTab = 2; }
                    break;
                case "Reportar incidencia":
                    if (new ReportarIncidenciaDialog(room) { Owner = Window.GetWindow(this) }.ShowDialog() == true) NotificarActualizacion(); break;
                case "Cambiar habitación":
                    if (new CambioHabitacionDialog(room) { Owner = Window.GetWindow(this) }.ShowDialog() == true) NotificarActualizacion(); break;
                case "Incidencias": AbrirDetalle(room); RoomDetail.SelectedTab = 3; break;
                case "Disponibilidad": AbrirDetalle(room); RoomDetail.SelectedTab = 1; break;
                case "Calendario general": VolverAlTablero(); NavegacionSolicitada?.Invoke("Reservas", null); break;
                default: AbrirDetalle(room); break;
            }
        }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "Operación de habitación", MessageBoxButton.OK, MessageBoxImage.Information); }
    }

    private void CrearReserva(HabitacionOperacionModel room)
    {
        var start = room.Ocupada && room.ReservaActual is not null ? room.ReservaActual.Salida.Date : DateTime.Today;
        var draft = new BorradorReserva { Llegada = start, Salida = start.AddDays(1), Personas = 1, HabitacionesRequeridas = 1, TipoPreferido = room.Habitacion.Tipo };
        draft.HabitacionesSeleccionadas.Add(room.Habitacion.Id);
        var dialog = new ReservaDialog(borrador: draft) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) NotificarActualizacion();
    }

    private void AbrirReservaExistente(int reservationId)
    {
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == reservationId);
        if (reservation is null) return;
        if (new ReservaDialog(existente: reservation) { Owner = Window.GetWindow(this) }.ShowDialog() == true) NotificarActualizacion();
    }

    private void NotificarActualizacion() { Recargar(); MostrarDetalleSeleccionado(); }

    private void RecargarAdministracion()
    {
        if (!_administracion) return;
        var rooms = _habitaciones.Buscar(AdminFilters.Busqueda, AdminFilters.Piso, AdminFilters.Tipo, AdminFilters.Ocupacion, AdminFilters.Limpieza, AdminFilters.Bloqueada, AdminFilters.CapacidadMinima, AdminFilters.Comodidad);
        AdminRoomBoard.Mostrar(rooms);
    }

    private void CreateRoom_Click(object sender, RoutedEventArgs e)
    {
        if (!NavigationService.Puede("Habitaciones y pisos")) return;
        if (new HabitacionDialog() { Owner = Window.GetWindow(this) }.ShowDialog() == true) RecargarAdministracion();
    }

    private void EditarHabitacion(HabitacionModel room)
    {
        if (!NavigationService.Puede("Habitaciones y pisos")) return;
        if (new HabitacionDialog(room) { Owner = Window.GetWindow(this) }.ShowDialog() == true) RecargarAdministracion();
    }
}
