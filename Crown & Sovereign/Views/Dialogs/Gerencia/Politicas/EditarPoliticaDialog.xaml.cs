using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

public partial class EditarPoliticaDialog : Window
{
    private readonly DefinicionPolitica _definition;
    private readonly string _current;
    private readonly PoliticaHotelService _service = new();
    private bool _confirming;
    public string ValorPropuesto { get; private set; } = "";
    public EditarPoliticaDialog(DefinicionPolitica definition, string current)
    {
        InitializeComponent(); _definition = definition; _current = current;
        TitleText.Text = definition.Nombre; DescriptionText.Text = definition.Descripcion;
        CurrentText.Text = current; RuleText.Text = definition.Flujo;
        if (definition.TipoEditor == "mascotas")
        {
            ValueBox.Visibility = Visibility.Collapsed; PetBox.Visibility = Visibility.Visible;
            PetBox.ItemsSource = new[] { "No se admiten mascotas.", "Se admiten mascotas.", "Admisión sujeta a condiciones." };
            if (PetBox.Items.Contains(current)) PetBox.SelectedItem = current;
        }
        else ValueBox.Text = current == "Sin configurar" ? "" : current.Replace(" horas", "", StringComparison.OrdinalIgnoreCase);
        if (definition.TipoEditor == "hora") ValueBox.ToolTip = "Formato 24 h, por ejemplo 14:30.";
        if (definition.TipoEditor == "horas") ValueBox.ToolTip = "Número entero de horas; debe ser 0 o mayor.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var proposed = _definition.TipoEditor == "mascotas" ? PetBox.SelectedItem as string ?? "" : ValueBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(proposed)) { ErrorText.Text = "Ingresa un valor válido antes de continuar."; return; }
        if (!_confirming)
        {
            try
            {
                var normalized = NormalizeForReview(proposed);
                ValorPropuesto = normalized;
                ErrorText.Text = $"Cambio propuesto: {_current} → {Display(normalized)}.\nAplicación: {_definition.Flujo}\nConfirma para guardar y registrar el cambio.";
                _confirming = true; SaveButton.Content = "Confirmar y guardar";
                ValueBox.IsReadOnly = true; PetBox.IsEnabled = false;
            }
            catch (Exception ex) { ErrorText.Text = ex.Message; }
            return;
        }
        try
        {
            var saved = _service.Guardar(_definition.Codigo, ValorPropuesto);
            ValorPropuesto = saved.Valor; DialogResult = true;
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
    }
    private string NormalizeForReview(string value)
    {
        if (_definition.TipoEditor == "hora" && !TimeOnly.TryParse(value, out _)) throw new InvalidOperationException("Usa un horario válido, por ejemplo 14:30.");
        if (_definition.TipoEditor == "horas" && (!int.TryParse(value.Replace(" horas", "", StringComparison.OrdinalIgnoreCase), out var n) || n < 0)) throw new InvalidOperationException("Escribe un número entero de horas igual o mayor que cero.");
        return value;
    }
    private static string Display(string value) => int.TryParse(value, out var hours) ? $"{hours} horas" : value;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
