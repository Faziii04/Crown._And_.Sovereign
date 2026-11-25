using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard;

public partial class PasoAdicionalesControl : UserControl
{
    public ComboBox Paquete => PackagePicker;
    public ComboBox Promocion => PromotionPicker;
    public ListBox Servicios => ServicesList;
    public TextBox Solicitud => SpecialRequest;
    public PasoAdicionalesControl() => InitializeComponent();
}
