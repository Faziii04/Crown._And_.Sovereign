using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class SelectorFolioPagoDialog : Window
{
    private readonly PagoService _payments = new();
    private readonly bool _soloConSaldo;
    public int? FolioIdSeleccionado { get; private set; }
    public SelectorFolioPagoDialog(bool soloConSaldo = true)
    {
        InitializeComponent();
        _soloConSaldo = soloConSaldo;
        DescriptionText.Text = soloConSaldo ? "Selecciona un folio con saldo pendiente." : "Selecciona el folio cuya actividad quieres facturar.";
        FilterBox.ItemsSource = new[] { "Todos", "Estancias activas", "Anticipos de reservas" };
        FilterBox.SelectedIndex = 0;
        Recargar();
    }
    private void Recargar() => FoliosGrid.ItemsSource = _payments.BuscarFolios(SearchBox.Text, FilterBox.SelectedItem?.ToString() ?? "Todos", _soloConSaldo);
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Folio_DoubleClick(object sender, MouseButtonEventArgs e) => Confirmar();
    private void Select_Click(object sender, RoutedEventArgs e) => Confirmar();
    private void Confirmar()
    {
        if (FoliosGrid.SelectedItem is not FolioSelectorRow folio) return;
        FolioIdSeleccionado = folio.FolioId;
        DialogResult = true;
    }
}
