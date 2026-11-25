using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Administracion;

public partial class HabitacionDialog : Window
{
    private readonly HabitacionService _service = new();
    private readonly HabitacionModel? _room;
    public HabitacionDialog(HabitacionModel? room = null)
    {
        InitializeComponent();
        _room = room;
        if (room is not null) DialogTitle.Text = $"Editar habitación {room.Numero}";
        FloorBox.ItemsSource = Enumerable.Range(1, 5).Select(floor => $"{floor}").ToList();
        TypeBox.ItemsSource = SampleData.Tipos;
        CapacityBox.ItemsSource = new[] { "1", "2", "3", "4", "5", "6" };
        NumberBox.Text = room?.Numero ?? "";
        FloorBox.SelectedItem = (room?.Piso ?? 1).ToString();
        TypeBox.SelectedItem = room?.Tipo ?? SampleData.Tipos[0];
        CapacityBox.SelectedItem = (room?.Capacidad ?? 2).ToString();
        AmenitiesBox.Text = room?.Comodidades ?? "Wi-Fi · TV · Aire acondicionado";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var number = NumberBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(number) || !int.TryParse(FloorBox.SelectedItem?.ToString(), out var floor) || !int.TryParse(CapacityBox.SelectedItem?.ToString(), out var capacity) || TypeBox.SelectedItem is not string type)
        { ValidationText.Text = "Completa el número, piso, tipo y capacidad."; return; }
        try
        {
            if (_room is null) _service.Crear(number, floor, type, capacity, AmenitiesBox.Text.Trim());
            else _service.Actualizar(_room, number, floor, type, capacity, AmenitiesBox.Text.Trim());
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
