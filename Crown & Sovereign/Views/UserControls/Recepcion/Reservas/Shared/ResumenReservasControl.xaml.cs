using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Shared;

public partial class ResumenReservasControl : UserControl
{
    public event Action<string>? NavegacionSolicitada;
    private sealed record SummaryMetric(string Clave,string Titulo,string Valor,string Contexto);
    public ResumenReservasControl(){InitializeComponent();Recargar();}
    public void Recargar()
    {
        var summary=new ReservaService().ObtenerResumenOperativo();
        MetricsList.ItemsSource=new[]
        {
            new SummaryMetric("Llegadas","Llegadas de hoy",summary.Llegadas.ToString(),$"{summary.CheckInsPendientes} pendientes de check-in"),
            new SummaryMetric("Salidas","Salidas de hoy",summary.Salidas.ToString(),$"{summary.CheckOutsPendientes} pendientes de check-out"),
            new SummaryMetric("Disponibilidad","Disponibles esta noche",summary.DisponiblesEstaNoche.ToString(),$"De {summary.TotalHabitaciones} habitaciones · estancia completa se valida aparte"),
            new SummaryMetric("Espera","Lista de espera",summary.EsperaActiva.ToString(),$"{summary.CoincidenciasEspera} con disponibilidad encontrada")
        };
    }
    private void Shortcut_Click(object sender,RoutedEventArgs e){if((sender as Button)?.Tag is string key)NavegacionSolicitada?.Invoke(key);}
}
