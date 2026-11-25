using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard;

public partial class PasoHuespedesControl : UserControl
{
    private List<HuespedModel> _guests = [];
    public ComboBox HuespedPrincipal => GuestPicker;
    public ListBox Acompanantes => CompanionsList;
    public PasoHuespedesControl() => InitializeComponent();

    public void ConfigurarHuespedes(IEnumerable<HuespedModel> guests)
    {
        _guests = guests.ToList();
        AplicarBusqueda();
    }

    private void GuestSearchChanged(object sender, TextChangedEventArgs e) => AplicarBusqueda();
    public void RefrescarListadoAcompanantes() => AplicarBusqueda();

    private void AplicarBusqueda()
    {
        if (GuestPicker is null) return;
        var query = GuestSearchBox.Text.Trim();
        var currentPrincipal = GuestPicker.SelectedItem as HuespedModel;
        var currentCompanions = CompanionsList.SelectedItems.Cast<HuespedModel>().Select(item => item.Id).ToHashSet();
        var matches = _guests.Where(guest => string.IsNullOrWhiteSpace(query) ||
            $"{guest.Nombre} {guest.Documento} {guest.Email} {guest.Telefono}".Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        GuestPicker.ItemsSource = matches;
        if (currentPrincipal is not null && matches.Any(item => item.Id == currentPrincipal.Id)) GuestPicker.SelectedItem = currentPrincipal;
        CompanionsList.ItemsSource = matches.Where(item => item.Id != (currentPrincipal?.Id ?? 0)).ToList();
        foreach (var guest in CompanionsList.Items.Cast<HuespedModel>().Where(item => currentCompanions.Contains(item.Id))) CompanionsList.SelectedItems.Add(guest);
    }
}
