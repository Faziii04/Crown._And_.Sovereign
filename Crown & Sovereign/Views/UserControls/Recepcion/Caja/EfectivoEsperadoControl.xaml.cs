using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class EfectivoEsperadoControl : UserControl
{
    private CashDeskShiftSummary? _summary;
    public EfectivoEsperadoControl() => InitializeComponent();
    public void Mostrar(CashDeskShiftSummary summary)
    {
        _summary = summary;
        ExpectedText.Text = $"Bs {summary.EfectivoEsperado:N2}";
        OpeningText.Text = $"Fondo de apertura     Bs {summary.Turno.MontoInicial:N2}";
        CashInText.Text = $"Cobros en efectivo   +Bs {summary.EfectivoCobros:N2}";
        CashOutText.Text = $"Devoluciones efectivo  −Bs {summary.EfectivoDevoluciones:N2}";
        FormulaText.Text = $"{summary.Turno.MontoInicial:N2} + {summary.EfectivoCobros:N2} − {summary.EfectivoDevoluciones:N2} = Bs {summary.EfectivoEsperado:N2}";
    }
    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (_summary is not null) new DesgloseEfectivoDialog(_summary) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
