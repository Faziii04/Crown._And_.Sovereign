using System.Windows.Controls;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Servicios;

public partial class SolicitudesHuespedControl : UserControl
{
    private readonly SolicitudServicioService _service = new();
    private ActiveGuestChoice? _guest;
    public SolicitudesHuespedControl()
    {
        InitializeComponent(); ScopeBox.ItemsSource=new[]{"Huésped seleccionado","Estancia seleccionada","Todas las solicitudes"};ScopeBox.SelectedIndex=2;StatusBox.ItemsSource = new[] { "Todos los estados", "Por confirmar", "Pendiente", "Confirmada", "En atención", "En curso", "Completada", "Cancelada", "No presentado" }; StatusBox.SelectedIndex = 0;DetailView.CancelacionSolicitada+=Cancelar; Recargar();
    }
    public void EstablecerHuesped(ActiveGuestChoice? guest){_guest=guest;if(guest is not null)ScopeBox.SelectedIndex=0;Recargar();}
    public void ScopeSolicitudes(string scope,string status){if(ScopeBox.Items.Contains(scope))ScopeBox.SelectedItem=scope;if(StatusBox.Items.Contains(status))StatusBox.SelectedItem=status;Recargar();}
    public void Recargar()
    {
        var scope=ScopeBox?.SelectedItem?.ToString()??"Todas las solicitudes";var status = StatusBox?.SelectedItem?.ToString(); var query = SearchBox?.Text??"";
        var rows=scope switch{"Huésped seleccionado" when _guest is not null=>_service.Listar(_guest.Id),"Estancia seleccionada" when _guest is not null=>_service.Listar(estanciaId:_guest.EstanciaId),_=>_service.Listar()};
        RequestsGrid.ItemsSource = rows.Where(item => status is null or "Todos los estados" || status=="Por confirmar"&&item.Status is ("Pendiente" or "Solicitada") || item.Status == status).Where(item => string.IsNullOrWhiteSpace(query) || $"{item.Guest} {item.Room} {item.Service} {item.Description} {item.Responsible}".Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        ScopeText.Text=$"Alcance: {scope}{(_guest is null?"":" · "+_guest.Name+" · Hab. "+_guest.Room)}";
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Status_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Request_Selected(object sender,SelectionChangedEventArgs e)
    {
        DetailView.Mostrar(RequestsGrid.SelectedItem as ServiceRequestView);
    }
    private void Cancelar(int requestId)
    {
        var request=SampleRequest(requestId);if(request is null)return;
        if(MessageBox.Show($"¿Cancelar la solicitud de {request.Service} para {request.Guest}?","Cancelar solicitud",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        try{_service.Cancelar(request.Id);Recargar();}catch(Exception ex){MessageBox.Show(ex.Message,"No se pudo cancelar",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
    private ServiceRequestView? SampleRequest(int requestId)=>_service.Listar().FirstOrDefault(item=>item.Id==requestId);
}
