using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

public partial class RendimientoControl : UserControl
{
    private readonly ConvenioService _service = new();
    public RendimientoControl()
    {
        InitializeComponent(); StartDate.SelectedDate = DateTime.Today.AddDays(-30); EndDate.SelectedDate = DateTime.Today;
        ProviderFilter.ItemsSource = new[] { new ConvenioProviderOption(null, "Todos los proveedores") }.Concat(SampleData.Convenios.Select(item => new ConvenioProviderOption(item.Id, $"{item.Empresa} · {item.Nombre}"))).ToList();
        ProviderFilter.DisplayMemberPath = nameof(ConvenioProviderOption.Label); ProviderFilter.SelectedValuePath = nameof(ConvenioProviderOption.Id); ProviderFilter.SelectedIndex = 0;
        Recargar();
    }
    private void Recargar()
    {
        if (StartDate.SelectedDate is not DateTime from || EndDate.SelectedDate is not DateTime to || to.Date < from.Date)
        { UsageGrid.ItemsSource = null; EmptyState.Text = "Selecciona un periodo válido."; EmptyState.Visibility = Visibility.Visible; UsageCount.Text = "—"; PriceTotal.Text = "—"; CostTotal.Text = "—"; MarginTotal.Text = "—"; PendingText.Text = "—"; CoverageCostText.Text = "—"; return; }
        var providerId = ProviderFilter.SelectedValue is int id ? id : (int?)null;
        var rows = _service.Rendimiento(from, to, providerId);
        UsageGrid.ItemsSource = rows;
        PendingText.Text = $"{_service.SolicitudesExternasPendientes(from, to, providerId)} · aún sin ejecución";
        CoverageCostText.Text = $"Adicional: {CostoPorCobertura(rows, "Adicional")} · Adquirido: {CostoPorCobertura(rows, "Adquirido previamente")} · Paquete: {CostoPorCobertura(rows, "Incluido en paquete")}";
        EmptyState.Text = "Todavía no se han registrado utilizaciones mediante convenios en este periodo.";
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UsageCount.Text = rows.Count.ToString();
        var priceRows = rows.Where(item => item.PrecioAplicado.HasValue).ToList();
        var costRows = rows.Where(item => item.CostoProveedorAplicado.HasValue).ToList();
        var marginRows = rows.Where(item => item.MargenEstimado.HasValue).ToList();
        PriceTotal.Text = priceRows.Count == 0 ? "No registrado" : $"{(priceRows.Count < rows.Count ? "Parcial · " : "")}Bs {priceRows.Sum(item => item.PrecioAplicado!.Value):N2}";
        CostTotal.Text = costRows.Count == 0 ? "No registrado" : $"{(costRows.Count < rows.Count ? "Parcial · " : "")}Bs {costRows.Sum(item => item.CostoProveedorAplicado!.Value):N2}";
        MarginTotal.Text = marginRows.Count == 0 ? "No calculable" : $"{(marginRows.Count < rows.Count ? "Parcial · " : "")}Bs {marginRows.Sum(item => item.MargenEstimado!.Value):N2}";
    }
    private static string CostoPorCobertura(IReadOnlyCollection<ConvenioUsoFila> rows, string coverage)
    {
        var all = rows.Where(item => item.Cobertura == coverage).ToList();
        if (all.Count == 0) return "Sin utilizaciones";
        var known = all.Where(item => item.CostoProveedorAplicado.HasValue).ToList();
        if (known.Count == 0) return "No registrado";
        return $"{(known.Count < all.Count ? "Parcial · " : "")}Bs {known.Sum(item => item.CostoProveedorAplicado!.Value):N2}";
    }
    private void Period_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized && ProviderFilter is not null) Recargar(); }
    private void Provider_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized && StartDate is not null) Recargar(); }
    private sealed record ConvenioProviderOption(int? Id, string Label);
}
