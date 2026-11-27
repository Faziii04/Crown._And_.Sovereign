using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CheckOutDialog : Window
{
    private readonly int _stayId;
    private readonly EstanciaService _service = new();
    public bool FueFinalizada { get; private set; }
    public bool ConsultarFolioSolicitado { get; private set; }
    public int? PestañaSolicitada { get; private set; }
    public CheckOutDialog(int stayId)
    {
        InitializeComponent(); _stayId = stayId;
        RefreshReadiness();
    }
    private void RefreshReadiness()
    {
        var stay = SampleData.Estancias.First(item => item.Id == _stayId && item.Estado == "Activa");
        var guest = SampleData.Huespedes.First(item => item.Id == stay.HuespedId);
        var folio = SampleData.Folios.FirstOrDefault(item => item.Id == stay.FolioId) ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == stay.Id);
        var folioId = folio?.Id;
        var rooms = _service.ObtenerHabitacionesEstancia(stay.Id);
        GuestText.Text = $"{guest.Nombre} · Estancia #{stay.Id}";
        RoomText.Text = $"Habitación(es) {string.Join(", ", rooms.Select(item => item.Numero))}";
        DepartureText.Text = $"Salida prevista: {stay.CheckOut:dddd d 'de' MMMM 'de' yyyy}";
        var charges = folioId is int id ? SampleData.Cargos.Where(item => item.FolioId == id && !item.Anulado).Sum(item => item.Monto) : 0m;
        var paid = folioId is int folioKey ? SampleData.Pagos.Where(item => item.FolioId == folioKey && item.Estado != "Anulado").Sum(item => item.Monto) : 0m;
        var balance = new FolioService().CalcularSaldoPorEstancia(stay.Id);
        ChargesText.Text = $"Bs {charges:N2}"; PaidText.Text = $"Bs {paid:N2}"; BalanceText.Text = $"Bs {balance:N2}";
        PayButton.IsEnabled = balance > 0 && folio is { Estado: "Abierto" };
        ConfirmButton.IsEnabled = balance <= 0 && folio is not null && folio.Estado == "Abierto";
        ChecklistText.Text = $"{(folio is { Estado: "Abierto" } ? "✓" : "! ")} Folio {(folio is null ? "no encontrado" : folio.Estado == "Abierto" ? "abierto y consultado" : $"en estado {folio.Estado}")}\n{(balance <= 0 ? "✓" : "! ")} Saldo {(balance <= 0 ? "liquidado" : $"pendiente · Bs {balance:N2}")}\n✓ Alojamiento y movimientos preservados\n✓ Solicitudes y citas consultadas";
        var warnings = new List<string>();
        var activeKeys = SampleData.Llaves.Count(item => item.EstanciaId == stay.Id && item.Estado == "Activa");
        if (activeKeys > 0) warnings.Add($"⚠ {activeKeys} llave(s) activa(s) sin devolución registrada.");
        var resourcesToReturn = SampleData.ReservaRecursos.Where(item => item.ReservaId == stay.ReservaId && item.Estado == "ENTREGADO").Sum(item => item.Cantidad);
        var resourcesPending = SampleData.ReservaRecursos.Where(item => item.ReservaId == stay.ReservaId && item.Estado is "SIN_VERIFICAR" or "PENDIENTE" or "ASIGNADO").Sum(item => item.Cantidad);
        if (resourcesToReturn > 0) warnings.Add($"⚠ {resourcesToReturn} unidad(es) de recursos entregados requieren verificación de devolución.");
        if (resourcesPending > 0) warnings.Add($"⚠ {resourcesPending} recurso(s) aún no tienen entrega confirmada.");
        var activeRequests = SampleData.SolicitudesServicio.Count(item => item.EstanciaId == stay.Id && item.Estado is "Solicitada" or "Pendiente" or "Confirmada" or "En atención" or "En curso");
        var appointments = SampleData.Citas.Count(item => item.EstanciaId == stay.Id && item.Inicio >= DateTime.Now && item.Estado is "Solicitada" or "Confirmada" or "En curso");
        if (activeRequests > 0) warnings.Add($"⚠ {activeRequests} solicitud(es) de servicio aún abiertas.");
        if (appointments > 0) warnings.Add($"⚠ {appointments} cita(s) futura(s) asociadas a la estancia.");
        var roomIds = rooms.Select(item => item.Id).ToHashSet();
        var incidents = SampleData.Incidencias.Count(item => roomIds.Contains(item.Habitacion) && item.Estado is not ("Resuelta" or "Cerrada" or "Finalizada"));
        if (incidents > 0) warnings.Add($"⚠ {incidents} incidencia(s) de habitación requieren seguimiento.");
        WarningsText.Text = warnings.Count == 0 ? "Sin avisos operativos pendientes." : string.Join(Environment.NewLine, warnings);
    }
    private void Pay_Click(object sender, RoutedEventArgs e)
    {
        var stay = SampleData.Estancias.First(item => item.Id == _stayId);
        var folioId = stay.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == _stayId).Id;
        var dialog = new PagoDialog(folioId) { Owner = this };
        if (dialog.ShowDialog() == true) RefreshReadiness();
    }
    private void Folio_Click(object sender, RoutedEventArgs e) { ConsultarFolioSolicitado = true; DialogResult = false; }
    private void Context_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out var index))
        {
            PestañaSolicitada = index;
            DialogResult = false;
        }
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _service.FinalizarPorEstancia(_stayId);
            FueFinalizada = true;
            var stay = SampleData.Estancias.First(item => item.Id == _stayId);
            var rooms = _service.ObtenerHabitacionesEstancia(_stayId);
            var folio = stay.FolioId is int id ? SampleData.Folios.FirstOrDefault(item => item.Id == id) : SampleData.Folios.FirstOrDefault(item => item.EstanciaId == stay.Id);
            PreparationPanel.Visibility = Visibility.Collapsed; CompletionPanel.Visibility = Visibility.Visible;
            TitleText.Text = "Salida completada";
            CompletedGuest.Text = $"{SampleData.Huespedes.First(item => item.Id == stay.HuespedId).Nombre} · Estancia #{stay.Id}";
            CompletedAt.Text = $"Fecha de salida: {stay.CheckOutReal:dd/MM/yyyy · HH:mm}";
            CompletedFolio.Text = $"Folio: {folio?.Estado ?? "Sin folio"}";
            CompletedBalance.Text = $"Saldo: Bs {new FolioService().CalcularSaldoPorEstancia(stay.Id):N2}";
            CompletedRooms.Text = string.Join(Environment.NewLine, rooms.Select(room => $"Habitación {room.Numero}: {room.Limpieza}"));
            ConfirmButton.Visibility = Visibility.Collapsed; CancelButton.Visibility = Visibility.Collapsed; DoneButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
    private void Done_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
