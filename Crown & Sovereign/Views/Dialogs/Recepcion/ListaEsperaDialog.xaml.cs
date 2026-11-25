using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class ListaEsperaDialog : Window
{
    private readonly ListaEsperaService _service = new();
    public ListaEsperaDialog()
    {
        InitializeComponent(); GuestBox.ItemsSource = SampleData.Huespedes; GuestBox.SelectedIndex = 0;
        TypeBox.ItemsSource = SampleData.Tipos; TypeBox.SelectedIndex = 0;
        PeopleBox.ItemsSource = Enumerable.Range(1, 16); PeopleBox.SelectedIndex = 1;
        RoomsBox.ItemsSource = Enumerable.Range(1, 8); RoomsBox.SelectedIndex = 0;
        ArrivalPicker.SelectedDate = DateTime.Today.AddDays(1); DeparturePicker.SelectedDate = DateTime.Today.AddDays(2);
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (GuestBox.SelectedItem is not HuespedModel guest || PeopleBox.SelectedItem is not int people || RoomsBox.SelectedItem is not int rooms)
                throw new InvalidOperationException("Selecciona un huésped y las cantidades solicitadas.");
            _service.Registrar(guest.Nombre, guest.Id, ArrivalPicker.SelectedDate ?? DateTime.MinValue, DeparturePicker.SelectedDate ?? DateTime.MinValue, TypeBox.SelectedItem?.ToString() ?? "", people, rooms, NotesBox.Text);
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
