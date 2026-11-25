using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Consumos;

public partial class CarritoConsumosControl : UserControl
{
    public event Action<int, int>? CantidadSolicitada;
    public event Action<int>? ProductoEliminado;
    public event Action? ConfirmacionSolicitada;
    public CarritoConsumosControl() => InitializeComponent();
    public void Mostrar(IReadOnlyList<ProductCartLine> lines, decimal total)
    {
        CartList.ItemsSource = lines; TotalText.Text = $"Bs {total:N2}";
    }
    private void Quantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id } button) return;
        CantidadSolicitada?.Invoke(id, button.Content?.ToString() == "+" ? 1 : -1);
    }
    private void Remove_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: int id }) ProductoEliminado?.Invoke(id); }
    private void Confirm_Click(object sender, RoutedEventArgs e) => ConfirmacionSolicitada?.Invoke();
}
