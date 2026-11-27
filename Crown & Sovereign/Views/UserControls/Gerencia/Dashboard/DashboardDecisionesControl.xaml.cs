using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardDecisionesControl : UserControl
{
    public event EventHandler? DecisionsRequested;
    public event EventHandler<DashboardDecision>? CorrectionReviewRequested;
    public DashboardDecisionesControl() => InitializeComponent();
    public void Cargar(IReadOnlyList<DashboardDecision> decisions)
    {
        DecisionList.ItemsSource = decisions.Take(5).ToList();
        EmptyText.Visibility = decisions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Open_Click(object sender, RoutedEventArgs e) => DecisionsRequested?.Invoke(this, EventArgs.Empty);
    private void Review_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not DashboardDecision item) return;
        CorrectionReviewRequested?.Invoke(this, item);
    }
}
