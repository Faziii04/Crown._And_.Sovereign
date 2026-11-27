using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CierreCajaDialog : Window
{
    private readonly int _turnoId;
    private readonly CajaService _cash = new();
    private readonly ObservableCollection<DenominationRow> _denominations = [];
    private decimal _expectedOnReview;
    private static readonly CultureInfo Bolivian = CultureInfo.GetCultureInfo("es-BO");
    public CierreCajaDialog(int turnoId)
    {
        InitializeComponent();
        _turnoId = turnoId;
        var shift = SampleData.TurnosCaja.First(item => item.Id == turnoId);
        ShiftText.Text = $"Turno #{turnoId:0000} · {SampleData.Usuarios.First(item => item.Id == shift.UsuarioId).Nombre} · apertura {shift.Apertura:dd MMM HH:mm}";
        foreach (var amount in new[] { 200m, 100m, 50m, 20m, 10m, 5m, 2m, 1m, 0.5m, 0.2m, 0.1m }) _denominations.Add(new DenominationRow(amount));
        DenominationGrid.ItemsSource = _denominations;
        var pendingRefunds = SampleData.Devoluciones.Count(item => item.Estado == "Pendiente");
        var approvedRefunds = SampleData.Devoluciones.Count(item => item.Estado == "Autorizada");
        var pendingCancellations = SampleData.Anulaciones.Count(item => item.Estado == "Pendiente");
        PendingOperationsText.Text = pendingRefunds + approvedRefunds + pendingCancellations == 0 ? "Sin correcciones financieras pendientes." : $"Aviso: {pendingRefunds} solicitudes de devolución pendientes, {approvedRefunds} autorizadas y {pendingCancellations} anulaciones por revisar. No forman parte del efectivo esperado hasta ejecutarse.";
        ActualizarResumen(_cash.ObtenerResumenTurno(turnoId));
    }
    private void ActualizarResumen(CashDeskShiftSummary summary)
    {
        _expectedOnReview = summary.EfectivoEsperado;
        OpeningText.Text = $"Fondo inicial                         Bs {summary.Turno.MontoInicial:N2}";
        CashText.Text = $"Cobros en efectivo               +Bs {summary.EfectivoCobros:N2}";
        RefundText.Text = $"Devoluciones en efectivo        −Bs {summary.EfectivoDevoluciones:N2}";
        CancellationText.Text = $"Anulaciones en efectivo          −Bs {summary.EfectivoAnulaciones:N2}";
        ExpectedText.Text = $"Bs {summary.EfectivoEsperado:N2}";
        NonCashText.Text = $"Bs {summary.NoEfectivo:N2}";
        ActualizarDiferencia();
    }
    private void Counted_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) ActualizarDiferencia(); }
    private void ActualizarDiferencia()
    {
        if (decimal.TryParse(CountedBox.Text, NumberStyles.Number, Bolivian, out var amount) || decimal.TryParse(CountedBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            var difference = amount - _expectedOnReview;
            DifferenceText.Text = difference == 0 ? "✓ Sin diferencia · Bs 0,00" : difference < 0 ? $"Faltante · −Bs {Math.Abs(difference):N2}" : $"Sobrante · +Bs {difference:N2}";
            DifferenceText.Foreground = difference == 0 ? System.Windows.Media.Brushes.SeaGreen : System.Windows.Media.Brushes.DarkGoldenrod;
        }
        else DifferenceText.Text = "Ingresa el efectivo contado para calcular la diferencia.";
    }
    private void Denomination_Changed(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: DenominationRow row }) row.Actualizar();
    }
    private void UseDenominations_Click(object sender, RoutedEventArgs e)
    {
        if (_denominations.Any(item => !item.CantidadValida)) { ValidationText.Text = "Las cantidades de denominaciones deben ser enteros no negativos."; return; }
        CountedBox.Text = _denominations.Sum(item => item.Subtotal).ToString("N2", Bolivian);
        ValidationText.Text = "";
    }
    private void CloseShift_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(CountedBox.Text, NumberStyles.Number, Bolivian, out var counted) && !decimal.TryParse(CountedBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out counted))
        { ValidationText.Text = "Ingresa el efectivo contado."; return; }
        try
        {
            var closure = _cash.Cerrar(_turnoId, counted, NotesBox.Text.Trim(), _expectedOnReview);
            new CierreCompletadoDialog(closure) { Owner = this }.ShowDialog();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ValidationText.Text = ex.Message;
            if (ex.Message.Contains("movimientos después", StringComparison.OrdinalIgnoreCase)) ActualizarResumen(_cash.ObtenerResumenTurno(_turnoId));
        }
    }
    public sealed class DenominationRow(decimal value) : INotifyPropertyChanged
    {
        private string _cantidadTexto = "0";
        public decimal Valor { get; } = value;
        public string TextoDenominacion => Valor < 1 ? $"Moneda Bs {Valor:N2}" : $"Billete / moneda Bs {Valor:N0}";
        public string CantidadTexto { get => _cantidadTexto; set { if (_cantidadTexto == value) return; _cantidadTexto = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CantidadTexto))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtotal))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CantidadValida))); } }
        public bool CantidadValida => int.TryParse(CantidadTexto, out var count) && count >= 0;
        public decimal Subtotal => CantidadValida ? Valor * int.Parse(CantidadTexto) : 0;
        public event PropertyChangedEventHandler? PropertyChanged;
        public void Actualizar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtotal)));
    }
}
