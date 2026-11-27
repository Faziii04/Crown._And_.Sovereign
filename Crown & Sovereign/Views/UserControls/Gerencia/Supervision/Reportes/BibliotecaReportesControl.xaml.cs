using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Reportes;

public partial class BibliotecaReportesControl : UserControl
{
    public event Action<DefinicionReporte>? ReporteSeleccionado;
    public BibliotecaReportesControl()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = new[] { "Todas" }.Concat(ReporteService.Catalogo.Select(x => x.Categoria).Distinct().OrderBy(x => x)).ToList();
        CategoryBox.SelectedIndex = 0;
        Refresh();
    }
    private void Refresh() => Cards.ItemsSource = ReporteService.FiltrarCatalogo(SearchBox.Text, CategoryBox.SelectedItem as string ?? "Todas");
    private void Filter_Changed(object sender, RoutedEventArgs e) => Refresh();
    private void Configure_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DefinicionReporte definition) ReporteSeleccionado?.Invoke(definition);
    }
}
