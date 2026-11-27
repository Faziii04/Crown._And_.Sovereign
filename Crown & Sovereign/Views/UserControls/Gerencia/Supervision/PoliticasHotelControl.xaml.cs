using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class PoliticasHotelControl : UserControl
{
    public event Action<string>? NavigationRequested;
    public PoliticasHotelControl()
    {
        InitializeComponent(); RefreshDiagnostics();
        Payments.ConsultarFinanzas += () => NavigationRequested?.Invoke("Finanzas");
        Accommodation.ConfigurationChanged += RefreshDiagnostics;
        Reservations.ConfigurationChanged += RefreshDiagnostics;
        Payments.ConfigurationChanged += RefreshDiagnostics;
        Services.ConfigurationChanged += RefreshDiagnostics;
    }
    private void RefreshDiagnostics()
    {
        var warnings = new PoliticaHotelService().Diagnosticos();
        DiagnosticsText.Text = warnings.Count == 0 ? "Configuración revisada · sin inconsistencias detectadas." : string.Join("   •   ", warnings.Take(5));
        DiagnosticsText.ToolTip = warnings.Count > 5 ? string.Join(Environment.NewLine, warnings) : null;
    }
}
