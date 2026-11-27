using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Data;
using Microsoft.Win32;
using System.IO;
using Crown___Sovereign.Views.Dialogs.Gerencia.Reportes;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Reportes;

public partial class GeneradorReportesControl : UserControl
{
    private DefinicionReporte? _definition;
    private ResultadoReporte? _result;
    private bool _loading;
    private readonly VistaPreviaReporteControl _preview = new();
    public event Action? VolverBiblioteca;
    public event Action<ResultadoReporte, ParametrosReporte>? ReporteGenerado;
    public GeneradorReportesControl()
    {
        InitializeComponent();
        PreviewHost.Content = _preview;
        PeriodBox.ItemsSource = ReporteService.Periodos;
        TypeBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.Tipos).Distinct().ToList(); TypeBox.SelectedIndex = 0;
        FloorBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.Habitaciones.Select(x => x.Piso.ToString()).Distinct().OrderBy(x => x)).ToList(); FloorBox.SelectedIndex = 0;
        ChannelBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.CanalesReserva.Select(x => x.Nombre)).Distinct().ToList(); ChannelBox.SelectedIndex = 0;
        PaymentBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.MetodosPago.Select(x => x.Nombre)).Distinct().ToList(); PaymentBox.SelectedIndex = 0;
        CategoryBox.ItemsSource = new[] { "Todas" }.Concat(SampleData.Productos.Select(x => x.Categoria).Concat(SampleData.Servicios.Select(x => x.Categoria)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()).ToList(); CategoryBox.SelectedIndex = 0;
        EmployeeBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.Empleados.Select(x => x.Nombre)).Distinct().ToList(); EmployeeBox.SelectedIndex = 0;
    }
    public void Seleccionar(DefinicionReporte definition, ParametrosReporte? reuse = null)
    {
        _loading = true; _definition = definition; TitleText.Text = definition.Nombre;
        var period = reuse?.Periodo ?? "Este mes"; PeriodBox.SelectedItem = period;
        var parameters = reuse ?? ReporteService.ResolverPeriodo(definition.Id, period);
        StartDate.SelectedDate = parameters.Desde; EndDate.SelectedDate = parameters.Hasta;
        Select(TypeBox, parameters.TipoHabitacion); Select(FloorBox, parameters.Piso); Select(ChannelBox, parameters.Canal);
        Select(PaymentBox, parameters.MetodoPago); Select(CategoryBox, parameters.Categoria); Select(EmployeeBox, parameters.Empleado);
        CompareBox.IsChecked = parameters.Comparar;
        ConfigureFilters(definition.Id); _loading = false; _result = null; DirtyText.Text = "Configura el reporte y genera una vista previa.";
    }
    public void AbrirResultado(ResultadoReporte result)
    {
        _result = result; _definition = ReporteService.Catalogo.First(x => x.Id == result.Id); _preview.Cargar(result);
        Seleccionar(_definition, new(result.Id, "Periodo personalizado", result.Desde, result.Hasta)); _result = result; _preview.Cargar(result);
        DirtyText.Text = "Resultado generado. Si cambias los parámetros, vuelve a generar para actualizarlo.";
    }
    public ParametrosReporte? ParametrosActuales() => _definition is null ? null : MakeParameters();
    private void ConfigureFilters(string id)
    {
        RoomFilters.Visibility = id is "ocupacion" or "disponibilidad" or "reservas" ? Visibility.Visible : Visibility.Collapsed;
        ChannelFilter.Visibility = id is "reservas" or "tarifas" ? Visibility.Visible : id == "saldos" ? Visibility.Visible : Visibility.Collapsed;
        if (id == "saldos") { ChannelLabel.Text = "Canal de reserva"; ChannelBox.ItemsSource = new[] { "Todos" }.Concat(SampleData.CanalesReserva.Select(x => x.Nombre)).Distinct().ToList(); Select(ChannelBox, "Todos"); }
        PaymentFilter.Visibility = id == "ingresos" ? Visibility.Visible : Visibility.Collapsed;
        CategoryFilter.Visibility = id is "inventario" or "servicios" ? Visibility.Visible : Visibility.Collapsed;
        EmployeeFilter.Visibility = id == "limpieza" ? Visibility.Visible : Visibility.Collapsed;
    }
    private ParametrosReporte MakeParameters()
    {
        var from = StartDate.SelectedDate ?? DateTime.Today; var to = EndDate.SelectedDate ?? from;
        return new(_definition!.Id, PeriodBox.SelectedItem as string ?? "Periodo personalizado", from.Date, to.Date,
            TipoHabitacion: TypeBox.SelectedItem as string ?? "Todos", Piso: FloorBox.SelectedItem as string ?? "Todos",
            Canal: ChannelBox.SelectedItem as string ?? "Todos", MetodoPago: PaymentBox.SelectedItem as string ?? "Todos",
            Categoria: CategoryBox.SelectedItem as string ?? "Todas", Empleado: EmployeeBox.SelectedItem as string ?? "Todos", Comparar: CompareBox.IsChecked == true);
    }
    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_definition is null) return;
        try
        {
            GenerateButton.IsEnabled = false; DirtyText.Text = "Calculando con los registros disponibles…";
            var parameters = MakeParameters();
            var result = await Task.Run(() => new ReporteService().Generar(parameters)).ConfigureAwait(true);
            _result = result[0]; _preview.Cargar(_result); DirtyText.Text = "Resultado generado. Los parámetros actuales están aplicados.";
            ReporteGenerado?.Invoke(_result, parameters);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo generar el reporte", MessageBoxButton.OK, MessageBoxImage.Warning); DirtyText.Text = "No se pudo generar el reporte."; }
        finally { GenerateButton.IsEnabled = true; }
    }
    private void Parameters_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _definition is null) return;
        if (ReferenceEquals(sender, StartDate) || ReferenceEquals(sender, EndDate))
        {
            _loading = true;
            PeriodBox.SelectedItem = "Periodo personalizado";
            _loading = false;
        }
        if (_result is not null) DirtyText.Text = "Los parámetros han cambiado. Genere nuevamente el reporte para actualizar los resultados.";
    }
    private void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _definition is null || PeriodBox.SelectedItem is not string period) return;
        if (period == "Periodo personalizado") { Parameters_Changed(sender, new RoutedEventArgs()); return; }
        _loading = true;
        try
        {
            var dates = ReporteService.ResolverPeriodo(_definition.Id, period);
            StartDate.SelectedDate = dates.Desde;
            EndDate.SelectedDate = dates.Hasta;
        }
        finally { _loading = false; }
        Parameters_Changed(sender, new RoutedEventArgs());
    }
    private void Reset_Click(object sender, RoutedEventArgs e) { if (_definition is not null) Seleccionar(_definition); }
    private void Back_Click(object sender, RoutedEventArgs e) => VolverBiblioteca?.Invoke();
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_result is null) { MessageBox.Show("Genera primero un reporte.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"CrownSovereign_{Safe(_result.Titulo)}_{_result.Desde:yyyy-MM-dd}_{_result.Hasta:yyyy-MM-dd}.csv", DefaultExt = ".csv", AddExtension = true };
        if (dialog.ShowDialog() != true) return;
        try { File.WriteAllText(dialog.FileName, ToCsv(_result), new System.Text.UTF8Encoding(true)); MessageBox.Show("El archivo CSV se guardó correctamente.", "Exportación", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_result is null) { MessageBox.Show("Genera primero un reporte.", "Imprimir", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        new VistaImpresionReporteDialog(_result).ShowDialog();
    }
    private static string ToCsv(ResultadoReporte r)
    {
        var lines = new List<string> { Csv("Crown & Sovereign"), Csv(r.Titulo), Csv("Categoría", r.Categoria), Csv("Periodo", $"{r.Desde:dd/MM/yyyy} - {r.Hasta:dd/MM/yyyy}"), Csv("Generado", r.Generado.ToString("dd/MM/yyyy HH:mm")), Csv("Usuario", r.Usuario), Csv("Filtros", r.Parametros), "", Csv(r.Detalle.Columns.Cast<System.Data.DataColumn>().Select(x => x.ColumnName).ToArray()) };
        foreach (System.Data.DataRow row in r.Detalle.Rows) lines.Add(Csv(row.ItemArray.Select(Format).ToArray()));
        lines.Add(""); lines.Add(Csv("Indicadores")); foreach (var kpi in r.Indicadores) lines.Add(Csv(kpi.Nombre, kpi.Valor, kpi.Contexto));
        lines.Add(""); lines.Add(Csv("Metodología", r.Metodologia)); foreach (var warning in r.Advertencias) lines.Add(Csv("Advertencia", warning));
        return string.Join(Environment.NewLine, lines);
    }
    private static string Csv(params string[] cells) => string.Join(",", cells.Select(cell => { var safe = cell ?? ""; if (safe.Length > 0 && "=+-@\t\r".Contains(safe[0])) safe = "'" + safe; return "\"" + safe.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""; }));
    private static string Format(object? x) => x switch { null or DBNull => "", DateTime d => d.ToString("dd/MM/yyyy HH:mm"), decimal m => m.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), _ => Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture) ?? "" };
    private static string Safe(string text) => string.Concat(text.Where(char.IsLetterOrDigit)).Replace(" ", "_");
    private static void Select(ComboBox combo, string value) { if (combo.Items.Contains(value)) combo.SelectedItem = value; else if (combo.Items.Count > 0) combo.SelectedIndex = 0; }
}
