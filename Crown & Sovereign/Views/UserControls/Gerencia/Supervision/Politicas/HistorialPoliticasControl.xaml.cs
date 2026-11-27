using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Politicas;

public sealed record PoliticaHistorialFila(ConfiguracionAuditModel Cambio, DateTime Fecha, string Politica, string ValorAnterior, string ValorNuevo, string Usuario);
public partial class HistorialPoliticasControl : UserControl
{
    private readonly PoliticaHotelService _service = new();
    private IReadOnlyList<PoliticaHistorialFila> _all = [];
    public HistorialPoliticasControl()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = new[] { "Todas", "Alojamiento", "Reservas y cancelaciones", "Pagos y garantías", "Servicios" }; CategoryBox.SelectedIndex = 0;
        UserBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.Usuarios.Select(x => x.Nombre)).Distinct().ToList(); UserBox.SelectedIndex = 0;
        Refresh();
    }
    public void Refresh()
    {
        _all = _service.Historial().Select(x => new PoliticaHistorialFila(x, x.Fecha, Friendly(x.Cambio), x.ValorAnterior, x.ValorNuevo, SampleData.Usuarios.FirstOrDefault(u => u.Id == x.UsuarioId)?.Nombre ?? $"Usuario #{x.UsuarioId}")).ToList();
        Apply();
    }
    private void Apply()
    {
        var userName = UserBox.SelectedItem as string ?? "Todos";
        var userId = userName == "Todos" ? (int?)null : SampleData.Usuarios.FirstOrDefault(x => x.Nombre == userName)?.Id;
        var rows = _service.Historial(SearchBox.Text, CategoryBox.SelectedItem as string ?? "Todas", userId, FromDate.SelectedDate, ToDate.SelectedDate)
            .Select(x => _all.FirstOrDefault(a => a.Cambio.Id == x.Id) ?? new PoliticaHistorialFila(x, x.Fecha, Friendly(x.Cambio), x.ValorAnterior, x.ValorNuevo, SampleData.Usuarios.FirstOrDefault(u => u.Id == x.UsuarioId)?.Nombre ?? $"Usuario #{x.UsuarioId}"));
        HistoryGrid.ItemsSource = rows.ToList();
    }
    private static string Friendly(string key) => key.Replace("POLITICA_SERVICIO.", "Servicio · ", StringComparison.OrdinalIgnoreCase).Replace("POLITICA.", "", StringComparison.OrdinalIgnoreCase).Replace('_', ' ').ToLowerInvariant();
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) Apply(); }
    private void Selection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not PoliticaHistorialFila row) { RestoreButton.IsEnabled = false; return; }
        DetailText.Text = $"Política: {row.Politica}\n\nValor anterior: {row.ValorAnterior}\nValor nuevo: {row.ValorNuevo}\n\nResponsable: {row.Usuario}\nFecha: {row.Fecha:dd/MM/yyyy HH:mm}\n\nRestaurar propone una nueva modificación; no borra el historial.";
        RestoreButton.IsEnabled = row.Cambio.Cambio.StartsWith("POLITICA.", StringComparison.OrdinalIgnoreCase) && !row.Cambio.Cambio.StartsWith("POLITICA_SERVICIO", StringComparison.OrdinalIgnoreCase);
    }
    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not PoliticaHistorialFila row) return;
        if (MessageBox.Show($"Proponer restaurar «{row.Politica}» a «{row.ValorAnterior}»?\nSe guardará como un nuevo cambio.", "Restaurar valor anterior", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _service.Restaurar(row.Cambio); Refresh(); } catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo restaurar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
