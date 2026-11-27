using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.PaquetesPromociones;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class PromocionesControl : UserControl
{
    private readonly PromocionService _service = new();
    public event EventHandler? CatalogoCambiado;
    public PromocionesControl() { InitializeComponent(); Refrescar(); }
    public void Refrescar()
    {
        if (!IsInitialized) return;
        var today = DateTime.Today;
        var source = _service.Listar().Select(item => new PromocionTarjetaView(item, Estado(item, today), TextoValor(item), $"{item.Desde:dd MMM yyyy} — {item.Hasta:dd MMM yyyy}",
            item.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase) ? "Sobre alojamiento · no acumulable" : "Alojamiento · regla no persistente",
            !item.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase), item.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase),
            TieneReservas(item.Id) ? "Condiciones protegidas · reserva asociada" : "Estado administrativo independiente de las fechas"));
        var search = SearchBox.Text.Trim(); var status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas"; var type = (TypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los tipos";
        var filtered = source.Where(card => string.IsNullOrWhiteSpace(search) || card.Promocion.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase));
        filtered = status switch { "Vigentes" => filtered.Where(card => card.Promocion.Activa && card.Promocion.Desde.Date <= today && card.Promocion.Hasta.Date >= today), "Próximas" => filtered.Where(card => card.Promocion.Activa && card.Promocion.Desde.Date > today), "Finalizadas" => filtered.Where(card => card.Promocion.Activa && card.Promocion.Hasta.Date < today), "Inactivas" => filtered.Where(card => !card.Promocion.Activa), _ => filtered };
        filtered = type switch { "Porcentaje" => filtered.Where(card => !card.EsPrototipo), "Prototipo" => filtered.Where(card => card.EsPrototipo), _ => filtered };
        var visibleCards = filtered.ToList(); PromotionCards.ItemsSource = visibleCards;
        PromotionEmpty.Visibility = visibleCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var months = Enumerable.Range(0, 4).Select(offset => new DateTime(today.Year, today.Month, 1).AddMonths(offset)).ToList();
        Timeline.ItemsSource = months.Select(month =>
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var active = _service.Listar().Where(item => item.Activa && item.Desde.Date <= monthEnd && item.Hasta.Date >= month).ToList();
            return new MesCampaniaView(month.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-BO")), active.Count == 0 ? "Sin campañas activas" : string.Join(" · ", active.Select(item => item.Nombre)), Math.Min(100, active.Count * 35));
        }).ToList();
        CatalogoCambiado?.Invoke(this, EventArgs.Empty);
    }
    private static bool TieneReservas(int id) => SampleData.Reservas.Any(item => item.PromocionId == id);
    private static string Estado(PromocionModel promo, DateTime today) => !promo.Activa ? "Inactiva" : promo.Desde.Date > today ? "Próxima" : promo.Hasta.Date < today ? "Finalizada" : "Vigente";
    private static string TextoValor(PromocionModel promo) => promo.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase) ? $"{promo.Valor:N2}% de descuento" : promo.TipoDescuento.Equals("Noches", StringComparison.OrdinalIgnoreCase) ? $"{promo.Valor:N0} noche(s) gratuita(s)" : $"{promo.Valor:N2} · regla sin tipo";
    private void Filter_Changed(object sender, EventArgs e) { if (IsInitialized) Refrescar(); }
    private void New_Click(object sender, RoutedEventArgs e) => NuevaPromocion();
    public void NuevaPromocion() { var dialog = new PromocionDialog { Owner = Window.GetWindow(this) }; if (dialog.ShowDialog() == true) Refrescar(); }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not int id) return;
        var promo = SampleData.Promociones.FirstOrDefault(item => item.Id == id); if (promo is null) return;
        var dialog = new PromocionDialog(promo) { Owner = Window.GetWindow(this) }; if (dialog.ShowDialog() == true) Refrescar();
    }
    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not int id) return;
        var promo = SampleData.Promociones.FirstOrDefault(item => item.Id == id); if (promo is null) return;
        try { _service.CambiarEstado(id, !promo.Activa); Refrescar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se puede cambiar la promoción", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not int id) return;
        var promo = SampleData.Promociones.FirstOrDefault(item => item.Id == id); if (promo is null) return;
        if (MessageBox.Show(Window.GetWindow(this), $"¿Eliminar «{promo.Nombre}»? Solo se permite si ninguna reserva está vinculada.", "Eliminar promoción", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _service.Eliminar(id); Refrescar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
