using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Simulador;

/// <summary>Coordinates the simulation input, additional-purchase section and economic summary.</summary>
public partial class SimuladorComercialControl : UserControl
{
    private readonly TarifaService _pricing = new();
    private readonly RecursoAdicionalService _resourceService = new();
    public SimuladorComercialControl()
    {
        InitializeComponent();
        Datos.ConfiguracionCambiada += (_, _) => Recalcular();
        Datos.PaqueteCambiado += (_, _) => Adicionales.ConfigurarPaquete(Datos.Paquete?.Id);
        Adicionales.CambiosCambiados += (_, _) => Recalcular();
        Resumen.CompararSolicitado += (_, _) => CompararSinPaquete();
        Adicionales.ConfigurarPaquete(Datos.Paquete?.Id);
        Recalcular();
    }

    private BorradorReserva CrearBorrador(bool incluirPaquete = true)
    {
        var package = incluirPaquete ? Datos.Paquete : null;
        var draft = new BorradorReserva
        {
            Llegada = Datos.Llegada.Date, Salida = Datos.Salida.Date, Canal = Datos.Canal,
            PaqueteId = package?.Id, CantidadPaquete = package is null ? 0 : Datos.CantidadPaquetes,
            PrecioPaqueteAcordado = package?.Precio, PromocionId = Datos.Promocion?.Id
        };
        draft.HabitacionesSeleccionadas.AddRange(Datos.HabitacionesSeleccionadas);
        draft.ServiciosAdquiridos.AddRange(Adicionales.ServiciosSeleccionados.Select(item => new ServicioAdquiridoBorrador
        { ServicioId = item.Id, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.Precio }));
        draft.RecursosAdicionales.AddRange(Adicionales.RecursosSeleccionados.Select(item => new ReservaRecursoBorrador
        { RecursoId = item.Id, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.Precio, TipoCobroAplicado = item.Modalidad }));
        return draft;
    }

    private void Recalcular()
    {
        if (Datos.Salida.Date <= Datos.Llegada.Date) { Resumen.MostrarError("La salida debe ser posterior a la llegada."); return; }
        try
        {
            var draft = CrearBorrador(); var summary = _pricing.CalcularResumenEconomico(draft); var warnings = new List<string>();
            if (draft.HabitacionesSeleccionadas.Count == 0) warnings.Add("Selecciona al menos una habitación disponible para cotizar el alojamiento.");
            if (summary.PreciosPendientes) warnings.Add("Hay precios adicionales pendientes de verificar.");
            if (Adicionales.RecursosSeleccionados.Any(item => _resourceService.ConsultarDisponibilidad(item.Id, Datos.Llegada, Datos.Salida) is int available && item.Cantidad > available))
                warnings.Add("La cantidad de uno o más recursos supera su disponibilidad registrada.");
            var benefits = Datos.Paquete is null ? ["Selecciona un paquete para revisar sus beneficios."] : Adicionales.BeneficiosIncluidos.Count == 0 ? ["Este paquete aún no tiene beneficios configurados."] : Adicionales.BeneficiosIncluidos;
            Resumen.Mostrar(summary, string.Join(" ", warnings), benefits);
        }
        catch (Exception ex) { Resumen.MostrarError(ex.Message); }
    }

    private void CompararSinPaquete()
    {
        try
        {
            var withPackage = _pricing.CalcularResumenEconomico(CrearBorrador()).Total;
            var withoutPackage = _pricing.CalcularResumenEconomico(CrearBorrador(incluirPaquete: false)).Total;
            var delta = withPackage - withoutPackage;
            Resumen.MostrarComparacion(Datos.Paquete is null
                ? $"No se eligió paquete. Total de referencia: Bs {withoutPackage:N2}."
                : $"Con {Datos.Paquete.Nombre}: Bs {withPackage:N2} · sin paquete: Bs {withoutPackage:N2} · diferencia: + Bs {delta:N2}. Los beneficios se muestran por separado.");
        }
        catch (Exception ex) { Resumen.MostrarComparacion(ex.Message); }
    }
}
