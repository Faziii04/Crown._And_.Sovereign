using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.ListaEspera;

public partial class ListaEsperaControl : UserControl
{
    private readonly ListaEsperaService _service = new();
    public event Action<BorradorReserva>? BorradorReservaSolicitado;
    public ListaEsperaControl() { InitializeComponent(); Recargar(); }
    public void Recargar() { WaitlistGrid.ItemsSource = _service.BuscarCoincidencias(); }
    public void MostrarCoincidencias() { Recargar(); }
    private void AddRequest_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ListaEsperaDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Recargar();
    }
    private void Contact_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CoincidenciaEsperaModel match) return;
        var dialog = new ContactarEsperaDialog(match) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        _service.RegistrarContacto(match.Solicitud.Id, dialog.Medio, dialog.Resultado, dialog.Observaciones); Recargar();
    }
    private void Convert_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CoincidenciaEsperaModel match) return;
        if (match.HabitacionesCoincidentes < match.Solicitud.Habitaciones)
        { MessageBox.Show(Window.GetWindow(this), "Todavía no hay suficientes habitaciones disponibles para toda la solicitud.", "Disponibilidad insuficiente", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var draft = new BorradorReserva { Llegada = match.Solicitud.Llegada, Salida = match.Solicitud.Salida, Personas = match.Solicitud.Personas, HabitacionesRequeridas = match.Solicitud.Habitaciones, TipoPreferido = match.Solicitud.TipoHabitacion, HuespedPrincipalId = match.Solicitud.HuespedId, ListaEsperaId = match.Solicitud.Id };
        if (draft.HuespedPrincipalId is null) draft.HuespedPrincipalId = SampleData.Huespedes.FirstOrDefault(item => item.Nombre == match.Solicitud.Huesped)?.Id;
        foreach (var room in match.Habitaciones.Take(draft.HabitacionesRequeridas)) draft.HabitacionesSeleccionadas.Add(room.Id);
        BorradorReservaSolicitado?.Invoke(draft);
    }
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CoincidenciaEsperaModel match) return;
        if (MessageBox.Show(Window.GetWindow(this), $"¿Cerrar la solicitud de {match.Huesped}?", "Cerrar solicitud", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _service.Cerrar(match.Solicitud.Id); Recargar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo cerrar la solicitud", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
