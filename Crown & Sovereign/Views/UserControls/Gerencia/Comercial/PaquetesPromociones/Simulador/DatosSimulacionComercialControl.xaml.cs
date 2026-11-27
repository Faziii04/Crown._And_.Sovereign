using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Simulador;

public partial class DatosSimulacionComercialControl : UserControl
{
    private readonly HabitacionDisponibilidadService _availability = new();
    private bool _ready;
    private bool _loadingRooms;
    private bool _refreshingPromotions;
    public event EventHandler? ConfiguracionCambiada;
    public event EventHandler? PaqueteCambiado;
    public DateTime Llegada => ArrivalPicker.SelectedDate ?? DateTime.Today.AddDays(1);
    public DateTime Salida => DeparturePicker.SelectedDate ?? Llegada.AddDays(1);
    public string Canal => ChannelPicker.SelectedItem?.ToString() ?? "Directa";
    public PaqueteModel? Paquete => PackagePicker.SelectedItem as PaqueteModel;
    public PromocionModel? Promocion => PromotionPicker.SelectedItem as PromocionModel;
    public int CantidadPaquetes => int.TryParse(PackageQuantity.SelectedItem?.ToString(), out var quantity) ? quantity : 1;
    public IReadOnlyList<int> HabitacionesSeleccionadas => AvailableRooms.SelectedItems.Cast<HabitacionModel>().Select(item => item.Id).ToList();

    public DatosSimulacionComercialControl()
    {
        InitializeComponent();
        ArrivalPicker.SelectedDate = DateTime.Today.AddDays(1); DeparturePicker.SelectedDate = DateTime.Today.AddDays(2);
        RoomTypePicker.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToList(); RoomTypePicker.SelectedIndex = 0;
        ChannelPicker.ItemsSource = SampleData.CanalesReserva.Select(item => item.Nombre).ToList(); ChannelPicker.SelectedItem = "Directa";
        PackagePicker.ItemsSource = SampleData.Paquetes.Where(item => item.Activo).OrderBy(item => item.Nombre).ToList();
        PackageQuantity.ItemsSource = Enumerable.Range(1, 5).ToList(); PackageQuantity.SelectedIndex = 0;
        _ready = true; LoadRooms(); RefreshPromotions();
    }

    private void LoadRooms()
    {
        if (Salida.Date <= Llegada.Date) { _loadingRooms = true; AvailableRooms.ItemsSource = null; _loadingRooms = false; return; }
        try
        {
            var selected = HabitacionesSeleccionadas.ToHashSet();
            var criteria = new ReservationSearchCriteria { Llegada = Llegada.Date, Salida = Salida.Date, Personas = 1, Habitaciones = 1, Canal = Canal };
            var type = RoomTypePicker.SelectedItem?.ToString();
            var rooms = _availability.BuscarHabitaciones(criteria, type).Select(item => SampleData.Habitaciones.First(room => room.Id == item.Id)).ToList();
            _loadingRooms = true; AvailableRooms.ItemsSource = rooms;
            foreach (var room in rooms.Where(item => selected.Contains(item.Id))) AvailableRooms.SelectedItems.Add(room);
            if (AvailableRooms.SelectedItems.Count == 0 && rooms.Count > 0) AvailableRooms.SelectedIndex = 0;
            _loadingRooms = false;
        }
        catch { _loadingRooms = false; AvailableRooms.ItemsSource = null; }
    }

    private void RefreshPromotions()
    {
        var selectedId = Promocion?.Id;
        var eligible = SampleData.Promociones.Where(item => item.Activa && item.Desde.Date <= Llegada.Date && item.Hasta.Date >= Llegada.Date).OrderBy(item => item.Nombre).ToList();
        _refreshingPromotions = true; PromotionPicker.ItemsSource = eligible; PromotionPicker.SelectedItem = eligible.FirstOrDefault(item => item.Id == selectedId); _refreshingPromotions = false;
    }

    private void Dates_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_ready) return; LoadRooms(); RefreshPromotions(); Notificar(); }
    private void RoomType_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_ready) return; LoadRooms(); Notificar(); }
    private void Channel_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_ready) return; LoadRooms(); Notificar(); }
    private void Package_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_ready) return; PaqueteCambiado?.Invoke(this, EventArgs.Empty); Notificar(); }
    private void Selection_Changed(object sender, EventArgs e)
    { if (_ready && !_loadingRooms && !_refreshingPromotions) Notificar(); }
    private void ClearPackage_Click(object sender, System.Windows.RoutedEventArgs e) => PackagePicker.SelectedItem = null;
    private void ClearPromotion_Click(object sender, System.Windows.RoutedEventArgs e) => PromotionPicker.SelectedItem = null;
    private void Notificar() { RoomSelectionText.Text = $"{HabitacionesSeleccionadas.Count} seleccionada(s)"; ConfiguracionCambiada?.Invoke(this, EventArgs.Empty); }
}
