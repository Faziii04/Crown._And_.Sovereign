using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Calculates a guest's entitlement from the conditions captured when their reservation bought a package.</summary>
public sealed class ServicioBeneficioService
{
    public void CapturarSnapshotDeCompras(int reservationId)
    {
        foreach(var purchase in SampleData.ReservaPaquetes.Where(item=>item.ReservaId==reservationId))
        foreach(var relationship in SampleData.PaquetesServicios.Where(item=>item.PaqueteId==purchase.PaqueteId))
            if(!SampleData.ReservaPaqueteServicios.Any(item=>item.ReservaId==reservationId&&item.PaqueteId==purchase.PaqueteId&&item.ServicioId==relationship.ServicioId))
                SampleData.ReservaPaqueteServicios.Add(new(reservationId,purchase.PaqueteId,relationship.ServicioId,relationship.TipoCobertura,relationship.LimiteUsos,relationship.Compartido));
        foreach(var purchase in SampleData.ReservaPaquetes.Where(item=>item.ReservaId==reservationId&&item.PaqueteId==2))
        foreach(var product in SampleData.ServiciosProductos.Where(item=>item.ServicioId==10))
            if(!SampleData.ReservaPaqueteServicioProductos.Any(item=>item.ReservaId==reservationId&&item.PaqueteId==purchase.PaqueteId&&item.ServicioId==10&&item.ProductoId==product.ProductoId))
                SampleData.ReservaPaqueteServicioProductos.Add(new(reservationId,purchase.PaqueteId,10,product.ProductoId,product.CantidadIncluida));
    }

    public ServiceBenefitEvaluation Evaluar(int estanciaId, int servicioId, DateTime? fecha = null)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa")
            ?? throw new InvalidOperationException("No se encontró la estancia activa.");
        var guest = SampleData.Huespedes.First(item => item.Id == stay.HuespedId);
        var service = SampleData.Servicios.First(item => item.Id == servicioId);
        var purchases = SampleData.ReservaPaquetes.Where(item => item.ReservaId == stay.ReservaId).ToList();
        var snapshots = purchases.SelectMany(purchase => SampleData.ReservaPaqueteServicios
            .Where(benefit => benefit.ReservaId == stay.ReservaId && benefit.PaqueteId == purchase.PaqueteId && benefit.ServicioId == servicioId)
            .Select(benefit => (Purchase: purchase, Benefit: benefit))).ToList();
        var packages = snapshots.Select(item => SampleData.Paquetes.FirstOrDefault(package => package.Id == item.Purchase.PaqueteId)?.Nombre)
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        var unlimited = snapshots.Any(item => item.Benefit.TipoCobertura.Equals("Ilimitado", StringComparison.OrdinalIgnoreCase));
        var allowance = snapshots.Where(item => item.Benefit.LimiteUsos.HasValue)
            .Sum(item => item.Benefit.LimiteUsos!.Value * Math.Max(1, item.Purchase.Cantidad));
        var shared = snapshots.Any(item => item.Benefit.Compartido);
        var usage = SampleData.UsosServicio.Where(item => item.ServicioId == servicioId && item.Incluido && item.EstanciaId is int useStayId &&
            SampleData.Estancias.Any(useStay => useStay.Id == useStayId && useStay.ReservaId == stay.ReservaId && (shared || useStay.Id == stay.Id)));
        var period = snapshots.Where(item => item.Benefit.TipoCobertura.Contains("día", StringComparison.OrdinalIgnoreCase)).ToList();
        if (period.Count > 0)
        {
            var useDate = (fecha ?? DateTime.Now).Date;
            usage = usage.Where(item => item.Fecha.Date == useDate);
            allowance = period.Sum(item => (item.Benefit.LimiteUsos ?? 0) * Math.Max(1, item.Purchase.Cantidad));
            unlimited = period.Any(item => item.Benefit.TipoCobertura.Equals("Ilimitado", StringComparison.OrdinalIgnoreCase));
        }
        var consumed = unlimited ? 0 : usage.Count();
        var remaining = unlimited ? int.MaxValue : Math.Max(0, allowance - consumed);
        var covered = unlimited || remaining > 0;
        var type = unlimited ? "Ilimitado" : allowance == 0 ? "No incluido" : "Limitado";
        var packageText = packages.Count == 0 ? "Sin beneficio adquirido" : string.Join(" · ", packages);
        var detail = unlimited ? $"Incluido en {packageText} sin límite." : covered
            ? $"{packageText} · {remaining} de {allowance} usos disponibles{(shared ? " para la reserva" : " en esta estancia")}."
            : allowance > 0 ? $"Beneficio de {packageText} agotado para {(period.Count > 0 ? "hoy" : "esta estancia")}." : "No está incluido en los paquetes comprados para esta reserva.";
        var charge = covered ? 0 : service.Precio;
        var label = unlimited ? "Incluido · ilimitado" : allowance == 0 ? "Servicio adicional" : covered ? $"Incluido · {remaining} restantes" : "Beneficio agotado";
        return new(guest.Id, stay.Id, stay.ReservaId, service.Id, type, packageText, allowance, consumed, remaining, shared,
            service.RequiereCita, service.DuracionMinutos, charge, label, detail, service.Activo && service.Reservable && service.Disponible);
    }
}
