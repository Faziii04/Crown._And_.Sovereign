using System.Windows.Controls;
namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;
public partial class FinanzasControl : UserControl
{
    public FinanzasControl() => InitializeComponent();
    public void AbrirCorreccion(string tipo, int id)
    {
        FinanceTabs.SelectedIndex = 3;
        CorrectionsControl.MostrarCorreccion(tipo, id);
    }
}
