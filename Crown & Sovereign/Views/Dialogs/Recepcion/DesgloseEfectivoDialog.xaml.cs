using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class DesgloseEfectivoDialog : Window
{
    public DesgloseEfectivoDialog(CashDeskShiftSummary summary)
    {
        InitializeComponent(); ShiftText.Text = $"Turno #{summary.Turno.Id:0000} · {summary.Turno.Apertura:dd/MM/yyyy HH:mm}";
        var rows = new CajaService().ListarMovimientos(summary.Turno.Id).Where(item => item.Tipo is "Cobro" or "Devolución" && (SampleData.MetodosPago.FirstOrDefault(m => m.Nombre == item.Metodo)?.EsEfectivo ?? false)).ToList();
        MovementsGrid.ItemsSource = rows; TotalText.Text = $"Saldo esperado en efectivo · Bs {summary.EfectivoEsperado:N2}";
    }
}
