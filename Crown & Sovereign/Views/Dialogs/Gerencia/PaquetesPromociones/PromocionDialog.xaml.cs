using System.Globalization;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.PaquetesPromociones;

public partial class PromocionDialog : Window
{
    private readonly PromocionService _service = new();
    private readonly PromocionModel? _original;
    public PromocionDialog(PromocionModel? promocion = null)
    {
        InitializeComponent(); _original = promocion;
        if (promocion is not null)
        {
            Heading.Text = $"Editar · {promocion.Nombre}"; NameBox.Text = promocion.Nombre;
            PercentBox.Text = promocion.Valor.ToString("0.##", CultureInfo.CurrentCulture);
            StartDate.SelectedDate = promocion.Desde.Date; EndDate.SelectedDate = promocion.Hasta.Date; ActiveBox.IsChecked = promocion.Activa;
        }
        else { StartDate.SelectedDate = DateTime.Today; EndDate.SelectedDate = DateTime.Today.AddMonths(1); }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(PercentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)) throw new InvalidOperationException("Indica un porcentaje válido.");
            _service.Guardar(_original?.Id, NameBox.Text, value, StartDate.SelectedDate ?? throw new InvalidOperationException("Selecciona la fecha inicial."), EndDate.SelectedDate ?? throw new InvalidOperationException("Selecciona la fecha final."), ActiveBox.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
}
