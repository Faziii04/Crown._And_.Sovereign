using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

public partial class DetalleIncidenciaDialog : Window
{
    public DetalleIncidenciaDialog(IncidenciaModel incident)
    {
        InitializeComponent(); var room = SampleData.Habitaciones.First(item => item.Id == incident.Habitacion);
        RoomText.Text = $"Habitación {room.Numero} · {room.Tipo} · Piso {room.Piso}"; DescriptionText.Text = incident.Descripcion;
        DetailsText.Text = $"Categoría: {incident.Categoria}\nPrioridad: {incident.Prioridad}\nReportada: {incident.FechaReporte:dd/MM/yyyy HH:mm}\nEstado de seguimiento: {incident.Estado}\nResponsable: {(string.IsNullOrWhiteSpace(incident.ReportadaPor) ? "No registrado" : incident.ReportadaPor)}";
        ImpactText.Text = incident.BloqueaHabitacion ? "Bloqueo formal activo" : incident.RequiereRevisionOperativa ? $"Revisión operativa pendiente · {incident.ImpactoObservado}" : "No bloquea la habitación";
        NotesText.Text = string.IsNullOrWhiteSpace(incident.Observaciones) ? "Sin observaciones adicionales." : incident.Observaciones;
    }
}
