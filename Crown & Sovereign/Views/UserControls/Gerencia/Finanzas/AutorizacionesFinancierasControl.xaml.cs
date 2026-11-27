using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class AutorizacionesFinancierasControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    private CorrectionDirectoryRow? _pendingSelection;
    public AutorizacionesFinancierasControl()
    {
        InitializeComponent();
        StateBox.ItemsSource = new[] { "Todos", "Pendiente", "Autorizada", "Rechazada", "Ejecutada" };
        StateBox.SelectedIndex = 0;
    }
    public void Actualizar() => Recargar();
    public void MostrarCorreccion(string type, int id)
    {
        StateBox.SelectedItem = "Pendiente";
        Recargar();
        RequestsList.SelectedItem = _service.Autorizaciones("Pendiente").FirstOrDefault(item => item.Tipo == type && item.Id == id);
        RequestsList.ScrollIntoView(RequestsList.SelectedItem);
    }
    private void State_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Recargar()
    {
        if (RequestsList is null || StateBox.SelectedItem is not string state) return;
        var preserve = (RequestsList.SelectedItem as CorrectionDirectoryRow)?.Id;
        var selectedType = (RequestsList.SelectedItem as CorrectionDirectoryRow)?.Tipo;
        RequestsList.ItemsSource = _service.Autorizaciones(state);
        RequestsList.SelectedItem = RequestsList.Items.Cast<CorrectionDirectoryRow>().FirstOrDefault(item => item.Id == preserve && item.Tipo == selectedType);
        if (RequestsList.SelectedItem is null) ShowDetails(null);
    }
    private void Request_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowDetails(RequestsList.SelectedItem as CorrectionDirectoryRow);
    private void ShowDetails(CorrectionDirectoryRow? row)
    {
        _pendingSelection = row;
        EmptyText.Visibility = row is null ? Visibility.Visible : Visibility.Collapsed;
        DetailPanel.Visibility = row is null ? Visibility.Collapsed : Visibility.Visible;
        ApproveButton.IsEnabled = row?.Estado == "Pendiente";
        RejectButton.IsEnabled = row?.Estado == "Pendiente";
        ActionStatus.Text = "";
        if (row is null) return;
        TitleText.Text = $"{row.Tipo} · solicitud #{row.Id}";
        StatusText.Text = $"Estado: {row.Estado} · {(row.EsEjecutable ? "autorizada; pendiente de ejecución en Caja" : "supervisión")}";
        GuestText.Text = row.Huesped;
        PaymentText.Text = $"Pago #{row.PagoId} · {FinanzasControl.Money(row.PagoOriginal)} · {row.Metodo}" + (row.FechaPago is DateTime paidAt ? $" · {paidAt:dd/MM/yyyy HH:mm}" : "");
        FolioText.Text = $"Folio #{row.FolioId}";
        RequesterText.Text = row.SolicitadoPor;
        ApproverText.Text = row.AutorizadoPor;
        DateText.Text = row.Fecha.ToString("dd/MM/yyyy HH:mm");
        ReasonText.Text = row.Motivo;
        DescriptionText.Text = string.IsNullOrWhiteSpace(row.Descripcion) ? "Sin descripción adicional." : row.Descripcion;
        if (row.Tipo == "Devolución")
        {
            var reserved = SampleData.Devoluciones.Where(item => item.PagoId == row.PagoId && item.Estado is "Autorizada" or "Ejecutada").Sum(item => item.Monto);
            ImpactText.Text = $"Impacto de efectivo estimado: salida de {FinanzasControl.Money(row.Monto)}. Pago original: {FinanzasControl.Money(row.PagoOriginal)}; devoluciones ya aprobadas o ejecutadas: {FinanzasControl.Money(reserved)}. La aprobación reserva capacidad y no modifica el folio ni ejecuta dinero.";
        }
        else ImpactText.Text = $"Impacto estimado: reverso del pago completo por {FinanzasControl.Money(row.PagoOriginal)}. La aprobación no ejecuta la operación; la ejecución requiere un turno de caja abierto.";
        LifecycleText.Text = $"Solicitó: {row.SolicitadoPor} · {row.Fecha:g}\n" + (row.Estado == "Rechazada" ? $"Rechazó: {row.AutorizadoPor} · Motivo: {row.MotivoRechazo}" : row.AutorizadoPor == "—" ? "Sin decisión registrada." : $"Decisión registrada por: {row.AutorizadoPor}") + (row.Ejecutada is DateTime executed ? $"\nEjecutó: {row.EjecutadoPor} · {executed:g}" : "");
    }
    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingSelection is not { Estado: "Pendiente" } row) return;
        var impact = row.Tipo == "Devolución" ? FinanzasControl.Money(row.Monto) : FinanzasControl.Money(row.PagoOriginal);
        if (MessageBox.Show($"¿Autorizar {row.Tipo.ToLowerInvariant()} por {impact}?\n\nEsta decisión no ejecuta el movimiento de caja.", "Confirmar autorización", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            var corrections = new CorreccionFinancieraService();
            if (row.Tipo == "Devolución") corrections.AutorizarDevolucion(row.Id); else corrections.AutorizarAnulacion(row.Id);
            Recargar();
        }
        catch (Exception ex) { ActionStatus.Text = ex.Message; }
    }
    private void Reject_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingSelection is not { Estado: "Pendiente" } row) return;
        var dialog = new MotivoRechazoFinancieroDialog("Rechazar " + row.Tipo.ToLowerInvariant(), $"Solicitud #{row.Id} · {row.Huesped} · Pago #{row.PagoId} · {FinanzasControl.Money(row.Monto)}") { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var corrections = new CorreccionFinancieraService();
            if (row.Tipo == "Devolución") corrections.RechazarDevolucion(row.Id, dialog.Motivo); else corrections.RechazarAnulacion(row.Id, dialog.Motivo);
            Recargar();
        }
        catch (Exception ex) { ActionStatus.Text = ex.Message; }
    }
}
