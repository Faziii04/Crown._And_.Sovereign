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
using Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;
using Crown___Sovereign.Views.UserControls.Gerencia.Supervision;
using Crown___Sovereign.Views.Windows;

namespace Crown___Sovereign;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<OpcionNavegacion> _opciones;
    private HuespedesControl? _guestWorkspace;
    public MainWindow()
    {
        InitializeComponent();
        // Keep the restored window usable on compact laptop displays and high-DPI work areas.
        var workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(900, workArea.Width);
        MinHeight = Math.Min(560, workArea.Height);
        Width = Math.Min(1200, workArea.Width * 0.92);
        Height = Math.Min(780, workArea.Height * 0.92);
        SizeChanged += (_, _) => Sidebar.Width = ActualWidth < 1100 ? 208 : 236;
        Sidebar.Width = ActualWidth < 1100 ? 208 : 236;
        var usuario = SesionActual.Usuario ?? throw new InvalidOperationException("No hay una sesión autenticada.");
        _opciones = NavigationService.ParaRol(usuario.Rol);
        Sidebar.Configurar(usuario, _opciones);
        Sidebar.NavigationRequested += (_, modulo) => AbrirModulo(modulo);
        Sidebar.LogoutRequested += (_, _) => CerrarSesion();
        TopBar.NotificationsRequested += (_, _) =>
        {
            var notifications = new NotificacionesWindow { Owner = this };
            notifications.NavigationRequested += (_, notification) =>
            {
                AbrirModulo(notification.ModuloDestino);
                if (notification.ModuloDestino == "Finanzas" && notification.TipoRegistroDestino is "Devolución" or "Anulación"
                    && notification.RegistroDestinoId is int correctionId && MainContent.Content is FinanzasControl finances)
                    finances.AbrirCorreccion(notification.TipoRegistroDestino, correctionId);
            };
            notifications.ShowDialog();
        };
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
        if (control is Crown___Sovereign.Views.UserControls.Gerencia.Dashboard.GerenciaDashboardControl manager) manager.NavigationRequested += (_, destino) =>
        {
            if (destino.StartsWith("Operaciones|", StringComparison.Ordinal))
            {
                AbrirModulo("Operaciones");
                if (MainContent.Content is Crown___Sovereign.Views.UserControls.Gerencia.Supervision.OperacionesControl operationsWorkspace)
                    operationsWorkspace.AbrirSeccion(destino[("Operaciones|".Length)..]);
                return;
            }
            AbrirModulo(destino);
            if (destino == "Convenios" && MainContent.Content is Crown___Sovereign.Views.UserControls.Gerencia.Comercial.ConveniosControl agreements)
                agreements.AbrirVencimientos("Próximos");
        };
        if (control is Crown___Sovereign.Views.UserControls.Gerencia.Dashboard.GerenciaDashboardControl managerDashboard)
            managerDashboard.CorrectionReviewRequested += (_, correction) =>
            {
                AbrirModulo("Finanzas");
                if (MainContent.Content is FinanzasControl financeView) financeView.AbrirCorreccion(correction.Tipo, correction.Id);
            };
        if (control is FinanzasControl finance) finance.NavigationRequested += (_, destino) =>
        {
            AbrirModulo(destino);
            if (MainContent.Content is Crown___Sovereign.Views.UserControls.Gerencia.Supervision.ReportesControl reports)
                reports.SeleccionarReporte("Ingresos y pagos");
        };
        if (control is PoliticasHotelControl policies) policies.NavigationRequested += destino => AbrirModulo(destino);
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
