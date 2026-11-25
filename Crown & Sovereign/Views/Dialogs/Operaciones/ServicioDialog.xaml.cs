using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class ServicioDialog : Window
{
    private readonly ServicioService _service = new();
    private readonly ServicioModel? _editing;
    private readonly int? _parentId;
    public ServicioDialog(ServicioModel? editing = null, int? parentId = null)
    {
        InitializeComponent(); _editing = editing; _parentId = editing?.PadreId ?? parentId;
        var gerente=SesionActual.Usuario?.Rol=="Gerente";PriceBox.IsEnabled=gerente;CoverageBox.IsEnabled=gerente;UsesBox.IsEnabled=gerente;
        if (editing is not null) DialogTitle.Text = $"Editar servicio · {editing.Nombre}";
        CategoryBox.ItemsSource = SampleData.Servicios.Select(item => item.Categoria).Distinct().ToList(); CategoryBox.ItemsSource = CategoryBox.Items.Cast<string>().Append("Bienestar").Append("Transporte").Append("Recreación").Distinct().ToList();
        ParentBox.ItemsSource = new ServicioModel?[] { null }.Concat(SampleData.Servicios.Where(item => item.PadreId is null)).ToList();
        ProviderBox.ItemsSource = new[] { "Interno", "Taxi Ejecutivo", "Andes Tours", "Rent a Car Central", "Spa Kallpa" };
        DurationBox.ItemsSource = new[] { "15", "30", "45", "60", "90", "120" };
        CoverageBox.ItemsSource = new[] { "No incluido", "Ilimitado", "Por día", "Por estancia" };
        ProductBox.ItemsSource = SampleData.Productos;
        NameBox.Text = editing?.Nombre ?? ""; CategoryBox.SelectedItem = editing?.Categoria ?? CategoryBox.Items.Cast<string>().FirstOrDefault(); ParentBox.SelectedItem = SampleData.Servicios.FirstOrDefault(item => item.Id == _parentId); ProviderBox.SelectedItem = editing?.Proveedor ?? "Interno"; PriceBox.Text = (editing?.Precio ?? 0).ToString("N2"); AppointmentCheck.IsChecked = editing?.RequiereCita ?? false; DurationBox.SelectedItem = (editing?.DuracionMinutos ?? 60).ToString(); CoverageBox.SelectedItem = editing?.Cobertura ?? "No incluido"; UsesBox.Text = editing?.UsosRestantes?.ToString() ?? ""; ProductBox.SelectedItem = SampleData.Productos.FirstOrDefault(item => item.Id == editing?.ProductoAsociadoId);
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(PriceBox.Text, out var price) || !int.TryParse(DurationBox.SelectedItem?.ToString(), out var duration) || (UsesBox.Text.Length > 0 && !int.TryParse(UsesBox.Text, out _))) { ValidationText.Text = "Revisa el precio, duración y número de usos."; return; }
        var parent = ParentBox.SelectedItem as ServicioModel;
        if (_editing is not null && parent?.Id == _editing.Id) { ValidationText.Text = "Un servicio no puede ser su propio padre."; return; }
        try
        {
            var uses = int.TryParse(UsesBox.Text, out var count) ? count : (int?)null;
            var productId = (ProductBox.SelectedItem as ProductoModel)?.Id;
            if (_editing is null) _service.Crear(NameBox.Text, CategoryBox.SelectedItem?.ToString() ?? "General", price, AppointmentCheck.IsChecked == true, parent?.Id ?? _parentId, CoverageBox.SelectedItem?.ToString() ?? "No incluido", uses, duration, ProviderBox.SelectedItem?.ToString() ?? "Interno", productId);
            else _service.Actualizar(_editing, NameBox.Text, CategoryBox.SelectedItem?.ToString() ?? "General", price, AppointmentCheck.IsChecked == true, CoverageBox.SelectedItem?.ToString() ?? "No incluido", uses, duration, ProviderBox.SelectedItem?.ToString() ?? "Interno", productId);
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
