using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Paquetes;

public partial class DetallePaqueteControl : UserControl
{
    private PaqueteModel? _package;
    public event Action<PaqueteModel>? EditRequested;
    public event Action<PaqueteModel>? DeleteRequested;
    public DetallePaqueteControl() => InitializeComponent();
    public void Mostrar(PaqueteModel? package)
    {
        _package = package;
        if (package is null) { Heading.Text = "Selecciona un paquete"; Description.Text = "Consulta beneficios, precios e imágenes del catálogo comercial."; BenefitsList.ItemsSource = null; ImagesList.ItemsSource = null; return; }
        Heading.Text = package.Nombre; Description.Text = package.Descripcion; Price.Text = $"Bs {package.Precio:N2}";
        var benefits = SampleData.PaquetesServicios.Where(item => item.PaqueteId == package.Id).Select(item =>
        {
            var service = SampleData.Servicios.FirstOrDefault(value => value.Id == item.ServicioId);
            var products = SampleData.ServiciosProductos.Where(value => value.ServicioId == item.ServicioId)
                .Select(value => SampleData.Productos.FirstOrDefault(product => product.Id == value.ProductoId) is { } product ? $"{product.Nombre} × {value.CantidadIncluida}" : "")
                .Where(value => value.Length > 0).ToList();
            var providers = SampleData.ConveniosServicios.Where(link => link.ServicioId == item.ServicioId && link.Activo).Select(link => SampleData.Convenios.FirstOrDefault(agreement => agreement.Id == link.ConvenioId)?.Empresa).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
            var scope = item.Compartido ? "compartido por reserva (prototipo)" : "por huésped";
            return new { Servicio = service?.Nombre ?? $"Servicio #{item.ServicioId}", Cobertura = item.TipoCobertura, UsosTexto = item.TipoCobertura == "Ilimitado" ? "Usos sin límite por estancia" : $"{item.LimiteUsos} uso(s) {(item.TipoCobertura == "Por día" ? "por día" : "por estancia")} · {scope}", Productos = products.Count > 0 ? "Productos incluidos: " + string.Join(" · ", products) : "", Proveedor = providers.Count > 0 ? $"Convenio(s): {string.Join(" · ", providers)}; validar disponibilidad antes de confirmar" : "" };
        }).ToList();
        BenefitsList.ItemsSource = benefits;
        var images = SampleData.PaquetesImagenes.Where(item => item.PaqueteId == package.Id).OrderBy(item => item.Orden).ToList();
        ImagesList.ItemsSource = images; BenefitCount.Text = benefits.Count.ToString();
        var reservations = SampleData.ReservaPaquetes.Where(item => item.PaqueteId == package.Id).ToList();
        ReservationCount.Text = reservations.Select(item => item.ReservaId).Distinct().Count().ToString();
        var qty = reservations.Sum(item => item.Cantidad);
        PerformanceText.Text = $"{reservations.Select(item => item.ReservaId).Distinct().Count()} reserva(s) asociada(s). {qty} unidad(es) de paquete registradas. El catálogo actual cuesta Bs {package.Precio:N2}; no se usa para reconstruir ingresos pasados.";
        var url = images.FirstOrDefault(item => item.EsPrincipal)?.Url ?? images.FirstOrDefault()?.Url;
        PackageImage.Source = null; ImagePlaceholder.Visibility = Visibility.Visible;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            try { PackageImage.Source = new BitmapImage(uri); ImagePlaceholder.Visibility = Visibility.Collapsed; }
            catch { PackageImage.Source = null; }
        }
    }
    private void Edit_Click(object sender, RoutedEventArgs e) { if (_package is not null) EditRequested?.Invoke(_package); }
    private void Delete_Click(object sender, RoutedEventArgs e) { if (_package is not null) DeleteRequested?.Invoke(_package); }
}
