using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.UserControls.Shared.Navigation;
using Crown___Sovereign.Views.UserControls.Recepcion.Dashboard;
using Crown___Sovereign.Views.UserControls.Operaciones.Dashboard;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;
using Crown___Sovereign.Views.UserControls.Recepcion.Estancias;
using Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas;
using Crown___Sovereign.Views.Windows;

namespace Crown___Sovereign;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<OpcionNavegacion> _opciones;
    private HuespedesControl? _guestWorkspace;
    public MainWindow()
    {
        InitializeComponent();
        var usuario = SesionActual.Usuario ?? throw new InvalidOperationException("No hay una sesión autenticada.");
        _opciones = NavigationService.ParaRol(usuario.Rol);
        Sidebar.Configurar(usuario, _opciones);
        Sidebar.NavigationRequested += (_, modulo) => AbrirModulo(modulo);
        Sidebar.LogoutRequested += (_, _) => CerrarSesion();
        TopBar.NotificationsRequested += (_, _) => MessageBox.Show("• Tareas de limpieza prioritarias\n• Productos por reponer\n• Solicitudes de servicio por revisar", "Notificaciones");
        AbrirModulo(_opciones[0].Nombre);
    }

    private void AbrirModulo(string modulo, int? guestId = null)
    {
        var abrirFolio = modulo == "Folio";
        var destino = abrirFolio ? "Estancias" : modulo;
        if (!NavigationService.Puede(destino))
        {
            MessageBox.Show("Tu rol no tiene acceso a esta sección.", "Acceso restringido", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var opcion = _opciones.First(o => o.Nombre == destino);
        Sidebar.Seleccionar(destino);
        TopBar.Actualizar(opcion.Grupo, destino);
        var role = SesionActual.Usuario!.Rol;
        UserControl control = destino == "Huéspedes" && _guestWorkspace is not null ? _guestWorkspace : new ModuleViewFactory().Crear(destino, role);
        if (control is RecepcionDashboardControl reception) reception.NavigationRequested += (_, destino) => AbrirModulo(destino);
        if (control is OperacionesDashboardControl operations) operations.NavigationRequested += (_, destino) => AbrirModulo(destino);
        if (control is HabitacionesControl rooms) rooms.NavegacionSolicitada += (destino, selectedGuest) => AbrirModulo(destino, selectedGuest);
        if (control is HuespedesControl guests)
        {
            if (_guestWorkspace is null) { _guestWorkspace = guests; guests.NavegacionSolicitada += (target, selectedId) => AbrirModulo(target, selectedId); }
            if (guestId is int selectedGuest) guests.SeleccionarHuesped(selectedGuest);
        }
        if (control is ReservasControl reservations && guestId is int reservationId) reservations.AbrirReserva(reservationId);
        if (control is HabitacionesControl roomWorkspace && guestId is int selectedRoomId) roomWorkspace.AbrirHabitacion(selectedRoomId);
        if (control is EstanciasControl stays && guestId is int id) stays.AbrirHuesped(id, abrirFolio);
        MainContent.Content = control;
    }

    private void CerrarSesion()
    {
        SesionActual.Cerrar();
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
