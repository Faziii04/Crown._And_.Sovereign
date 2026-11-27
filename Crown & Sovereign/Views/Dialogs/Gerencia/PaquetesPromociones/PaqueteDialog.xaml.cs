using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.PaquetesPromociones;

public partial class PaqueteDialog : Window
{
    private static readonly string[] Coberturas = ["Ilimitado", "Por día", "Por estancia"];
    private readonly PaqueteService _service = new();
    private readonly PaqueteModel? _original;
    private readonly List<BeneficioPaqueteEdicion> _benefits = [];
    private bool _refreshingBenefits;
    private bool _updatingCatalogFilters;
    public IReadOnlyList<string> CoverageOptions => Coberturas;

    public PaqueteDialog(PaqueteModel? paquete = null)
    {
        InitializeComponent();
        _original = paquete;
        DialogHeading.Text = paquete is null ? "Nuevo paquete" : $"Editar · {paquete.Nombre}";
        if (paquete is not null)
        {
            NameBox.Text = paquete.Nombre; DescriptionBox.Text = paquete.Descripcion; PriceBox.Text = paquete.Precio.ToString("0.00", CultureInfo.CurrentCulture); ActiveBox.IsChecked = paquete.Activo;
            ImagesBox.Text = string.Join(Environment.NewLine, _service.Imagenes(paquete.Id).OrderBy(image => image.Orden).Select(image => image.Url));
            foreach (var benefit in _service.Beneficios(paquete.Id))
            {
                var service = SampleData.Servicios.FirstOrDefault(item => item.Id == benefit.ServicioId);
                if (service is not null) _benefits.Add(new() { ServicioId = service.Id, Servicio = service.Nombre, Cobertura = benefit.TipoCobertura, LimiteUsos = benefit.LimiteUsos, Compartido = benefit.Compartido, Proveedor = TextoProveedor(service) });
            }
        }
        RefreshBenefits(); LoadCatalogFilters(); LoadServices(); RefreshReview();
    }

    private void LoadCatalogFilters()
    {
        var services = ServiciosElegibles().ToList();
        _updatingCatalogFilters = true;
        CategoryFilter.ItemsSource = new[] { "Todas las categorías" }.Concat(services.Select(item => item.Categoria).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item)).ToList();
        CategoryFilter.SelectedIndex = 0;
        ProviderFilter.ItemsSource = new[] { "Todos los proveedores" }.Concat(services.SelectMany(item => Proveedores(item.Id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item)).ToList();
        ProviderFilter.SelectedIndex = 0;
        InclusionFilter.ItemsSource = new[] { "Todos", "Sin agregar", "Ya agregados" };
        InclusionFilter.SelectedIndex = 0;
        UpdateSubcategoryOptions();
        _updatingCatalogFilters = false;
    }

    private static IEnumerable<ServicioModel> ServiciosElegibles() => SampleData.Servicios.Where(item => item.Activo && item.Reservable);

    private void UpdateSubcategoryOptions()
    {
        var category = CategoryFilter.SelectedItem as string;
        var services = ServiciosElegibles();
        if (!string.IsNullOrWhiteSpace(category) && category != "Todas las categorías")
            services = services.Where(item => item.Categoria.Equals(category, StringComparison.OrdinalIgnoreCase));

        var subcategories = services
            .Where(item => item.PadreId is not null)
            .Select(NombrePadre)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
        SubcategoryFilter.ItemsSource = new[] { "Todas las subcategorías" }.Concat(subcategories).ToList();
        SubcategoryFilter.SelectedIndex = 0;
    }

