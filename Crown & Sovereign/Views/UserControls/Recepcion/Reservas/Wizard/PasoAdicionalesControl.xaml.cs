using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard.Adicionales;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard;

public partial class PasoAdicionalesControl : UserControl
{
    public ComboBox Paquete => PackageSection.Paquete;
    public ComboBox CantidadPaquete => PackageSection.Cantidad;
    public ComboBox Promocion => PackageSection.Promocion;
    public void MostrarPrecioPaquete(decimal? price) => PackageSection.MostrarPrecioAcordado(price);
    public TextBox Solicitud => SpecialSection.Solicitud;
    public RecursosAdicionalesReservaControl Recursos => ResourcesSection;
    public ServiciosAdicionalesReservaControl ServiciosAdquiridos => ServicesSection;

    public PasoAdicionalesControl()
    {
        InitializeComponent();
        PackageSection.SeleccionCambiada += (_, _) => { };
    }

    public void Configurar(BorradorReserva draft, IEnumerable<HuespedModel> guests, DateTime arrival, DateTime departure, int? reservationId)
    {
        Recursos.Configurar(draft, arrival, departure, reservationId);
        ServiciosAdquiridos.Configurar(draft, guests);
    }
}
