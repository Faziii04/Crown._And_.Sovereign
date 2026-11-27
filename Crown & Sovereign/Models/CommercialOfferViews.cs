using Crown___Sovereign.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Crown___Sovereign.Models;

public sealed record ConvenioFilaView(ConvenioModel Convenio, string EstadoVigencia, int DiasParaVencer, int ServiciosActivos,
    string ServiciosResumen, int Utilizaciones, decimal? CostosRegistrados, int SolicitudesPendientes, bool RequiereRevision, string Advertencia)
{
    public string Iniciales => string.Join("", Convenio.Empresa.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => word[0])).ToUpperInvariant();
    public string Periodo => $"{Convenio.VigenciaDesde:dd/MM/yyyy} – {Convenio.VigenciaHasta:dd/MM/yyyy}";
    public string MensajeVigencia => EstadoVigencia switch { "Por vencer" => $"Vence en {Math.Max(0, DiasParaVencer)} días", "Por iniciar" => "Vigencia futura", "Vencido" => "Vigencia vencida", "Inactivo" => "Desactivado", _ => "Convenio vigente" };
    public string CostoTexto => CostosRegistrados is decimal amount ? $"Bs {amount:N2} registrados" : "Sin costos históricos";
}
public sealed record ConvenioServicioVista(ConvenioServicioModel Relacion, ConvenioModel Convenio, ServicioModel Servicio, decimal PrecioOficial,
    decimal MargenEstimado, decimal? PorcentajeMargen, string EstadoMargen)
{ public string EmpresaConvenio => $"{Convenio.Empresa} · {Convenio.Nombre}"; public string Vigencia => $"{Convenio.VigenciaDesde:dd/MM/yyyy}–{Convenio.VigenciaHasta:dd/MM/yyyy}"; }
public sealed record ConvenioUsoFila(DateTime Fecha, string Servicio, string Huesped, string Proveedor, string Estado,
    string Cobertura, decimal? PrecioAplicado, decimal? CostoProveedorAplicado, decimal? MargenEstimado, int? ReservaId)
{
    public string PrecioTexto => PrecioAplicado is decimal amount ? $"Bs {amount:N2}" : "No registrado";
    public string CostoTexto => CostoProveedorAplicado is decimal amount ? $"Bs {amount:N2}" : "No registrado";
    public string MargenTexto => MargenEstimado is decimal amount ? $"Bs {amount:N2}" : "No calculable";
}
public sealed record ConvenioIndicadores(int Vigentes, int ServiciosExternos, int ProximosVencimientos, int RequierenRevision);
public sealed class ConvenioServicioEdicion : INotifyPropertyChanged
{
    private bool _seleccionado;
    private decimal _costo;
    private string _observaciones = "";
    public int ServicioId { get; init; }
    public string Nombre { get; init; } = "";
    public string Categoria { get; init; } = "";
    public decimal PrecioOficial { get; init; }
    public bool Seleccionado { get => _seleccionado; set { if (_seleccionado != value) { _seleccionado = value; OnPropertyChanged(); } } }
    public decimal Costo { get => _costo; set { if (_costo != value) { _costo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Margen)); OnPropertyChanged(nameof(PorcentajeMargen)); OnPropertyChanged(nameof(EstadoMargen)); } } }
    public string Observaciones { get => _observaciones; set { if (_observaciones != value) { _observaciones = value; OnPropertyChanged(); } } }
    public decimal Margen => PrecioOficial - Costo;
    public decimal? PorcentajeMargen => PrecioOficial > 0 ? decimal.Round(Margen / PrecioOficial * 100m, 2) : null;
    public string EstadoMargen => Margen < 0 ? "Costo supera precio" : Margen == 0 ? "Sin margen" : "Positivo";
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed record PaqueteTarjetaView(PaqueteModel Paquete, string Beneficios, int CantidadBeneficios, string ImagenUrl, string EstadoPersistencia)
{ public string PrecioTexto => $"Bs {Paquete.Precio:N2}"; }
public sealed record ServicioPaqueteOpcion(int Id, string Nombre, string Ruta, bool EsExterno, string Proveedor, bool YaIncluido)
{ public bool DisponibleParaAgregar => !YaIncluido; }
public sealed class BeneficioPaqueteEdicion : INotifyPropertyChanged
{
    public int ServicioId { get; set; }
    public string Servicio { get; set; } = "";
    private string _cobertura = "Por estancia";
    private int? _limiteUsos = 1;
    private bool _compartido = true;
    public string Cobertura { get => _cobertura; set { if (_cobertura == value) return; _cobertura = value; OnPropertyChanged(); OnPropertyChanged(nameof(HabilitaLimite)); } }
    public int? LimiteUsos { get => _limiteUsos; set { if (_limiteUsos == value) return; _limiteUsos = value; OnPropertyChanged(); } }
    public bool HabilitaLimite => Cobertura != "Ilimitado";
    public bool Compartido { get => _compartido; set { if (_compartido == value) return; _compartido = value; OnPropertyChanged(); } }
    public string Proveedor { get; set; } = "Interno";
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
}
public sealed record OfertaEscenarioView(string Nombre, decimal Alojamiento, decimal Paquetes, decimal Recursos, decimal Servicios, decimal Descuento, decimal Total);
public sealed record PromocionTarjetaView(PromocionModel Promocion, string EstadoVista, string ImporteTexto, string Periodo, string Alcance, bool EsPrototipo, bool PuedeEditar, string AccionEstado);
public sealed record MesCampaniaView(string Mes, string Resumen, double Intensidad);
public sealed class SimuladorLinea : INotifyPropertyChanged
{
    private bool _seleccionado;
    private int _cantidad = 1;
    public int Id { get; init; }
    public string Nombre { get; init; } = "";
    public string Categoria { get; init; } = "";
    public decimal Precio { get; init; }
    public string Modalidad { get; init; } = "";
    public bool Seleccionado { get => _seleccionado; set { if (_seleccionado != value) { _seleccionado = value; OnPropertyChanged(); } } }
    public int Cantidad { get => _cantidad; set { var next = Math.Clamp(value, 1, 20); if (_cantidad != next) { _cantidad = next; OnPropertyChanged(); } } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
