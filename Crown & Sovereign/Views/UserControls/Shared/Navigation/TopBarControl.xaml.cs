using System.Windows;
using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Shared.Navigation;

public partial class TopBarControl : UserControl
{
    public event EventHandler? NotificationsRequested;
    public TopBarControl()
    {
        InitializeComponent();
        TodayLabel.Text = DateTime.Today.ToString("dddd, d 'de' MMMM", new System.Globalization.CultureInfo("es-BO"));
    }
    public void Actualizar(string grupo, string modulo)
    {
        Breadcrumb.Text = grupo.ToUpperInvariant();
        PageTitle.Text = modulo;
    }
    private void Notifications_Click(object sender, RoutedEventArgs e) => NotificationsRequested?.Invoke(this, EventArgs.Empty);
}
