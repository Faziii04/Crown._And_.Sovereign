using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Windows;

public partial class NotificacionesWindow : Window
{
    private readonly NotificacionService _service = new();
    public event EventHandler<NotificacionModel>? NavigationRequested;
    public NotificacionesWindow()
    {
        InitializeComponent();
        var role = SesionActual.Usuario?.Rol ?? "";
        var notifications = _service.Listar(role);
        NotificationList.ItemsSource = notifications;
        SummaryText.Text = $"{notifications.Count(item => !item.Leida)} sin leer · {notifications.Count} en total";
    }
    private void NotificationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotificationList.SelectedItem is not NotificacionModel item) return;
        _service.MarcarLeida(item.Id);
        if (!string.IsNullOrWhiteSpace(item.ModuloDestino) && NavigationService.Puede(item.ModuloDestino))
        {
            NavigationRequested?.Invoke(this, item);
            DialogResult = true;
            Close();
        }
        else
        {
            SummaryText.Text = "Notificación marcada como leída. No hay una sección asociada para este rol.";
            NotificationList.Items.Refresh();
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
