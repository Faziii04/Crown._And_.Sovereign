using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class ResumenPerfilHuespedControl:UserControl
{
 private readonly HuespedService _service=new(); public ResumenPerfilHuespedControl()=>InitializeComponent();
 public void Mostrar(HuespedModel guest){var summary=_service.ObtenerResumenPerfil(guest.Id);CompletedCount.Text=summary.EstanciasCompletadas.ToString();NightsCount.Text=summary.NochesCompletadas.ToString();ReservationsCount.Text=summary.Reservas.ToString();LastVisit.Text=summary.UltimaVisita?.ToString("dd/MM/yyyy")??"Sin visitas";SituationText.Text=summary.EstanciaActual is { } stay?$"Actualmente alojado · {stay.Relacion} · Reserva #{stay.ReservaId} · {string.Join(", ",stay.Habitaciones.Select(room=>$"habitación {room.Numero}, piso {room.Piso}"))} · {stay.CheckIn:dd/MM}–{stay.CheckOut:dd/MM}":summary.ProximaReserva is { } next?$"Próxima reserva #{next.Id} · {next.Llegada:dd/MM/yyyy}–{next.Salida:dd/MM/yyyy} · {next.Estado}":"No tiene un alojamiento activo o una próxima reserva.";AttentionList.ItemsSource=summary.Atenciones.Count==0?new[]{"No hay asuntos pendientes identificados."}:summary.Atenciones;ActivityList.ItemsSource=summary.ActividadReciente;}
}
