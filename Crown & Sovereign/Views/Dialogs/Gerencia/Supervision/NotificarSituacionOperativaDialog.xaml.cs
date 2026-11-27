using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Supervision;

public partial class NotificarSituacionOperativaDialog : Window
{
    private readonly SupervisionAlerta _alert;
    private readonly string _module;
    private readonly SupervisionOperativaService _service = new();
    public NotificarSituacionOperativaDialog(SupervisionAlerta alert)
    {
        InitializeComponent(); _alert = alert; _module = ModuleFor(alert);
        SituationText.Text = $"{alert.Nivel} · {alert.Entidad}\n{alert.Titulo}\n{alert.Detalle}\nConsecuencia: {alert.Consecuencia}";
        RecipientText.Text = $"Encargado de servicios · {_module}";
        MessageBox.Text = $"Favor revisar esta situación operativa: {alert.Titulo}. {alert.Detalle}";
    }
    private static string ModuleFor(SupervisionAlerta alert) => alert.Seccion switch
    {
        "Habitaciones" => alert.Titulo.Contains("bloqueo", StringComparison.OrdinalIgnoreCase) ? "Mantenimiento" : "Limpieza",
        "Limpieza" => "Limpieza", "Mantenimiento" => "Mantenimiento", "Servicios" => "Agenda de servicios",
        "Inventario y recursos" => "Productos e inventario", _ => "Inicio"
    };
    private void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MessageBox.Text)) { ErrorText.Text = "Escribe un mensaje breve para el equipo."; return; }
        try { _service.NotificarSituacion(_alert, MessageBox.Text); DialogResult = true; }
        catch (Exception error) { ErrorText.Text = error.Message; }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
