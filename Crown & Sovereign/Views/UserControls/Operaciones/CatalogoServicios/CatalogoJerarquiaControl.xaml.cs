using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class CatalogoJerarquiaControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    private ServicioModel? _selected;
    private string _filter = "Todos";
    public event EventHandler? CatalogChanged;
    public CatalogoJerarquiaControl()
    {
        InitializeComponent(); StateFilter.ItemsSource = new[] { "Todos", "Activos", "Inactivos", "Reservables", "No reservables", "Requieren cita", "Con proveedores", "Incluidos en paquetes", "Requieren revisión" };
        StateFilter.SelectedIndex = 0; ServiceDetail.ConfigurationChanged += (_, _) => CatalogChanged?.Invoke(this, EventArgs.Empty); Loaded += (_, _) => Refrescar();
    }
    public void Filtrar(string estado) { _filter = estado; StateFilter.SelectedItem = estado; Refrescar(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) => Refrescar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (StateFilter.SelectedItem is string value) { _filter = value; Refrescar(); } }
    private void Refrescar()
    {
        if (ServiceTree is null) return;
        var all = SampleData.Servicios;
        var selectedId = _selected?.Id;
        var roots = all.Where(item => item.PadreId is null).OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).ToList();
        var categories = roots.GroupBy(item => item.Categoria).Select(group => new ServiceTreeNode
        {
            EsCategoria = true, Categoria = group.Key,
            Children = group.Select(item => Build(item, all, new HashSet<int>())).Where(node => MatchesBranch(node, SearchBox.Text, _filter)).ToList()
        }).Where(item => item.Children.Count > 0).ToList();
        ServiceTree.ItemsSource = categories;
        foreach (var item in categories) ExpandNode(ServiceTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem, true);
        if (selectedId is int id && Find(categories, id) is ServiceTreeNode node) SelectNode(ServiceTree, node);
        else { _selected = null; ServiceDetail.Mostrar(null); }
    }
    private static ServiceTreeNode Build(ServicioModel service, IReadOnlyList<ServicioModel> all, HashSet<int> path)
    {
        if (!path.Add(service.Id)) return new() { Service = service, Children = [] };
        var children = all.Where(item => item.PadreId == service.Id).OrderBy(item => item.Nombre).Select(item => Build(item, all, new(path))).ToList();
        return new() { Service = service, Children = children };
    }
    private bool MatchesBranch(ServiceTreeNode node, string text, string filter)
    {
        if (node.Service is null) return node.Children.Any(item => MatchesBranch(item, text, filter));
        var service = node.Service;
        var status = filter switch { "Activos" => service.Activo, "Inactivos" => !service.Activo, "Reservables" => service.Activo && service.Reservable,
            "No reservables" => !service.Reservable, "Requieren cita" => service.Activo && service.Reservable && service.RequiereCita,
            "Con proveedores" => new ConvenioService().EsExterno(service.Id), "Incluidos en paquetes" => SampleData.PaquetesServicios.Any(item => item.ServicioId == service.Id),
            "Requieren revisión" => _service.Revisiones().Any(item => item.ServicioId == service.Id), _ => true };
        var matchesText = string.IsNullOrWhiteSpace(text) || _service.Listar(text, "Todos").Any(item => item.Id == service.Id);
        return (status && matchesText) || node.Children.Any(item => MatchesBranch(item, text, filter));
    }
    private static ServiceTreeNode? Find(IEnumerable<ServiceTreeNode> nodes, int id) { foreach (var node in nodes) { if (node.Service?.Id == id) return node; var found = Find(node.Children, id); if (found is not null) return found; } return null; }
    private static void SelectNode(ItemsControl parent, ServiceTreeNode target)
    {
        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem container) continue;
            if (ReferenceEquals(item, target)) { container.IsSelected = true; container.BringIntoView(); return; }
            container.IsExpanded = true; SelectNode(container, target);
        }
    }
    private static void ExpandNode(TreeViewItem? item, bool expanded) { if (item is null) return; item.IsExpanded = expanded; item.UpdateLayout(); foreach (var child in item.Items) ExpandNode(item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem, expanded); }
    private void Expand_Click(object sender, RoutedEventArgs e) { foreach (var item in ServiceTree.Items) ExpandNode(ServiceTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem, true); }
    private void Collapse_Click(object sender, RoutedEventArgs e) { foreach (var item in ServiceTree.Items) ExpandNode(ServiceTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem, false); }
    private void Service_Selected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not ServiceTreeNode { Service: not null } node) { _selected = null; ServiceDetail.Mostrar(null); return; }
        _selected = node.Service; ServiceDetail.Mostrar(_selected);
    }
    private void NewRoot_Click(object sender, RoutedEventArgs e) => Abrir(null, null);
    private void NewChild_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) { MessageBox.Show("Selecciona un servicio principal para crear el subservicio.", "Catálogo de servicios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (_selected.PadreId is not null) { MessageBox.Show("Este catálogo conserva dos niveles. Selecciona el servicio principal al que deseas agregar el subservicio.", "Elige el nivel principal", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Abrir(null, _selected.Id);
    }
    private void Abrir(ServicioModel? editing, int? parent)
    {
        var dialog = new ServicioDialog(editing, parent) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { _selected = dialog.Guardado; Refrescar(); if (_selected is not null && Find(ServiceTree.ItemsSource.Cast<ServiceTreeNode>(), _selected.Id) is ServiceTreeNode node) SelectNode(ServiceTree, node); CatalogChanged?.Invoke(this, EventArgs.Empty); }
    }
}
