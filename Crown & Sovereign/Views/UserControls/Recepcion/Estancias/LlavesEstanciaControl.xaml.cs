using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class LlavesEstanciaControl : UserControl
{
    private int _stayId;
    private readonly LlaveService _service = new();
    public LlavesEstanciaControl() => InitializeComponent();
    public void SetReadOnly(bool readOnly) { IssueButton.IsEnabled = !readOnly; RevokeButton.IsEnabled = !readOnly; }
    public void Mostrar(int stayId) { _stayId = stayId; KeysGrid.ItemsSource = _service.ListarPorEstancia(stayId); }
    private void Issue_Click(object sender, RoutedEventArgs e) { try { _service.EmitirPorEstancia(_stayId); Mostrar(_stayId); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private void Revoke_Click(object sender, RoutedEventArgs e) { if (KeysGrid.SelectedItem is LlaveModel key) { _service.Revocar(key.Id); Mostrar(_stayId); } }
}
