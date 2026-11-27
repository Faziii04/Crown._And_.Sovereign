using System.Windows;

namespace Crown___Sovereign.Views.Dialogs.Gerencia;

public partial class MotivoRechazoFinancieroDialog : Window
{
    public string Motivo => ReasonBox.Text.Trim();
    public MotivoRechazoFinancieroDialog(string title, string summary)
    {
        InitializeComponent();
        Title = title;
        RequestText.Text = summary;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (Motivo.Length < 5) { ErrorText.Text = "Escribe un motivo de al menos cinco caracteres."; return; }
        DialogResult = true;
    }
}
