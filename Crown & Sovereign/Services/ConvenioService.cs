using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Commercial provider agreements and service terms. All estimated contribution is revenue less recorded provider cost, never net profit.</summary>
public sealed class ConvenioService
{
    public IReadOnlyList<ConvenioFilaView> Listar(string busqueda = "", string? categoria = null, string? vigencia = null, bool soloConServicios = false, bool soloRequierenRevision = false)
    {
        var today = DateTime.Today;
        return SampleData.Convenios
            .Select(item => Resumen(item, today))
            .Where(row => string.IsNullOrWhiteSpace(busqueda) || $"{row.Convenio.Empresa} {row.Convenio.Nombre} {row.Convenio.Categoria} {row.ServiciosResumen} {row.Convenio.Contacto}".Contains(busqueda.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(row => string.IsNullOrWhiteSpace(categoria) || categoria == "Todos los proveedores" || row.Convenio.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            .Where(row => string.IsNullOrWhiteSpace(vigencia) || vigencia == "Todos" || row.EstadoVigencia == vigencia)
            .Where(row => !soloConServicios || row.ServiciosActivos > 0)
            .Where(row => !soloRequierenRevision || row.RequiereRevision)
            .OrderBy(row => row.EstadoVigencia == "Por vencer" ? 0 : row.EstadoVigencia == "Vigente" ? 1 : 2)
            .ThenBy(row => row.Convenio.VigenciaHasta).ThenBy(row => row.Convenio.Empresa).ToList();
    }

    public ConvenioIndicadores Indicadores()
    {
        var today = DateTime.Today;
        var current = SampleData.Convenios.Where(item => EsVigente(item, today)).ToList();
        var serviceIds = SampleData.ConveniosServicios.Where(link => link.Activo && current.Any(item => item.Id == link.ConvenioId))
            .Where(link => SampleData.Servicios.Any(service => service.Id == link.ServicioId && service.Activo && service.Reservable))
            .Select(item => item.ServicioId).Distinct().Count();
        var expiring = SampleData.Convenios.Count(item => item.Activo && item.VigenciaDesde.Date <= today && item.VigenciaHasta.Date >= today && item.VigenciaHasta.Date <= today.AddDays(90));
        var linkedServiceIds = SampleData.ConveniosServicios.Select(link => link.ServicioId).Distinct();
        var unservedExternal = linkedServiceIds.Where(id => SampleData.Servicios.Any(service => service.Id == id && service.Activo && service.Reservable))
            .Count(id => CompararProveedores(id, today).Count == 0);
        var issues = SampleData.Convenios.Count(item => item.Activo && (item.VigenciaHasta.Date < item.VigenciaDesde.Date || !SampleData.ConveniosServicios.Any(link => link.ConvenioId == item.Id && link.Activo)))
            + SampleData.ConveniosServicios.Count(link => link.Activo && SampleData.Servicios.FirstOrDefault(service => service.Id == link.ServicioId) is { } service && link.CostoProveedor > service.Precio)
            + unservedExternal;
        return new(current.Count, serviceIds, expiring, issues);
    }

    public IReadOnlyList<string> Categorias() => new[] { "Todos los proveedores" }.Concat(SampleData.Convenios.Select(item => item.Categoria).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item)).ToList();

    public bool EsExterno(int servicioId)
    {
        var service = SampleData.Servicios.FirstOrDefault(item => item.Id == servicioId) ?? throw new InvalidOperationException("No se encontró el servicio.");
        return SampleData.ConveniosServicios.Any(item => item.ServicioId == servicioId && item.Activo);
    }

    public IReadOnlyList<ConvenioServicioVista> CompararProveedores(int servicioId, DateTime? fecha = null)
    {
        var date = (fecha ?? DateTime.Today).Date;
        var service = SampleData.Servicios.FirstOrDefault(item => item.Id == servicioId) ?? throw new InvalidOperationException("No se encontró el servicio.");
        if (!service.Activo || !service.Reservable) return [];
        return SampleData.ConveniosServicios.Where(link => link.ServicioId == servicioId && link.Activo)
            .Select(link => (Link: link, Agreement: SampleData.Convenios.FirstOrDefault(item => item.Id == link.ConvenioId)))
            .Where(pair => pair.Agreement is not null && EsVigente(pair.Agreement, date))
            .Select(pair => CrearVista(pair.Link, service, pair.Agreement!))
            .OrderBy(item => item.Convenio.Empresa).ToList();
    }

    public IReadOnlyList<ConvenioServicioVista> ServiciosDeConvenio(int convenioId, bool incluirInactivos = false)
    {
        AsegurarConvenio(convenioId);
        return SampleData.ConveniosServicios.Where(link => link.ConvenioId == convenioId && (incluirInactivos || link.Activo))
            .Select(link => (Link: link, Service: SampleData.Servicios.FirstOrDefault(item => item.Id == link.ServicioId)))
            .Where(pair => pair.Service is not null).Select(pair => CrearVista(pair.Link, pair.Service!, AsegurarConvenio(pair.Link.ConvenioId))).OrderBy(item => item.Servicio.Categoria).ThenBy(item => item.Servicio.Nombre).ToList();
    }

    public ConvenioModel Guardar(int? id, string nombre, string empresa, string categoria, DateTime desde, DateTime hasta, bool activo,
        string descripcion, string contacto, string telefono, string email, string direccion, string condiciones, string logoUrl,
        IReadOnlyCollection<(int ServicioId, decimal Costo, string Observaciones, bool Activo)> servicios)
    {
        Autorizar();
        ValidarConvenio(id, nombre, empresa, categoria, desde, hasta, email, logoUrl, servicios);
        var current = id is int existing ? AsegurarConvenio(existing) : null;
        if (id is int agreementId && SampleData.ConveniosServicios.Where(item => item.ConvenioId == agreementId).GroupBy(item => item.ServicioId).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Hay asociaciones convenio-servicio duplicadas. Corrige los datos antes de editar este convenio.");
        var saved = new ConvenioModel(id ?? NuevoId(SampleData.Convenios.Select(item => item.Id)), empresa.Trim(), categoria.Trim(), desde.Date, hasta.Date, activo,
            nombre.Trim(), descripcion.Trim(), contacto.Trim(), telefono.Trim(), email.Trim(), direccion.Trim(), condiciones.Trim(), logoUrl.Trim());
        if (current is null) SampleData.Convenios.Add(saved);
        else SampleData.Convenios[SampleData.Convenios.IndexOf(current)] = saved;
        foreach (var item in servicios)
        {
            var matches = SampleData.ConveniosServicios.Select((value, index) => (value, index)).Where(pair => pair.value.ConvenioId == saved.Id && pair.value.ServicioId == item.ServicioId).ToList();
            if (matches.Count > 1) throw new InvalidOperationException("Hay asociaciones repetidas para este servicio. Corrige los datos antes de guardar.");
            var link = new ConvenioServicioModel(saved.Id, item.ServicioId, item.Costo, item.Observaciones.Trim(), item.Activo);
            if (matches.Count == 0) SampleData.ConveniosServicios.Add(link); else SampleData.ConveniosServicios[matches[0].index] = link;
        }
        // Existing service links omitted from the edit are deactivated, never removed, to preserve their reference.
        var selectedIds = servicios.Select(item => item.ServicioId).ToHashSet();
        for (var i = 0; i < SampleData.ConveniosServicios.Count; i++)
        {
            var link = SampleData.ConveniosServicios[i];
            if (link.ConvenioId == saved.Id && !selectedIds.Contains(link.ServicioId) && link.Activo)
                SampleData.ConveniosServicios[i] = link with { Activo = false };
        }
        Registrar(id is null ? "Creación de convenio externo" : "Actualización de convenio externo", $"{saved.Empresa} · {saved.Nombre}");
        return saved;
    }

    public ConvenioModel Renovar(int originalId, DateTime desde, DateTime hasta)
    {
        Autorizar();
        var old = AsegurarConvenio(originalId);
        if (hasta.Date < desde.Date) throw new InvalidOperationException("La vigencia final no puede ser anterior a la inicial.");
        // Return a draft only; caller saves it as a new agreement after reviewing inherited service terms.
        return old with { Id = NuevoId(SampleData.Convenios.Select(item => item.Id)), Nombre = $"{old.Nombre} · Renovación", VigenciaDesde = desde.Date, VigenciaHasta = hasta.Date, Activo = true };
    }

    public void CambiarEstado(int id, bool activo)
    {
        Autorizar();
        var agreement = AsegurarConvenio(id);
        if (!activo)
        {
            var affected = SolicitudesPendientes(id);
            if (affected.Count > 0) throw new InvalidOperationException($"No se puede desactivar: hay {affected.Count} solicitud(es) pendiente(s) asignada(s) a este convenio. Cambia su proveedor o resuélvelas primero.");
        }
        SampleData.Convenios[SampleData.Convenios.IndexOf(agreement)] = agreement with { Activo = activo };
        Registrar(activo ? "Activación de convenio" : "Desactivación de convenio", $"{agreement.Empresa} · {agreement.Nombre}");
    }

    public void CambiarEstadoServicio(int convenioId, int servicioId, bool activo)
    {
        Autorizar(); AsegurarConvenio(convenioId);
        var matches = SampleData.ConveniosServicios.Select((item, index) => (item, index)).Where(pair => pair.item.ConvenioId == convenioId && pair.item.ServicioId == servicioId).ToList();
        if (matches.Count != 1) throw new InvalidOperationException("La asociación convenio-servicio no existe o está duplicada.");
        SampleData.ConveniosServicios[matches[0].index] = matches[0].item with { Activo = activo };
    }

    public IReadOnlyList<ConvenioUsoFila> Rendimiento(DateTime desde, DateTime hasta, int? convenioId = null)
    {
        if (hasta.Date < desde.Date) throw new InvalidOperationException("El periodo final no puede ser anterior al inicial.");
        var records = SampleData.ServiciosReserva.Select(item => (Record: item, At: item.FechaUtilizacion))
            .Where(pair => pair.Record.Estado == "Completado" && pair.At is DateTime at && at.Date >= desde.Date && at.Date <= hasta.Date && pair.Record.ConvenioId is not null)
            .Where(pair => convenioId is null || pair.Record.ConvenioId == convenioId)
            .Select(pair =>
            {
                var item = pair.Record;
                var at = pair.At!.Value;
                var service = SampleData.Servicios.FirstOrDefault(value => value.Id == item.ServicioId);
                var agreement = SampleData.Convenios.FirstOrDefault(value => value.Id == item.ConvenioId);
                var guest = SampleData.Huespedes.FirstOrDefault(value => value.Id == item.HuespedId);
                decimal? margin = item.PrecioAplicado is decimal revenue && item.CostoProveedorAplicado is decimal cost ? revenue - cost : null;
                var coverage = item.ServicioAdquiridoId.HasValue ? "Adquirido previamente" : item.Incluido ? "Incluido en paquete" : "Adicional";
                return new ConvenioUsoFila(at, service?.Nombre ?? "Servicio no disponible", guest?.Nombre ?? "Huésped no disponible", agreement?.Empresa ?? "Convenio no disponible", item.Estado,
                    coverage, item.PrecioAplicado, item.CostoProveedorAplicado, margin, item.ReservaId);
            }).ToList();
        return records.OrderByDescending(item => item.Fecha).ToList();
    }

    public int SolicitudesExternasPendientes(DateTime desde, DateTime hasta, int? convenioId = null)
    {
        if (hasta.Date < desde.Date) return 0;
        return SampleData.SolicitudesServicio.Count(request =>
        {
            var date = (request.FechaDeseada ?? request.Solicitada).Date;
            if (date < desde.Date || date > hasta.Date || request.Estado is "Completada" or "Cancelada" or "No presentado") return false;
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == request.ServicioId);
            if (service is null || !EsExterno(service.Id)) return false;
            if (convenioId is null) return true;
            if (request.ConvenioId == convenioId) return true;
            return CompararProveedores(service.Id, date).Any(item => item.Convenio.Id == convenioId);
        });
    }

    public IReadOnlyList<SolicitudServicioModel> SolicitudesPendientes(int convenioId)
    {
        var services = SampleData.ConveniosServicios.Where(item => item.ConvenioId == convenioId && item.Activo).Select(item => item.ServicioId).ToHashSet();
        return SampleData.SolicitudesServicio.Where(item => services.Contains(item.ServicioId) && item.Estado is "Solicitada" or "Pendiente" or "Confirmada" or "En atención" or "En curso")
            .Where(item => item.ConvenioId == convenioId).ToList();
    }

    public void Eliminar(int id)
    {
        Autorizar(); var agreement = AsegurarConvenio(id);
        if (SampleData.UsosServicio.Any(item => item.ConvenioId == id) || SampleData.SolicitudesServicio.Any(item => item.ConvenioId == id) || SampleData.ServiciosReserva.Any(item => item.ConvenioId == id))
            throw new InvalidOperationException("No se puede eliminar: el convenio conserva operaciones históricas. Desactívalo para impedir nuevas asignaciones.");
        if (SampleData.ConveniosServicios.Any(item => item.ConvenioId == id)) throw new InvalidOperationException("No se puede eliminar: el convenio conserva asociaciones de servicios. Desactívalo para nuevas operaciones.");
        SampleData.Convenios.Remove(agreement);
    }

    public static bool EsVigente(ConvenioModel item, DateTime fecha) => item.Activo && item.VigenciaDesde.Date <= fecha.Date && item.VigenciaHasta.Date >= fecha.Date;

    private ConvenioFilaView Resumen(ConvenioModel item, DateTime today)
    {
        var status = !item.Activo ? "Inactivo" : item.VigenciaHasta.Date < today ? "Vencido" : item.VigenciaDesde.Date > today ? "Por iniciar" : item.VigenciaHasta.Date <= today.AddDays(30) ? "Por vencer" : "Vigente";
        var relations = ServiciosDeConvenio(item.Id);
        var serviceLabels = relations.Take(3).Select(row => row.Servicio.Nombre).ToList();
        var uses = SampleData.ServiciosReserva.Where(usage => usage.ConvenioId == item.Id).ToList();
        var pending = SolicitudesPendientes(item.Id).Count;
        var warnings = new List<string>();
        if (item.Activo && relations.Count == 0) warnings.Add("Sin servicios asociados");
        if (relations.Any(row => row.EstadoMargen == "Margen negativo")) warnings.Add("Costo mayor al precio oficial");
        if (item.VigenciaHasta.Date < item.VigenciaDesde.Date) warnings.Add("Fechas inválidas");
        var reviews = warnings.Count > 0 || status == "Por vencer";
        return new ConvenioFilaView(item, status, (item.VigenciaHasta.Date - today).Days, relations.Count, string.Join(" · ", serviceLabels), uses.Count,
            uses.Count > 0 && uses.All(row => row.CostoProveedorAplicado.HasValue) ? uses.Sum(row => row.CostoProveedorAplicado!.Value) : null, pending, reviews, string.Join(" · ", warnings));
    }

    private static ConvenioServicioVista CrearVista(ConvenioServicioModel link, ServicioModel service, ConvenioModel agreement)
    {
        var margin = service.Precio - link.CostoProveedor;
        var percent = service.Precio > 0 ? (decimal?)decimal.Round(margin / service.Precio * 100m, 2) : null;
        return new(link, agreement, service, service.Precio, margin, percent, margin < 0 ? "Margen negativo" : margin == 0 ? "Sin margen" : "Margen positivo");
    }

    private static bool EsServicioFinal(ServicioModel service) => !SampleData.Servicios.Any(item => item.PadreId == service.Id);
    private static void ValidarConvenio(int? id, string name, string company, string category, DateTime from, DateTime to, string email, string logo,
        IReadOnlyCollection<(int ServicioId, decimal Costo, string Observaciones, bool Activo)> services)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(category)) throw new InvalidOperationException("Completa el nombre del convenio, la empresa y el tipo de proveedor.");
        if (to.Date < from.Date) throw new InvalidOperationException("La vigencia final no puede ser anterior a la inicial.");
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new InvalidOperationException("Escribe un correo de contacto válido.");
        if (!string.IsNullOrWhiteSpace(logo) && (!Uri.TryCreate(logo, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) throw new InvalidOperationException("El logo debe usar una URL http(s) completa.");
        if (services.Select(item => item.ServicioId).Distinct().Count() != services.Count) throw new InvalidOperationException("No repitas un servicio dentro del mismo convenio.");
        foreach (var row in services)
        {
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == row.ServicioId && item.Activo && item.Reservable && EsServicioFinal(item));
            if (service is null) throw new InvalidOperationException("Solo puedes asociar servicios finales activos y reservables del catálogo.");
            if (row.Costo < 0) throw new InvalidOperationException($"El costo de {service.Nombre} no puede ser negativo.");
        }
        if (SampleData.Convenios.Any(item => item.Id != id && item.Empresa.Equals(company.Trim(), StringComparison.OrdinalIgnoreCase) && item.Nombre.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe un convenio con ese nombre para esta empresa.");
    }

    private static ConvenioModel AsegurarConvenio(int id) => SampleData.Convenios.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("No se encontró el convenio.");
    private static int NuevoId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;
    private static void Autorizar() { if (!NavigationService.Puede("Convenios")) throw new UnauthorizedAccessException("Solo el Gerente puede administrar convenios de proveedores."); }
    private static void Registrar(string accion, string detalle) => SampleData.Auditoria.Insert(0, new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", accion, "—", detalle));
}
