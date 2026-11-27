using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.PaquetesPromociones;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Paquetes;

public partial class PaquetesControl : UserControl
{
    private readonly PaqueteService _service = new();
    private int? _selectedId;
    public event EventHandler? CatalogoCambiado;
    public PaquetesControl()
    {
        InitializeComponent(); Catalog.PaqueteSeleccionado += package => { _selectedId = package?.Id; Details.Mostrar(package); };
        Catalog.FiltroCambiado += (_, _) => Filtrar(); Details.EditRequested += package => Editar(package); Details.DeleteRequested += Eliminar; Recargar();
    }
    public void Filtrar()
    {
        var items = ConstruirTarjetas().Where(item => item.Paquete.Nombre.Contains(Catalog.Filtro, StringComparison.OrdinalIgnoreCase) || item.Paquete.Descripcion.Contains(Catalog.Filtro, StringComparison.OrdinalIgnoreCase));
        items = Catalog.EstadoFiltro switch
        {
            "Disponibles" => items.Where(item => item.Paquete.Activo),
            "En revisión" => items.Where(item => !item.Paquete.Activo || item.CantidadBeneficios == 0 || SampleData.PaquetesServicios.Any(benefit => benefit.PaqueteId == item.Paquete.Id && !SampleData.Servicios.Any(service => service.Id == benefit.ServicioId && service.Activo && service.Reservable))),
            _ => items
        };
        Catalog.Recargar(items, _selectedId);
    }
    private List<PaqueteTarjetaView> ConstruirTarjetas() => _service.Listar().Select(package =>
    {
        var benefits = _service.Beneficios(package.Id);
        var names = benefits.Take(3).Select(item => SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Nombre ?? "Servicio no disponible").ToList();
        if (benefits.Count > 3) names.Add($"+{benefits.Count - 3} más");
        var warning = benefits.Count == 0 ? "Sin beneficios · configurar" : !package.Activo ? "Inactivo · solo prototipo" : "Activo solo en prototipo";
        return new PaqueteTarjetaView(package, benefits.Count == 0 ? "Sin beneficios configurados" : string.Join(" · ", names), benefits.Count, _service.Imagenes(package.Id).FirstOrDefault(item => item.EsPrincipal)?.Url ?? _service.Imagenes(package.Id).FirstOrDefault()?.Url ?? "", warning);
    }).ToList();
    public void Recargar()
    {
        var all = ConstruirTarjetas(); EmptyState.Visibility = all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Catalog.Visibility = all.Count == 0 ? Visibility.Collapsed : Visibility.Visible; Details.Visibility = all.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        Filtrar(); if (_selectedId is null && all.Count > 0) Catalog.Seleccionar(all[0].Paquete.Id);
        CatalogoCambiado?.Invoke(this, EventArgs.Empty);
    }
    private void New_Click(object sender, RoutedEventArgs e)
    {
        CrearNuevo();
    }
    public void CrearNuevo()
    { var dialog = new PaqueteDialog { Owner = Window.GetWindow(this) }; if (dialog.ShowDialog() == true) { _selectedId = SampleData.Paquetes.Last().Id; Recargar(); } }
    private void Editar(PaqueteModel package)
    {
        var dialog = new PaqueteDialog(package) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { _selectedId = package.Id; Recargar(); }
    }
    private void Eliminar(PaqueteModel package)
    {
        if (MessageBox.Show(Window.GetWindow(this), $"¿Eliminar «{package.Nombre}»? Esta acción solo se permite si ningún huésped lo ha comprado.", "Eliminar paquete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _service.Eliminar(package.Id); _selectedId = null; Recargar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
