using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class BeneficiosServicioDialog : Window
{
    public BeneficiosServicioDialog(ActiveGuestChoice guest)
    {
        InitializeComponent();GuestText.Text=$"{guest.Name} · Hab. {guest.Room} · Estancia #{guest.EstanciaId} · Reserva #{guest.ReservaId}";
        BenefitsGrid.ItemsSource=SampleData.Servicios.Where(item=>item.Activo&&item.Reservable).Select(service=>new { Service=service.Nombre,Benefit=new ServicioBeneficioService().Evaluar(guest.EstanciaId,service.Id) }).ToList();
    }
}
