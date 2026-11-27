using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class ServicioDialog : Window
{
    private readonly CatalogoServiciosService _service = new();
    private readonly ServicioModel? _editing;
    private bool _updatingPreview;
    public ServicioModel? Guardado { get; private set; }
    public ServicioDialog(ServicioModel? editing = null, int? parentId = null)
    {
        InitializeComponent(); _editing = editing;
        var manager = SesionActual.Usuario?.Rol == "Gerente";
        ActiveCheck.IsEnabled = manager; ReservableCheck.IsEnabled = manager;
        DialogTitle.Text = editing is null ? "Nuevo servicio" : $"Editar servicio · {editing.Nombre}";
        var categories = SampleData.Servicios.Select(item => item.Categoria).Distinct(StringComparer.OrdinalIgnoreCase)
            .Concat(["Bienestar", "Transporte", "Recreación", "Alimentación", "Habitación"]).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item).ToList();
        CategoryBox.ItemsSource = categories;
        var sentinel = new ServicioModel { Id = 0, Nombre = "(Sin servicio principal)" };
        ParentBox.ItemsSource = new[] { sentinel }.Concat(SampleData.Servicios.Where(item => item.PadreId is null && item.Id != editing?.Id).OrderBy(item => item.Nombre)).ToList();
        NameBox.Text = editing?.Nombre ?? ""; CategoryBox.SelectedItem = editing?.Categoria ?? categories.FirstOrDefault();
        ParentBox.SelectedItem = ParentBox.Items.Cast<ServicioModel>().FirstOrDefault(item => item.Id == (editing?.PadreId ?? parentId)) ?? sentinel;
        DescriptionBox.Text = editing?.Descripcion ?? ""; ActiveCheck.IsChecked = editing?.Activo ?? true; ReservableCheck.IsChecked = editing?.Reservable ?? false;
        AppointmentCheck.IsChecked = editing?.RequiereCita ?? false; DurationBox.Text = editing?.DuracionMinutos.ToString() ?? "60";
        PriceBox.Text = editing?.Precio.ToString("0.00", CultureInfo.CurrentCulture) ?? "0"; PriceBox.IsEnabled = manager;
        PreviousButton.IsEnabled = false; UpdateConfirmation();
        NameBox.TextChanged += (_, _) => UpdateConfirmation(); CategoryBox.SelectionChanged += (_, _) => UpdateConfirmation(); ParentBox.SelectionChanged += (_, _) => UpdateConfirmation();
        DescriptionBox.TextChanged += (_, _) => UpdateConfirmation(); ActiveCheck.Checked += (_, _) => UpdateConfirmation(); ActiveCheck.Unchecked += (_, _) => UpdateConfirmation();
        ReservableCheck.Checked += (_, _) => UpdateConfirmation(); ReservableCheck.Unchecked += (_, _) => UpdateConfirmation(); AppointmentCheck.Checked += (_, _) => UpdateConfirmation(); AppointmentCheck.Unchecked += (_, _) => UpdateConfirmation();
        DurationBox.TextChanged += (_, _) => UpdateConfirmation(); PriceBox.TextChanged += (_, _) => UpdateConfirmation();
    }
    private void Steps_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (Steps is null) return; SyncButtons(); UpdateConfirmation(); }
    private void SyncButtons() { var index = Steps.SelectedIndex; PreviousButton.IsEnabled = index > 0; NextButton.Visibility = index < 3 ? Visibility.Visible : Visibility.Collapsed; SaveButton.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed; }
    private void Previous_Click(object sender, RoutedEventArgs e) { if (Steps.SelectedIndex > 0) Steps.SelectedIndex--; }
    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidStep(Steps.SelectedIndex)) return;
        if (Steps.SelectedIndex < 3) Steps.SelectedIndex++;
    }
    private bool ValidStep(int step)
    {
        ValidationText.Text = "";
        if (step == 0 && (string.IsNullOrWhiteSpace(NameBox.Text) || CategoryBox.SelectedItem is null)) { ValidationText.Text = "Completa el nombre y la categoría."; return false; }
        if (step == 1 && (!int.TryParse(DurationBox.Text, out var duration) || duration < 0 || (AppointmentCheck.IsChecked == true && duration < 15))) { ValidationText.Text = "Usa una duración válida; las citas requieren al menos 15 minutos."; return false; }
        if (step == 2 && (!decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price < 0)) { ValidationText.Text = "El precio oficial debe ser un número igual o mayor que cero."; return false; }
        return true;
    }
    private void UpdateConfirmation()
    {
        if (_updatingPreview || ConfirmationText is null) return;
        _updatingPreview = true;
        var parent = (ParentBox.SelectedItem as ServicioModel)?.Id is int id && id != 0 ? SampleData.Servicios.FirstOrDefault(item => item.Id == id)?.Nombre : null;
        var route = string.Join(" › ", new[] { CategoryBox.SelectedItem?.ToString(), parent, NameBox.Text }.Where(item => !string.IsNullOrWhiteSpace(item)));
        var price = decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : _editing?.Precio ?? 0m;
        var oldPrice = _editing?.Precio;
        HierarchyPreview.Text = $"Vista de jerarquía: {route}";
        var acquiredCount = _editing is null ? 0 : SampleData.ServiciosAdquiridos.Count(item => item.ServicioId == _editing.Id && item.PrecioUnitarioAcordado.HasValue);
        var pendingRequests = _editing is null ? 0 : SampleData.SolicitudesServicio.Count(item => item.ServicioId == _editing.Id && item.Estado is not ("Completada" or "Cancelada" or "No presentado"));
        var pendingAppointments = _editing is null ? 0 : SampleData.Citas.Count(item => item.ServicioId == _editing.Id && item.Estado is not ("Completada" or "Cancelada" or "No presentado"));
        var packageEntitlements = _editing is null ? 0 : SampleData.ReservaPaqueteServicios.Where(item => item.ServicioId == _editing.Id).Select(item => item.ReservaId).Distinct().Count();
        var stateImpact = ActiveCheck.IsChecked == false ? $"\nDesactivar impedirá nuevas adquisiciones. Beneficios en reservas: {packageEntitlements}; compras adicionales con precio acordado: {acquiredCount}; solicitudes abiertas: {pendingRequests}; citas abiertas: {pendingAppointments}. Los registros existentes se conservan." : "";
        ConfirmationText.Text = $"{(_editing is null ? "Nuevo" : "Editar")}: {NameBox.Text}\nRuta: {route}\nDescripción: {(string.IsNullOrWhiteSpace(DescriptionBox.Text) ? "Sin descripción" : DescriptionBox.Text.Trim())}\nEstado: {(ActiveCheck.IsChecked == true ? "Activo" : "Inactivo")} · {(ReservableCheck.IsChecked == true ? "Oferta reservable" : "Agrupación organizativa")}\nOperación: {(AppointmentCheck.IsChecked == true ? $"Requiere cita · {DurationBox.Text} min" : "Sin cita") }\nPrecio oficial: Bs {price:N2}" + (oldPrice is decimal old && old != price ? $" (antes Bs {old:N2})" : "" ) + $"\nAdquisiciones con precio acordado protegido: {acquiredCount}." + stateImpact + "\n\nCambiar el precio no modifica costos de proveedor, coberturas de paquetes ni precios históricos.";
        _updatingPreview = false; SyncButtons();
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidStep(0) || !ValidStep(1) || !ValidStep(2)) return;
        int? parentId = (ParentBox.SelectedItem as ServicioModel)?.Id is int id && id != 0 ? id : null;
        var price = decimal.Parse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture); var duration = int.Parse(DurationBox.Text);
        try
        {
            if (_editing is null) Guardado = _service.Crear(NameBox.Text, CategoryBox.SelectedItem!.ToString()!, DescriptionBox.Text, parentId, ActiveCheck.IsChecked == true, ReservableCheck.IsChecked == true, AppointmentCheck.IsChecked == true, duration, price);
            else { _service.Guardar(_editing, NameBox.Text, CategoryBox.SelectedItem!.ToString()!, DescriptionBox.Text, parentId, ActiveCheck.IsChecked == true, ReservableCheck.IsChecked == true, AppointmentCheck.IsChecked == true, duration, price); Guardado = _editing; }
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
