using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class DetalleTurnoCajaDialog : Window
{
    public DetalleTurnoCajaDialog(int turnoId)
    {
        InitializeComponent();
        var cash = new CajaService(); var summary = cash.ObtenerResumenTurno(turnoId); var shift = summary.Turno; var closure = cash.ObtenerCierre(turnoId);
        TitleText.Text = $"Detalle del turno #{shift.Id:0000}"; SubtitleText.Text = $"{SampleData.Usuarios.FirstOrDefault(item => item.Id == shift.UsuarioId)?.Nombre ?? "—"} · {shift.Apertura:dd/MM/yyyy HH:mm} · {(shift.Cierre is DateTime close ? close.ToString("dd/MM/yyyy HH:mm") : "Abierto")}";
        OpeningText.Text = $"Bs {shift.MontoInicial:N2}"; ExpectedText.Text = $"Bs {(closure?.Esperado ?? summary.EfectivoEsperado):N2}"; CountedText.Text = closure is null ? "Pendiente" : $"Bs {closure.Contado:N2}"; DifferenceText.Text = closure is null ? "—" : $"Bs {closure.Diferencia:N2}";
        MovementsGrid.ItemsSource = cash.ListarMovimientos(turnoId);
    }
}
