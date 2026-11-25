using System.Globalization;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class SolicitudCorreccionPagoDialog : Window
{
    private readonly PaymentDirectoryRow _row;
    private readonly string _type;
    private readonly CorreccionFinancieraService _service = new();
    public SolicitudCorreccionPagoDialog(PaymentDirectoryRow row, string type)
    {
        InitializeComponent(); _row = row; _type = type;
        TitleText.Text = $"Solicitar {type.ToLowerInvariant()}"; SubtitleText.Text = $"Pago #{row.Id:0000} · {row.Huesped} · Bs {row.Monto:N2}";
        ReasonBox.ItemsSource = type == "Devolución" ? new[] { "Cobro duplicado", "Servicio cancelado", "Ajuste autorizado", "Error de importe", "Otro" } : new[] { "Pago duplicado", "Error de registro", "Pago asociado incorrectamente", "Otro" };
        ReasonBox.SelectedIndex = 0; AmountBox.Text = (row.Monto - row.Reembolsado).ToString("N2", CultureInfo.GetCultureInfo("es-BO")); AmountPanel.Visibility = type == "Devolución" ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionBox.Text)) { ErrorText.Text = "Describe el motivo para dejar un registro auditable."; return; }
        try
        {
            var reason = ReasonBox.SelectedItem?.ToString() ?? "Otro";
            if (_type == "Devolución")
            {
                if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("es-BO"), out var amount) && !decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)) { ErrorText.Text = "Ingresa un importe válido."; return; }
                _service.SolicitarDevolucion(_row.Id, amount, reason, DescriptionBox.Text.Trim());
            }
            else _service.SolicitarAnulacion(_row.Id, reason, DescriptionBox.Text.Trim());
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
