using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CheckOutDialog : Window
{
    private readonly int _stayId;
    private readonly EstanciaService _service = new();
    public CheckOutDialog(int stayId)
    {
        InitializeComponent(); _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var guest = SampleData.Huespedes.First(item => item.Id == stay.HuespedId);
        var room = SampleData.Habitaciones.First(item => item.Id == guest.Habitacion);
        GuestText.Text = guest.Nombre; RoomText.Text = $"Habitación {room.Numero} · {room.Tipo} · salida prevista {stay.CheckOut:dd/MM/yyyy}";
        BalanceText.Text = $"Bs {new FolioService().CalcularSaldoPorEstancia(stayId):N2}";
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try { _service.FinalizarPorEstancia(_stayId); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
