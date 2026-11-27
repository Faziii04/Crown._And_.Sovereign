using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Coordinates service catalog configuration and its schema-shaped relationships in shared prototype data.</summary>
public sealed class CatalogoServiciosService
{
    public IReadOnlyList<ServicioModel> Listar(string query = "", string estado = "Todos")
    {
        var text = query.Trim();
        return SampleData.Servicios.Where(service => estado switch
        {
            "Activos" => service.Activo,
            "Inactivos" => !service.Activo,
            "Reservables" => service.Activo && service.Reservable,
            "No reservables" => !service.Reservable,
            "Requieren cita" => service.Activo && service.Reservable && service.RequiereCita,
            "Con proveedores" => new ConvenioService().EsExterno(service.Id),
            "Incluidos en paquetes" => SampleData.PaquetesServicios.Any(link => link.ServicioId == service.Id),
            "Requieren revisión" => Revisar(service).Problemas.Count > 0,
            _ => true
        }).Where(service => text.Length == 0 || Coincide(service, text)).OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).ToList();
    }

    public IReadOnlyList<ServicioRevisionModel> Revisiones() => SampleData.Servicios.Select(Revisar).Where(item => item.Problemas.Count > 0).OrderBy(item => item.Servicio).ToList();
    public IReadOnlyList<ServicioRevisionModel> EstadoConfiguracion() => SampleData.Servicios.Select(item =>
    {
        var result = Revisar(item);
        return result with { Estado = !item.Activo ? result.Problemas.Count == 0 ? "Inactivo" : "Inactivo · requiere revisión" : result.Problemas.Count == 0 ? "Configuración revisada" : "Requiere revisión" };
    }).OrderBy(item => item.Estado).ThenBy(item => item.Servicio).ToList();
    public int TotalActivos => SampleData.Servicios.Count(item => item.Activo);
    public int TotalReservables => SampleData.Servicios.Count(item => item.Activo && item.Reservable);
    public int TotalConCita => SampleData.Servicios.Count(item => item.Activo && item.Reservable && item.RequiereCita);

    public IReadOnlyList<EmpleadoModel> EmpleadosDe(int servicioId) => SampleData.EmpleadosServicios.Where(link => link.ServicioId == servicioId && link.Activo)
        .Select(link => SampleData.Empleados.FirstOrDefault(employee => employee.Id == link.EmpleadoId)).Where(item => item is not null).Cast<EmpleadoModel>().ToList();
    public IReadOnlyList<ConvenioServicioVista> ProveedoresDe(int servicioId) => new ConvenioService().CompararProveedores(servicioId);
    public IReadOnlyList<(ServicioProductoModel Relacion, ProductoModel? Producto)> ProductosDe(int servicioId) => SampleData.ServiciosProductos.Where(link => link.ServicioId == servicioId)
        .Select(link => (link, SampleData.Productos.FirstOrDefault(product => product.Id == link.ProductoId))).ToList();
    public IReadOnlyList<(PaqueteModel Paquete, PaqueteServicioModel Beneficio)> PaquetesDe(int servicioId) => SampleData.PaquetesServicios.Where(link => link.ServicioId == servicioId)
        .Select(link => (SampleData.Paquetes.FirstOrDefault(package => package.Id == link.PaqueteId), link)).Where(pair => pair.Item1 is not null)
        .Select(pair => (pair.Item1!, pair.link)).ToList();
    public IReadOnlyList<ServicioImagenModel> ImagenesDe(int servicioId) => SampleData.ServiciosImagenes.Where(item => item.ServicioId == servicioId).OrderByDescending(item => item.EsPrincipal).ThenBy(item => item.Orden).ToList();

    public void Guardar(ServicioModel service, string nombre, string categoria, string descripcion, int? padreId, bool activo, bool reservable,
        bool requiereCita, int duracion, decimal precio)
    {
        AutorizarCatalogo();
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(categoria)) throw new InvalidOperationException("Completa el nombre y la categoría.");
        if (precio < 0 || duracion < 0 || (requiereCita && duracion < 15)) throw new InvalidOperationException("El precio no puede ser negativo; una cita requiere una duración de al menos 15 minutos.");
        if (service.PadreId != padreId) ValidarPadre(service.Id, padreId);
        var gerente = SesionActual.Usuario?.Rol == "Gerente";
        service.Nombre = nombre.Trim(); service.Categoria = categoria.Trim(); service.Descripcion = descripcion.Trim(); service.PadreId = padreId;
        if (gerente) { service.Activo = activo; service.Reservable = reservable; service.Precio = precio; }
        service.RequiereCita = requiereCita; service.DuracionMinutos = duracion;
        ComercialRepositorio.GuardarServicio(service);
    }

    public ServicioModel Crear(string nombre, string categoria, string descripcion, int? padreId, bool activo, bool reservable,
        bool requiereCita, int duracion, decimal precio)
    {
        AutorizarCatalogo(); ValidarPadre(null, padreId);
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(categoria)) throw new InvalidOperationException("Completa el nombre y la categoría.");
        if (precio < 0 || duracion < 0 || (requiereCita && duracion < 15)) throw new InvalidOperationException("Revisa el precio y la duración.");
        var manager = SesionActual.Usuario?.Rol == "Gerente";
        var service = new ServicioModel { Id = Persistencia.SiguienteId("servicio", "servicio_id", () => SampleData.Servicios.Count == 0 ? 1 : SampleData.Servicios.Max(item => item.Id) + 1),
            Nombre = nombre.Trim(), Categoria = categoria.Trim(), Descripcion = descripcion.Trim(), PadreId = padreId, Activo = manager ? activo : true,
            Reservable = manager ? reservable : true, RequiereCita = requiereCita, DuracionMinutos = duracion, Precio = manager ? precio : 0m };
        SampleData.Servicios.Add(service);
        ComercialRepositorio.GuardarServicio(service);
        return service;
    }

    public void ValidarPadre(int? servicioId, int? nuevoPadreId)
    {
        if (nuevoPadreId is null) return;
        if (!SampleData.Servicios.Any(item => item.Id == nuevoPadreId)) throw new InvalidOperationException("El servicio padre ya no existe.");
        if (servicioId == nuevoPadreId) throw new InvalidOperationException("Un servicio no puede ser su propio padre.");
        var vistos = new HashSet<int>(); var current = nuevoPadreId;
        while (current is int id)
        {
            if (!vistos.Add(id) || id == servicioId) throw new InvalidOperationException("Ese cambio crearía un ciclo en la jerarquía.");
            current = SampleData.Servicios.FirstOrDefault(item => item.Id == id)?.PadreId;
        }
    }

    public (int Solicitudes, int Citas, int BeneficiosAdquiridos, int UsosHistoricos) Dependencias(int servicioId) =>
        (SampleData.SolicitudesServicio.Count(item => item.ServicioId == servicioId && item.Estado is not ("Completada" or "Cancelada" or "No presentado")),
         SampleData.Citas.Count(item => item.ServicioId == servicioId && item.Estado is not ("Completada" or "Cancelada" or "No presentado")),
         SampleData.ServiciosAdquiridos.Count(item => item.ServicioId == servicioId && item.Estado == "ADQUIRIDO"),
         SampleData.UsosServicio.Count(item => item.ServicioId == servicioId) + SampleData.ServiciosReserva.Count(item => item.ServicioId == servicioId));

    public void CambiarActivo(ServicioModel service, bool activo)
    {
        if (SesionActual.Usuario?.Rol != "Gerente") throw new UnauthorizedAccessException("Solo el Gerente puede cambiar el estado administrativo del catálogo.");
        service.Activo = activo; // Parent state never cascades; historical and purchased rows remain intact.
        ComercialRepositorio.GuardarServicio(service);
    }

    public void AsociarEmpleado(int servicioId, int empleadoId, bool activo = true)
    {
        AutorizarOperativo();
        if (!SampleData.Servicios.Any(item => item.Id == servicioId) || !SampleData.Empleados.Any(item => item.Id == empleadoId)) throw new InvalidOperationException("El servicio o empleado ya no existe.");
        var index = SampleData.EmpleadosServicios.FindIndex(item => item.ServicioId == servicioId && item.EmpleadoId == empleadoId);
        if (index >= 0) SampleData.EmpleadosServicios[index] = SampleData.EmpleadosServicios[index] with { Activo = activo };
        else SampleData.EmpleadosServicios.Add(new(empleadoId, servicioId, activo));
        ComercialRepositorio.GuardarEmpleadoServicio(servicioId, empleadoId);
    }

    public void AsociarProducto(int servicioId, int productoId, int cantidad)
    {
        AutorizarOperativo();
        if (cantidad < 1) throw new InvalidOperationException("La cantidad incluida debe ser mayor que cero.");
        if (!SampleData.Servicios.Any(item => item.Id == servicioId) || !SampleData.Productos.Any(item => item.Id == productoId)) throw new InvalidOperationException("El servicio o producto ya no existe.");
        var index = SampleData.ServiciosProductos.FindIndex(item => item.ServicioId == servicioId && item.ProductoId == productoId);
        if (index >= 0) SampleData.ServiciosProductos[index] = SampleData.ServiciosProductos[index] with { CantidadIncluida = cantidad };
        else SampleData.ServiciosProductos.Add(new(servicioId, productoId, cantidad));
        ComercialRepositorio.GuardarServicioProducto(servicioId, productoId);
        // Configuration only: product stock is never consumed here.
    }

    public void QuitarProducto(int servicioId, int productoId) { AutorizarOperativo(); SampleData.ServiciosProductos.RemoveAll(item => item.ServicioId == servicioId && item.ProductoId == productoId); ComercialRepositorio.QuitarServicioProducto(servicioId, productoId); }
    public void GuardarImagen(int servicioId, string url, string descripcion, bool principal)
    {
        AutorizarImagenes();
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Indica una URL http(s) válida.");
        if (principal) for (var i = 0; i < SampleData.ServiciosImagenes.Count; i++) if (SampleData.ServiciosImagenes[i].ServicioId == servicioId && SampleData.ServiciosImagenes[i].EsPrincipal) SampleData.ServiciosImagenes[i] = SampleData.ServiciosImagenes[i] with { EsPrincipal = false };
        var id = Persistencia.SiguienteId("servicio_imagen", "imagen_id", () => SampleData.ServiciosImagenes.Count == 0 ? 1 : SampleData.ServiciosImagenes.Max(item => item.Id) + 1);
        SampleData.ServiciosImagenes.Add(new(id, servicioId, uri.ToString(), descripcion.Trim(), principal, SampleData.ServiciosImagenes.Count(item => item.ServicioId == servicioId)));
        ComercialRepositorio.SincronizarImagenesServicio(servicioId);
    }
    public void QuitarImagen(int id)
    {
        AutorizarImagenes();
        var servicioId = SampleData.ServiciosImagenes.FirstOrDefault(item => item.Id == id)?.ServicioId;
        SampleData.ServiciosImagenes.RemoveAll(item => item.Id == id);
        if (servicioId is int servicio) ComercialRepositorio.SincronizarImagenesServicio(servicio);
    }

    private ServicioRevisionModel Revisar(ServicioModel service)
    {
        var issues = new List<string>();
        if (service.Precio < 0) issues.Add("Precio oficial inválido.");
        if (service.RequiereCita && service.DuracionMinutos < 15) issues.Add("La cita no tiene duración válida.");
        if (service.Activo && service.Reservable && service.RequiereCita && !SampleData.EmpleadosServicios.Any(item => item.ServicioId == service.Id && item.Activo) && new ConvenioService().CompararProveedores(service.Id).Count == 0) issues.Add("Cita sin empleados elegibles ni convenio vigente asociado.");
        if (SampleData.ServiciosProductos.Any(link => link.ServicioId == service.Id && (!SampleData.Productos.Any(product => product.Id == link.ProductoId) || SampleData.Productos.First(product => product.Id == link.ProductoId).Stock <= 0))) issues.Add("Hay productos asociados sin existencias.");
        if (SampleData.PaquetesServicios.Any(link => link.ServicioId == service.Id) && (!service.Activo || !service.Reservable)) issues.Add("Incluido en un paquete, pero no está activo y reservable.");
        if (service.PadreId is int parent && !SampleData.Servicios.Any(item => item.Id == parent)) issues.Add("El servicio padre no existe.");
        try { ValidarPadre(service.Id, service.PadreId); } catch (InvalidOperationException) { issues.Add("La jerarquía contiene un ciclo."); }
        return new(service.Id, service.Nombre, service.Categoria, !service.Activo ? "Inactivo" : "Requiere revisión", issues);
    }

    private bool Coincide(ServicioModel service, string query)
    {
        var parent = SampleData.Servicios.FirstOrDefault(item => item.Id == service.PadreId)?.Nombre ?? "";
        var employees = EmpleadosDe(service.Id).Select(item => item.Nombre);
        var providers = ProveedoresDe(service.Id).Select(item => item.Convenio.Empresa);
        var products = ProductosDe(service.Id).Select(item => item.Producto?.Nombre ?? "");
        var packages = PaquetesDe(service.Id).Select(item => item.Paquete.Nombre);
        return new[] { service.Nombre, service.Categoria, service.Descripcion, parent }.Concat(employees).Concat(providers).Concat(products).Concat(packages).Any(item => item.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
    private static void AutorizarCatalogo() { if (SesionActual.Usuario?.Rol != "Gerente" && !NavigationService.Puede("Catálogo de servicios")) throw new UnauthorizedAccessException("Tu rol no puede editar el catálogo de servicios."); }
    private static void AutorizarOperativo() { if (SesionActual.Usuario?.Rol != "Gerente" && !NavigationService.Puede("Catálogo de servicios")) throw new UnauthorizedAccessException("Tu rol no puede editar asociaciones operativas."); }
    private static void AutorizarImagenes() { if (SesionActual.Usuario?.Rol != "Gerente") throw new UnauthorizedAccessException("Solo Gerencia puede administrar las imágenes comerciales del catálogo."); }
}
