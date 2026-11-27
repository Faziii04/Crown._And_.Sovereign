using System.Windows;
using System.Windows.Controls;

namespace Crown___Sovereign.Views.UserControls.Shared.Cards;

public partial class ConvenioCardControl : UserControl
{
    public event EventHandler<int>? DetailRequested;
    public event EventHandler<int>? EditRequested;
    public ConvenioCardControl() => InitializeComponent();
    private void Detail_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) DetailRequested?.Invoke(this, id); }
    private void Edit_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) EditRequested?.Invoke(this, id); }
}
