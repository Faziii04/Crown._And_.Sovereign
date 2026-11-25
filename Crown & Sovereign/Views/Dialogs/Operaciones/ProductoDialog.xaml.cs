using System.Globalization;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class ProductoDialog : Window
{
    private readonly ProductoModel? _product;
    private readonly InventarioService _service = new();
    public ProductoDialog(ProductoModel? product = null)
    {
        InitializeComponent(); _product = product;
        CategoryBox.ItemsSource = new[] { "Alimentos", "Bebidas", "Higiene", "Amenities", "Otros" };
        if (product is null) return;
        TitleText.Text = "Editar producto"; NameBox.Text = product.Nombre; CategoryBox.Text = product.Categoria;
        PriceBox.Text = product.Precio.ToString(CultureInfo.CurrentCulture); StockBox.Text = product.Stock.ToString(); StockBox.IsReadOnly = true;
        MinimumBox.Text = product.StockMinimo.ToString(); IncludedBox.IsChecked = product.IncluidoEnPaquete;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || !int.TryParse(StockBox.Text, out var stock) || !int.TryParse(MinimumBox.Text, out var minimum)) { MessageBox.Show(this, "Revisa el precio y las cantidades.", "Datos inválidos", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { if (_product is null) _service.Crear(NameBox.Text, CategoryBox.Text, price, stock, minimum, IncludedBox.IsChecked == true); else _service.Actualizar(_product, NameBox.Text, CategoryBox.Text, price, minimum, IncludedBox.IsChecked == true); DialogResult = true; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "No se pudo guardar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
