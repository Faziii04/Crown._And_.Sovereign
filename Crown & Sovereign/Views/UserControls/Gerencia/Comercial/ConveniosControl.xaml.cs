using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Convenios;
using Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class ConveniosControl : UserControl
{
    private readonly ConvenioService _service = new();
    private ConvenioDetalleControl? _detail;
    public ConveniosControl()
    {
        InitializeComponent();
        ActualizarResumen();
    }

    public void AbrirVencimientos(string? estado = "Próximos")
    {
        WorkspaceTabs.SelectedIndex = 2;
        if (string.IsNullOrWhiteSpace(estado)) ExpirationsView.FijarFiltro(null);
        else ExpirationsView.FijarFiltro(estado);
    }

    private void ActualizarResumen()
    {
        var metrics = _service.Indicadores();
        ActiveCount.Text = metrics.Vigentes.ToString(); ExternalServicesCount.Text = metrics.ServiciosExternos.ToString();
        ExpiringCount.Text = metrics.ProximosVencimientos.ToString(); ReviewCount.Text = metrics.RequierenRevision.ToString();
        ProvidersView?.Recargar(); ExpirationsView?.Recargar();
    }

    private void New_Click(object sender, RoutedEventArgs e) => AbrirDialogo(null);
    private void EditRequested(object sender, int id) => AbrirDialogo(SampleData.Convenios.FirstOrDefault(item => item.Id == id));
    private void RenewRequested(object sender, int id) => AbrirRenovacion(id);
    private void DetailRequested(object sender, int id)
    {
        var agreement = SampleData.Convenios.FirstOrDefault(item => item.Id == id);
        if (agreement is null) return;
        _detail = new ConvenioDetalleControl(id);
        _detail.BackRequested += (_, _) => MostrarDirectorio();
        _detail.EditRequested += (_, agreementId) => AbrirDialogo(SampleData.Convenios.FirstOrDefault(item => item.Id == agreementId));
        _detail.RenewRequested += (_, agreementId) => AbrirRenovacion(agreementId);
        WorkspaceTabs.Visibility = Visibility.Collapsed; DetailHost.Content = _detail; DetailHost.Visibility = Visibility.Visible;
    }
    private void MostrarDirectorio()
    {
        DetailHost.Content = null; DetailHost.Visibility = Visibility.Collapsed; WorkspaceTabs.Visibility = Visibility.Visible;
        ActualizarResumen();
    }
    private void CatalogChanged(object sender, EventArgs e) => ActualizarResumen();

    private void AbrirDialogo(ConvenioModel? agreement)
    {
        var dialog = new ConvenioDialog(agreement) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { ActualizarResumen(); _detail?.Cargar(); }
    }

    private void AbrirRenovacion(int id)
    {
        var original = SampleData.Convenios.FirstOrDefault(item => item.Id == id); if (original is null) return;
        var start = original.VigenciaHasta.Date >= DateTime.Today ? original.VigenciaHasta.Date.AddDays(1) : DateTime.Today;
        var end = start.AddYears(1);
        try
        {
            var draft = _service.Renovar(id, start, end);
            var dialog = new ConvenioDialog(draft, id) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) { ActualizarResumen(); _detail?.Cargar(); }
        }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo iniciar la renovación", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Metric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string metric) return;
        switch (metric)
        {
            case "vigentes": WorkspaceTabs.SelectedIndex = 0; ProvidersView.FijarFiltro("Vigente"); break;
            case "servicios": WorkspaceTabs.SelectedIndex = 1; break;
            case "vencimientos": AbrirVencimientos("Próximos"); break;
            case "revision": WorkspaceTabs.SelectedIndex = 0; ProvidersView.FijarRevision(); break;
        }
    }
}
