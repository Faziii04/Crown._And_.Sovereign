using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

/// <summary>Calculates a guest's entitlement from the conditions captured when their reservation bought a package.</summary>
public sealed class ServicioBeneficioService
{
    public ServiceDraftBenefit EvaluarBorrador(int? packageId, int serviceId, DateTime arrival, DateTime departure, int packageQuantity = 1)
    {
        var benefits = packageId is int id ? SampleData.PaquetesServicios.Where(item => item.PaqueteId == id && item.ServicioId == serviceId).ToList() : [];
        var unlimited = benefits.Any(item => item.TipoCobertura.Equals("Ilimitado", StringComparison.OrdinalIgnoreCase));
        var nights = Math.Max(1, (departure.Date - arrival.Date).Days);
        var allowance = benefits.Where(item => item.LimiteUsos.HasValue).Sum(item => item.LimiteUsos!.Value * (item.TipoCobertura.Contains("día", StringComparison.OrdinalIgnoreCase) ? nights : 1) * Math.Max(1, packageQuantity));
        var label = unlimited ? "Incluido sin límite" : allowance > 0 ? $"Incluido con límite · {allowance} uso(s)" : "No incluido";
        return new(label, allowance, unlimited);
    }

    public void CapturarSnapshotDeCompras(int reservationId)
    {
        foreach(var purchase in SampleData.ReservaPaquetes.Where(item=>item.ReservaId==reservationId))
        foreach(var relationship in SampleData.PaquetesServicios.Where(item=>item.PaqueteId==purchase.PaqueteId))
            if(!SampleData.ReservaPaqueteServicios.Any(item=>item.ReservaId==reservationId&&item.PaqueteId==purchase.PaqueteId&&item.ServicioId==relationship.ServicioId))
                SampleData.ReservaPaqueteServicios.Add(new(reservationId,purchase.PaqueteId,relationship.ServicioId,relationship.TipoCobertura,relationship.LimiteUsos,relationship.Compartido));
        foreach(var purchase in SampleData.ReservaPaquetes.Where(item=>item.ReservaId==reservationId))
        foreach(var relationship in SampleData.ReservaPaqueteServicios.Where(item=>item.ReservaId==reservationId&&item.PaqueteId==purchase.PaqueteId))
        foreach(var product in SampleData.ServiciosProductos.Where(item=>item.ServicioId==relationship.ServicioId))
            if(!SampleData.ReservaPaqueteServicioProductos.Any(item=>item.ReservaId==reservationId&&item.PaqueteId==purchase.PaqueteId&&item.ServicioId==relationship.ServicioId&&item.ProductoId==product.ProductoId))
                SampleData.ReservaPaqueteServicioProductos.Add(new(reservationId,purchase.PaqueteId,relationship.ServicioId,product.ProductoId,product.CantidadIncluida));
    }

    public ServiceBenefitEvaluation Evaluar(int estanciaId, int servicioId, DateTime? fecha = null, int? huespedId = null)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa")
            ?? throw new InvalidOperationException("No se encontró la estancia activa.");
        var guestId = huespedId ?? stay.HuespedId;
        if (guestId != stay.HuespedId && !SampleData.ReservaHuespedes.Any(item => item.ReservaId == stay.ReservaId && item.HuespedId == guestId))
            throw new InvalidOperationException("El huésped no pertenece a la reserva seleccionada.");
        var guest = SampleData.Huespedes.First(item => item.Id == guestId);
        var service = SampleData.Servicios.First(item => item.Id == servicioId);
        var acquired = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == stay.ReservaId && item.ServicioId == servicioId && item.Estado is "ADQUIRIDO" or "CUMPLIDO" && (item.HuespedId is null || item.HuespedId == guest.Id)).ToList();
        var acquiredAllowance = acquired.Sum(item => item.Cantidad);
        var acquiredUsed = SampleData.UsosServicio.Count(item => item.EstanciaId is int usageStayId && SampleData.Estancias.Any(usageStay => usageStay.Id == usageStayId && usageStay.ReservaId == stay.ReservaId) && item.ServicioId == servicioId && item.ServicioAdquiridoId is not null && acquired.Any(line => line.ServicioAdquiridoId == item.ServicioAdquiridoId && (line.HuespedId is null || line.HuespedId == item.HuespedId)) && item.Estado is "Completado" or "Confirmado");
        var acquiredRemaining = Math.Max(0, acquiredAllowance - acquiredUsed);
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
            SampleData.Estancias.Any(useStay => useStay.Id == useStayId && useStay.ReservaId == stay.ReservaId && (shared || item.HuespedId == guest.Id)));
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
        var covered = unlimited || remaining > 0 || acquiredRemaining > 0;
        var type = unlimited ? "Ilimitado" : allowance == 0 ? "No incluido" : "Limitado";
        var packageText = packages.Count == 0 ? "Sin beneficio adquirido" : string.Join(" · ", packages);
        var detail = unlimited ? $"Incluido en {packageText} sin límite." : covered
            ? $"{packageText} · {remaining} de {allowance} usos disponibles{(shared ? " para la reserva" : " en esta estancia")}."
            : allowance > 0 ? $"Beneficio de {packageText} agotado para {(period.Count > 0 ? "hoy" : "esta estancia")}." : "No está incluido en los paquetes comprados para esta reserva.";
        var charge = covered ? 0 : service.Precio;
        var label = acquiredRemaining > 0 ? $"Adquirido previamente · {acquiredRemaining} sesión(es)" : acquiredAllowance > 0 && !unlimited && remaining == 0 ? "Servicio adquirido agotado" : unlimited ? "Incluido · ilimitado" : allowance == 0 ? "Servicio adicional" : covered ? $"Incluido · {remaining} restantes" : "Beneficio agotado";
        if (acquiredRemaining > 0 && (unlimited || remaining > 0)) label += unlimited ? " · más beneficio ilimitado" : $" · {remaining} uso(s) de paquete";
        if (acquiredRemaining > 0) detail += $" {acquiredRemaining} sesión(es) adquirida(s) previamente siguen disponibles; la compra no requiere otro cargo.";
        else if (acquiredAllowance > 0) detail += $" Se adquirieron {acquiredAllowance} sesión(es) y ya se utilizaron {acquiredUsed}.";
        var availableByEntitlement = acquiredRemaining > 0 || covered;
        return new(guest.Id, stay.Id, stay.ReservaId, service.Id, type, packageText, allowance, consumed, remaining, shared,
            service.RequiereCita, service.DuracionMinutos, acquiredRemaining > 0 ? 0m : charge, label, detail, (service.Activo && service.Reservable) || availableByEntitlement, acquiredAllowance, acquiredUsed, acquiredRemaining);
    }
}