    private void CatalogFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingCatalogFilters || !IsInitialized) return;
        if (ReferenceEquals(sender, CategoryFilter))
        {
            _updatingCatalogFilters = true;
            UpdateSubcategoryOptions();
            _updatingCatalogFilters = false;
        }
        LoadServices();
    }

    private void ClearCatalogFilters_Click(object sender, RoutedEventArgs e)
    {
        _updatingCatalogFilters = true;
        ServiceSearch.Clear();
        CategoryFilter.SelectedIndex = 0;
        UpdateSubcategoryOptions();
        ProviderFilter.SelectedIndex = 0;
        InclusionFilter.SelectedIndex = 0;
        _updatingCatalogFilters = false;
        LoadServices();
    }

    private void LoadServices()
    {
        if (ServiceList is null || CategoryFilter is null) return;
        var filter = ServiceSearch?.Text?.Trim() ?? "";
        if (SearchPrompt is not null) SearchPrompt.Visibility = filter.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var selectedCategory = CategoryFilter.SelectedItem as string;
        var selectedSubcategory = SubcategoryFilter.SelectedItem as string;
        var selectedProvider = ProviderFilter.SelectedItem as string;
        var selectedInclusion = InclusionFilter.SelectedItem as string;
        var eligible = ServiciosElegibles()
            .Where(item => selectedCategory is null || selectedCategory == "Todas las categorías" || item.Categoria.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
            .Where(item => selectedSubcategory is null || selectedSubcategory == "Todas las subcategorías" || NombrePadre(item).Equals(selectedSubcategory, StringComparison.OrdinalIgnoreCase))
            .Where(item => selectedProvider is null || selectedProvider == "Todos los proveedores" || Proveedores(item.Id).Contains(selectedProvider, StringComparer.OrdinalIgnoreCase))
            .Where(item => selectedInclusion switch
            {
                "Sin agregar" => !_benefits.Any(benefit => benefit.ServicioId == item.Id),
                "Ya agregados" => _benefits.Any(benefit => benefit.ServicioId == item.Id),
                _ => true
            })
            .Where(item => string.IsNullOrWhiteSpace(filter) || $"{item.Nombre} {item.Categoria} {NombrePadre(item)} {string.Join(" ", Proveedores(item.Id))}".Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Categoria).ThenBy(item => item.Nombre)
            .Select(item =>
            {
                var parent = NombrePadre(item);
                var route = string.IsNullOrWhiteSpace(parent) || parent.Equals(item.Categoria, StringComparison.OrdinalIgnoreCase) ? item.Categoria : $"{item.Categoria}  ›  {parent}";
                var proveedores = Proveedores(item.Id);
                return new ServicioPaqueteOpcion(item.Id, item.Nombre, route, proveedores.Count > 0, proveedores.Count == 0 ? "Sin convenio asociado" : $"Convenios: {string.Join(" · ", proveedores)}", _benefits.Any(benefit => benefit.ServicioId == item.Id));
            })
            .ToList();
        ServiceList.ItemsSource = eligible;
        ServiceResultCount.Text = eligible.Count == 1 ? "1 servicio" : $"{eligible.Count} servicios";
        ServicesEmpty.Visibility = eligible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string NombrePadre(ServicioModel item) => item.PadreId is int id ? SampleData.Servicios.FirstOrDefault(parent => parent.Id == id)?.Nombre ?? "" : "";
    private void ServiceSearch_TextChanged(object sender, TextChangedEventArgs e) { if (IsInitialized && !_updatingCatalogFilters) LoadServices(); }

    private void AddService_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not int id || _benefits.Any(item => item.ServicioId == id)) return;
        var service = SampleData.Servicios.First(item => item.Id == id);
        _benefits.Add(new() { ServicioId = service.Id, Servicio = service.Nombre, Cobertura = "Por estancia", LimiteUsos = 1, Compartido = true, Proveedor = TextoProveedor(service) });
        RefreshBenefits(); LoadServices(); ErrorText.Text = ""; RefreshReview();
    }

    private void RemoveBenefit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is int id) _benefits.RemoveAll(item => item.ServicioId == id);
        RefreshBenefits(); LoadServices(); RefreshReview();
    }

    private void Coverage_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingBenefits || (sender as FrameworkElement)?.DataContext is not BeneficioPaqueteEdicion benefit) return;
        benefit.LimiteUsos = benefit.Cobertura == "Ilimitado" ? null : benefit.LimiteUsos ?? 1;
        RefreshReview();
    }

    private void RefreshBenefits()
    {
        _refreshingBenefits = true;
        BenefitsList.ItemsSource = _benefits.ToList();
        BenefitCountText.Text = $"{_benefits.Count} configurado(s)";
        BenefitsEmpty.Visibility = _benefits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _refreshingBenefits = false;
    }

    private static List<string> Proveedores(int servicioId) => SampleData.ConveniosServicios.Where(link => link.ServicioId == servicioId && link.Activo)
        .Select(link => SampleData.Convenios.FirstOrDefault(item => item.Id == link.ConvenioId)?.Empresa).Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private static string TextoProveedor(ServicioModel service) { var names = Proveedores(service.Id); return names.Count == 0 ? "Sin convenio asociado" : $"Convenios: {string.Join(" · ", names)}"; }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (Steps.SelectedIndex == 0)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(DescriptionBox.Text) || !decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price <= 0)
            { ErrorText.Text = "Completa nombre, descripción y precio mayor que cero antes de continuar."; return; }
            Steps.SelectedIndex = 1;
        }
        else { RefreshReview(); Steps.SelectedIndex = 2; }
    }

    private void Steps_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || NextButton is null) return;
        var isLast = Steps.SelectedIndex == 2;
        NextButton.Visibility = isLast ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.Visibility = isLast ? Visibility.Visible : Visibility.Collapsed;
        if (isLast) RefreshReview();
    }

    private void RefreshReview()
    {
        if (!IsInitialized || ReviewName is null) return;
        ReviewName.Text = string.IsNullOrWhiteSpace(NameBox.Text) ? "Nombre del paquete" : NameBox.Text.Trim();
        ReviewPrice.Text = decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) && price > 0 ? $"Bs {price:N2} · precio adicional" : "Indica el precio adicional";
        ReviewBenefits.Text = _benefits.Count == 0 ? "Sin beneficios configurados. El paquete se vendería sin servicios incluidos." : string.Join(Environment.NewLine, _benefits.Select(item => $"• {item.Servicio}: {item.Cobertura}{(item.Cobertura == "Ilimitado" ? "" : $" · {item.LimiteUsos ?? 0} uso(s)")}{(item.Proveedor.StartsWith("Convenios:", StringComparison.Ordinal) ? " · requiere coordinación con proveedor" : "")}"));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_benefits.Count == 0 && MessageBox.Show(this, "Este paquete no incluye servicios. ¿Quieres guardarlo así?", "Paquete sin beneficios", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            if (_original is not null && SampleData.ReservaPaquetes.Any(item => item.PaqueteId == _original.Id) && MessageBox.Show(this, "El precio y los beneficios ya adquiridos se conservan como snapshot. Este cambio afecta a nuevas compras. ¿Continuar?", "Cambios para futuras reservas", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes) return;
            var price = decimal.Parse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture);
            var benefits = _benefits.Select(item => new PaqueteServicioModel(_original?.Id ?? 0, item.ServicioId, item.Cobertura, item.Cobertura == "Ilimitado" ? null : item.LimiteUsos, item.Compartido)).ToList();
            _service.Guardar(_original?.Id, NameBox.Text, DescriptionBox.Text, price, ActiveBox.IsChecked == true, benefits, ImagesBox.Text.Split(["\r\n", "\n"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
