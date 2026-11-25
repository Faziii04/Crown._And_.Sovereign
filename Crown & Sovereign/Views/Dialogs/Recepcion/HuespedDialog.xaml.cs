using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class HuespedDialog : Window
{
    private readonly HuespedService _service = new();
    private readonly HuespedModel? _guest;
    private HuespedModel? _duplicate;
    public int? HuespedGuardado { get; private set; }
    public HuespedDialog(HuespedModel? guest = null)
    {
        InitializeComponent();
        _guest = guest;
        if (guest is not null) DialogTitle.Text = $"Editar perfil · {guest.Nombre}";
        NameBox.Text = guest?.Nombre ?? ""; DocumentBox.Text = guest?.Documento ?? ""; EmailBox.Text = guest?.Email ?? ""; PhoneBox.Text = guest?.Telefono ?? ""; ConsentCheck.IsChecked = guest?.ConsentimientoDatos ?? true;
        DocumentBox.IsReadOnly = guest is not null;
        FindDocumentButton.Visibility = guest is null ? Visibility.Visible : Visibility.Collapsed;
        ConsentCheck.Visibility = guest is null ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_guest is null) { var created = _service.Crear(NameBox.Text, DocumentBox.Text, EmailBox.Text, PhoneBox.Text); created.ConsentimientoDatos = ConsentCheck.IsChecked == true; HuespedGuardado = created.Id; }
            else { _service.Actualizar(_guest, NameBox.Text, EmailBox.Text, PhoneBox.Text); HuespedGuardado = _guest.Id; }
            DialogResult = true;
        }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
    private void FindDocument_Click(object sender, RoutedEventArgs e)
    {
        _duplicate = _service.BuscarPorDocumento(DocumentBox.Text).FirstOrDefault();
        if (_duplicate is null) { DuplicateText.Text = "No hay un huésped registrado con este documento."; UseExistingButton.Visibility = Visibility.Collapsed; return; }
        DuplicateText.Text = $"Coincidencia encontrada: {_duplicate.Nombre} · {_duplicate.Email} · {_duplicate.Telefono}";
        UseExistingButton.Visibility = Visibility.Visible;
    }
    private void UseExisting_Click(object sender, RoutedEventArgs e) { if (_duplicate is null) return; HuespedGuardado = _duplicate.Id; DialogResult = true; }
}
