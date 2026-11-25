using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Consumos;

public partial class CatalogoConsumosControl : UserControl
{
    private readonly InventarioService _service = new();
    public event Action<ProductoModel>? ProductoAgregado;
    public CatalogoConsumosControl()
    {
        InitializeComponent(); CategoryBox.ItemsSource = new[] { "Todas las categorías" }.Concat(SampleData.Productos.Select(item => item.Categoria).Distinct()).ToList(); CategoryBox.SelectedIndex = 0; Recargar();
    }
    public void Recargar()
    {
        var products = _service.Buscar(SearchBox.Text, CategoryBox.SelectedItem?.ToString());
        ProductList.ItemsSource = products.Select(product => new ProductCard(product, product.IncluidoEnPaquete ? "Puede estar incluido en paquete" : "Cargo adicional")).ToList();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Category_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Add_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: ProductCard card }) ProductoAgregado?.Invoke(card.Producto); }
    public sealed record ProductCard(ProductoModel Producto, string CoberturaTexto)
    { public int Id => Producto.Id; public string Nombre => Producto.Nombre; public string Categoria => Producto.Categoria; public decimal Precio => Producto.Precio; public int Stock => Producto.Stock; }
}
