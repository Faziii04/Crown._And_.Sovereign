using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class PagoDialog : Window
{
    private readonly int _folioId;
    private readonly decimal _balance;
    private readonly PagoService _payments = new();
    private readonly ObservableCollection<PagoLineaDraft> _parts = [];
    private static readonly CultureInfo Bolivian = CultureInfo.GetCultureInfo("es-BO");
    public IReadOnlyList<string> Metodos => SampleData.MetodosPago.Where(item => item.Activo).Select(item => item.Nombre).ToList();
    public PagoDialog(int folioId)
    {
        InitializeComponent();
        _folioId = folioId;
        var folio = SampleData.Folios.First(item => item.Id == folioId);
        _balance = new FolioService().CalcularSaldoFolio(folioId);
        var row = _payments.BuscarFolios("", "Todos", false).FirstOrDefault(item => item.FolioId == folioId);
        AccountText.Text = row is null ? $"Folio #{folioId}" : $"{row.Huésped} · {row.Contexto} · {row.Habitación} · Folio #{folioId}";
        BalanceText.Text = $"Bs {_balance:N2}";
        var shift = new CajaService().ObtenerTurnoActual();
        ShiftText.Text = shift is null ? "Caja cerrada" : $"Caja abierta · Turno #{shift.Id:0000}";
        _parts.Add(new() { Metodo = Metodos.FirstOrDefault() ?? "Efectivo", MontoTexto = _balance.ToString("N2", Bolivian) });
        DataContext = this;
        PartsGrid.ItemsSource = _parts;
        ActualizarTotales();
    }
    private void AddPart_Click(object sender, RoutedEventArgs e) => _parts.Add(new() { Metodo = Metodos.FirstOrDefault() ?? "Efectivo", MontoTexto = "0,00" });
    private void RemovePart_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PagoLineaDraft line) _parts.Remove(line);
        ActualizarTotales();
    }
    private void PartAmount_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) ActualizarTotales();
    }
    private void ActualizarTotales()
    {
        var total = _parts.Sum(item => item.Monto);
        TotalText.Text = $"Bs {total:N2}";
        RemainingText.Text = $"Bs {Math.Max(0, _balance - total):N2}";
        RemainingText.Foreground = total > _balance ? System.Windows.Media.Brushes.Firebrick : System.Windows.Media.Brushes.SeaGreen;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var parts = _parts.Select(item => (item.Metodo, item.Monto)).ToList();
        if (parts.Count == 0 || parts.Any(item => item.Monto <= 0)) { ValidationText.Text = "Agrega un importe positivo para cada medio de pago."; return; }
        try
        {
            _ = _payments.Registrar(_folioId, parts.Select(item => new PagoParte(item.Metodo, item.Monto)).ToList(), ReferenceBox.Text);
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
    public sealed class PagoLineaDraft
    {
        public string Metodo { get; set; } = "Efectivo";
        public string MontoTexto { get; set; } = "0,00";
        public decimal Monto => decimal.TryParse(MontoTexto, NumberStyles.Number, Bolivian, out var parsed) || decimal.TryParse(MontoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
    }
}
