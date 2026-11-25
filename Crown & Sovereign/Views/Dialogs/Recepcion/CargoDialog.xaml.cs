using System.Globalization;
using System.Windows;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class CargoDialog : Window
{
    private readonly int _id;
    private readonly bool _porEstancia;
    private readonly FolioService _folio = new();
    public CargoDialog(int id, bool porEstancia = false) { InitializeComponent(); _id = id; _porEstancia = porEstancia; }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConceptBox.Text) || !decimal.TryParse(AmountBox.Text, NumberStyles.Number, new CultureInfo("es-BO"), out var amount)) { ValidationText.Text = "Completa un concepto y un importe válido."; return; }
        try { if (_porEstancia) _folio.RegistrarCargoPorEstancia(ConceptBox.Text.Trim(), amount, _id); else _folio.RegistrarCargo(ConceptBox.Text.Trim(), amount, _id); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
