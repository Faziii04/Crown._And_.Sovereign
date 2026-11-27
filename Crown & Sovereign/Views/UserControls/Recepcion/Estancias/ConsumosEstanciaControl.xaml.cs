using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class ConsumosEstanciaControl : UserControl
{
    private readonly InventarioService _inventario = new();
    public ConsumosEstanciaControl() => InitializeComponent();
    public void Mostrar(int stayId)
    {
        var rows = _inventario.ConsultarConsumosPorEstancia(stayId).Select(item =>
        {
            var guest = Crown___Sovereign.Data.SampleData.Huespedes.FirstOrDefault(person => person.Id == item.Consumo.HuespedId);
            var cargo = item.Consumo.CargoId is int cargoId ? Crown___Sovereign.Data.SampleData.Cargos.FirstOrDefault(entry => entry.Id == cargoId) : null;
            var unitPrice = item.Consumo.PrecioUnitarioAcordado;
            var amount = cargo?.Monto ?? (unitPrice is decimal snapshot ? snapshot * item.Consumo.Cantidad : item.Consumo.Incluido ? 0m : (decimal?)null);
            return new { item.Consumo.Fecha, Producto = item.Producto, Guest = guest?.Nombre ?? "—", Origen = string.IsNullOrWhiteSpace(item.Consumo.Origen) ? "No registrado" : item.Consumo.Origen, Cantidad = item.Consumo.Cantidad, Coverage = item.Consumo.Incluido ? "Incluido" : "Adicional", UnitPrice = unitPrice is decimal price ? $"Bs {price:N2}" : "No registrado", Amount = amount is decimal total ? $"Bs {total:N2}" : "Sin importe asociado" };
        }).ToList();
        ConsumptionGrid.ItemsSource = rows;
        EmptyText.Visibility = rows.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        var source = _inventario.ConsultarConsumosPorEstancia(stayId).Select(item => item.Consumo).ToList();
        var products = source.Sum(item => item.Cantidad);
        var included = source.Where(item => item.Incluido).Sum(item => item.Cantidad);
        var additional = source.Where(item => !item.Incluido).Sum(item => item.Cantidad);
        Metrics.ItemsSource = new[] { new { Title = "Productos consumidos", Value = products.ToString() }, new { Title = "Unidades incluidas", Value = included.ToString() }, new { Title = "Unidades adicionales", Value = additional.ToString() } };
    }
}
