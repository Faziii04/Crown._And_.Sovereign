using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class ResumenOperativoControl : UserControl
{
    public event EventHandler<SupervisionAlerta>? AlertNavigationRequested;
    public event EventHandler<SupervisionAlerta>? NotificationRequested;
    public ResumenOperativoControl() => InitializeComponent();
    public void Cargar(SupervisionSnapshot snapshot)
    {
        ReadyText.Text = snapshot.HabitacionesListas.ToString(); OccupiedText.Text = snapshot.HabitacionesOcupadas.ToString();
        PreparingText.Text = snapshot.HabitacionesPreparacion.ToString(); BlockedText.Text = snapshot.HabitacionesBloqueadas.ToString();
        TotalText.Text = snapshot.HabitacionesTotales.ToString(); RoomBasis.Text = "Estado actual de habitación (sin reconstrucción histórica). La suma de las categorías exclusivas del tablero corresponde al total.";
        TasksText.Text = snapshot.TareasProgramadas.ToString(); CompletedText.Text = snapshot.TareasCompletadas.ToString();
        InProgressText.Text = snapshot.TareasEnCurso.ToString(); PendingText.Text = snapshot.TareasPendientes.ToString();
        InspectionsText.Text = snapshot.Inspecciones.Count(item => item.Resultado == "Pendiente").ToString();
        AlertsGrid.ItemsSource = snapshot.Alertas.Where(item => item.Nivel != "Informativa").ToList();
        if (AlertsGrid.Items.Count == 0) AlertsGrid.ItemsSource = snapshot.Alertas;
        CommitmentsGrid.ItemsSource = snapshot.Servicios.Where(item => item.Fecha is DateTime date && date >= DateTime.Today && date < DateTime.Today.AddDays(7))
            .Select(item => new { item.Fecha, Servicio = $"{item.Tipo}: {item.Servicio} · {item.Huesped}", item.Estado }).Take(12).ToList();
        var lost = SampleData.ObjetosOlvidados.Where(item => item.Estado != "Devuelto").ToList();
        LostPropertyText.Text = lost.Count == 0 ? "No hay objetos en custodia registrados." : $"{lost.Count} registro(s) en custodia · {string.Join(", ", lost.Take(4).Select(item => item.Descripcion))}";
        LimitationText.Text = "La muestra no conserva un historial de estados de habitación, bitácora de bloqueos ni marcas reales de inicio y fin de limpieza. El resumen de periodo se limita a eventos con fecha registrada.";
        DiagnosticsList.ItemsSource = snapshot.Diagnosticos;
        DiagnosticsEmpty.Visibility = snapshot.Diagnosticos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Navigate_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is SupervisionAlerta alert) AlertNavigationRequested?.Invoke(this, alert); }
    private void Notify_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is SupervisionAlerta alert) NotificationRequested?.Invoke(this, alert); }
}
