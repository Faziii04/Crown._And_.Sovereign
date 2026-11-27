using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

public partial class ConvenioDetalleControl : UserControl
{
    private readonly ConvenioService _service = new();
    public int ConvenioId { get; }
    public event EventHandler? BackRequested;
    public event EventHandler<int>? EditRequested;
    public event EventHandler<int>? RenewRequested;
    public ConvenioDetalleControl(int convenioId)
    {
        InitializeComponent(); ConvenioId = convenioId; Cargar();
    }
    public void Cargar()
    {
        var agreement = SampleData.Convenios.FirstOrDefault(item => item.Id == ConvenioId);
        if (agreement is null) { StatusText.Text = "Convenio no disponible"; return; }
        var summary = _service.Listar().First(item => item.Convenio.Id == ConvenioId);
        Initials.Text = summary.Iniciales; CompanyText.Text = agreement.Empresa; NameText.Text = agreement.Nombre; CategoryText.Text = agreement.Categoria;
        StatusText.Text = summary.EstadoVigencia; PeriodText.Text = summary.Periodo; StateButton.Content = agreement.Activo ? "Desactivar" : "Activar";
        InfoCompany.Text = agreement.Empresa; InfoName.Text = agreement.Nombre; InfoContact.Text = Blank(agreement.Contacto); InfoPhone.Text = Blank(agreement.Telefono);
        InfoEmail.Text = Blank(agreement.Email); InfoAddress.Text = Blank(agreement.Direccion); InfoDescription.Text = Blank(agreement.Descripcion); TermsText.Text = Blank(agreement.Condiciones);
        var relations = _service.ServiciosDeConvenio(ConvenioId, incluirInactivos: true);
        ServicesGrid.ItemsSource = relations; ServicesEmpty.Visibility = relations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var history = _service.Rendimiento(DateTime.MinValue, DateTime.MaxValue, ConvenioId);
        UsageGrid.ItemsSource = history; HistoryEmpty.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ServiceCount.Text = relations.Count(item => item.Relacion.Activo).ToString(); UsageCount.Text = history.Count.ToString();
        var costs = history.Where(item => item.CostoProveedorAplicado.HasValue).ToList();
        CostTotal.Text = costs.Count == 0 ? "No registrado" : $"{(costs.Count < history.Count ? "Parcial · " : "")}Bs {costs.Sum(item => item.CostoProveedorAplicado!.Value):N2}";
        RenewButton.Visibility = agreement.Activo ? Visibility.Visible : Visibility.Collapsed;
    }
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "No registrado" : value;
    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);
    private void Edit_Click(object sender, RoutedEventArgs e) => EditRequested?.Invoke(this, ConvenioId);
    private void Renew_Click(object sender, RoutedEventArgs e) => RenewRequested?.Invoke(this, ConvenioId);
    private void State_Click(object sender, RoutedEventArgs e)
    {
        var agreement = SampleData.Convenios.FirstOrDefault(item => item.Id == ConvenioId); if (agreement is null) return;
        var action = agreement.Activo ? "desactivar" : "activar";
        if (MessageBox.Show(Window.GetWindow(this), $"¿Quieres {action} {agreement.Empresa} · {agreement.Nombre}? La información y las operaciones históricas se conservarán.", "Estado del convenio", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _service.CambiarEstado(ConvenioId, !agreement.Activo); Cargar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo cambiar el estado", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
