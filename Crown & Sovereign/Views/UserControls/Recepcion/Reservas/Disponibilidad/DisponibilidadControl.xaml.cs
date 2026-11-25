using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Disponibilidad;

public partial class DisponibilidadControl : UserControl
{
    private readonly HabitacionDisponibilidadService _service = new();
    private readonly MatrizDisponibilidadControl _matrix = new();
    private readonly ResultadosDisponibilidadControl _results = new();
    private ReservationSearchCriteria _criteria = new();
    public event Action<BorradorReserva>? BorradorReservaSolicitado;

    public DisponibilidadControl()
    {
        InitializeComponent();
        _matrix.ReservaSolicitada += criteria => { SearchControl.FijarFechas(criteria.Llegada, criteria.Salida); Buscar(criteria); };
        _results.VolverSolicitado += () => ResultsHost.Content = _matrix;
        _results.HabitacionesSolicitadas += SeleccionarHabitaciones;
        ResultsHost.Content = _matrix;
    }

    public void Recargar() { _matrix.Recargar(); if (ResultsHost.Content == _results) Buscar(_criteria); }
    public void BuscarUnaNoche(DateTime fecha)
    {
        _criteria = new() { Llegada = fecha.Date, Salida = fecha.Date.AddDays(1), Personas = 1, Habitaciones = 1 };
        SearchControl.FijarFechas(_criteria.Llegada, _criteria.Salida);
        Buscar(_criteria);
    }
    private void SearchControl_BuscarSolicitado(ReservationSearchCriteria criteria) => Buscar(criteria);
    private void SearchControl_LimpiarSolicitado() { ResultsHost.Content = _matrix; _matrix.Recargar(DateTime.Today, 7); }
    private void Buscar(ReservationSearchCriteria criteria)
    {
        try { _criteria = criteria; var found = _service.BuscarTipos(criteria); _results.Mostrar(criteria, found); ResultsHost.Content = _results; }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo consultar disponibilidad", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void SeleccionarHabitaciones(TipoDisponibilidadModel type)
    {
        var criteria = new ReservationSearchCriteria { Llegada = _criteria.Llegada, Salida = _criteria.Salida, Personas = _criteria.Personas, Habitaciones = _criteria.Habitaciones, TipoHabitacion = type.Tipo, Comodidades = _criteria.Comodidades };
        var draft = new BorradorReserva { Llegada = criteria.Llegada, Salida = criteria.Salida, Personas = criteria.Personas, HabitacionesRequeridas = criteria.Habitaciones, TipoPreferido = type.Tipo };
        draft.Comodidades.UnionWith(criteria.Comodidades);
        try
        {
            var candidates = _service.BuscarHabitaciones(criteria, type.Tipo);
            var dialog = new SeleccionHabitacionesDialog(criteria, candidates) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;
            draft.HabitacionesSeleccionadas.AddRange(dialog.HabitacionesSeleccionadas);
            _service.ValidarSeleccion(criteria, draft.HabitacionesSeleccionadas);
            BorradorReservaSolicitado?.Invoke(draft);
        }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "Revisa las habitaciones", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
