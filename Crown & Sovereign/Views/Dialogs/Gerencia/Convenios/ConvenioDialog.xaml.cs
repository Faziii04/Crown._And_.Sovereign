using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Convenios;

public partial class ConvenioDialog : Window
{
    private static readonly string[] TiposProveedor = ["Transporte", "Turismo", "Bienestar", "Recreación", "Salud", "Lavandería", "Otros"];
    private readonly ConvenioService _service = new();
    private readonly ConvenioModel? _seed;
    private readonly int? _editingId;
    private readonly List<ConvenioServicioEdicion> _serviceRows = [];

    public ConvenioDialog(ConvenioModel? convenio = null, int? copiarServiciosDeConvenioId = null)
    {
        InitializeComponent();
        _seed = convenio;
        _editingId = convenio is not null && SampleData.Convenios.Any(item => item.Id == convenio.Id) ? convenio.Id : null;
        TitleText.Text = _editingId is null ? (copiarServiciosDeConvenioId.HasValue ? "Renovar convenio" : "Nuevo convenio") : $"Editar · {convenio!.Empresa}";
        ProviderType.ItemsSource = TiposProveedor;
        var sourceId = copiarServiciosDeConvenioId ?? _editingId;
        var existing = sourceId is int sid ? _service.ServiciosDeConvenio(sid, incluirInactivos: true).ToDictionary(item => item.Servicio.Id, item => item.Relacion) : new Dictionary<int, ConvenioServicioModel>();
        foreach (var service in SampleData.Servicios.Where(item => item.Activo && item.Reservable && !SampleData.Servicios.Any(child => child.PadreId == item.Id)).OrderBy(item => item.Categoria).ThenBy(item => item.Nombre))
        {
            existing.TryGetValue(service.Id, out var relation);
            _serviceRows.Add(new ConvenioServicioEdicion
            {
                ServicioId = service.Id,
                Nombre = service.Nombre,
                Categoria = service.Categoria,
                PrecioOficial = service.Precio,
                Seleccionado = relation?.Activo == true,
                Costo = relation?.CostoProveedor ?? 0m,
                Observaciones = relation?.Observaciones ?? ""
            });
        }
        if (convenio is not null)
        {
            AgreementName.Text = convenio.Nombre; CompanyName.Text = convenio.Empresa; ProviderType.Text = convenio.Categoria;
            StartDate.SelectedDate = convenio.VigenciaDesde; EndDate.SelectedDate = convenio.VigenciaHasta;
            Description.Text = convenio.Descripcion; ContactName.Text = convenio.Contacto; Phone.Text = convenio.Telefono;
            Email.Text = convenio.Email; Address.Text = convenio.Direccion; Terms.Text = convenio.Condiciones;
            LogoUrl.Text = convenio.LogoUrl; ActiveCheck.IsChecked = convenio.Activo;
        }
        else
        {
            StartDate.SelectedDate = DateTime.Today; EndDate.SelectedDate = DateTime.Today.AddYears(1);
            if (copiarServiciosDeConvenioId.HasValue && _seed is not null)
            {
                AgreementName.Text = _seed.Nombre; CompanyName.Text = _seed.Empresa; ProviderType.Text = _seed.Categoria;
                Description.Text = _seed.Descripcion; ContactName.Text = _seed.Contacto; Phone.Text = _seed.Telefono;
                Email.Text = _seed.Email; Address.Text = _seed.Direccion; Terms.Text = _seed.Condiciones;
            }
        }
        foreach (var row in _serviceRows)
            row.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ConvenioServicioEdicion.Seleccionado)) ActualizarConteoServicios();
            };
        CargarServicios();
    }

    private void CargarServicios()
    {
        var filter = ServiceSearch.Text.Trim();
        var rows = _serviceRows.Where(item => string.IsNullOrWhiteSpace(filter) || $"{item.Nombre} {item.Categoria}".Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        ServicesGrid.ItemsSource = rows;
        ActualizarConteoServicios(rows.Count);
    }

    private void ActualizarConteoServicios(int? visibles = null)
    {
        if (ServiceCount is not null)
            ServiceCount.Text = $"{visibles ?? _serviceRows.Count} servicio(s) · {_serviceRows.Count(item => item.Seleccionado)} seleccionado(s)";
        if (Steps?.SelectedIndex == 2 && ReviewServices is not null)
            ReviewServices.ItemsSource = _serviceRows.Where(item => item.Seleccionado).ToList();
    }

    private void ServiceSearch_TextChanged(object sender, TextChangedEventArgs e) { if (IsInitialized) CargarServicios(); }

    private bool ValidarDatosGenerales()
    {
        if (string.IsNullOrWhiteSpace(AgreementName.Text) || string.IsNullOrWhiteSpace(CompanyName.Text) || string.IsNullOrWhiteSpace(ProviderType.Text))
        { ErrorText.Text = "Completa el nombre del convenio, la empresa y el tipo de proveedor."; return false; }
        if (StartDate.SelectedDate is not DateTime from || EndDate.SelectedDate is not DateTime to || to.Date < from.Date)
        { ErrorText.Text = "Selecciona una vigencia válida; la fecha final no puede ser anterior a la inicial."; return false; }
        if (!string.IsNullOrWhiteSpace(Email.Text) && !Email.Text.Contains('@'))
        { ErrorText.Text = "Escribe un correo de contacto válido."; return false; }
        ErrorText.Text = ""; return true;
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (Steps.SelectedIndex == 0)
        {
            if (!ValidarDatosGenerales()) return;
            Steps.SelectedIndex = 1;
        }
        else
        {
            ServicesGrid.CommitEdit(DataGridEditingUnit.Cell, true); ServicesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            ActualizarConteoServicios();
            ReviewCompany.Text = CompanyName.Text.Trim(); ReviewName.Text = AgreementName.Text.Trim();
            ReviewPeriod.Text = $"{StartDate.SelectedDate:dd/MM/yyyy} – {EndDate.SelectedDate:dd/MM/yyyy} · {(ActiveCheck.IsChecked == true ? "Administrativamente activo" : "Inactivo")}";
            ReviewServices.ItemsSource = _serviceRows.Where(item => item.Seleccionado).ToList();
            Steps.SelectedIndex = 2;
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) { ErrorText.Text = ""; Steps.SelectedIndex = Math.Max(0, Steps.SelectedIndex - 1); }

    private void Steps_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || NextButton is null) return;
        var last = Steps.SelectedIndex == 2;
        NextButton.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.Visibility = last ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = Steps.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (last && ReviewServices is not null) ReviewServices.ItemsSource = _serviceRows.Where(item => item.Seleccionado).ToList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ServicesGrid.CommitEdit(DataGridEditingUnit.Cell, true); ServicesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            ActualizarConteoServicios();
            if (!ValidarDatosGenerales()) return;
            var links = _serviceRows.Where(item => item.Seleccionado).Select(item => (item.ServicioId, item.Costo, item.Observaciones, true)).ToList();
            _service.Guardar(_editingId, AgreementName.Text, CompanyName.Text, ProviderType.Text, StartDate.SelectedDate!.Value, EndDate.SelectedDate!.Value,
                ActiveCheck.IsChecked == true, Description.Text, ContactName.Text, Phone.Text, Email.Text, Address.Text, Terms.Text, LogoUrl.Text, links);
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
