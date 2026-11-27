using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class RecursosEstanciaControl : UserControl
{
    private int _stayId;
    public event EventHandler? DatosActualizados;
    public RecursosEstanciaControl() => InitializeComponent();
    public void SetReadOnly(bool readOnly) => RequestButton.IsEnabled = !readOnly;
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var service = new RecursoAdicionalService();
        var rows = service.ObtenerRecursosReserva(stay.ReservaId).Select(item =>
        {
            var resource = SampleData.RecursosAdicionales.FirstOrDefault(entry => entry.RecursoId == item.RecursoId);
            var charge = item.CargoId is int cargoId ? SampleData.Cargos.FirstOrDefault(entry => entry.Id == cargoId)?.Monto
                : item.PrecioUnitarioAcordado is decimal price && item.TipoCobroAplicado is string type
                    ? service.CalcularImporte(price, type, item.Cantidad, Math.Max(0, (SampleData.Reservas.First(reservation => reservation.Id == stay.ReservaId).Salida.Date - SampleData.Reservas.First(reservation => reservation.Id == stay.ReservaId).Llegada.Date).Days)) : 0m;
            return new StayResourceRow(item.RecursoId, resource?.Nombre ?? $"Recurso #{item.RecursoId}", item.Cantidad,
                item.TipoCobroAplicado switch { "POR_NOCHE" => "Por noche", "POR_ESTANCIA" => "Por estancia", _ => "Sin verificar" },
                charge ?? 0m, Etiqueta(item.Estado), item.Observaciones, item.CargoId, item.Estado);
        }).ToList();
        ResourceGrid.ItemsSource = rows;
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Metrics.ItemsSource = new[]
        {
            new { Title = "Unidades solicitadas", Value = rows.Where(item => item.EstadoRaw != "CANCELADO").Sum(item => item.Cantidad).ToString() },
            new { Title = "Entregadas", Value = rows.Where(item => item.EstadoRaw == "ENTREGADO").Sum(item => item.Cantidad).ToString() },
            new { Title = "Pendientes", Value = rows.Where(item => item.EstadoRaw is "SIN_VERIFICAR" or "PENDIENTE" or "ASIGNADO").Sum(item => item.Cantidad).ToString() },
            new { Title = "Por devolver", Value = rows.Where(item => item.EstadoRaw == "ENTREGADO").Sum(item => item.Cantidad).ToString() }
        };
        RequestButton.IsEnabled = stay.Estado == "Activa";
    }
    private static string Etiqueta(string estado) => estado switch
    {
        "SIN_VERIFICAR" => "Requiere revisión", "PENDIENTE" => "Pendiente de preparación", "ASIGNADO" => "Asignado",
        "ENTREGADO" => "Entregado", "DEVUELTO" => "Devuelto", "CANCELADO" => "Cancelado", _ => estado
    };
    private void Resource_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void Request_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SolicitudRecursoEstanciaDialog(_stayId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); }
    }
}
