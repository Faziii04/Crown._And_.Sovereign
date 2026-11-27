using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

public partial class ProveedoresConveniosControl : UserControl
{
    private readonly ConvenioService _service = new();
    private IReadOnlyList<ConvenioFilaView> _rows = [];
    public event EventHandler<int>? DetailRequested;
    public event EventHandler<int>? EditRequested;
    public event EventHandler<int>? RenewRequested;
    public event EventHandler? CatalogChanged;

    public ProveedoresConveniosControl()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = _service.Categorias(); CategoryBox.SelectedIndex = 0;
        StatusBox.ItemsSource = new[] { "Todos", "Vigente", "Por vencer", "Vencido", "Por iniciar", "Inactivo" }; StatusBox.SelectedIndex = 0;
        Recargar();
    }

    public void Recargar()
    {
        var state = StatusBox.SelectedItem?.ToString();
        var statusFilter = state == "Todos" ? null : state;
        _rows = _service.Listar(SearchBox.Text, CategoryBox.SelectedItem?.ToString(), statusFilter, HasServices.IsChecked == true, NeedsReview.IsChecked == true);
        AgreementCards.ItemsSource = _rows; AgreementGrid.ItemsSource = _rows;
        ResultsText.Text = $"{_rows.Count} convenio(s)";
        CardsEmpty.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TableEmpty.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void FijarFiltro(string? status)
    {
        StatusBox.SelectedItem = string.IsNullOrWhiteSpace(status) ? "Todos" : status;
        Recargar();
    }

    public void FijarRevision()
    {
        StatusBox.SelectedItem = "Todos"; NeedsReview.IsChecked = true; Recargar();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsInitialized) Recargar(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized) Recargar(); }
    private void Cards_Click(object sender, RoutedEventArgs e) { CardsPanel.Visibility = Visibility.Visible; TablePanel.Visibility = Visibility.Collapsed; }
    private void List_Click(object sender, RoutedEventArgs e) { CardsPanel.Visibility = Visibility.Collapsed; TablePanel.Visibility = Visibility.Visible; }
    private void QuickStatus_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is string status) FijarFiltro(status); }
    private void Card_DetailRequested(object sender, int id) => DetailRequested?.Invoke(this, id);
    private void Card_EditRequested(object sender, int id) => EditRequested?.Invoke(this, id);
    private void Grid_Detail_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) DetailRequested?.Invoke(this, id); }
    private void Grid_Edit_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) EditRequested?.Invoke(this, id); }
    private void Grid_Renew_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) RenewRequested?.Invoke(this, id); }

    public void NotificarCambio()
    {
        Recargar(); CatalogChanged?.Invoke(this, EventArgs.Empty);
    }
}
