using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class LlavesEstanciaControl : UserControl
{
    private int _stayId;
    private readonly LlaveService _service = new();
    public event EventHandler? DatosActualizados;
    public LlavesEstanciaControl() => InitializeComponent();
    public void SetReadOnly(bool readOnly) { IssueButton.IsEnabled = !readOnly; RevokeButton.IsEnabled = !readOnly; }
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var keys = _service.ListarPorEstancia(stayId);
        KeysGrid.ItemsSource = keys;
        Metrics.ItemsSource = new[]
        {
            new { Title = "Llaves activas", Value = keys.Count(item => item.Estado == "Activa").ToString() },
            new { Title = "Devueltas", Value = keys.Count(item => item.Estado == "Devuelta").ToString() },
            new { Title = "Revocadas", Value = keys.Count(item => item.Estado == "Revocada").ToString() }
        };
        IssueButton.IsEnabled = Crown___Sovereign.Data.SampleData.Estancias.Any(item => item.Id == stayId && item.Estado == "Activa");
        Key_SelectionChanged(this, null!);
    }
    private void Issue_Click(object sender, RoutedEventArgs e) { try { _service.EmitirPorEstancia(_stayId); Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private void Key_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { var active = KeysGrid.SelectedItem is LlaveModel key && key.Estado == "Activa"; ReturnButton.IsEnabled = active; RevokeButton.IsEnabled = active; }
    private void Return_Click(object sender, RoutedEventArgs e) { if (KeysGrid.SelectedItem is LlaveModel key) { try { _service.RegistrarDevolucion(key.Id); Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message); } } }
    private void Revoke_Click(object sender, RoutedEventArgs e) { if (KeysGrid.SelectedItem is LlaveModel key) { try { _service.Revocar(key.Id); Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message); } } }
}
