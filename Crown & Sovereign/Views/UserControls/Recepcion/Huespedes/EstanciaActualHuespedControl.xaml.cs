using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class EstanciaActualHuespedControl:UserControl
{
 private readonly HuespedService _service=new(); public EstanciaActualHuespedControl()=>InitializeComponent();
 public void Mostrar(HuespedModel guest){var stay=_service.ObtenerEstanciaActual(guest.Id);CurrentStayText.Text=stay is null?"El huésped no tiene una estancia activa.":$"Estancia #{stay.EstanciaId} · Reserva #{stay.ReservaId}\n{stay.Relacion} · Entrada {stay.CheckIn:dd/MM/yyyy} · Salida {stay.CheckOut:dd/MM/yyyy}\nPaquete: {stay.Paquete}";RoomsList.ItemsSource=stay?.Habitaciones.Select(room=>$"Habitación {room.Numero} · Piso {room.Piso} · {room.Tipo}").ToList()??[];CompanionsList.ItemsSource=stay?.Huespedes.Select(person=>$"{person.Nombre}{(person.Id==guest.Id?" (seleccionado)":"")}").ToList()??[];BenefitsGrid.ItemsSource=_service.ObtenerBeneficios(guest.Id);var finance=_service.ObtenerResumenFinanciero(guest.Id);FinancialText.Text=$"Cargos Bs {finance.Cargos:N2} · Pagos Bs {finance.Pagos:N2} · Saldo individual registrado Bs {finance.Saldo:N2}";FinancialNote.Text=finance.Nota;}
}
