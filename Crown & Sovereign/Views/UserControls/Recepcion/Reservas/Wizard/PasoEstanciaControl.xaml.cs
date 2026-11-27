using System.Windows.Controls;
using Crown___Sovereign.Data;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard;

public partial class PasoEstanciaControl : UserControl
{
    public DatePicker Arrival => ArrivalPicker;
    public DatePicker Departure => DeparturePicker;
    public ComboBox Channel => ChannelPicker;
    public TextBlock Duration => StayDuration;
    public TextBox GuestCount => GuestCountBox;
    public PasoEstanciaControl() { InitializeComponent(); ChannelPicker.ItemsSource = SampleData.CanalesReserva.Select(item=>item.Nombre).ToList(); ChannelPicker.SelectedIndex = 0; }
}
