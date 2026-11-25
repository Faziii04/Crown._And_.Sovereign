using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Resumen;

public partial class ResumenHabitacionesControl : UserControl
{
    public event Action<string>? FiltroSolicitado;
    public ResumenHabitacionesControl() => InitializeComponent();
    public void Mostrar(ResumenOperativoHabitaciones summary, string? selected = null, string scope = "Todo el hotel")
    {
        ReadyValue.Text = summary.Listas.ToString(); ReadyNote.Text = $"{summary.LlegadasHoy} llegadas hoy · {scope}";
        OccupiedValue.Text = summary.Ocupadas.ToString(); OccupiedNote.Text = $"{summary.SalidasHoy} salidas hoy · no incluye bloqueadas · {scope}";
        PrepValue.Text = summary.PorPreparar.ToString(); PrepNote.Text = $"{summary.InspeccionesPendientes} pendientes de inspección · {scope}";
        BlockedValue.Text = summary.Bloqueadas.ToString(); BlockedNote.Text = $"{summary.BloqueadasConHuesped} con huésped · {scope}";
        ApplyCardStyle(ReadyCard, selected == "Listas para ingreso"); ApplyCardStyle(OccupiedCard, selected == "Ocupadas"); ApplyCardStyle(PrepCard, selected == "Por preparar"); ApplyCardStyle(BlockedCard, selected == "Bloqueadas");
    }
    private static void ApplyCardStyle(Border card, bool selected) { card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selected ? "#3E5974" : "#DCE2E8")); card.BorderThickness = new Thickness(selected ? 2 : 1); }
    private void Metric_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.Tag is string filter) FiltroSolicitado?.Invoke(filter); }
}
