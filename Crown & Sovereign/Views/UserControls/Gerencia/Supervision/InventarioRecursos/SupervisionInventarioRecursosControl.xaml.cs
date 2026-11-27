using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class SupervisionInventarioRecursosControl : UserControl
{
    private IReadOnlyList<SupervisionProductoFila> _products = [];
    private IReadOnlyList<SupervisionRecursoFila> _resources = [];
    public SupervisionInventarioRecursosControl() => InitializeComponent();
    public void Cargar(SupervisionSnapshot snapshot)
    {
        _products = snapshot.Productos; _resources = snapshot.Recursos;
        ConsumptionGrid.ItemsSource = snapshot.Consumos;
        ResourcesGrid.ItemsSource = _resources;
        ApplyFilters();
    }
    public void AbrirAlerta(SupervisionAlerta alert)
    {
        var product = _products.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (product is not null) { InventoryTabs.SelectedIndex = 0; ProductsGrid.SelectedItem = product; ProductsGrid.ScrollIntoView(product); return; }
        var resource = _resources.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (resource is not null) { InventoryTabs.SelectedIndex = 1; ResourcesGrid.SelectedItem = resource; ResourcesGrid.ScrollIntoView(resource); }
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void ApplyFilters()
    {
        if (ProductsGrid is null) return;
        var query = SearchBox.Text.Trim(); var stock = (StockBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los productos";
        ProductsGrid.ItemsSource = _products.Where(item => (stock == "Todos los productos" || stock == "Bajo mínimo" && item.Stock < item.Minimo || stock == "Agotados" && item.Stock <= 0)
            && (string.IsNullOrWhiteSpace(query) || $"{item.Producto} {item.Categoria} {item.ServiciosAfectados}".Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
    }
}

