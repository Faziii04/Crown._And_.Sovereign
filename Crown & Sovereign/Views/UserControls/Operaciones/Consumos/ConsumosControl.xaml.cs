using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Shared;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Consumos;

public partial class ConsumosControl : UserControl
{
    private readonly Dictionary<int, int> _cart = [];
    private readonly InventarioService _inventory = new();
    private ActiveGuestChoice? _guest;
    public ConsumosControl()
    {
        InitializeComponent(); CatalogView.ProductoAgregado += Agregar; CartView.CantidadSolicitada += CambiarCantidad; CartView.ProductoEliminado += Eliminar; CartView.ConfirmacionSolicitada += Confirmar;
        if (new HuespedService().ListarActivos().FirstOrDefault() is { } guest) MostrarHuesped(guest);
        ActualizarCarrito();
    }
    private void ChangeGuest_Click(object sender, RoutedEventArgs e)
    {
        var selector = new SelectorHuespedDialog { Owner = Window.GetWindow(this) };
        if (selector.ShowDialog() == true && selector.HuespedSeleccionado is { } guest) MostrarHuesped(guest);
    }
    private void MostrarHuesped(ActiveGuestChoice guest)
    {
        if (_guest?.Id != guest.Id) _cart.Clear();
        _guest = guest;
        GuestText.Text = $"{guest.Name} · Hab. {guest.Room} · Piso {guest.Floor} · {guest.Package}";
        ActualizarCarrito();
    }
    private void Agregar(ProductoModel product)
    {
        if (product.Stock < _cart.GetValueOrDefault(product.Id) + 1) { MessageBox.Show("No hay existencias suficientes.", "Inventario"); return; }
        _cart[product.Id] = _cart.GetValueOrDefault(product.Id) + 1; ActualizarCarrito();
    }
    private void CambiarCantidad(int id, int delta)
    {
        var quantity = _cart.GetValueOrDefault(id) + delta;
        if (quantity <= 0) _cart.Remove(id);
        else if (quantity > _inventory.Buscar().First(product => product.Id == id).Stock) { MessageBox.Show("La cantidad supera el stock disponible.", "Inventario"); return; }
        else _cart[id] = quantity;
        ActualizarCarrito();
    }
    private void Eliminar(int id) { _cart.Remove(id); ActualizarCarrito(); }
    private void ActualizarCarrito()
    {
        var lines = _cart.Select(item => new ProductCartLine(_inventory.Buscar().First(product => product.Id == item.Key), item.Value)).ToList();
        CartView.Mostrar(lines, lines.Where(line => !line.Producto.IncluidoEnPaquete || _guest?.Package == "Sin paquete").Sum(line => line.Subtotal));
    }
    private void Confirmar()
    {
        if (_guest is null || _cart.Count == 0) { MessageBox.Show("Selecciona un huésped activo y agrega productos al pedido.", "Consumo"); return; }
        if (MessageBox.Show($"¿Registrar {_cart.Values.Sum()} unidades para {_guest.Name}? Los productos adicionales se cargarán al folio.", "Confirmar consumo", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _inventory.RegistrarConsumos(_cart.Select(item => (item.Key, item.Value)).ToList(), _guest.Id); _cart.Clear(); ActualizarCarrito(); CatalogView.Recargar(); MessageBox.Show("Consumo registrado; inventario y folio actualizados.", "Consumos"); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se registró el consumo", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
