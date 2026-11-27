using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class DetalleEstanciaControl : UserControl
{
    private int _stayId;
    public event EventHandler? VolverASolicitado;
    public event EventHandler? DatosActualizados;
    public event EventHandler? SalidaCompletada;
    public DetalleEstanciaControl()
    {
        InitializeComponent();
        FolioView.DatosActualizados += (_, _) => Refrescar();
        ServicesView.DatosActualizados += (_, _) => Refrescar();
        ResourcesView.DatosActualizados += (_, _) => Refrescar();
        KeysView.DatosActualizados += (_, _) => Refrescar();
    }
    public void SeleccionarFolio() => WorkspaceTabs.SelectedIndex = 1;
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == stayId);
        if (stay is null) return;
        var primary = SampleData.Huespedes.First(item => item.Id == stay.HuespedId);
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == stay.ReservaId);
        var rooms = new EstanciaService().ObtenerHabitacionesEstancia(stayId);
        var roomNames = string.Join(", ", rooms.Select(item => item.Numero));
        var roomInfo = rooms.Count == 0 ? "Sin habitación asignada" : $"Habitación(es) {roomNames} · Piso(s) {string.Join(", ", rooms.Select(item => item.Piso).Distinct())} · {string.Join(", ", rooms.Select(item => item.Tipo).Distinct())}";
        var packageNames = string.Join(", ", SampleData.ReservaPaquetes.Where(item => item.ReservaId == stay.ReservaId).Select(item => SampleData.Paquetes.FirstOrDefault(package => package.Id == item.PaqueteId)?.Nombre).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct());
        GuestTitle.Text = primary.Nombre;
        StayIdText.Text = $"Estancia #{stay.Id} · {(stay.Estado == "Activa" ? "● Activa" : "Finalizada")}";
        StaySubtitle.Text = $"{roomInfo} · Reserva #{stay.ReservaId} · {(packageNames.Length > 0 ? packageNames : reservation?.Paquete ?? "Sin paquete")}";
        StayDates.Text = $"Ingreso real {stay.CheckIn:dd/MM/yyyy HH:mm} · Salida prevista {stay.CheckOut:dd/MM/yyyy HH:mm}";
        if (stay.Estado == "Finalizada")
        {
            StayDates.Text += stay.CheckOutReal is DateTime checkout ? $" · Salida registrada {checkout:dd/MM/yyyy HH:mm}" : " · Histórica";
            CheckOutButton.Visibility = Visibility.Collapsed;
        }
        var readOnly = stay.Estado == "Finalizada";
        CheckOutButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        ExtendButton.IsEnabled = !readOnly; MoveButton.IsEnabled = !readOnly;
        FolioView.SetReadOnly(readOnly); ServicesView.SetReadOnly(readOnly); ResourcesView.SetReadOnly(readOnly); KeysView.SetReadOnly(readOnly);
        SummaryView.Mostrar(stayId); FolioView.Mostrar(stayId); ServicesView.Mostrar(stayId); ConsumptionView.Mostrar(stayId); ResourcesView.Mostrar(stayId); KeysView.Mostrar(stayId); ActivityView.Mostrar(stayId);
    }
    private void Refrescar() { Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); }
    private void Back_Click(object sender, RoutedEventArgs e) => VolverASolicitado?.Invoke(this, EventArgs.Empty);
    private void Extend_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExtenderEstanciaDialog(_stayId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Refrescar();
    }
    private void Move_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CambioHabitacionEstanciaDialog(_stayId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Refrescar();
    }
    private void CheckOut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CheckOutDialog(_stayId) { Owner = Window.GetWindow(this) };
        var result = dialog.ShowDialog();
        if (dialog.ConsultarFolioSolicitado) { WorkspaceTabs.SelectedIndex = 1; return; }
        if (dialog.PestañaSolicitada is int tabIndex) { WorkspaceTabs.SelectedIndex = tabIndex; return; }
        if (result == true)
        {
            Mostrar(_stayId);
            if (dialog.FueFinalizada) SalidaCompletada?.Invoke(this, EventArgs.Empty);
        }
    }
}
