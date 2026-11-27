using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Reportes;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class ReportesControl : UserControl
{
    public ReportesControl()
    {
        InitializeComponent();
        Library.ReporteSeleccionado += definition => { Generator.Seleccionar(definition); ReportTabs.SelectedIndex = 1; };
        Generator.VolverBiblioteca += () => ReportTabs.SelectedIndex = 0;
        Generator.ReporteGenerado += (_, _) => Recent.Refrescar();
        Recent.ConsultarSolicitado += result => { Generator.AbrirResultado(result); ReportTabs.SelectedIndex = 1; };
        Recent.ReutilizarSolicitado += (result, parameters) => { Generator.Seleccionar(ReporteService.Catalogo.First(x => x.Id == result.Id), parameters); ReportTabs.SelectedIndex = 1; };
    }

    public void SeleccionarReporte(string name)
    {
        var id = name switch { "Ingresos y pagos" => "ingresos", "Ocupación" => "ocupacion", "Servicios" => "servicios", "Inventario" => "inventario", "Limpieza" => "limpieza", "Mantenimiento" => "mantenimiento", _ => "" };
        var definition = ReporteService.Catalogo.FirstOrDefault(x => x.Id == id || x.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (definition is null) return;
        Generator.Seleccionar(definition);
        ReportTabs.SelectedIndex = 1;
    }
}
