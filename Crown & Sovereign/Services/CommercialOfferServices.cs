using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Commercial package catalog. Catalog edits are future-facing; reservation snapshots remain independent.</summary>
public sealed class PaqueteService
{
    public IReadOnlyList<PaqueteModel> Listar(string filtro = "") => SampleData.Paquetes
        .Where(item => string.IsNullOrWhiteSpace(filtro) || $"{item.Nombre} {item.Descripcion}".Contains(filtro.Trim(), StringComparison.OrdinalIgnoreCase))
        .OrderBy(item => item.Nombre).ToList();

    public IReadOnlyList<PaqueteServicioModel> Beneficios(int paqueteId) => SampleData.PaquetesServicios.Where(item => item.PaqueteId == paqueteId).ToList();
    public IReadOnlyList<PaqueteImagenModel> Imagenes(int paqueteId) => SampleData.PaquetesImagenes.Where(item => item.PaqueteId == paqueteId).OrderBy(item => item.Orden).ToList();

    public PaqueteModel Guardar(int? id, string nombre, string descripcion, decimal precio, bool activo,
        IReadOnlyCollection<PaqueteServicioModel> beneficios, IReadOnlyCollection<string> urlsImagen)
    {
        Autorizar(); Validar(nombre, descripcion, precio, beneficios);
        if (SampleData.Paquetes.Any(item => item.Id != id && item.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe un paquete con ese nombre.");
        var images = urlsImagen.Select((url, index) => (Url: url.Trim(), Index: index)).Where(item => item.Url.Length > 0).ToList();
        if (images.Any(item => !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            throw new InvalidOperationException("Cada imagen debe tener una URL completa que empiece por http:// o https://.");
        var item = id is int packageId
            ? SampleData.Paquetes.FirstOrDefault(existing => existing.Id == packageId) ?? throw new InvalidOperationException("No se encontró el paquete.")
            : null;
        var saved = new PaqueteModel(id ?? (SampleData.Paquetes.Count == 0 ? 1 : SampleData.Paquetes.Max(existing => existing.Id) + 1), nombre.Trim(), descripcion.Trim(), precio, activo);

        // Validate everything before changing the shared catalog.
        if (item is not null)
            foreach (var reservationId in SampleData.ReservaPaquetes.Where(purchase => purchase.PaqueteId == item.Id).Select(purchase => purchase.ReservaId).Distinct())
                new ServicioBeneficioService().CapturarSnapshotDeCompras(reservationId);
        if (item is null) SampleData.Paquetes.Add(saved);
        else SampleData.Paquetes[SampleData.Paquetes.IndexOf(item)] = saved;
        SampleData.PaquetesServicios.RemoveAll(benefit => benefit.PaqueteId == saved.Id);
        SampleData.PaquetesServicios.AddRange(beneficios.Select(benefit => benefit with { PaqueteId = saved.Id }));
        SampleData.PaquetesImagenes.RemoveAll(image => image.PaqueteId == saved.Id);
        SampleData.PaquetesImagenes.AddRange(images.Select((image, index) => new PaqueteImagenModel(
            SampleData.PaquetesImagenes.Count == 0 ? 1 : SampleData.PaquetesImagenes.Max(existing => existing.Id) + 1,
            saved.Id, image.Url, "Imagen del paquete", index == 0, index + 1)));
        Registrar(id is null ? "Creación de paquete" : "Actualización de paquete", $"{saved.Nombre} · Bs {saved.Precio:N2}");
        return saved;
    }

    public void Eliminar(int id)
    {
        Autorizar();
        var package = SampleData.Paquetes.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("No se encontró el paquete.");
        if (SampleData.ReservaPaquetes.Any(item => item.PaqueteId == id) || SampleData.ReservaPaqueteServicios.Any(item => item.PaqueteId == id))
            throw new InvalidOperationException("No se puede eliminar: hay huéspedes con este paquete o beneficios adquiridos. Puedes desactivarlo para nuevas reservas.");
        SampleData.Paquetes.Remove(package); SampleData.PaquetesServicios.RemoveAll(item => item.PaqueteId == id); SampleData.PaquetesImagenes.RemoveAll(item => item.PaqueteId == id);
    }

    private static void Validar(string nombre, string descripcion, decimal precio, IReadOnlyCollection<PaqueteServicioModel> benefits)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(descripcion)) throw new InvalidOperationException("Completa el nombre y la descripción del paquete.");
        if (precio <= 0) throw new InvalidOperationException("El precio adicional debe ser mayor que cero.");
        if (benefits.Select(item => item.ServicioId).Distinct().Count() != benefits.Count) throw new InvalidOperationException("Un servicio no puede repetirse dentro del paquete.");
        foreach (var benefit in benefits)
        {
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == benefit.ServicioId);
            if (service is null || !service.Activo || !service.Reservable) throw new InvalidOperationException("Solo se pueden incluir servicios activos y reservables. La disponibilidad de una fecha se confirma en Operaciones; no selecciones agrupaciones organizativas.");
            if (benefit.TipoCobertura is not ("Ilimitado" or "Por día" or "Por estancia")) throw new InvalidOperationException("El tipo de cobertura no es compatible.");
            if (benefit.TipoCobertura == "Ilimitado" && benefit.LimiteUsos is not null) throw new InvalidOperationException("Un beneficio ilimitado no debe guardar un límite numérico.");
            if (benefit.TipoCobertura != "Ilimitado" && benefit.LimiteUsos is not > 0) throw new InvalidOperationException("Los beneficios por día o estancia necesitan un límite entero mayor que cero.");
        }
    }

    private static void Autorizar() { if (!NavigationService.Puede("Paquetes y promociones")) throw new UnauthorizedAccessException("Solo Gerencia puede modificar paquetes comerciales."); }
    private static void Registrar(string accion, string detalle) => SampleData.Auditoria.Insert(0, new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", accion, "—", detalle));
}

/// <summary>Only percentage discounts are persistable with the supplied promotion structure.</summary>
public sealed class PromocionService
{
    public IReadOnlyList<PromocionModel> Listar() => SampleData.Promociones.OrderBy(item => item.Desde).ToList();

    public PromocionModel Guardar(int? id, string nombre, decimal porcentaje, DateTime desde, DateTime hasta, bool activa)
    {
        Autorizar();
        if (string.IsNullOrWhiteSpace(nombre)) throw new InvalidOperationException("Escribe el nombre de la promoción.");
        if (porcentaje is <= 0 or > 100) throw new InvalidOperationException("El porcentaje debe estar entre 0,01 % y 100 %.");
        if (hasta.Date < desde.Date) throw new InvalidOperationException("La fecha final no puede ser anterior a la fecha inicial.");
        var current = id is int existing ? SampleData.Promociones.FirstOrDefault(item => item.Id == existing) ?? throw new InvalidOperationException("No se encontró la promoción.") : null;
        if (current?.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase) != true && current is not null)
            throw new InvalidOperationException("Esta promoción usa una regla de noches del prototipo y no puede convertirse en una regla persistente sin una ampliación del esquema.");
        if (current is not null && TieneReservas(id!.Value)) throw new InvalidOperationException("La promoción ya se aplicó a reservas. Sus reglas comerciales no se pueden reconstruir históricamente; conserva sus condiciones y crea otra campaña.");
        var saved = new PromocionModel(id ?? (SampleData.Promociones.Count == 0 ? 1 : SampleData.Promociones.Max(item => item.Id) + 1), nombre.Trim(), "Porcentaje", porcentaje, desde.Date, hasta.Date) { Activa = activa };
        if (current is null) SampleData.Promociones.Add(saved); else SampleData.Promociones[SampleData.Promociones.IndexOf(current)] = saved;
        SampleData.Auditoria.Insert(0, new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", id is null ? "Creación de promoción" : "Actualización de promoción", "—", $"{saved.Nombre} · {saved.Valor:N2}%"));
        return saved;
    }

    public void CambiarEstado(int id, bool activa)
    {
        Autorizar(); var current = SampleData.Promociones.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("No se encontró la promoción.");
        if (TieneReservas(id)) throw new InvalidOperationException("La promoción ya se aplicó a reservas. No se puede cambiar su estado porque el esquema no conserva las reglas históricas.");
        SampleData.Promociones[SampleData.Promociones.IndexOf(current)] = current with { Activa = activa };
    }

    public void Eliminar(int id)
    {
        Autorizar(); var current = SampleData.Promociones.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("No se encontró la promoción.");
        if (TieneReservas(id)) throw new InvalidOperationException("No se puede eliminar: la promoción está asociada a reservas. El esquema no conserva una copia de su descuento original.");
        SampleData.Promociones.Remove(current);
    }

    private static bool TieneReservas(int id) => SampleData.Reservas.Any(item => item.PromocionId == id);
    private static void Autorizar() { if (!NavigationService.Puede("Paquetes y promociones")) throw new UnauthorizedAccessException("Solo Gerencia puede modificar promociones."); }
}
