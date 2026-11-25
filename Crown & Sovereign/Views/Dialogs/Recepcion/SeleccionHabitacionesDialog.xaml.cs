using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public sealed class HabitacionSeleccionItem
{
    public int Id { get; init; }
    public string Numero { get; init; } = "";
    public string Detalle { get; init; } = "";
    public decimal TotalEstancia { get; init; }
    public bool IsSelected { get; set; }
}

public partial class SeleccionHabitacionesDialog : Window
{
    private readonly ReservationSearchCriteria _criteria;
    public IReadOnlyList<int> HabitacionesSeleccionadas { get; private set; } = [];
    public SeleccionHabitacionesDialog(ReservationSearchCriteria criteria, IReadOnlyList<HabitacionDisponibleModel> candidates)
    {
        InitializeComponent(); _criteria = criteria;
        IntroText.Text = $"{criteria.Llegada:dd MMM} – {criteria.Salida:dd MMM} · {criteria.Personas} personas · selecciona {criteria.Habitaciones} habitación(es) que cubran la estancia completa.";
        RoomList.ItemsSource = candidates.Select(item => new HabitacionSeleccionItem { Id = item.Id, Numero = item.Numero, Detalle = $"{item.Tipo} · Piso {item.Piso} · capacidad {item.Capacidad} · {item.EstadoLimpieza}", TotalEstancia = item.TotalEstancia }).ToList();
    }
    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var selected = RoomList.Items.Cast<HabitacionSeleccionItem>().Where(item => item.IsSelected).ToList();
        try
        {
            new HabitacionDisponibilidadService().ValidarSeleccion(_criteria, selected.Select(item => item.Id).ToArray());
            HabitacionesSeleccionadas = selected.Select(item => item.Id).ToList(); DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
