using System.Windows;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Shared;

public partial class SelectorHuespedDialog : Window
{
    public ActiveGuestChoice? HuespedSeleccionado { get; private set; }
    public SelectorHuespedDialog() { InitializeComponent(); GuestSelector.HuespedSeleccionado += guest => { HuespedSeleccionado = guest; DialogResult = true; }; }
    private void Accept_Click(object sender, RoutedEventArgs e) { if (HuespedSeleccionado is not null) DialogResult = true; else MessageBox.Show("Selecciona y confirma un huésped primero."); }
}
