using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

public partial class VencimientosControl : UserControl
{
    private readonly ConvenioService _service = new();
    public event EventHandler<int>? DetailRequested;
    public event EventHandler<int>? RenewRequested;
    public VencimientosControl()
    {
        InitializeComponent();
        HorizonBox.ItemsSource = new[] { "30 días", "60 días", "90 días", "180 días" }; HorizonBox.SelectedItem = "90 días";
        StatusBox.ItemsSource = new[] { "Próximos", "Vigente", "Por vencer", "Vencido", "Por iniciar", "Todos" }; StatusBox.SelectedItem = "Próximos";
        Recargar();
    }
    public void Recargar()
    {
        var horizon = int.TryParse(HorizonBox.SelectedItem?.ToString()?.Split(' ').FirstOrDefault(), out var days) ? days : 90;
        var status = StatusBox.SelectedItem?.ToString() ?? "Próximos";
        var rows = _service.Listar(vigencia: null).Where(row => row.Convenio.Activo);
        rows = status switch
        {
            "Por vencer" => rows.Where(row => row.EstadoVigencia == "Por vencer"),
            "Vigente" => rows.Where(row => ConvenioService.EsVigente(row.Convenio, DateTime.Today)),
            "Vencido" => rows.Where(row => row.EstadoVigencia == "Vencido"),
            "Por iniciar" => rows.Where(row => row.EstadoVigencia == "Por iniciar"),
            "Todos" => rows,
            _ => rows.Where(row => row.EstadoVigencia == "Por vencer" || row.EstadoVigencia == "Vigente" && row.Convenio.VigenciaHasta.Date <= DateTime.Today.AddDays(horizon))
        };
        AgreementsGrid.ItemsSource = rows.OrderBy(row => row.Convenio.VigenciaHasta).ToList();
        EmptyState.Visibility = AgreementsGrid.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    public void FijarFiltro(string? estado)
    {
        StatusBox.SelectedItem = string.IsNullOrWhiteSpace(estado) ? "Todos" : estado;
        Recargar();
    }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized && HorizonBox.SelectedItem is not null && StatusBox.SelectedItem is not null) Recargar(); }
    private void Detail_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) DetailRequested?.Invoke(this, id); }
    private void Renew_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) RenewRequested?.Invoke(this, id); }
}
