using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AgendaServicios;

public partial class AgendaServiciosControl : UserControl
{
    public AgendaServiciosControl()
    {
        InitializeComponent(); TodayView.AgendaActualizada += Recargar; CalendarView.AgendaActualizada += Recargar; RequestsView.SolicitudActualizada += Recargar;
    }
    private void Recargar() { TodayView.Recargar(); CalendarView.Recargar(); RequestsView.Recargar(); }
    private void NewAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (new NuevaCitaDialog() { Owner = Window.GetWindow(this) }.ShowDialog() == true) Recargar();
    }
}
