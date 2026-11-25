using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class EstadosHabitacionControl : UserControl
{
    public EstadosHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        OccupancyText.Text = room.Ocupada ? "Ocupada" : "Desocupada"; GuestText.Text = room.Ocupada ? $"{room.Huespedes.Count} huésped(es) · {room.ReservaActual?.Salida:dd MMM}" : "Sin huéspedes alojados";
        CleaningText.Text = room.EstadoLimpieza; InspectionText.Text = room.UltimaInspeccion is null ? "" : room.UltimaInspeccion.Aprobada ? "Última inspección aprobada" : "Última inspección no aprobada";
        MaintenanceText.Text = room.EstadoMantenimiento; IncidentText.Text = room.Incidencias.FirstOrDefault()?.Descripcion ?? "Sin incidencias abiertas";
    }
}
