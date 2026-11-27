using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardDecisionesControl : UserControl
{
    public event EventHandler? DecisionsRequested;
    public event EventHandler? DecisionsChanged;
    public DashboardDecisionesControl() => InitializeComponent();
    public void Cargar(IReadOnlyList<DashboardDecision> decisions)
    {
        DecisionList.ItemsSource = decisions.Take(5).ToList();
        EmptyText.Visibility = decisions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Open_Click(object sender, RoutedEventArgs e) => DecisionsRequested?.Invoke(this, EventArgs.Empty);
    private void Review_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not DashboardDecision item) return;
        var detail = $"{item.Tipo} pendiente\n\nHuésped: {item.Huesped}\nImporte relacionado: Bs {item.Monto:N2}\nSolicitada: {item.Fecha:g}\nMotivo: {item.Motivo}\n\nLa autorización no ejecuta la operación de caja.";
        var result = MessageBox.Show(detail + "\n\nSí: autorizar · No: rechazar · Cancelar: volver sin cambios.", "Revisión financiera", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return;
        try
        {
            var service = new CorreccionFinancieraService();
            if (item.Tipo == "Devolución")
            {
                if (result == MessageBoxResult.Yes) service.AutorizarDevolucion(item.Id); else service.RechazarDevolucion(item.Id);
            }
            else
            {
                if (result == MessageBoxResult.Yes) service.AutorizarAnulacion(item.Id); else service.RechazarAnulacion(item.Id);
            }
            DecisionsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo autorizar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
