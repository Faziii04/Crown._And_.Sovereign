using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;
using Crown___Sovereign.Views.UserControls.Shared.Cards;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class SolicitarServicioControl : UserControl
{
    public event Action? SolicitudCreada;
    public event Action<ActiveGuestChoice?>? HuespedActualizado;
    private ActiveGuestChoice? _guest;
    public ActiveGuestChoice? HuespedActual=>_guest;
    public SolicitarServicioControl()
    {
        InitializeComponent();
        GuestSelector.HuespedSeleccionado += MostrarHuesped;
        CoverageFilter.ItemsSource = new[] { "Todos", "Incluidos", "Adicionales", "Requieren cita" }; CoverageFilter.SelectedIndex = 0;
        if (GuestSelector.HuespedActual is { } guest) MostrarHuesped(guest);
        else RefrescarCatalogo();
    }
    private void MostrarHuesped(ActiveGuestChoice? guest)
    {
        _guest = guest;
        SelectedGuestText.Text = guest is null ? "Selecciona un huésped activo para consultar su cobertura." : $"{guest.Name} · Hab. {guest.Room} · {guest.Package} · {guest.Role} · Estancia #{guest.EstanciaId}";
        BenefitsButton.IsEnabled=guest is not null;
        RefrescarCatalogo();
        HuespedActualizado?.Invoke(guest);
    }
    private void RefrescarCatalogo()
    {
        if (CategoryList is null || CoverageFilter is null) return;
        var query = ServiceSearch?.Text?.Trim() ?? ""; var filter = CoverageFilter.SelectedItem?.ToString() ?? "Todos";
        var services = SampleData.Servicios.Where(service => service.Activo && service.Reservable)
            .Where(service => string.IsNullOrWhiteSpace(query) || $"{service.Nombre} {service.Categoria} {service.Descripcion}".Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(service => filter switch { "Requieren cita" => service.RequiereCita, "Incluidos" => _guest is not null && (new ServicioBeneficioService().Evaluar(_guest.EstanciaId,service.Id) is var benefit && (benefit.CoverageType!="No incluido" && benefit.Remaining>0 || benefit.PurchasedRemaining>0)), "Adicionales" => _guest is not null && (new ServicioBeneficioService().Evaluar(_guest.EstanciaId,service.Id) is var benefit && (benefit.CoverageType=="No incluido" || benefit.Remaining==0) && benefit.PurchasedRemaining==0), _ => true })
            .ToList();
        CategoryList.ItemsSource = services.GroupBy(service => service.Categoria).OrderBy(group => group.Key).Select(group => new ServiceCategoryGroup(group.Key,group.GroupBy(service=>service.PadreId is int parentId?SampleData.Servicios.FirstOrDefault(parent=>parent.Id==parentId)?.Nombre??"Otros":service.Nombre).OrderBy(parent=>parent.Key).Select(parent=>new ServiceParentGroup(parent.Key,parent.ToList())).ToList())).ToList();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) => RefrescarCatalogo();
    private void Filter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefrescarCatalogo();
    private void ServiceCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ServicioCardControl card) { card.Guest = _guest; card.SolicitudSolicitada -= Solicitar; card.SolicitudSolicitada += Solicitar; }
    }
    private void Benefits_Click(object sender,RoutedEventArgs e)
    {
        if(_guest is null)return;
        new BeneficiosServicioDialog(_guest){Owner=Window.GetWindow(this)}.ShowDialog();
    }
    private void Solicitar(ServicioModel service)
    {
        if (_guest is null) { MessageBox.Show("Selecciona un huésped activo en el panel izquierdo."); return; }
        var dialog = new SolicitudServicioDialog(_guest, service) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) SolicitudCreada?.Invoke();
    }
}
