using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Detalle;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Disponibilidad;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Calendario;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Listado;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Grupos;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas.ListaEspera;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas;

public partial class ReservasControl : UserControl
{
    private readonly Dictionary<string, UserControl> _views;
    private string _activeView = "Disponibilidad";
    public ReservasControl()
    {
        InitializeComponent();
        var availability=new DisponibilidadControl();
        var calendar=new CalendarioReservasControl();
        var list=new ListadoReservasControl();
        var groups=new GruposReservaControl();
        var waitlist=new ListaEsperaControl();
        _views=new(){{"Disponibilidad",availability},{"Calendario",calendar},{"Listado",list},{"Grupos",groups},{"Lista de espera",waitlist}};
        availability.BorradorReservaSolicitado += draft => AbrirReserva(draft.Llegada,draft.TipoPreferido,borrador:draft);
        calendar.NuevaReservaSolicitada += () => AbrirReserva();
        calendar.ReservaSeleccionada += AbrirDetalle;
        calendar.HabitacionSeleccionada += (roomId,date) => AbrirReserva(date,SampleData.Habitaciones.First(room=>room.Id==roomId).Tipo,roomId);
        list.NuevaReservaSolicitada += () => AbrirReserva();
        list.ReservaSeleccionada += AbrirDetalle;
        list.DatosActualizados += Recargar;
        waitlist.BorradorReservaSolicitado += draft => AbrirReserva(draft.Llegada, draft.TipoPreferido, borrador: draft);
        groups.BorradorReservaSolicitado += draft => AbrirReserva(draft.Llegada, borrador:draft);
        SummaryView.NavegacionSolicitada += ActivarAtajo;
        WorkspaceHost.Content=availability;
        ActualizarNavegacion();
    }
    private void Navigation_Click(object sender,RoutedEventArgs e)
    {
        if((sender as Button)?.Tag is not string view||!_views.TryGetValue(view,out var control))return;
        _activeView=view;WorkspaceHost.Content=control;SummaryView.Recargar();ActualizarNavegacion();
    }
    private void ActualizarNavegacion()
    {
        foreach(var button in new[]{AvailabilityTab,CalendarTab,ListTab,GroupsTab,WaitlistTab})
        {
            var selected=(string)button.Tag==_activeView;
            button.Background=selected?new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E9EFF5")):System.Windows.Media.Brushes.Transparent;
            button.Foreground=selected?(System.Windows.Media.Brush)FindResource("Primary"):(System.Windows.Media.Brush)FindResource("Muted");
            button.FontWeight=selected?FontWeights.SemiBold:FontWeights.Normal;
            button.BorderBrush=selected?(System.Windows.Media.Brush)FindResource("Primary"):System.Windows.Media.Brushes.Transparent;
            button.BorderThickness=new Thickness(0,0,0,selected?2:0);
        }
    }
    private void ActivarAtajo(string shortcut)
    {
        if(shortcut=="Llegadas"){AbrirVista("Listado");((ListadoReservasControl)_views["Listado"]).AplicarFiltroRapido("Llegadas de hoy");}
        else if(shortcut=="Salidas"){AbrirVista("Listado");((ListadoReservasControl)_views["Listado"]).AplicarFiltroRapido("Salidas de hoy");}
        else if(shortcut=="Disponibilidad"){AbrirVista("Disponibilidad");((DisponibilidadControl)_views["Disponibilidad"]).BuscarUnaNoche(DateTime.Today);}
        else if(shortcut=="Espera"){AbrirVista("Lista de espera");((ListaEsperaControl)_views["Lista de espera"]).MostrarCoincidencias();}
    }
    private void AbrirVista(string view){_activeView=view;WorkspaceHost.Content=_views[view];ActualizarNavegacion();}
    private void NewReservation_Click(object sender,RoutedEventArgs e)=>AbrirReserva();
    private void AbrirReserva(DateTime? llegada=null,string? tipo=null,int? habitacionId=null,BorradorReserva? borrador=null)
    {
        var dialog=new ReservaDialog(llegada,tipo,habitacionId:habitacionId,borrador:borrador){Owner=Window.GetWindow(this)};
        if(dialog.ShowDialog()==true)Recargar();
    }
    private void AbrirDetalle(int id)
    {
        var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==id);if(reservation is null)return;
        var sourceView = _activeView;
        var detail = new DetalleReservaControl(); detail.Cargar(id);
        detail.VolverSolicitado += () =>
        {
            SummaryView.Visibility=Visibility.Visible; NavigationBar.Visibility=Visibility.Visible; NewReservationButton.Visibility=Visibility.Visible;
            HeaderTitle.Text="Reservas"; HeaderSubtitle.Text="Consulta disponibilidad y gestiona las reservas del hotel.";
            _activeView=sourceView; WorkspaceHost.Content=_views[sourceView]; ActualizarNavegacion(); Recargar();
        };
        detail.DatosActualizados += Recargar;
        SummaryView.Visibility=Visibility.Collapsed; NavigationBar.Visibility=Visibility.Collapsed; NewReservationButton.Visibility=Visibility.Collapsed;
        HeaderTitle.Text=$"Reserva #{reservation.Id}"; HeaderSubtitle.Text=$"{reservation.Huesped} · {reservation.Llegada:dd/MM/yyyy} – {reservation.Salida:dd/MM/yyyy}";
        WorkspaceHost.Content=detail;
    }
    public void AbrirReserva(int id) { AbrirVista("Listado"); AbrirDetalle(id); }
    private void Recargar()
    {
        if(_views["Disponibilidad"] is DisponibilidadControl availability)availability.Recargar();
        if(_views["Calendario"] is CalendarioReservasControl calendar)calendar.Recargar();
        if(_views["Listado"] is ListadoReservasControl list)list.Recargar();
        if(_views["Grupos"] is GruposReservaControl groups)groups.Recargar();
        if(_views["Lista de espera"] is ListaEsperaControl waitlist)waitlist.Recargar();
        SummaryView.Recargar();
    }
}
