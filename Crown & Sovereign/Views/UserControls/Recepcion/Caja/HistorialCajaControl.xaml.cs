using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class HistorialCajaControl : UserControl
{
    private readonly CajaService _cash = new();
    public HistorialCajaControl()
    {
        InitializeComponent(); StateBox.ItemsSource = new[] { "Todos", "Abiertos", "Cerrados" }; StateBox.SelectedIndex = 0; Recargar();
    }
    public void Recargar()
    {
        if (ClosuresGrid is null) return;
        var filter = StateBox.SelectedItem?.ToString() ?? "Todos";
        var rows = _cash.ListarTurnos(SearchBox?.Text ?? "", filter == "Abiertos" ? "Todos" : filter)
            .Where(item => filter != "Abiertos" || item.Estado == "Abierto")
            .Select(item =>
            {
                var closure = _cash.ObtenerCierre(item.Id);
                return new ShiftHistoryRow(item.Id, SampleData.Usuarios.FirstOrDefault(user => user.Id == item.UsuarioId)?.Nombre ?? "—", item.Apertura, item.Cierre?.ToString("dd/MM/yyyy HH:mm") ?? "—", item.Estado, Format(closure?.Esperado), Format(closure?.Contado), Format(closure?.Diferencia));
            }).ToList();
        ClosuresGrid.ItemsSource = rows;
    }
    private void Filter_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Shift_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ClosuresGrid.SelectedItem is ShiftHistoryRow row) new DetalleTurnoCajaDialog(row.TurnoId) { Owner = System.Windows.Window.GetWindow(this) }.ShowDialog();
    }
    private static string Format(decimal? amount) => amount is decimal value ? $"Bs {value:N2}" : "—";
    public sealed record ShiftHistoryRow(int TurnoId, string Usuario, DateTime Apertura, string Cierre, string Estado, string Esperado, string Contado, string Diferencia)
    { public string Turno => $"#{TurnoId:0000}"; }
}
