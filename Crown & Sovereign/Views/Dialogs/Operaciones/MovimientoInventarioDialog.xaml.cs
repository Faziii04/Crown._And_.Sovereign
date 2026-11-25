using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class MovimientoInventarioDialog : Window
{
    private readonly InventarioService _service = new();
    public MovimientoInventarioDialog() { InitializeComponent(); ProductBox.ItemsSource = _service.Buscar(); ProductBox.SelectedIndex = 0; TypeBox.ItemsSource = new[] { "Entrada", "Salida", "Ajuste" }; TypeBox.SelectedIndex = 0; }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (ProductBox.SelectedItem is not ProductoModel product || !int.TryParse(QuantityBox.Text, out var quantity)) { MessageBox.Show(this, "Selecciona el producto e indica la cantidad.", "Datos inválidos", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { switch (TypeBox.SelectedItem as string) { case "Entrada": _service.RegistrarEntrada(product.Id, quantity, ReasonBox.Text); break; case "Salida": _service.RegistrarSalida(product.Id, quantity); break; default: _service.Ajustar(product.Id, quantity, ReasonBox.Text); break; } DialogResult = true; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Movimiento no aplicado", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
