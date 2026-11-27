using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class SupervisionLimpiezaControl : UserControl
{
    private IReadOnlyList<SupervisionTareaFila> _tasks = [];
    public SupervisionLimpiezaControl() => InitializeComponent();
    public void Cargar(SupervisionSnapshot snapshot)
    {
        _tasks = snapshot.Tareas;
        ScheduledText.Text = snapshot.TareasProgramadas.ToString(); CompletedText.Text = snapshot.TareasCompletadas.ToString();
        InProgressText.Text = snapshot.TareasEnCurso.ToString(); PendingText.Text = snapshot.TareasPendientes.ToString();
        AttentionText.Text = snapshot.Tareas.Count(item => item.Atencion != "—").ToString();
        var selected = (FloorBox.SelectedItem as ComboBoxItem)?.Tag;
        FloorBox.Items.Clear(); FloorBox.Items.Add(new ComboBoxItem { Content = "Todos los pisos", IsSelected = true });
        foreach (var floor in _tasks.Select(item => item.Piso).Distinct().OrderBy(item => item)) FloorBox.Items.Add(new ComboBoxItem { Content = $"Piso {floor}", Tag = floor });
        FloorBox.SelectedIndex = 0;
        TaskGrid.ItemsSource = _tasks; WorkloadGrid.ItemsSource = snapshot.Carga; InspectionGrid.ItemsSource = snapshot.Inspecciones;
        var completeChecks = snapshot.Inspecciones.Where(item => item.Resultado is "Aprobada" or "Rechazada").ToList();
        var rate = completeChecks.Count == 0 ? "Sin observaciones" : $"{completeChecks.Count(item => item.Resultado == "Aprobada")} de {completeChecks.Count} inspecciones concluidas fueron aprobadas ({(double)completeChecks.Count(item => item.Resultado == "Aprobada") / completeChecks.Count:P0}).";
        PerformanceText.Text = $"Tareas programadas para el periodo: {snapshot.TareasProgramadas}. Completadas: {snapshot.TareasCompletadas}. Inspecciones con resultado registrado: {completeChecks.Count}. Tasa de aprobación: {rate}\n\nLas tareas completadas no conservan una marca de tiempo real en el modelo local; el periodo de tareas usa la fecha programada, no la fecha de finalización.";
    }
    public void AbrirAlerta(SupervisionAlerta alert)
    {
        var task = _tasks.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (task is null) return;
        TaskGrid.SelectedItem = task; TaskGrid.ScrollIntoView(task);
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void ApplyFilters()
    {
        if (TaskGrid is null) return;
        var query = SearchBox.Text.Trim(); var status = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los estados";
        var floor = (FloorBox.SelectedItem as ComboBoxItem)?.Tag as int?;
        TaskGrid.ItemsSource = _tasks.Where(item => (status == "Todos los estados" || item.Estado == status) && (!floor.HasValue || item.Piso == floor.Value)
            && (string.IsNullOrWhiteSpace(query) || $"{item.Habitacion} {item.Responsable} {item.Tipo}".Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
    }
}

