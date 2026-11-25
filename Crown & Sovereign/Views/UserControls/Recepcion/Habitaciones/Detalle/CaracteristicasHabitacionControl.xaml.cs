using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class CaracteristicasHabitacionControl : UserControl
{
    public CaracteristicasHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        var value = room.Habitacion; TitleText.Text = $"Características de la habitación {value.Numero}";
        GeneralText.Text = $"Número: {value.Numero}\nPiso: {value.Piso}\nTipo: {value.Tipo}\nCapacidad: {value.Capacidad} personas";
        AmenitiesList.ItemsSource = value.Comodidades.Split(['·', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        DescriptionText.Text = $"Habitación {value.Tipo.ToLowerInvariant()} en el piso {value.Piso}, con capacidad máxima para {value.Capacidad} personas.";
    }
}
