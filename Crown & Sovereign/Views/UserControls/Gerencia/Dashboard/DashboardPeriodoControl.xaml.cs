using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardPeriodoControl : UserControl
{
    public event EventHandler<DashboardPeriod>? PeriodChanged;
    private bool _initializing = true;
    public DashboardPeriodoControl()
    {
        InitializeComponent();
        PeriodBox.ItemsSource = new[] { "Hoy", "Últimos 7 días", "Este mes", "Mes anterior", "Últimos 30 días", "Últimos 90 días", "Este año", "Personalizado" };
        PeriodBox.SelectedItem = "Este mes";
        _initializing = false;
        FromPicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToPicker.SelectedDate = DateTime.Today;
        Publish();
    }
    private void PeriodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || !IsLoaded) return;
        var custom = PeriodBox.SelectedItem?.ToString() == "Personalizado";
        FromPicker.Visibility = ToPicker.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        if (custom) { FromPicker.SelectedDate ??= DateTime.Today; ToPicker.SelectedDate ??= DateTime.Today; }
        Publish();
    }
    private void CustomDate_Changed(object? sender, SelectionChangedEventArgs e) { if (!_initializing && PeriodBox.SelectedItem?.ToString() == "Personalizado") Publish(); }
    private void CompareBox_Changed(object sender, RoutedEventArgs e) => Publish();
    private void Refresh_Click(object sender, RoutedEventArgs e) => Publish();
    private void Publish()
    {
        if (RangeText is null) return;
        var today = DateTime.Today;
        var selected = PeriodBox.SelectedItem?.ToString() ?? "Este mes";
        DateTime from, to;
        switch (selected)
        {
            case "Hoy": from = to = today; break;
            case "Últimos 7 días": from = today.AddDays(-6); to = today; break;
            case "Este mes": from = new DateTime(today.Year, today.Month, 1); to = today; break;
            case "Mes anterior": from = new DateTime(today.Year, today.Month, 1).AddMonths(-1); to = new DateTime(today.Year, today.Month, 1).AddDays(-1); break;
            case "Últimos 30 días": from = today.AddDays(-29); to = today; break;
            case "Últimos 90 días": from = today.AddDays(-89); to = today; break;
            case "Este año": from = new DateTime(today.Year, 1, 1); to = today; break;
            default:
                from = FromPicker.SelectedDate?.Date ?? today; to = ToPicker.SelectedDate?.Date ?? today;
                if (to < from) { RangeText.Text = "La fecha final debe ser igual o posterior a la inicial."; return; }
                break;
        }
        RangeText.Text = $"{from:dd MMM yyyy} – {to:dd MMM yyyy}";
        var length = (to - from).Days + 1;
        var compare = CompareBox.IsChecked == true;
        var comparisonFrom = compare ? from.AddDays(-length) : from;
        var comparisonTo = compare ? from.AddDays(-1) : to;
        PeriodChanged?.Invoke(this, new DashboardPeriod(selected, from, to, comparisonFrom, comparisonTo) { IncluirComparacion = compare });
    }
}
