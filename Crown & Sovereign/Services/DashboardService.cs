using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed class DashboardService
{
    public IReadOnlyList<DashboardMetric> Recepcion()
    {
        var arrivals = SampleData.Reservas.Count(r => r.Estado != "Cancelada" && r.Llegada.Date == DateTime.Today);
        var departures = SampleData.Reservas.Count(r => r.Estado != "Cancelada" && r.Salida.Date == DateTime.Today);
        var occupied = SampleData.Habitaciones.Count(r => r.Estado == "Ocupada" && !r.Bloqueada);
        var available = SampleData.Habitaciones.Count(r => r.Estado == "Disponible" && !r.Bloqueada);
        return [new("Llegadas de hoy", arrivals.ToString(), "Reservas previstas", "Information"), new("Salidas de hoy", departures.ToString(), "Revisar folios", "Warning"), new("Ocupación actual", $"{occupied * 100 / Math.Max(1, SampleData.Habitaciones.Count)}%", $"{occupied} habitaciones ocupadas", "Success"), new("Habitaciones disponibles", available.ToString(), "Operativas y listas", "Neutral")];
    }

    public IReadOnlyList<DashboardArrival> LlegadasYSalidas()
    {
        var today = SampleData.Reservas.Where(r => r.Estado != "Cancelada" && (r.Llegada.Date == DateTime.Today || r.Salida.Date == DateTime.Today)).OrderBy(r => r.Llegada).Take(12);
        return today.Select(r => new DashboardArrival(r.Llegada.Date == DateTime.Today ? "15:00" : "11:00", r.Huesped, SampleData.Habitaciones.First(h => h.Id == r.Habitacion).Numero, r.Llegada.Date == DateTime.Today ? "Llegada" : "Salida", r.Llegada.Date == DateTime.Today ? "Check-in" : "Check-out", r.Id)).ToList();
    }

    public IReadOnlyList<DashboardMetric> Operaciones()
    {
        return [new("Citas próximas", SampleData.Citas.Count(c => c.Inicio >= DateTime.Now && c.Estado != "Cancelada").ToString(), "Agenda de hoy", "Information"), new("Solicitudes pendientes", SampleData.Citas.Count(c => c.Estado == "Solicitada").ToString(), "Por asignar", "Warning"), new("Habitaciones por limpiar", SampleData.Tareas.Count(t => t.Estado == "Pendiente").ToString(), "Tareas pendientes", "Warning"), new("Incidencias abiertas", SampleData.Incidencias.Count(i => i.Estado != "Resuelta").ToString(), $"{SampleData.Habitaciones.Count(h => h.Bloqueada)} habitaciones bloqueadas", "Danger"), new("Stock bajo", SampleData.Productos.Count(p => p.Stock <= p.StockMinimo).ToString(), "Productos por reponer", "Warning")];
    }

    public IReadOnlyList<DashboardAlert> AlertasOperacion() =>
        [.. SampleData.Incidencias.Where(i => i.Prioridad == "Alta").Take(3).Select(i => { var room = SampleData.Habitaciones.First(r => r.Id == i.Habitacion); return new DashboardAlert(i.Descripcion, $"Habitación {room.Numero} · Piso {room.Piso}", i.Estado, "Revisar", "Mantenimiento"); }),
         .. SampleData.Productos.Where(p => p.Stock <= p.StockMinimo).Take(3).Select(p => new DashboardAlert(p.Nombre, $"Existencia: {p.Stock} · mínimo {p.StockMinimo}", "Reponer", "Ver inventario", "Productos e inventario"))];

    public IReadOnlyList<DashboardMetric> Administracion() =>
        [new("Empleados", SampleData.Empleados.Count.ToString(), "Personal registrado"), new("Cuentas activas", SampleData.Usuarios.Count(u => u.Activo).ToString(), "De cuatro roles"), new("Cuentas inactivas", SampleData.Usuarios.Count(u => !u.Activo).ToString(), "Revisión de acceso", "Warning"), new("Habitaciones", SampleData.Habitaciones.Count.ToString(), "En cinco pisos"), new("Módulos", "12", "Configuración del hotel")];

    public IReadOnlyList<DashboardMetric> Gerencia() =>
        [new("Ingresos del mes", $"Bs {SampleData.Movimientos.Where(m => m.Tipo == "Pago" && m.Fecha.Month == DateTime.Today.Month).Sum(m => m.Monto):N0}", "Pagos registrados"), new("Ocupación promedio", $"{SampleData.Habitaciones.Count(r => r.Estado == "Ocupada") * 100 / Math.Max(1, SampleData.Habitaciones.Count)}%", "Habitaciones ocupadas"), new("Reservas activas", SampleData.Reservas.Count(r => r.Estado is "Confirmada" or "Pendiente").ToString(), "En cartera"), new("Ingresos por servicios", $"Bs {SampleData.Cargos.Where(c => c.Concepto.Contains("Servicio", StringComparison.OrdinalIgnoreCase)).Sum(c => c.Monto):N0}", "Cargos en folio")];
}
