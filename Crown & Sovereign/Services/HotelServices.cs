using Crown___Sovereign.Data;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed class AutenticacionService
{
    /// <summary>Valida usuario y contraseña contra la tabla usuario. Sin base configurada usa las cuentas de demostración.</summary>
    public UsuarioModel? Autenticar(string nombre, string clave)
    {
        nombre = (nombre ?? "").Trim();
        if (!DatabaseConnection.Configurada)
            return SampleData.Usuarios.FirstOrDefault(u=>u.Activo&&u.Usuario.Equals(nombre,StringComparison.OrdinalIgnoreCase)&&u.ClaveDemo==clave);

        try
        {
            var fila = DatabaseConnection.Consultar(
                @"SELECT u.usuario_id, u.username, u.password_hash, COALESCE(u.estado, 'ACTIVO'), r.nombre, e.nombre, e.apellido
                  FROM public.usuario u
                  JOIN public.rol r ON r.rol_id = u.rol_id
                  LEFT JOIN public.empleado e ON e.empleado_id = u.empleado_id
                  WHERE lower(u.username) = lower(@usuario)",
                l => new { Id = l.GetInt32(0), Usuario = l.GetString(1), Hash = l.GetString(2), Estado = l.GetString(3), Rol = l.GetString(4),
                           Nombre = l.IsDBNull(5) ? null : l.GetString(5), Apellido = l.IsDBNull(6) ? null : l.GetString(6) },
                DatabaseConnection.Parametro("usuario", nombre)).FirstOrDefault();

            if (fila is null || !fila.Estado.Equals("ACTIVO", StringComparison.OrdinalIgnoreCase) || !ClaveCorrecta(clave, fila.Hash))
                return null;

            string completo = string.IsNullOrWhiteSpace(fila.Nombre) ? fila.Usuario : $"{fila.Nombre} {fila.Apellido}".Trim();
            return new UsuarioModel(fila.Id, fila.Usuario, "", completo, CargadorBaseDatos.RolDeInterfaz(fila.Rol));
        }
        catch (Exception ex)
        {
            // Sin conexión no se entra: nunca se cae a las cuentas de demostración.
            System.Diagnostics.Debug.WriteLine("No se pudo validar el acceso: " + ex.Message);
            return null;
        }
    }

    private static bool ClaveCorrecta(string clave, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(clave, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}
public sealed partial class HabitacionService
{
    public IReadOnlyList<HabitacionModel> ListarHabitaciones()=>SampleData.Habitaciones;
    public HabitacionModel? Obtener(int id)=>SampleData.Habitaciones.FirstOrDefault(room=>room.Id==id);
    public HabitacionModel Crear(string numero,int piso,string tipo,int capacidad,string comodidades)
    {
        if(!NavigationService.Puede("Habitaciones y pisos"))throw new UnauthorizedAccessException("Solo Administración puede crear habitaciones.");
        if(SampleData.Habitaciones.Any(room=>room.Numero.Equals(numero.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("El número de habitación ya está registrado.");
        if(piso is <1 or >5||capacidad is <1 or >6)throw new InvalidOperationException("Revisa el piso y la capacidad seleccionados.");
        var room=new HabitacionModel{Id=SampleData.Habitaciones.Max(item=>item.Id)+1,Numero=numero.Trim(),Piso=piso,Tipo=tipo,Capacidad=capacidad,Comodidades=comodidades,Estado="Disponible",Limpieza="Sin verificar"};
        SampleData.Habitaciones.Add(room);SampleData.Tarifas[room.Id]=420m;return room;
    }
    public void Actualizar(HabitacionModel room,string numero,int piso,string tipo,int capacidad,string comodidades)
    {
        if(!NavigationService.Puede("Habitaciones y pisos"))throw new UnauthorizedAccessException("Solo Administración puede editar habitaciones.");
        if(SampleData.Habitaciones.Any(item=>item.Id!=room.Id&&item.Numero.Equals(numero.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("El número de habitación ya está registrado.");
        if(piso is <1 or >5||capacidad is <1 or >6)throw new InvalidOperationException("Revisa el piso y la capacidad seleccionados.");
        room.Numero=numero.Trim();room.Piso=piso;room.Tipo=tipo;room.Capacidad=capacidad;room.Comodidades=comodidades;
    }
    public IReadOnlyList<HabitacionModel> Buscar(string texto="",int? piso=null,string? tipo=null,string? ocupacion=null,string? limpieza=null,bool? bloqueada=null,int? capacidadMinima=null,string? comodidad=null)
    {
        return SampleData.Habitaciones
            .Where(room=>string.IsNullOrWhiteSpace(texto)||$"{room.Numero} {room.Tipo} {room.Huesped} {room.Comodidades}".Contains(texto,StringComparison.OrdinalIgnoreCase))
            .Where(room=>piso is null||room.Piso==piso)
            .Where(room=>string.IsNullOrWhiteSpace(tipo)||tipo=="Todos los tipos"||room.Tipo==tipo)
            .Where(room=>string.IsNullOrWhiteSpace(ocupacion)||ocupacion=="Todas"||room.Estado==ocupacion)
            .Where(room=>string.IsNullOrWhiteSpace(limpieza)||limpieza=="Todas"||room.Limpieza==limpieza)
            .Where(room=>bloqueada is null||room.Bloqueada==bloqueada)
            .Where(room=>capacidadMinima is null||room.Capacidad>=capacidadMinima)
            .Where(room=>string.IsNullOrWhiteSpace(comodidad)||comodidad=="Todas"||room.Comodidades.Contains(comodidad,StringComparison.OrdinalIgnoreCase))
            .OrderBy(room=>room.Piso).ThenBy(room=>room.Numero).ToList();
    }
    public bool ConsultarDisponibilidad(int room,DateTime desde,DateTime hasta)=>new HabitacionDisponibilidadService().EsDisponible(room,desde,hasta);
    public bool ConsultarDisponibilidad(int room,DateTime desde,DateTime hasta,int excluirReservaId)=>new HabitacionDisponibilidadService().EsDisponible(room,desde,hasta,excluirReservaId);
    public void Bloquear(int room,bool bloquear)=>SampleData.Habitaciones.First(r=>r.Id==room).Bloqueada=bloquear;
}
public sealed class ReservaService
{
    public IReadOnlyList<ReservaModel> ListarReservas()=>SampleData.Reservas;
    public ResumenEconomicoReserva CalcularResumenEconomico(ReservaModel reservation)
    {
        var rooms = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservation.Id).Select(item => item.HabitacionId).Append(reservation.Habitacion).Distinct().ToList();
        var package = SampleData.ReservaPaquetes.FirstOrDefault(item => item.ReservaId == reservation.Id);
        var draft = new BorradorReserva
        {
            Llegada = reservation.Llegada, Salida = reservation.Salida, PaqueteId = package?.PaqueteId,
            CantidadPaquete = package?.Cantidad ?? 1, PrecioPaqueteAcordado = package?.PrecioCompra, PromocionId = reservation.PromocionId, Canal=reservation.Canal
        };
        draft.HabitacionesSeleccionadas.AddRange(rooms);
        draft.RecursosAdicionales.AddRange(SampleData.ReservaRecursos.Where(item => item.ReservaId == reservation.Id).Select(item => new ReservaRecursoBorrador
        { RecursoId = item.RecursoId, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, TipoCobroAplicado = item.TipoCobroAplicado }));
        draft.ServiciosAdquiridos.AddRange(SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservation.Id).Select(item => new ServicioAdquiridoBorrador
        { ServicioId = item.ServicioId, Cantidad = item.Cantidad, PrecioUnitarioAcordado = item.PrecioUnitarioAcordado, HuespedId = item.HuespedId }));
        return new TarifaService().CalcularResumenEconomico(draft,reservation);
    }
    public IReadOnlyList<string> ObtenerRecursosDePreparacion(int reservationId) => SampleData.ReservaRecursos
        .Where(item => item.ReservaId == reservationId && item.Estado is "PENDIENTE" or "ASIGNADO" or "SIN_VERIFICAR")
        .Select(item => $"{SampleData.RecursosAdicionales.FirstOrDefault(resource => resource.RecursoId == item.RecursoId)?.Nombre ?? "Recurso"} × {item.Cantidad}{(string.IsNullOrWhiteSpace(item.Observaciones) ? "" : $" · {item.Observaciones}")}")
        .ToList();
    public IReadOnlyList<ReservationAvailabilityLine> ObtenerDisponibilidad(DateTime desde, int dias)
        => new HabitacionDisponibilidadService().ObtenerDisponibilidadPorNoche(desde, dias);

    public IReadOnlyList<ReservationTimelineRoom> ObtenerCalendario(DateTime desde, int dias, int? piso = null, string? tipo = null, string? estado = null, string busquedaHabitacion = "", bool soloDisponibles = false)
    {
        var culture = new System.Globalization.CultureInfo("es-BO");
        var start = desde.Date; var end = start.AddDays(dias); const double dayWidth = 82;
        var availability = new HabitacionDisponibilidadService();
        return SampleData.Habitaciones
            .Where(h => piso is null || h.Piso == piso)
            .Where(h => string.IsNullOrWhiteSpace(tipo) || tipo == "Todos los tipos" || h.Tipo == tipo)
            .Where(h => string.IsNullOrWhiteSpace(busquedaHabitacion) || $"{h.Numero} {h.Tipo}".Contains(busquedaHabitacion, StringComparison.OrdinalIgnoreCase))
            .Where(h => !soloDisponibles || availability.EsDisponible(h.Id, start, end))
            .Select(h => new ReservationTimelineRoom
            {
                RoomId = h.Id,
                RoomNumber = h.Numero,
                RoomType = h.Tipo,
                FloorLabel = $"Piso {h.Piso}",
                CalendarWidth = dias * dayWidth,
                Days = Enumerable.Range(0, dias).Select(offset =>
                {
                    var date = desde.Date.AddDays(offset);
                    var reserva = SampleData.Reservas.FirstOrDefault(r => r.Habitacion == h.Id && r.Estado != "Cancelada" && date >= r.Llegada.Date && date < r.Salida.Date);
                    var status = h.Bloqueada ? "Mantenimiento" : reserva is null ? "Libre" : reserva.Estado == "Pendiente" ? "Pendiente" : reserva.Llegada.Date < DateTime.Today ? "Estancia activa" : "Confirmada";
                    var guest = reserva?.Huesped ?? "";
                    var detail = reserva is null ? (h.Bloqueada ? "Habitación fuera de servicio" : "Disponible") : $"{reserva.Huesped} · #{reserva.Id}";
                    var previous = date == desde.Date ? null : SampleData.Reservas.FirstOrDefault(r => r.Habitacion == h.Id && r.Estado != "Cancelada" && date.AddDays(-1) >= r.Llegada.Date && date.AddDays(-1) < r.Salida.Date);
                    var startsInWindow = reserva is not null && (date == desde.Date || previous?.Id != reserva.Id);
                    return new ReservationTimelineCell(h.Id, date, date.ToString("ddd dd", culture), startsInWindow ? guest : "", status, detail, reserva?.Id, startsInWindow);
                }).ToList(),
                Bars = TimelineReservations(h.Id, start, end).Select(reservation =>
                {
                    var visibleStart = reservation.Llegada.Date < start ? start : reservation.Llegada.Date;
                    var visibleEnd = reservation.Salida.Date > end ? end : reservation.Salida.Date;
                    var room = h;
                    var guestIds = SampleData.ReservaHuespedes.Where(link => link.ReservaId == reservation.Id).Select(link => link.HuespedId).Distinct().ToList();
                    var guests = guestIds.Count == 0 ? Math.Max(1, reservation.Personas) : guestIds.Count;
                    var status = SampleData.Estancias.Any(stay => stay.ReservaId == reservation.Id && stay.Estado == "Activa") ? "En estancia" : reservation.Estado;
                    return new ReservationBarModel(reservation.Id, reservation.Huesped, status, reservation.Llegada, reservation.Salida,
                        (visibleStart - start).TotalDays * dayWidth, Math.Max(dayWidth * .65, (visibleEnd - visibleStart).TotalDays * dayWidth),
                        $"{reservation.Huesped} · Reserva #{reservation.Id}\nLlegada {reservation.Llegada:dd/MM/yyyy} · salida {reservation.Salida:dd/MM/yyyy}\nHabitación {room.Numero} · {guests} huéspedes\nEstado: {status}", reservation.GrupoId.HasValue);
                }).ToList()
            })
            .Where(row => string.IsNullOrWhiteSpace(estado) || estado == "Todos los estados" || row.Days.Any(day => day.Status == estado))
            .OrderBy(row => row.RoomNumber)
            .ToList();

        IEnumerable<ReservaModel> TimelineReservations(int roomId, DateTime rangeStart, DateTime rangeEnd)
        {
            var assignedIds = SampleData.ReservaHabitaciones.Where(item => item.HabitacionId == roomId).Select(item => item.ReservaId).ToHashSet();
            return SampleData.Reservas.Where(item => (item.Habitacion == roomId || assignedIds.Contains(item.Id)) && item.Estado != "Cancelada" && item.Llegada.Date < rangeEnd && item.Salida.Date > rangeStart);
        }
    }

    public IReadOnlyList<ReservationDirectoryRow> ListarDirectorio(string? busqueda = null, string? estado = null)
    {
        return SampleData.Reservas
            .Where(r => string.IsNullOrWhiteSpace(busqueda) || $"{r.Id} {r.Huesped} {SampleData.Habitaciones.First(h => h.Id == r.Habitacion).Numero} {r.Canal}".Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrWhiteSpace(estado) || estado == "Todos los estados" || r.Estado == estado)
            .OrderBy(r => r.Llegada)
            .Select(r => new ReservationDirectoryRow(r.Id, r.Huesped, SampleData.Habitaciones.First(h => h.Id == r.Habitacion).Numero, r.Llegada, r.Salida, r.Canal, r.Paquete, r.Estado))
            .ToList();
    }

    public ReservationInboxPage ListarBandeja(ReservationListCriteria filter)
    {
        var today=DateTime.Today;
        var rows=SampleData.Reservas.AsEnumerable()
            .Where(item=>item.Estado!="Cancelada"||filter.FiltroRapido=="Todas"||filter.FiltroRapido=="Canceladas")
            .Where(item=>filter.FiltroRapido switch
            {
                "Llegadas de hoy"=>item.Llegada.Date==today&&item.Estado is "Confirmada" or "Pendiente",
                "Salidas de hoy"=>item.Salida.Date==today&&(item.Estado=="En estancia"||SampleData.Estancias.Any(stay=>stay.ReservaId==item.Id&&stay.Estado=="Activa")),
                "Pendientes"=>item.Estado=="Pendiente",
                "En estancia"=>item.Estado=="En estancia"||SampleData.Estancias.Any(stay=>stay.ReservaId==item.Id&&stay.Estado=="Activa"),
                "Próximas"=>item.Llegada.Date>=today&&item.Estado is "Confirmada" or "Pendiente",
                "Canceladas"=>item.Estado=="Cancelada",
                _=>true
            })
            .Where(item=>string.IsNullOrWhiteSpace(filter.Busqueda)||$"{item.Id} {item.Huesped} {item.Canal}".Contains(filter.Busqueda,StringComparison.OrdinalIgnoreCase))
            .Where(item=>filter.Estado=="Todos los estados"||string.IsNullOrWhiteSpace(filter.Estado)||item.Estado==filter.Estado)
            .Where(item=>!filter.LlegadaDesde.HasValue||item.Llegada.Date>=filter.LlegadaDesde.Value.Date)
            .Where(item=>!filter.LlegadaHasta.HasValue||item.Llegada.Date<=filter.LlegadaHasta.Value.Date)
            .Where(item=>!filter.SalidaDesde.HasValue||item.Salida.Date>=filter.SalidaDesde.Value.Date)
            .Where(item=>!filter.SalidaHasta.HasValue||item.Salida.Date<=filter.SalidaHasta.Value.Date)
            .Where(item=>filter.Canal=="Todos los canales"||string.IsNullOrWhiteSpace(filter.Canal)||item.Canal==filter.Canal)
            .Where(item=>!filter.SoloGrupos||item.GrupoId.HasValue)
            .Select(item=>
            {
                var roomIds=SampleData.ReservaHabitaciones.Where(link=>link.ReservaId==item.Id).Select(link=>link.HabitacionId).Append(item.Habitacion).Distinct().ToList();
                var rooms=roomIds.Select(id=>SampleData.Habitaciones.First(room=>room.Id==id)).ToList();
                if(filter.TipoHabitacion!="Todos los tipos"&&!rooms.Any(room=>room.Tipo==filter.TipoHabitacion))return null;
                if(filter.Piso.HasValue&&!rooms.Any(room=>room.Piso==filter.Piso.Value))return null;
                var active=SampleData.Estancias.Any(stay=>stay.ReservaId==item.Id&&stay.Estado=="Activa")||item.Estado=="En estancia";
                var isArrivalToday=item.Llegada.Date==today;
                var isDepartureToday=item.Salida.Date==today;
                var readiness=CheckInReadiness(item,rooms);
                var action=item.Estado switch
                {
                    "Pendiente"=>"Confirmar",
                    "Cancelada"=>"Ver historial",
                    "Finalizada"=>"Ver detalles",
                    _ when active&&isDepartureToday=>"Check-out",
                    _ when active=>"Ver estancia",
                    _ when isArrivalToday&&readiness is null=>"Check-in",
                    _ when isArrivalToday=>"Ver reserva",
                    _=>"Ver reserva"
                };
                var estimated=rooms.Sum(room=>new TarifaService().CalcularAlojamiento(item,room,item.Llegada,item.Salida));
                estimated+=SampleData.ReservaPaquetes.Where(link=>link.ReservaId==item.Id).Sum(link=>link.PrecioCompra*link.Cantidad);
                var guests=SampleData.ReservaHuespedes.Count(link=>link.ReservaId==item.Id);
                return new ReservationInboxRow(item.Id,item.Huesped,string.Join(", ",rooms.Select(room=>room.Numero)),rooms[0].Tipo,rooms[0].Piso,item.Llegada,item.Salida,(item.Salida.Date-item.Llegada.Date).Days,Math.Max(item.Personas,guests),item.Canal,item.Paquete,item.Estado,action,action=="Ver reserva"&&isArrivalToday?readiness:null,item.GrupoId.HasValue,estimated);
            }).Where(item=>item is not null).Select(item=>item!).OrderBy(item=>filter.FiltroRapido=="Llegadas de hoy"&&item.PrimaryAction=="Check-in"?0:1).ThenBy(item=>item.Arrival).ToList();
        var total=rows.Count;var pageSize=Math.Clamp(filter.TamanoPagina,1,100);var page=Math.Clamp(filter.Pagina,1,Math.Max(1,(int)Math.Ceiling(total/(double)pageSize)));
        return new ReservationInboxPage(rows.Skip((page-1)*pageSize).Take(pageSize).ToList(),total,page,pageSize);
    }

    public IReadOnlyList<ReservationGuestAssignment> ObtenerHuespedes(int reservaId)
    {
        var guests=SampleData.ReservaHuespedes.Where(item=>item.ReservaId==reservaId).ToList();
        if(guests.Count==0&&SampleData.Reservas.FirstOrDefault(item=>item.Id==reservaId) is { } reservation&&SampleData.Huespedes.FirstOrDefault(item=>item.Nombre==reservation.Huesped) is { } primary)guests.Add(new(reservaId,primary.Id,true));
        return guests.Select(item=>(Guest:SampleData.Huespedes.First(person=>person.Id==item.HuespedId),Link:item))
            .Select(item=>{var assignment=SampleData.ReservaHabitacionHuespedes.FirstOrDefault(link=>link.ReservaId==reservaId&&link.HuespedId==item.Guest.Id);var roomId=assignment is null?null:SampleData.ReservaHabitaciones.FirstOrDefault(room=>room.ReservaHabitacionId==assignment.ReservaHabitacionId)?.HabitacionId;return new ReservationGuestAssignment(item.Guest,roomId is int id?SampleData.Habitaciones.FirstOrDefault(room=>room.Id==id):null,item.Link.Principal);}).ToList();
    }

    public ReservaModel CrearReserva(string guest,int room,DateTime inicio,DateTime fin)
    {
        if(!NavigationService.Puede("Reservas")) throw new UnauthorizedAccessException("Tu rol no puede crear reservas.");
        if(fin<=inicio) throw new InvalidOperationException("La salida debe ser posterior a la llegada.");
        if(!new HabitacionService().ConsultarDisponibilidad(room,inicio,fin)) throw new InvalidOperationException("La habitación no está disponible para esas fechas.");
        var r=new ReservaModel{Id=SampleData.Reservas.Max(x=>x.Id)+1,Huesped=guest,Habitacion=room,Llegada=inicio,Salida=fin,Estado="Confirmada",Canal="Directa",FechaCreacion=DateTime.Now};r.AlojamientoNochesAcordadas.AddRange(CapturarNoches([room],inicio,fin,r.Canal));SampleData.Reservas.Insert(0,r);return r;
    }
    public ReservaModel CrearReserva(HuespedModel principal,int room,DateTime inicio,DateTime fin,IEnumerable<int> acompanantes)
    {
        var reserva=CrearReserva(principal.Nombre,room,inicio,fin);
        GuardarAsignaciones(reserva,room,[principal.Id,.. acompanantes.Distinct().Where(id=>id!=principal.Id)]);
        return reserva;
    }

    public ReservaModel CrearReserva(BorradorReserva borrador, bool pendiente = false)
    {
        if (!NavigationService.Puede("Reservas")) throw new UnauthorizedAccessException("Tu rol no puede crear reservas.");
        if (borrador.ReservaConfirmadaId is int createdId)
            return SampleData.Reservas.FirstOrDefault(item => item.Id == createdId) ?? throw new InvalidOperationException("El borrador ya fue confirmado, pero no se encontró la reserva asociada.");
        HabitacionDisponibilidadService.ValidarFechasReserva(borrador.Llegada, borrador.Salida);
        var guestIds = new[] { borrador.HuespedPrincipalId }.Where(id => id.HasValue).Select(id => id!.Value).Concat(borrador.HuespedesAcompanantes).Distinct().ToList();
        if (borrador.HuespedPrincipalId is not int principalId || !SampleData.Huespedes.Any(item => item.Id == principalId)) throw new InvalidOperationException("Selecciona un huésped principal antes de confirmar.");
        if (borrador.Personas < 1 || borrador.HabitacionesRequeridas < 1) throw new InvalidOperationException("Revisa la cantidad de huéspedes y habitaciones.");
        if (guestIds.Count > borrador.Personas) throw new InvalidOperationException("La cantidad de huéspedes seleccionados supera la indicada en la estancia.");
        if (borrador.GrupoId is int groupId && !SampleData.GruposReservas.Any(group => group.Id == groupId)) throw new InvalidOperationException("El grupo seleccionado ya no existe.");
        if (borrador.PaqueteId is int packageId && !SampleData.Paquetes.Any(item => item.Id == packageId && item.Activo)) throw new InvalidOperationException("El paquete ya no está disponible.");
        var criteria = Criterios(borrador);
        new HabitacionDisponibilidadService().ValidarSeleccion(criteria, borrador.HabitacionesSeleccionadas);
        ValidarAdicionales(borrador, guestIds, null);

        var firstGuest = SampleData.Huespedes.First(item => item.Id == principalId);
        var roomIds = borrador.HabitacionesSeleccionadas.ToList();
        var reservation = new ReservaModel
        {
            Id = Persistencia.SiguienteId("reserva", "reserva_id", () => SampleData.Reservas.Count == 0 ? 1 : SampleData.Reservas.Max(item => item.Id) + 1),
            Huesped = firstGuest.Nombre,
            Habitacion = roomIds[0],
            Llegada = borrador.Llegada.Date,
            Salida = borrador.Salida.Date,
            Estado = pendiente ? "Pendiente" : "Confirmada",
            Canal = borrador.Canal,
            Paquete = SampleData.Paquetes.FirstOrDefault(item => item.Id == borrador.PaqueteId)?.Nombre ?? "Sin paquete",
            GrupoId = borrador.GrupoId,
            Personas = borrador.Personas,
            PromocionId = borrador.PromocionId,
            FechaCreacion = DateTime.Now
        };
        reservation.AlojamientoNochesAcordadas.AddRange(CapturarNoches(roomIds,reservation.Llegada,reservation.Salida,reservation.Canal));
        SampleData.Reservas.Insert(0, reservation);
        SampleData.ReservaHuespedes.AddRange(guestIds.Select((id, index) => new ReservaHuespedModel(reservation.Id, id, index == 0)));
        var assignmentIds = new List<int>();
        foreach (var roomId in roomIds)
        {
            var assignmentId = Persistencia.SiguienteId("reserva_habitacion", "reserva_habitacion_id", () => SampleData.ReservaHabitaciones.Count == 0 ? 1 : SampleData.ReservaHabitaciones.Max(item => item.ReservaHabitacionId) + 1);
            assignmentIds.Add(assignmentId);
            SampleData.ReservaHabitaciones.Add(new(assignmentId, reservation.Id, roomId));
        }
        var guestIndex = 0;
        for (var roomIndex = 0; roomIndex < roomIds.Count && guestIndex < guestIds.Count; roomIndex++)
        {
            var room = SampleData.Habitaciones.First(item => item.Id == roomIds[roomIndex]);
            for (var occupied = 0; occupied < room.Capacidad && guestIndex < guestIds.Count; occupied++, guestIndex++)
                SampleData.ReservaHabitacionHuespedes.Add(new(assignmentIds[roomIndex], reservation.Id, guestIds[guestIndex]));
        }
        if (borrador.PaqueteId is int selectedPackage) SampleData.ReservaPaquetes.Add(new(reservation.Id, selectedPackage, borrador.CantidadPaquete, borrador.PrecioPaqueteAcordado ?? SampleData.Paquetes.First(item => item.Id == selectedPackage).Precio));
        new ServicioBeneficioService().CapturarSnapshotDeCompras(reservation.Id);
        GuardarAdicionales(reservation.Id, borrador);
        if (!string.IsNullOrWhiteSpace(borrador.SolicitudesEspeciales)) SampleData.SolicitudesEspeciales.Add(new(Persistencia.SiguienteId("solicitud_especial", "solicitud_id", () => SampleData.SolicitudesEspeciales.Count + 1), reservation.Id, borrador.SolicitudesEspeciales.Trim(), "Pendiente"));
        try { ReservaRepositorio.Guardar(reservation.Id); }
        catch (Exception ex)
        {
            ReservaRepositorio.QuitarDeMemoria(reservation.Id);
            throw new InvalidOperationException("No se pudo guardar la reserva en la base de datos. " + ex.Message, ex);
        }
        AuditoriaRepositorio.Registrar(new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", "Creación de reserva", "—", $"Reserva #{reservation.Id} · {reservation.Huesped}"));
        if (borrador.ListaEsperaId is int requestId) new ListaEsperaService().MarcarConvertida(requestId);
        borrador.ReservaConfirmadaId = reservation.Id;
        return reservation;
    }

    public void ActualizarReserva(ReservaModel reserva, BorradorReserva borrador)
    {
        if (!NavigationService.Puede("Reservas")) throw new UnauthorizedAccessException("Tu rol no puede editar reservas.");
        if (reserva.Estado is "En estancia" or "Finalizada" or "Cancelada") throw new InvalidOperationException("Esta reserva ya no admite edición de alojamiento.");
        HabitacionDisponibilidadService.ValidarFechasReserva(borrador.Llegada, borrador.Salida);
        var criteria = Criterios(borrador);
        new HabitacionDisponibilidadService().ValidarSeleccion(criteria, borrador.HabitacionesSeleccionadas, reserva.Id);
        var principal = SampleData.Huespedes.FirstOrDefault(item => item.Id == borrador.HuespedPrincipalId) ?? throw new InvalidOperationException("Selecciona un huésped principal.");
        var guestIds = new[] { principal.Id }.Concat(borrador.HuespedesAcompanantes).Distinct().ToList();
        if (guestIds.Count > borrador.Personas) throw new InvalidOperationException("La cantidad de huéspedes seleccionados supera la indicada en la estancia.");
        ValidarAdicionales(borrador, guestIds, reserva.Id);
        reserva.Huesped = principal.Nombre; reserva.Habitacion = borrador.HabitacionesSeleccionadas[0]; reserva.Llegada = borrador.Llegada.Date; reserva.Salida = borrador.Salida.Date;
        reserva.Canal = borrador.Canal; reserva.Personas = borrador.Personas; reserva.GrupoId = borrador.GrupoId;
        reserva.Paquete = SampleData.Paquetes.FirstOrDefault(item => item.Id == borrador.PaqueteId)?.Nombre ?? "Sin paquete"; reserva.PromocionId = borrador.PromocionId;
        SampleData.ReservaHabitaciones.RemoveAll(item => item.ReservaId == reserva.Id);
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item => item.ReservaId == reserva.Id);
        SampleData.ReservaHuespedes.RemoveAll(item => item.ReservaId == reserva.Id);
        SampleData.ReservaHuespedes.AddRange(guestIds.Select((id, index) => new ReservaHuespedModel(reserva.Id, id, index == 0)));
        foreach (var roomId in borrador.HabitacionesSeleccionadas)
        {
            var assignmentId = Persistencia.SiguienteId("reserva_habitacion", "reserva_habitacion_id", () => SampleData.ReservaHabitaciones.Count == 0 ? 1 : SampleData.ReservaHabitaciones.Max(item => item.ReservaHabitacionId) + 1);
            SampleData.ReservaHabitaciones.Add(new(assignmentId, reserva.Id, roomId));
            var room = SampleData.Habitaciones.First(item => item.Id == roomId);
            foreach (var guestId in guestIds.Take(room.Capacidad).Where(id => !SampleData.ReservaHabitacionHuespedes.Any(link => link.ReservaId == reserva.Id && link.HuespedId == id))) SampleData.ReservaHabitacionHuespedes.Add(new(assignmentId, reserva.Id, guestId));
        }
        var previousPackages = SampleData.ReservaPaquetes.Where(item => item.ReservaId == reserva.Id).ToList();
        SampleData.ReservaPaquetes.RemoveAll(item => item.ReservaId == reserva.Id);
        if (borrador.PaqueteId is int packageId)
        {
            var oldPurchase = previousPackages.FirstOrDefault(item => item.PaqueteId == packageId);
            SampleData.ReservaPaquetes.Add(new(reserva.Id, packageId, borrador.CantidadPaquete, borrador.PrecioPaqueteAcordado ?? oldPurchase?.PrecioCompra ?? SampleData.Paquetes.First(item => item.Id == packageId).Precio));
        }
        SampleData.ReservaPaqueteServicios.RemoveAll(item => item.ReservaId == reserva.Id && item.PaqueteId != borrador.PaqueteId);
        new ServicioBeneficioService().CapturarSnapshotDeCompras(reserva.Id);
        GuardarAdicionales(reserva.Id, borrador);
        SampleData.SolicitudesEspeciales.RemoveAll(item => item.ReservaId == reserva.Id);
        if (!string.IsNullOrWhiteSpace(borrador.SolicitudesEspeciales)) SampleData.SolicitudesEspeciales.Add(new(Persistencia.SiguienteId("solicitud_especial", "solicitud_id", () => SampleData.SolicitudesEspeciales.Count + 1), reserva.Id, borrador.SolicitudesEspeciales.Trim(), "Pendiente"));
        try { ReservaRepositorio.Guardar(reserva.Id); }
        catch (Exception ex)
        {
            ReservaRepositorio.Recargar(reserva.Id);
            throw new InvalidOperationException("No se pudieron guardar los cambios de la reserva en la base de datos. " + ex.Message, ex);
        }
    }

    public ResumenEconomicoReserva CalcularResumenEconomico(BorradorReserva borrador) => new TarifaService().CalcularResumenEconomico(borrador,borrador.ReservaOrigenId is int id?SampleData.Reservas.FirstOrDefault(item=>item.Id==id):null);

    public void ValidarAdicionales(BorradorReserva borrador, IReadOnlyCollection<int> guestIds, int? reservationIdExcluir)
    {
        if (borrador.PromocionId is int promotionId)
        {
            var promotion = SampleData.Promociones.FirstOrDefault(item => item.Id == promotionId);
            if (promotion is null || !promotion.Activa || promotion.Desde.Date > borrador.Llegada.Date || promotion.Hasta.Date < borrador.Llegada.Date)
                throw new InvalidOperationException("La promoción no está activa o no es válida para la fecha de llegada. Elige una campaña aplicable.");
        }
        if (borrador.PaqueteId.HasValue && (borrador.CantidadPaquete is < 1 or > 5 || !SampleData.Paquetes.Any(item => item.Id == borrador.PaqueteId && item.Activo)))
            throw new InvalidOperationException("Revisa el paquete seleccionado y su cantidad.");
        if (borrador.PaqueteId is int packageId)
        {
            var package = SampleData.Paquetes.First(item => item.Id == packageId);
            var previous = reservationIdExcluir is int reservation ? SampleData.ReservaPaquetes.FirstOrDefault(item => item.ReservaId == reservation && item.PaqueteId == packageId) : null;
            var agreed = borrador.PrecioPaqueteAcordado;
            if (agreed is null || agreed != (previous?.PrecioCompra ?? package.Precio)) throw new InvalidOperationException("El precio del paquete cambió o no está verificado. Revisa las condiciones antes de confirmar.");
        }
        if (borrador.RecursosAdicionales.Select(item => item.RecursoId).Distinct().Count() != borrador.RecursosAdicionales.Count)
            throw new InvalidOperationException("Cada recurso debe aparecer una sola vez con su cantidad total.");
        var resources = new RecursoAdicionalService();
        foreach (var line in borrador.RecursosAdicionales)
        {
            var resource = SampleData.RecursosAdicionales.FirstOrDefault(item => item.RecursoId == line.RecursoId) ?? throw new InvalidOperationException("Uno de los recursos ya no existe.");
            if (!resource.Activo && reservationIdExcluir is null) throw new InvalidOperationException($"{resource.Nombre} no está activo para nuevas reservas.");
            if (line.Cantidad <= 0) throw new InvalidOperationException("Quita los recursos con cantidad cero de la selección.");
            if (line.PrecioUnitarioAcordado is null || line.TipoCobroAplicado is null) throw new InvalidOperationException($"No se conoce el precio o modalidad acordada de {resource.Nombre}.");
            var oldResource = reservationIdExcluir is int resourceReservationId ? SampleData.ReservaRecursos.FirstOrDefault(item => item.ReservaId == resourceReservationId && item.RecursoId == resource.RecursoId) : null;
            if (oldResource is null && (line.PrecioUnitarioAcordado != resource.Precio || line.TipoCobroAplicado != resource.TipoCobro)) throw new InvalidOperationException($"La tarifa de {resource.Nombre} cambió durante el borrador. Revisa la condición y vuelve a confirmar.");
            resources.ValidarCantidadDisponible(resource.RecursoId, borrador.Llegada, borrador.Salida, line.Cantidad, reservationIdExcluir);
        }
        if (borrador.ServiciosAdquiridos.Select(item => item.ServicioId).Distinct().Count() != borrador.ServiciosAdquiridos.Count)
            throw new InvalidOperationException("Consolida las cantidades: debe existir una sola línea por servicio.");
        foreach (var line in borrador.ServiciosAdquiridos)
        {
            var service = SampleData.Servicios.FirstOrDefault(item => item.Id == line.ServicioId && item.Activo && item.Reservable) ?? throw new InvalidOperationException("Uno de los servicios adquiridos ya no está disponible.");
            if (line.Cantidad <= 0) throw new InvalidOperationException("Quita los servicios con cantidad cero de la selección.");
            if (line.PrecioUnitarioAcordado is null || line.PrecioUnitarioAcordado < 0) throw new InvalidOperationException($"No se pudo determinar el precio aplicable para {service.Nombre}.");
            if (line.HuespedId is int guestId && !guestIds.Contains(guestId)) throw new InvalidOperationException("El destinatario del servicio debe pertenecer a esta reserva.");
            var packageBenefit = borrador.PaqueteId is int servicePackageId ? SampleData.PaquetesServicios.FirstOrDefault(item => item.PaqueteId == servicePackageId && item.ServicioId == service.Id) : null;
            if (packageBenefit?.TipoCobertura.Equals("Ilimitado", StringComparison.OrdinalIgnoreCase) == true)
                throw new InvalidOperationException($"{service.Nombre} ya está incluido sin límite; no se puede adquirir como extra.");
            if (reservationIdExcluir is int reservationId)
            {
                var old = SampleData.ServiciosAdquiridos.FirstOrDefault(item => item.ReservaId == reservationId && item.ServicioId == line.ServicioId);
                if (old is not null)
                {
                    var used = SampleData.UsosServicio.Count(item => item.ServicioAdquiridoId == old.ServicioAdquiridoId && item.Estado is "Completado" or "Confirmado");
                    if (line.Cantidad < used) throw new InvalidOperationException($"No puedes reducir {service.Nombre} por debajo de las unidades ya utilizadas ({used}).");
                    if (old.CargoId.HasValue && (line.Cantidad != old.Cantidad || line.PrecioUnitarioAcordado != old.PrecioUnitarioAcordado || line.HuespedId != old.HuespedId))
                        throw new InvalidOperationException($"{service.Nombre} ya tiene un cargo asociado; su condición histórica requiere una corrección financiera.");
                }
                else if (line.PrecioUnitarioAcordado != service.Precio) throw new InvalidOperationException($"La tarifa de {service.Nombre} cambió durante el borrador. Revisa el precio antes de confirmar.");
            }
            else if (line.PrecioUnitarioAcordado != service.Precio) throw new InvalidOperationException($"La tarifa de {service.Nombre} cambió durante el borrador. Revisa el precio antes de confirmar.");
        }
        if (reservationIdExcluir is int existingId)
        {
            foreach (var old in SampleData.ReservaRecursos.Where(item => item.ReservaId == existingId && item.CargoId.HasValue))
                if (!borrador.RecursosAdicionales.Any(item => item.RecursoId == old.RecursoId && item.Cantidad == old.Cantidad && item.PrecioUnitarioAcordado == old.PrecioUnitarioAcordado && item.TipoCobroAplicado == old.TipoCobroAplicado))
                    throw new InvalidOperationException("Un recurso con cargo histórico no puede modificarse o quitarse desde el borrador; requiere una corrección financiera.");
            foreach (var old in SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == existingId && item.CargoId.HasValue))
                if (!borrador.ServiciosAdquiridos.Any(item => item.ServicioId == old.ServicioId && item.Cantidad == old.Cantidad && item.PrecioUnitarioAcordado == old.PrecioUnitarioAcordado && item.HuespedId == old.HuespedId))
                    throw new InvalidOperationException("Un servicio con cargo histórico no puede modificarse o quitarse desde el borrador; requiere una corrección financiera.");
        }
    }

    private static void GuardarAdicionales(int reservationId, BorradorReserva borrador)
    {
        foreach (var old in SampleData.ReservaRecursos.Where(item => item.ReservaId == reservationId && !item.CargoId.HasValue).ToList())
            if (!borrador.RecursosAdicionales.Any(item => item.RecursoId == old.RecursoId)) SampleData.ReservaRecursos.Remove(old);
        foreach (var line in borrador.RecursosAdicionales)
        {
            var index = SampleData.ReservaRecursos.FindIndex(item => item.ReservaId == reservationId && item.RecursoId == line.RecursoId);
            var saved = new ReservaRecursoModel(reservationId, line.RecursoId, line.Cantidad, line.PrecioUnitarioAcordado, line.TipoCobroAplicado, line.Estado, line.Observaciones, line.CargoId);
            if (index < 0) SampleData.ReservaRecursos.Add(saved); else if (!SampleData.ReservaRecursos[index].CargoId.HasValue) SampleData.ReservaRecursos[index] = saved;
        }
        foreach (var old in SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservationId && !item.CargoId.HasValue).ToList())
            if (!borrador.ServiciosAdquiridos.Any(item => item.ServicioId == old.ServicioId)) SampleData.ServiciosAdquiridos.Remove(old);
        foreach (var line in borrador.ServiciosAdquiridos)
        {
            var index = SampleData.ServiciosAdquiridos.FindIndex(item => item.ReservaId == reservationId && item.ServicioId == line.ServicioId);
            var old = index >= 0 ? SampleData.ServiciosAdquiridos[index] : null;
            var acquiredId = old?.ServicioAdquiridoId ?? Persistencia.SiguienteId("reserva_servicio_adquirido", "servicio_adquirido_id", () => SampleData.ServiciosAdquiridos.Count == 0 ? 1 : SampleData.ServiciosAdquiridos.Max(item => item.ServicioAdquiridoId) + 1);
            var saved = new ReservaServicioAdquiridoModel(acquiredId, reservationId, line.ServicioId, line.HuespedId, line.ReservaHabitacionId, line.Cantidad, line.PrecioUnitarioAcordado, old?.FechaRegistro ?? DateTime.Now, line.Estado, line.CargoId, line.Observaciones);
            if (index < 0) SampleData.ServiciosAdquiridos.Add(saved); else if (!old!.CargoId.HasValue) SampleData.ServiciosAdquiridos[index] = saved;
        }
    }
    public void ActualizarReserva(ReservaModel reserva,string guest,int room,DateTime inicio,DateTime fin,string canal,string paquete,int? principalId=null,IEnumerable<int>? acompanantes=null)
    {
        if(!NavigationService.Puede("Reservas"))throw new UnauthorizedAccessException("Tu rol no puede editar reservas.");
        if(fin<=inicio)throw new InvalidOperationException("La salida debe ser posterior a la llegada.");
        if(!new HabitacionService().ConsultarDisponibilidad(room,inicio,fin,reserva.Id))throw new InvalidOperationException("La habitación no está disponible para esas fechas.");
        reserva.Huesped=guest;reserva.Habitacion=room;reserva.Llegada=inicio;reserva.Salida=fin;reserva.Canal=canal;reserva.Paquete=paquete;
        if(principalId is int guestId)GuardarAsignaciones(reserva,room,[guestId,.. (acompanantes??[]).Distinct().Where(id=>id!=guestId)]);
    }
    private static void GuardarAsignaciones(ReservaModel reserva,int room,IEnumerable<int> guestIds)
    {
        SampleData.ReservaHuespedes.RemoveAll(item=>item.ReservaId==reserva.Id);
        SampleData.ReservaHabitaciones.RemoveAll(item=>item.ReservaId==reserva.Id);
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item=>item.ReservaId==reserva.Id);
        SampleData.ReservaHabitaciones.Add(new(reserva.Id,reserva.Id,room));
        var principal=true;
        foreach(var guestId in guestIds)
        {
            SampleData.ReservaHuespedes.Add(new(reserva.Id,guestId,principal));
            SampleData.ReservaHabitacionHuespedes.Add(new(reserva.Id,reserva.Id,guestId));
            principal=false;
        }
    }
    public void Cancelar(ReservaModel r){if(!NavigationService.Puede("Reservas"))throw new UnauthorizedAccessException();if(r.Estado is "En estancia" or "Finalizada")throw new InvalidOperationException("Una estancia iniciada o finalizada no se puede cancelar.");ReservaRepositorio.CambiarEstado(r.Id,"Cancelada");r.Estado="Cancelada";AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Cancelación de reserva",$"Reserva #{r.Id}","Cancelada"));}
    public ResumenReservasModel ObtenerResumenOperativo()
    {
        var today=DateTime.Today;
        var arrivals=SampleData.Reservas.Where(item=>item.Llegada.Date==today&&item.Estado is "Confirmada" or "Pendiente").ToList();
        var departures=SampleData.Reservas.Where(item=>item.Salida.Date==today&&item.Estado is "En estancia" or "Confirmada").ToList();
        var availability=new HabitacionDisponibilidadService();
        var tonight=SampleData.Habitaciones.Count(room=>availability.EsDisponible(room.Id,today,today.AddDays(1)));
        var wait=new ListaEsperaService().BuscarCoincidencias();
        return new(arrivals.Count,arrivals.Count(item=>item.Estado=="Confirmada"),departures.Count,departures.Count(item=>SampleData.Estancias.Any(stay=>stay.ReservaId==item.Id&&stay.Estado=="Activa")),tonight,SampleData.Habitaciones.Count,wait.Count,wait.Count(item=>item.HabitacionesCoincidentes>=item.Solicitud.Habitaciones));
    }
    private static List<PrecioAlojamientoAcordadoModel> CapturarNoches(IEnumerable<int> roomIds,DateTime llegada,DateTime salida,string canal){var tariff=new TarifaService();return roomIds.SelectMany(id=>{var room=SampleData.Habitaciones.First(item=>item.Id==id);return tariff.DesglosarAlojamiento(room,llegada,salida,canal).Select(night=>new PrecioAlojamientoAcordadoModel(id,night.Fecha,night.Precio));}).ToList();}
    private static ReservationSearchCriteria Criterios(BorradorReserva draft)=>new(){Llegada=draft.Llegada,Salida=draft.Salida,Personas=draft.Personas,Habitaciones=draft.HabitacionesRequeridas,TipoHabitacion=draft.TipoPreferido,Canal=draft.Canal,Comodidades=draft.Comodidades.ToList()};
    private static string? CheckInReadiness(ReservaModel reservation,IReadOnlyList<HabitacionModel> rooms)
    {
        if(reservation.Estado=="Pendiente")return "Confirma la reserva antes del check-in.";
        if(reservation.Llegada.Date>DateTime.Today)return "La fecha de llegada aún no ha llegado.";
        if(rooms.Any(room=>room.Bloqueada))return "Una habitación asignada está bloqueada por mantenimiento.";
        if(rooms.Any(room=>room.Limpieza is "Pendiente de limpieza" or "Sin verificar"))return "Una habitación aún requiere limpieza o inspección.";
        return null;
    }
}
public sealed partial class HuespedService
{
    public HuespedModel? Obtener(int id)=>SampleData.Huespedes.FirstOrDefault(g=>g.Id==id);
    public IReadOnlyList<ActiveGuestChoice> ListarActivos(string busqueda="",int? piso=null,string? habitacion=null)
    {
        var activeStays = SampleData.Estancias.Where(stay=>stay.Estado=="Activa").ToList();
        return activeStays.Select(stay =>
            {
                var guest=SampleData.Huespedes.FirstOrDefault(item=>item.Id==stay.HuespedId);
                var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==stay.ReservaId);
                var roomAssignment=SampleData.ReservaHabitacionHuespedes.FirstOrDefault(item=>item.ReservaId==stay.ReservaId&&item.HuespedId==stay.HuespedId);
                var roomId=(roomAssignment is null?null:SampleData.ReservaHabitaciones.FirstOrDefault(item=>item.ReservaHabitacionId==roomAssignment.ReservaHabitacionId)?.HabitacionId)
                    ??SampleData.ReservaHabitaciones.FirstOrDefault(item=>item.ReservaId==stay.ReservaId)?.HabitacionId;
                var room=roomId is int id?SampleData.Habitaciones.FirstOrDefault(item=>item.Id==id):null;
                return (Stay:stay,Guest:guest,Reservation:reservation,Room:room);
            })
            .Where(item=>item.Guest is not null&&item.Reservation is not null&&item.Room is not null)
            .Where(item=>piso is null||item.Room!.Piso==piso)
            .Where(item=>string.IsNullOrWhiteSpace(habitacion)||habitacion=="Todas las habitaciones"||item.Room!.Numero==habitacion)
            .Where(item=>string.IsNullOrWhiteSpace(busqueda)||$"{item.Guest!.Nombre} {item.Guest.Documento} {item.Room!.Numero} {item.Reservation!.Id} {item.Guest.Paquete}".Contains(busqueda,StringComparison.OrdinalIgnoreCase))
            .Select(item=>
            {
                var guest=item.Guest!;var reservation=item.Reservation!;var room=item.Room!;
                var packageNames=SampleData.ReservaPaquetes.Where(purchase=>purchase.ReservaId==reservation.Id).Select(purchase=>SampleData.Paquetes.FirstOrDefault(package=>package.Id==purchase.PaqueteId)?.Nombre).Where(name=>!string.IsNullOrWhiteSpace(name)).Distinct().ToList();
                if(packageNames.Count==0&&!string.IsNullOrWhiteSpace(reservation.Paquete)&&reservation.Paquete!="Sin paquete")packageNames.Add(reservation.Paquete);
                var pending=SampleData.SolicitudesServicio.Count(request=>request.EstanciaId==item.Stay.Id&&request.Estado is "Solicitada" or "Pendiente" or "En atención" or "En curso");
                var appointments=SampleData.Citas.Count(appointment=>appointment.EstanciaId==item.Stay.Id&&appointment.Inicio>=DateTime.Today&&appointment.Estado is not ("Cancelada" or "No presentado"));
                return new ActiveGuestChoice(guest.Id,guest.Nombre,guest.Documento,room.Numero,room.Piso,packageNames.Count==0?"Sin paquete":string.Join(" · ",packageNames),$"#{reservation.Id}",guest.RolEnReserva)
                { EstanciaId=item.Stay.Id,ReservaId=reservation.Id,CheckIn=item.Stay.CheckIn,CheckOut=item.Stay.CheckOut,SolicitudesPendientes=pending,CitasProximas=appointments };
            })
            .OrderBy(item=>item.Floor).ThenBy(item=>item.Room).ThenBy(item=>item.Name).ToList();
    }
    public IReadOnlyList<HuespedModel> Buscar(string q)=>SampleData.Huespedes.Where(g=>string.IsNullOrWhiteSpace(q)||$"{g.Nombre} {g.Documento} {g.Email} {g.Telefono}".Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();
    public IReadOnlyList<GuestDirectoryEntry> BuscarDirectorio(string q="",bool? conEstancia=null)
    {
        return Buscar(q).Where(g=>conEstancia is null|| (g.Habitacion>0)==conEstancia)
            .Select(g=>new GuestDirectoryEntry(g.Id,string.Concat(g.Nombre.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part=>char.ToUpperInvariant(part[0]))),g.Nombre,g.Documento,$"{g.Email} · {g.Telefono}",g.Habitacion>0?$"Habitación {SampleData.Habitaciones.First(r=>r.Id==g.Habitacion).Numero} · estancia activa":"Sin estancia",g.Paquete)).ToList();
    }
    public HuespedModel Crear(string nombre,string documento,string correo,string telefono)
    {
        if(!NavigationService.Puede("Huéspedes"))throw new UnauthorizedAccessException("Tu rol no puede registrar huéspedes.");
        if(string.IsNullOrWhiteSpace(nombre)||string.IsNullOrWhiteSpace(documento))throw new InvalidOperationException("Nombre y documento son obligatorios.");
        if(SampleData.Huespedes.Any(g=>g.Documento.Equals(documento.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("El documento ya está registrado.");
        ValidarDatos(nombre,documento,correo,telefono);
        var guest=new HuespedModel{Id=Persistencia.SiguienteId("huesped","huesped_id",()=>SampleData.Huespedes.Max(g=>g.Id)+1),Nombre=nombre.Trim(),Documento=documento.Trim(),Email=correo.Trim(),Telefono=telefono.Trim()};HuespedRepositorio.Insertar(guest);SampleData.Huespedes.Insert(0,guest);return guest;
    }
    private static void ValidarDatos(string nombre,string documento,string correo,string telefono)
    {
        if(nombre.Trim().Length>200||documento.Trim().Length>50||correo.Trim().Length>150)throw new InvalidOperationException("El nombre, el documento o el correo son demasiado largos.");
        if(!System.Text.RegularExpressions.Regex.IsMatch(nombre.Trim(),@"^[\p{L}][\p{L}\s.'\-]*$"))throw new InvalidOperationException("El nombre solo puede tener letras, espacios, puntos, guiones y apóstrofes.");
        if(!string.IsNullOrWhiteSpace(correo)&&!System.Text.RegularExpressions.Regex.IsMatch(correo.Trim(),@"^[^@\s]+@[^@\s]+\.[^@\s]+$"))throw new InvalidOperationException("El correo no tiene un formato válido.");
        if(!string.IsNullOrWhiteSpace(telefono)&&!System.Text.RegularExpressions.Regex.IsMatch(telefono.Trim(),@"^\+?[\d\s\-()]{6,30}$"))throw new InvalidOperationException("El teléfono no tiene un formato válido.");
    }
    public void Actualizar(HuespedModel guest,string nombre,string correo,string telefono)
    {
        if(!NavigationService.Puede("Huéspedes"))throw new UnauthorizedAccessException("Tu rol no puede editar huéspedes.");
        if(string.IsNullOrWhiteSpace(nombre))throw new InvalidOperationException("El nombre es obligatorio.");
        ValidarDatos(nombre,guest.Documento,correo,telefono);
        HuespedRepositorio.Actualizar(guest.Id,nombre.Trim(),correo.Trim(),telefono.Trim());guest.Nombre=nombre.Trim();guest.Email=correo.Trim();guest.Telefono=telefono.Trim();
    }
}

public sealed class SolicitudServicioService
{
    public IReadOnlyList<ServiceRequestView> Listar(int? guestId=null,int? estanciaId=null)
    {
        return SampleData.SolicitudesServicio.Where(item=>(guestId is null||item.HuespedId==guestId)&&(estanciaId is null||item.EstanciaId==estanciaId))
            .Select(item=>
            {
                var guest=SampleData.Huespedes.First(g=>g.Id==item.HuespedId);var service=SampleData.Servicios.First(entry=>entry.Id==item.ServicioId);
                var stay=item.EstanciaId is int stayId?SampleData.Estancias.FirstOrDefault(value=>value.Id==stayId):null;
                var reservation=stay is null?item.ReservaId:stay.ReservaId;
                var roomLink=stay is null?null:SampleData.ReservaHabitacionHuespedes.FirstOrDefault(link=>link.ReservaId==stay.ReservaId&&link.HuespedId==guest.Id);
                var roomId=stay is null?guest.Habitacion:(roomLink is null?null:SampleData.ReservaHabitaciones.FirstOrDefault(link=>link.ReservaHabitacionId==roomLink.ReservaHabitacionId)?.HabitacionId)??SampleData.ReservaHabitaciones.FirstOrDefault(link=>link.ReservaId==reservation)?.HabitacionId??guest.Habitacion;
                var room=SampleData.Habitaciones.FirstOrDefault(value=>value.Id==roomId)?.Numero??"—";
                var appointment=item.CitaServicioId is int citaId?SampleData.Citas.FirstOrDefault(value=>value.Id==citaId):null;
                var usage=item.UsoServicioId is int usageId?SampleData.UsosServicio.FirstOrDefault(value=>value.Id==usageId):null;
                return new ServiceRequestView(item.Id,guest.Nombre,room,service.Nombre,item.Solicitada,item.FechaDeseada,item.Estado,item.RequiereCita)
                {GuestId=guest.Id,StayId=item.EstanciaId??0,ReservationId=reservation??0,Category=service.Categoria,ScopeText=stay is null?"Sin estancia vinculada":$"Estancia #{stay.Id} · Reserva #{stay.ReservaId}",Responsible=item.Responsable,Description=item.Descripcion,Coverage=item.CoberturaEstimada,EstimatedCharge=usage?.Cargo??0,ConfirmedDate=appointment?.Estado=="Confirmada"?appointment.Inicio:null,CompletedDate=item.Completada,Outcome=item.Estado};
            })
            .OrderByDescending(item=>item.RequestedAt).ToList();
    }
    public SolicitudServicioModel Crear(int guestId,int serviceId,DateTime? fecha,string descripcion)
        => Crear(guestId,SampleData.Estancias.FirstOrDefault(item=>item.HuespedId==guestId&&item.Estado=="Activa")?.Id??0,serviceId,fecha,descripcion);
    public SolicitudServicioModel Crear(int guestId,int estanciaId,int serviceId,DateTime? fecha,string descripcion,string origen="",string destino="",int pasajeros=1,string contacto="")
    {
        if(!NavigationService.Puede("Servicios"))throw new UnauthorizedAccessException("Tu rol no puede solicitar servicios.");
        var guest=SampleData.Huespedes.FirstOrDefault(item=>item.Id==guestId)??throw new InvalidOperationException("Selecciona un huésped alojado.");
        var service=SampleData.Servicios.FirstOrDefault(item=>item.Id==serviceId&&item.Activo&&item.Reservable)??throw new InvalidOperationException("El servicio no está disponible para solicitud.");
        if((service.RequiereCita||service.RequiereTraslado)&&fecha is null)throw new InvalidOperationException("Este servicio requiere fecha y hora deseadas.");
        if(fecha is DateTime desired&&desired<=DateTime.Now)throw new InvalidOperationException("La fecha deseada debe ser futura.");
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.Id==estanciaId&&item.Estado=="Activa"&&(item.HuespedId==guestId||SampleData.ReservaHuespedes.Any(link=>link.ReservaId==item.ReservaId&&link.HuespedId==guestId)))??throw new InvalidOperationException("El huésped no está asociado a la estancia activa seleccionada.");
        if(stay.CheckOut<=DateTime.Now)throw new InvalidOperationException("La estancia ya terminó.");
        if(fecha is DateTime preferred&&preferred.AddMinutes(service.DuracionMinutos)>=stay.CheckOut)throw new InvalidOperationException("El horario debe finalizar antes del checkout.");
        if(service.RequiereTraslado&&(string.IsNullOrWhiteSpace(origen)||string.IsNullOrWhiteSpace(destino)||pasajeros<1))throw new InvalidOperationException("Indica origen, destino y cantidad de pasajeros para coordinar el traslado.");
        if(SampleData.SolicitudesServicio.Any(item=>item.EstanciaId==stay.Id&&item.ServicioId==service.Id&&item.Estado is "Solicitada" or "Pendiente" or "Confirmada" or "En atención" or "En curso"&&item.FechaDeseada?.Date==fecha?.Date))throw new InvalidOperationException("Ya existe una solicitud activa para este servicio y fecha.");
        var coverage=new ServicioBeneficioService().Evaluar(stay.Id,service.Id,fecha,guestId);
        var request=new SolicitudServicioModel(Persistencia.SiguienteId("reserva_servicio","reserva_servicio_id",()=>SampleData.SolicitudesServicio.Count==0?1:SampleData.SolicitudesServicio.Max(item=>item.Id)+1),guest.Id,service.Id,DateTime.Now,fecha,descripcion,"Pendiente",service.RequiereCita)
        {EstanciaId=stay.Id,ReservaId=stay.ReservaId,UsuarioSolicitaId=SesionActual.Usuario?.Id??0,CoberturaEstimada=coverage.Label+(coverage.EstimatedCharge>0?$" · Bs {coverage.EstimatedCharge:N2}":" · Sin cargo estimado")};
        if(service.RequiereTraslado)request=request with{OrigenTraslado=origen.Trim(),DestinoTraslado=destino.Trim(),Pasajeros=pasajeros,ContactoTraslado=contacto.Trim()};
        CitaServicioModel? appointment=null;
        if(service.RequiereCita&&fecha is DateTime start)
        {
            appointment=new CitaServicioModel(Persistencia.SiguienteId("cita_servicio","cita_id",()=>SampleData.Citas.Count==0?1:SampleData.Citas.Max(item=>item.Id)+1),guest.Id,service.Id,start,start.AddMinutes(service.DuracionMinutos),"Por asignar","Solicitada"){EstanciaId=stay.Id,SolicitudId=request.Id,PoliticaAplicadaJson=PoliticaHotelService.SnapshotActualServicio()};
            request=request with{Estado="Solicitada",CitaServicioId=appointment.Id};
        }
        var transfer=service.RequiereTraslado?new TrasladoModel(Persistencia.SiguienteId("traslado","traslado_id",()=>SampleData.Traslados.Count==0?1:SampleData.Traslados.Max(item=>item.Id)+1),guest.Id,origen.Trim(),destino.Trim(),fecha??DateTime.Now,"Solicitado"){EstanciaId=stay.Id,ServicioId=service.Id,SolicitudId=request.Id,Pasajeros=pasajeros,Observaciones=descripcion.Trim()}:null;
        ServicioRepositorio.InsertarSolicitud(request,appointment,transfer);
        if(appointment is not null)SampleData.Citas.Add(appointment);
        SampleData.SolicitudesServicio.Add(request);
        if(transfer is not null)SampleData.Traslados.Add(transfer);
        return request;
    }
    public void Cancelar(int requestId,string motivo="Cancelada por recepción")
    {
        if(!NavigationService.Puede("Servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(index<0)throw new InvalidOperationException("No se encontró la solicitud.");
        var request=SampleData.SolicitudesServicio[index];if(request.Estado is "Completada" or "Cancelada" or "No presentado")throw new InvalidOperationException("La solicitud ya está cerrada.");
        var cancelledAt=DateTime.Now;
        ServicioRepositorio.CancelarSolicitud(requestId,cancelledAt,motivo);
        SampleData.SolicitudesServicio[index]=request with{Estado="Cancelada",Cancelada=cancelledAt,MotivoCancelacion=motivo};
        if(request.CitaServicioId is int appointmentId){var appt=SampleData.Citas.FindIndex(item=>item.Id==appointmentId);if(appt>=0&&SampleData.Citas[appt].Estado is not ("Completada" or "En curso"))SampleData.Citas[appt]=SampleData.Citas[appt] with{Estado="Cancelada"};}
    }
    public void IniciarAtencion(int requestId,int? responsableId=null)
    {
        if(!NavigationService.Puede("Atención de servicios"))throw new UnauthorizedAccessException("Solo el equipo de servicios inicia la atención.");
        var index=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(index<0)throw new InvalidOperationException("No se encontró la solicitud.");
        var request=SampleData.SolicitudesServicio[index];if(request.RequiereCita)throw new InvalidOperationException("Las solicitudes con cita se procesan desde la agenda.");
        if(request.Estado is not ("Pendiente" or "Confirmada"))throw new InvalidOperationException("La solicitud no está pendiente de atención.");
        var responsible=responsableId is int id?SampleData.Empleados.FirstOrDefault(item=>item.Id==id&&item.Cargo=="Servicios"):null;
        SampleData.SolicitudesServicio[index]=request with{Estado="En atención",Aceptada=request.Aceptada??DateTime.Now,InicioAtencion=DateTime.Now,ResponsableId=responsible?.Id??request.ResponsableId,Responsable=responsible?.Nombre??request.Responsable};
    }
    public void CompletarAtencion(int requestId, int? convenioId = null)
    {
        if(!NavigationService.Puede("Atención de servicios"))throw new UnauthorizedAccessException("Solo el equipo de servicios confirma la entrega.");
        var request=SampleData.SolicitudesServicio.FirstOrDefault(item=>item.Id==requestId)??throw new InvalidOperationException("No se encontró la solicitud.");
        if(request.Estado!="En atención")throw new InvalidOperationException("Inicia la atención antes de registrar la entrega.");
        var service=SampleData.Servicios.First(item=>item.Id==request.ServicioId);var guest=SampleData.Huespedes.First(item=>item.Id==request.HuespedId);
        new ServicioService().RegistrarUtilizacion(service,guest,false,request.EstanciaId,request.Id,convenioId,request.FechaDeseada);
    }
}
public sealed class CitaServicioService
{
    public IReadOnlyList<CitaServicioModel> Listar()=>SampleData.Citas.OrderBy(item=>item.Inicio).ToList();
    public IReadOnlyList<CitaServicioModel> ListarPorEstancia(int estanciaId)=>SampleData.Citas.Where(item=>item.EstanciaId==estanciaId).OrderBy(item=>item.Inicio).ToList();
    public IReadOnlyList<AppointmentQueueRow> ConsultarAgenda(DateTime? fecha=null,string? categoria=null,string? empleado=null)
    {
        return SampleData.Citas.Where(item=>fecha is null||item.Inicio.Date==fecha.Value.Date)
            .Select(item=>{var guest=SampleData.Huespedes.First(person=>person.Id==item.HuespedId);var service=SampleData.Servicios.First(entry=>entry.Id==item.ServicioId);var stay=item.EstanciaId is int stayId?SampleData.Estancias.FirstOrDefault(value=>value.Id==stayId):null;var roomLink=stay is null?null:SampleData.ReservaHabitacionHuespedes.FirstOrDefault(link=>link.ReservaId==stay.ReservaId&&link.HuespedId==guest.Id);var roomId=stay is null?guest.Habitacion:(roomLink is null?null:SampleData.ReservaHabitaciones.FirstOrDefault(link=>link.ReservaHabitacionId==roomLink.ReservaHabitacionId)?.HabitacionId)??SampleData.ReservaHabitaciones.FirstOrDefault(link=>link.ReservaId==stay!.ReservaId)?.HabitacionId??guest.Habitacion;return new AppointmentQueueRow(item.Id,item.Inicio,item.Fin,service.Nombre,guest.Nombre,SampleData.Habitaciones.FirstOrDefault(room=>room.Id==roomId)?.Numero??"—",item.AsignadoA,item.Estado,service.Categoria);})
            .Where(item=>string.IsNullOrWhiteSpace(categoria)||categoria=="Todas las áreas"||item.Category==categoria)
            .Where(item=>string.IsNullOrWhiteSpace(empleado)||empleado=="Todo el personal"||item.Employee==empleado)
            .OrderBy(item=>item.Start).ToList();
    }
    public bool TieneConflictoHorario(string responsable, DateTime inicio, DateTime fin, int? excluirCitaId = null)
        => HasConflict(responsable, inicio, fin, excluirCitaId);
    public bool EsResponsableElegible(int servicioId, string responsable)
    {
        var employee = SampleData.Empleados.FirstOrDefault(item => item.Nombre.Equals(responsable, StringComparison.OrdinalIgnoreCase) && item.Cargo == "Servicios");
        if (employee is null) return false;
        var eligible = SampleData.EmpleadosServicios.Where(item => item.ServicioId == servicioId && item.Activo).Select(item => item.EmpleadoId).ToHashSet();
        return eligible.Count == 0 || eligible.Contains(employee.Id);
    }
    public CitaServicioModel Programar(int guestId,int serviceId,DateTime inicio,string responsable)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException("Tu rol no puede programar citas.");
        var guest=SampleData.Huespedes.FirstOrDefault(item=>item.Id==guestId)??throw new InvalidOperationException("Selecciona un huésped.");
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.HuespedId==guestId&&item.Estado=="Activa")??throw new InvalidOperationException("No se encontró la estancia activa del huésped.");
        var service=SampleData.Servicios.FirstOrDefault(item=>item.Id==serviceId&&item.Activo&&item.Reservable&&item.RequiereCita)??throw new InvalidOperationException("Selecciona un servicio activo y reservable que requiera cita.");
        ValidarResponsable(serviceId,responsable);
        if(inicio<=DateTime.Now)throw new InvalidOperationException("La cita debe programarse para una fecha futura.");
        if(inicio>=stay.CheckOut)throw new InvalidOperationException("La cita debe ocurrir durante la estancia activa.");
        var finish=inicio.AddMinutes(service.DuracionMinutos);
        if(HasConflict(responsable,inicio,finish,null))throw new InvalidOperationException("El responsable ya tiene una cita que se superpone con ese horario.");
        var appointment=new CitaServicioModel(SampleData.Citas.Count==0?1:SampleData.Citas.Max(item=>item.Id)+1,guest.Id,serviceId,inicio,finish,responsable,"Confirmada"){EstanciaId=stay.Id,UsuarioConfirmaId=SesionActual.Usuario?.Id,Confirmada=DateTime.Now,PoliticaAplicadaJson=PoliticaHotelService.SnapshotActualServicio()};SampleData.Citas.Add(appointment);return appointment;
    }
    public void Iniciar(int citaId)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.Citas.FindIndex(item=>item.Id==citaId);if(index<0)throw new InvalidOperationException("No se encontró la cita.");
        var cita=SampleData.Citas[index];if(cita.Estado!="Confirmada")throw new InvalidOperationException("Operaciones debe confirmar horario y responsable antes de iniciar la cita.");
        SampleData.Citas[index]=cita with{Estado="En curso"};
        if(cita.SolicitudId is int requestId){var requestIndex=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(requestIndex>=0)SampleData.SolicitudesServicio[requestIndex]=SampleData.SolicitudesServicio[requestIndex] with{Estado="En atención",InicioAtencion=DateTime.Now};}
    }
    public void Reprogramar(int citaId,DateTime inicio,string responsable)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.Citas.FindIndex(item=>item.Id==citaId);if(index<0)throw new InvalidOperationException("No se encontró la cita.");
        var current=SampleData.Citas[index];if(current.Estado is "Completada" or "Cancelada")throw new InvalidOperationException("No se puede reprogramar una cita cerrada.");
        if(inicio<=DateTime.Now)throw new InvalidOperationException("La cita debe programarse en el futuro.");
        var service=SampleData.Servicios.First(item=>item.Id==current.ServicioId);ValidarResponsable(service.Id,responsable);var finish=inicio.AddMinutes(service.DuracionMinutos);
        if(HasConflict(responsable,inicio,finish,citaId))throw new InvalidOperationException("El responsable ya tiene una cita que se superpone con ese horario.");
        var stay=current.EstanciaId is int sid?SampleData.Estancias.FirstOrDefault(item=>item.Id==sid):null;
        if(stay is not null&&inicio>=stay.CheckOut)throw new InvalidOperationException("La cita debe ocurrir antes del checkout.");
        SampleData.Citas[index]=current with{Inicio=inicio,Fin=finish,AsignadoA=responsable,Estado="Confirmada",UsuarioConfirmaId=SesionActual.Usuario?.Id,Confirmada=DateTime.Now};
        if(current.SolicitudId is int requestId){var requestIndex=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(requestIndex>=0){var request=SampleData.SolicitudesServicio[requestIndex];var staff=SampleData.Empleados.FirstOrDefault(item=>item.Nombre==responsable);SampleData.SolicitudesServicio[requestIndex]=request with{Estado="Confirmada",FechaDeseada=inicio,Aceptada=request.Aceptada??DateTime.Now,Responsable=responsable,ResponsableId=staff?.Id};}}
    }
    public void Completar(int citaId, int? convenioId = null)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.Citas.FindIndex(item=>item.Id==citaId);if(index<0)throw new InvalidOperationException("No se encontró la cita.");
        var cita=SampleData.Citas[index];if(cita.Estado!="En curso")throw new InvalidOperationException("Inicia la cita antes de confirmarla como realizada.");
        new ServicioService().RegistrarUtilizacion(SampleData.Servicios.First(service=>service.Id==cita.ServicioId),SampleData.Huespedes.First(guest=>guest.Id==cita.HuespedId),true,cita.EstanciaId,cita.SolicitudId,convenioId,cita.Inicio);
        SampleData.Citas[index]=cita with{Estado="Completada"};
    }
    public void Cancelar(int citaId)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.Citas.FindIndex(item=>item.Id==citaId);if(index<0)return;var appointment=SampleData.Citas[index];if(appointment.Estado is "Completada" or "En curso")throw new InvalidOperationException("No se puede cancelar una cita que ya comenzó o fue completada.");SampleData.Citas[index]=appointment with{Estado="Cancelada"};
        if(appointment.SolicitudId is int requestId){var requestIndex=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(requestIndex>=0)SampleData.SolicitudesServicio[requestIndex]=SampleData.SolicitudesServicio[requestIndex] with{Estado="Cancelada",Cancelada=DateTime.Now,MotivoCancelacion="Cancelada por Operaciones"};}
    }
    public void MarcarNoPresentado(int citaId)
    {
        if(!NavigationService.Puede("Agenda de servicios"))throw new UnauthorizedAccessException();
        var index=SampleData.Citas.FindIndex(item=>item.Id==citaId);if(index<0)return;var appointment=SampleData.Citas[index];if(appointment.Estado!="Confirmada")throw new InvalidOperationException("Solo una cita confirmada puede marcarse como no presentada.");SampleData.Citas[index]=appointment with{Estado="No presentado"};
        if(appointment.SolicitudId is int requestId){var requestIndex=SampleData.SolicitudesServicio.FindIndex(item=>item.Id==requestId);if(requestIndex>=0)SampleData.SolicitudesServicio[requestIndex]=SampleData.SolicitudesServicio[requestIndex] with{Estado="No presentado"};}
    }
    private static bool HasConflict(string responsible,DateTime start,DateTime finish,int? exceptId)=>SampleData.Citas.Any(item=>item.Id!=exceptId&&item.AsignadoA==responsible&&item.Estado is not ("Cancelada" or "No presentado" or "Completada")&&start<item.Fin&&finish>item.Inicio);
    private static void ValidarResponsable(int serviceId,string responsible)
    {
        if (!new CitaServicioService().EsResponsableElegible(serviceId, responsible))
            throw new InvalidOperationException("El responsable no existe como colaborador de servicios o no está asociado como elegible para este servicio.");
    }
}
public sealed class InventarioService
{
    public IReadOnlyList<(ConsumoModel Consumo,string Producto)> ConsultarConsumos(int guestId)=>SampleData.Consumos.Where(item=>item.HuespedId==guestId).OrderByDescending(item=>item.Fecha).Select(item=>(item,SampleData.Productos.First(p=>p.Id==item.ProductoId).Nombre)).ToList();
    public IReadOnlyList<(ConsumoModel Consumo,string Producto)> ConsultarConsumosPorEstancia(int estanciaId)=>SampleData.Consumos.Where(item=>item.EstanciaId==estanciaId).OrderByDescending(item=>item.Fecha).Select(item=>(item,SampleData.Productos.First(p=>p.Id==item.ProductoId).Nombre)).ToList();
    public IReadOnlyList<ProductoModel> Buscar(string busqueda="",string? categoria=null)=>SampleData.Productos.Where(product=>string.IsNullOrWhiteSpace(busqueda)||$"{product.Nombre} {product.Categoria}".Contains(busqueda,StringComparison.OrdinalIgnoreCase)).Where(product=>string.IsNullOrWhiteSpace(categoria)||categoria=="Todas las categorías"||product.Categoria==categoria).OrderBy(product=>product.Nombre).ToList();
    public IReadOnlyList<InventoryMovementRow> ConsultarMovimientos(string busqueda="",string? tipo=null)=>SampleData.MovimientosInventario.Where(item=>string.IsNullOrWhiteSpace(tipo)||tipo=="Todos los movimientos"||item.Tipo==tipo).Select(item=>new InventoryMovementRow(item.Fecha,SampleData.Productos.First(product=>product.Id==item.ProductoId).Nombre,item.Tipo,item.Cantidad,item.Motivo,SampleData.Productos.First(product=>product.Id==item.ProductoId).Stock)).Where(item=>string.IsNullOrWhiteSpace(busqueda)||$"{item.Producto} {item.Motivo}".Contains(busqueda,StringComparison.OrdinalIgnoreCase)).OrderByDescending(item=>item.Fecha).ToList();
    public ProductoModel Crear(string nombre,string categoria,decimal precio,int stock,int minimo,bool incluido)
    {
        if(!NavigationService.Puede("Productos e inventario"))throw new UnauthorizedAccessException("Tu rol no puede crear productos.");
        if(string.IsNullOrWhiteSpace(nombre)||precio<0||stock<0||minimo<0)throw new InvalidOperationException("Revisa nombre, precio y existencias.");
        var product=new ProductoModel{Id=SampleData.Productos.Max(item=>item.Id)+1,Nombre=nombre.Trim(),Categoria=categoria,Precio=precio,Stock=stock,StockMinimo=minimo,IncluidoEnPaquete=incluido};SampleData.Productos.Add(product);return product;
    }
    public void Actualizar(ProductoModel product,string nombre,string categoria,decimal precio,int minimo,bool incluido)
    {
        if(!NavigationService.Puede("Productos e inventario"))throw new UnauthorizedAccessException("Tu rol no puede editar productos.");
        if(string.IsNullOrWhiteSpace(nombre)||precio<0||minimo<0)throw new InvalidOperationException("Revisa nombre, precio y stock mínimo.");
        product.Nombre=nombre.Trim();product.Categoria=categoria;product.Precio=precio;product.StockMinimo=minimo;product.IncluidoEnPaquete=incluido;
    }
    public void RegistrarSalida(int id,int cantidad)
    {if(!NavigationService.Puede("Consumos")&&!NavigationService.Puede("Productos e inventario"))throw new UnauthorizedAccessException();var p=SampleData.Productos.First(x=>x.Id==id);if(cantidad<1||cantidad>p.Stock)throw new InvalidOperationException("Revisa la cantidad disponible.");p.Stock-=cantidad;RegistrarMovimiento(p,"Salida",cantidad,"Salida manual");}
    public void RegistrarEntrada(int id,int cantidad,string motivo="Entrada de proveedor"){if(!NavigationService.Puede("Productos e inventario"))throw new UnauthorizedAccessException();if(cantidad<1)throw new InvalidOperationException("La cantidad debe ser positiva.");var product=SampleData.Productos.First(x=>x.Id==id);product.Stock+=cantidad;RegistrarMovimiento(product,"Entrada",cantidad,motivo);}
    public void Ajustar(int id,int diferencia,string motivo)
    {if(!NavigationService.Puede("Productos e inventario"))throw new UnauthorizedAccessException();if(diferencia==0||string.IsNullOrWhiteSpace(motivo))throw new InvalidOperationException("Indica una diferencia y el motivo del ajuste.");var product=SampleData.Productos.First(x=>x.Id==id);if(product.Stock+diferencia<0)throw new InvalidOperationException("El ajuste dejaría las existencias en negativo.");product.Stock+=diferencia;RegistrarMovimiento(product,"Ajuste",diferencia,motivo);}
    public void RegistrarConsumo(int id,int cantidad,int guestId)
    {
        RegistrarConsumos([(id,cantidad)],guestId);
    }
    public void RegistrarConsumos(IReadOnlyList<(int ProductoId,int Cantidad)> lineas,int guestId)
    {
        if(!NavigationService.Puede("Consumos"))throw new UnauthorizedAccessException("Solo el equipo de servicios registra consumos.");
        if(!SampleData.Huespedes.Any(item=>item.Id==guestId&&item.Habitacion>0))throw new InvalidOperationException("Selecciona un huésped con estancia activa.");
        if(lineas.Count==0||lineas.Any(line=>line.Cantidad<1))throw new InvalidOperationException("El pedido está vacío o tiene cantidades inválidas.");
        var products=lineas.Select(line=>(Line:line,Product:SampleData.Productos.First(item=>item.Id==line.ProductoId))).ToList();
        if(products.Any(item=>item.Line.Cantidad>item.Product.Stock))throw new InvalidOperationException("Una o más cantidades superan las existencias disponibles.");
        var guest=SampleData.Huespedes.First(item=>item.Id==guestId);
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.Estado=="Activa"&&(item.HuespedId==guestId||SampleData.ReservaHuespedes.Any(link=>link.ReservaId==item.ReservaId&&link.HuespedId==guestId)))??throw new InvalidOperationException("No se encontró la estancia activa del huésped.");
        var charges=products.Where(item=>!(item.Product.IncluidoEnPaquete&&guest.Paquete!="Sin paquete")&&item.Product.Precio*item.Line.Cantidad>0)
            .Select(item=>($"Consumo · {item.Product.Nombre} × {item.Line.Cantidad}",item.Product.Precio*item.Line.Cantidad)).ToList();
        if(charges.Count>0) new FolioService().ValidarCargosPorEstancia(charges,stay.Id);
        foreach(var (line,product) in products)
        {
            var included=product.IncluidoEnPaquete&&guest.Paquete!="Sin paquete";
            product.Stock-=line.Cantidad;
            int? cargoId=null;
            var amount=included?0m:product.Precio*line.Cantidad;
            if(amount>0)
            {
                new FolioService().RegistrarCargoPorEstancia($"Consumo · {product.Nombre} × {line.Cantidad}",amount,stay.Id);
                cargoId=SampleData.Cargos.Last().Id;
            }
            SampleData.Consumos.Add(new ConsumoModel(SampleData.Consumos.Count==0?1:SampleData.Consumos.Max(item=>item.Id)+1,guestId,product.Id,line.Cantidad,DateTime.Now,included)
            {EstanciaId=stay.Id,PrecioUnitarioAcordado=included?0m:product.Precio,CargoId=cargoId,Origen=product.Categoria});
            RegistrarMovimiento(product,"Consumo",line.Cantidad,$"Huésped {guest.Nombre}");
        }
    }
    private static void RegistrarMovimiento(ProductoModel product,string tipo,int cantidad,string motivo)=>SampleData.MovimientosInventario.Insert(0,new(SampleData.MovimientosInventario.Count+1,product.Id,DateTime.Now,tipo,cantidad,motivo));
}
public sealed class ServicioService
{
    public IReadOnlyList<ServicioModel> Listar()=>SampleData.Servicios;
    public IReadOnlyList<ServicioModel> ListarReservables()=>SampleData.Servicios.Where(item=>item.Activo&&item.Reservable).OrderBy(item=>item.Categoria).ThenBy(item=>item.Nombre).ToList();
    public ServicioModel Crear(string nombre,string categoria,decimal precio,bool requiereCita,int? padreId,string cobertura,int? usos,int duracion,string proveedor,int? productoId=null)
    {
        var manager=SesionActual.Usuario?.Rol=="Gerente";
        if(!manager&&!NavigationService.Puede("Catálogo de servicios"))throw new UnauthorizedAccessException("Tu rol no puede editar el catálogo de servicios.");
        if(string.IsNullOrWhiteSpace(nombre)||precio<0||duracion<15)throw new InvalidOperationException("Revisa el nombre, precio y duración del servicio.");
        if (!string.IsNullOrWhiteSpace(proveedor) && !proveedor.Equals("Interno",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Los proveedores se vinculan desde Convenios; no se guarda un nombre de proveedor dentro del servicio.");
        if (cobertura != "No incluido" || usos is not null) throw new InvalidOperationException("La cobertura se configura por paquete en Paquetes y promociones.");
        var service=new ServicioModel{Id=SampleData.Servicios.Count==0?1:SampleData.Servicios.Max(item=>item.Id)+1,Nombre=nombre.Trim(),Categoria=categoria.Trim(),Precio=manager?precio:0,RequiereCita=requiereCita,PadreId=padreId,DuracionMinutos=duracion};SampleData.Servicios.Add(service);
        if(productoId is int associatedProduct) SampleData.ServiciosProductos.Add(new(service.Id,associatedProduct,1));
        return service;
    }
    public void Actualizar(ServicioModel service,string nombre,string categoria,decimal precio,bool requiereCita,string cobertura,int? usos,int duracion,string proveedor,int? productoId)
    {
        var manager=SesionActual.Usuario?.Rol=="Gerente";
        if(!manager&&!NavigationService.Puede("Catálogo de servicios"))throw new UnauthorizedAccessException("Tu rol no puede editar el catálogo de servicios.");
        if(string.IsNullOrWhiteSpace(nombre)||precio<0||duracion<15)throw new InvalidOperationException("Revisa el nombre, precio y duración del servicio.");
        if (!string.IsNullOrWhiteSpace(proveedor) && !proveedor.Equals("Interno",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Los proveedores se administran mediante relaciones de Convenios.");
        if (cobertura != "No incluido" || usos is not null) throw new InvalidOperationException("La cobertura se administra en la relación del servicio con cada paquete.");
        service.Nombre=nombre.Trim();service.Categoria=categoria.Trim();if(manager)service.Precio=precio;service.RequiereCita=requiereCita;service.DuracionMinutos=duracion;
        if(productoId is int associatedProduct && !SampleData.ServiciosProductos.Any(item=>item.ServicioId==service.Id&&item.ProductoId==associatedProduct)) SampleData.ServiciosProductos.Add(new(service.Id,associatedProduct,1));
    }
    public IReadOnlyList<UsoServicioModel> ConsultarUtilizaciones(int guestId)=>SampleData.UsosServicio.Where(x=>x.HuespedId==guestId).OrderByDescending(x=>x.Fecha).ToList();
    public IReadOnlyList<UsoServicioModel> ConsultarUtilizacionesPorEstancia(int estanciaId)=>SampleData.UsosServicio.Where(x=>x.EstanciaId==estanciaId).OrderByDescending(x=>x.Fecha).ToList();
    public IReadOnlyList<SolicitudServicioModel> ConsultarSolicitudesPorEstancia(int estanciaId)=>SampleData.SolicitudesServicio.Where(x=>x.EstanciaId==estanciaId).OrderByDescending(x=>x.Solicitada).ToList();
    public IReadOnlyList<StayServiceRequestRow> ConsultarSolicitudesYCitasPorEstancia(int estanciaId)
    {
        var requests=SampleData.SolicitudesServicio.Where(item=>item.EstanciaId==estanciaId).Select(item=>new StayServiceRequestRow(item.FechaDeseada??item.Solicitada,"Solicitud",SampleData.Servicios.First(service=>service.Id==item.ServicioId).Nombre,item.Descripcion,item.Estado));
        var appointments=SampleData.Citas.Where(item=>item.EstanciaId==estanciaId).Select(item=>new StayServiceRequestRow(item.Inicio,"Cita",SampleData.Servicios.First(service=>service.Id==item.ServicioId).Nombre,item.AsignadoA,item.Estado));
        return requests.Concat(appointments).OrderBy(item=>item.Fecha).ToList();
    }
    public IReadOnlyList<ServiceBenefitRow> ConsultarBeneficios(int guestId,string? paqueteAsociado=null)
    {
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.HuespedId==guestId&&item.Estado=="Activa");
        if(stay is null)return [];
        return SampleData.Servicios.Where(service=>service.Reservable).Select(service=>
        {
            var benefit=new ServicioBeneficioService().Evaluar(stay.Id,service.Id,huespedId:guestId);
            var purchased = benefit.PurchasedRemaining;
            var remaining=benefit.CoverageType=="Ilimitado"?"Sin límite":benefit.Allowance>0?$"{benefit.Remaining} de {benefit.Allowance} usos":"—";
            var coverage = purchased > 0 ? $"Adquirido previamente · {purchased} sesión(es)" + (benefit.Remaining > 0 ? $" · {benefit.Remaining} de paquete" : "") : benefit.CoverageType=="No incluido"?"No incluido":benefit.Label;
            if (purchased > 0) remaining = $"Paquete: {remaining} · Adquiridas: {purchased}";
            return (Service: service, Benefit: benefit, Row: new ServiceBenefitRow(service.Nombre,service.Categoria,coverage,remaining,$"Bs {benefit.EstimatedCharge:N2}",service.RequiereCita));
        }).Where(item=>item.Service.Activo||item.Benefit.PurchasedRemaining>0||item.Benefit.Remaining>0||item.Benefit.CoverageType=="Ilimitado").Select(item=>item.Row).ToList();
    }
    public ServiceBenefitEvaluation EvaluarCobertura(int estanciaId,int servicioId,DateTime? fecha=null)=>new ServicioBeneficioService().Evaluar(estanciaId,servicioId,fecha);
    public IReadOnlyList<ServiceUsageHistoryRow> ConsultarHistorial(int guestId)=>ConsultarUtilizaciones(guestId).Select(item=>new ServiceUsageHistoryRow(item.Fecha,SampleData.Servicios.First(service=>service.Id==item.ServicioId).Nombre,item.Incluido?"Incluido":"Adicional",item.Cargo,item.Estado)).ToList();
    public void RegistrarUtilizacion(ServicioModel servicio,HuespedModel huesped)
        => RegistrarUtilizacion(servicio,huesped,false);
    public void RegistrarUtilizacion(ServicioModel servicio,HuespedModel huesped,bool citaConfirmada)
        => RegistrarUtilizacion(servicio,huesped,citaConfirmada,null);
    public void RegistrarUtilizacion(ServicioModel servicio,HuespedModel huesped,bool citaConfirmada,int? estanciaId)
        => RegistrarUtilizacion(servicio,huesped,citaConfirmada,estanciaId,null);
    public void RegistrarUtilizacion(ServicioModel servicio,HuespedModel huesped,bool citaConfirmada,int? estanciaId,int? solicitudId)
        => RegistrarUtilizacion(servicio,huesped,citaConfirmada,estanciaId,solicitudId,null);
    public void RegistrarUtilizacion(ServicioModel servicio,HuespedModel huesped,bool citaConfirmada,int? estanciaId,int? solicitudId,int? convenioId,DateTime? fechaProgramada = null)
    {
        if(!NavigationService.Puede("Atención de servicios"))throw new UnauthorizedAccessException("Solo el equipo de servicios confirma uso.");
        if(servicio.RequiereCita&&!citaConfirmada)throw new InvalidOperationException("Este servicio requiere una cita programada.");
        var stay=estanciaId is int id?SampleData.Estancias.FirstOrDefault(item=>item.Id==id&&item.Estado=="Activa"&&(item.HuespedId==huesped.Id||SampleData.ReservaHuespedes.Any(link=>link.ReservaId==item.ReservaId&&link.HuespedId==huesped.Id))):SampleData.Estancias.FirstOrDefault(item=>item.Estado=="Activa"&&(item.HuespedId==huesped.Id||SampleData.ReservaHuespedes.Any(link=>link.ReservaId==item.ReservaId&&link.HuespedId==huesped.Id)));
        if(stay is null)throw new InvalidOperationException("No se encontró la estancia activa del huésped.");
        var requestIndex=solicitudId is int rid?SampleData.SolicitudesServicio.FindIndex(item=>item.Id==rid):-1;
        if(solicitudId is not null&&requestIndex<0)throw new InvalidOperationException("No se encontró la solicitud asociada.");
        if(requestIndex>=0&&SampleData.SolicitudesServicio[requestIndex].ProcesamientoCompleto)throw new InvalidOperationException("La solicitud ya fue procesada; no se puede duplicar el consumo.");
        var external = new ConvenioService().EsExterno(servicio.Id);
        var serviceDate = (fechaProgramada ?? (requestIndex >= 0 ? SampleData.SolicitudesServicio[requestIndex].FechaDeseada : null) ?? DateTime.Today).Date;
        ConvenioServicioModel? providerLink = null;
        if (external)
        {
            if (convenioId is null) throw new InvalidOperationException("Selecciona un proveedor externo antes de registrar la utilización.");
            providerLink = SampleData.ConveniosServicios.FirstOrDefault(link => link.ConvenioId == convenioId && link.ServicioId == servicio.Id && link.Activo);
            var agreement = providerLink is null ? null : SampleData.Convenios.FirstOrDefault(item => item.Id == providerLink.ConvenioId);
            if (providerLink is null || agreement is null || !ConvenioService.EsVigente(agreement, serviceDate))
                throw new InvalidOperationException($"El proveedor seleccionado no tiene un convenio vigente para la fecha del servicio ({serviceDate:dd/MM/yyyy}).");
        }
        else if (convenioId is not null) throw new InvalidOperationException("Este servicio no está configurado como externo.");
        if(SampleData.UsosServicio.Any(x=>x.EstanciaId==stay.Id&&x.ServicioId==servicio.Id&&x.Estado is "Confirmado" or "Completado"&&solicitudId is null))throw new InvalidOperationException("Este servicio ya tiene una utilización registrada en esta estancia.");
        var acquired = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == stay.ReservaId && item.ServicioId == servicio.Id && item.Estado == "ADQUIRIDO" && (item.HuespedId is null || item.HuespedId == huesped.Id))
            .FirstOrDefault(item => SampleData.UsosServicio.Count(usage => usage.ServicioAdquiridoId == item.ServicioAdquiridoId && usage.Estado is "Completado" or "Confirmado") < item.Cantidad);
        var benefit=new ServicioBeneficioService().Evaluar(stay.Id,servicio.Id,null,huesped.Id);
        if(!servicio.Activo&&!benefit.Available&&requestIndex<0)throw new InvalidOperationException("El servicio está desactivado y no hay una solicitud o beneficio adquirido pendiente para esta utilización.");
        var incluido=acquired is null && benefit.CoverageType!="No incluido"&&benefit.Remaining>0;
        var charge=acquired is not null || incluido ? 0 : servicio.Precio;
        // Preserve the value attributed to this use without charging the guest again.
        // Package benefits have no reliable per-service revenue allocation in the schema.
        decimal? attributedPrice = acquired is not null ? acquired.PrecioUnitarioAcordado : incluido ? null : charge;
        var usedAt = DateTime.Now;
        var coverage = acquired is not null ? "Servicio adquirido" : incluido ? "Paquete incluido" : "Adicional";
        var usage=new UsoServicioModel(SampleData.UsosServicio.Count==0?1:SampleData.UsosServicio.Max(item=>item.Id)+1,huesped.Id,servicio.Id,usedAt,incluido,charge,"Completado")
        {EstanciaId=stay.Id,ServicioAdquiridoId=acquired?.ServicioAdquiridoId,ConvenioId=providerLink?.ConvenioId,CostoProveedorAplicado=providerLink?.CostoProveedor,PrecioAplicado=attributedPrice,Cobertura=coverage};
        SampleData.UsosServicio.Add(usage);
        var reservationServiceId = SampleData.ServiciosReserva.Count == 0 ? 1 : SampleData.ServiciosReserva.Max(item => item.Id) + 1;
        var reservationService = new ReservaServicioModel(reservationServiceId, stay.ReservaId, huesped.Id, servicio.Id, "Completado", incluido)
        { EstanciaId = stay.Id, ServicioAdquiridoId = acquired?.ServicioAdquiridoId, ConvenioId = providerLink?.ConvenioId, CostoProveedorAplicado = providerLink?.CostoProveedor, PrecioAplicado = attributedPrice, PrecioReferencia = servicio.Precio, FechaUtilizacion = usedAt, CargoId = acquired?.CargoId };
        SampleData.ServiciosReserva.Add(reservationService);
        if (acquired is not null && SampleData.UsosServicio.Count(item => item.ServicioAdquiridoId == acquired.ServicioAdquiridoId && item.Estado is "Completado" or "Confirmado") >= acquired.Cantidad)
        {
            var acquiredIndex = SampleData.ServiciosAdquiridos.FindIndex(item => item.ServicioAdquiridoId == acquired.ServicioAdquiridoId);
            if (acquiredIndex >= 0) SampleData.ServiciosAdquiridos[acquiredIndex] = acquired with { Estado = "CUMPLIDO" };
        }
        int? cargoId=null;
        if(charge>0){new FolioService().RegistrarCargoPorEstancia($"{servicio.Nombre} · {huesped.Nombre}",charge,stay.Id);cargoId=SampleData.Cargos.Last().Id;}
        if (cargoId is not null) SampleData.ServiciosReserva[SampleData.ServiciosReserva.FindIndex(item => item.Id == reservationServiceId)] = reservationService with { CargoId = cargoId };
        if(requestIndex>=0)
        {
            var request=SampleData.SolicitudesServicio[requestIndex];
            SampleData.SolicitudesServicio[requestIndex]=request with{Estado="Completada",Completada=DateTime.Now,ProcesamientoCompleto=true,UsoServicioId=usage.Id,CargoId=cargoId,ConvenioId=providerLink?.ConvenioId,CostoProveedorAplicado=providerLink?.CostoProveedor};
            if(request.CitaServicioId is int appointmentId){var appointmentIndex=SampleData.Citas.FindIndex(item=>item.Id==appointmentId);if(appointmentIndex>=0)SampleData.Citas[appointmentIndex]=SampleData.Citas[appointmentIndex] with{Estado="Completada"};}
        }
    }
}
public sealed class LimpiezaService
{
    public IReadOnlyList<TareaModel> Listar(string? estado=null,int? piso=null)=>SampleData.Tareas.Where(task=>(string.IsNullOrWhiteSpace(estado)||estado=="Todos los estados"||task.Estado==estado)&&(!piso.HasValue||task.Piso==piso)).OrderBy(task=>task.Hora).ToList();
    public IReadOnlyList<TareaModel> ListarHabitacion(int habitacion)=>SampleData.Tareas.Where(task=>task.Habitacion==habitacion).OrderByDescending(task=>task.Hora).ToList();
    public TareaModel? TareaActiva(int habitacion)=>SampleData.Tareas.Where(task=>task.Habitacion==habitacion&&task.Estado!="Inspeccionada").OrderByDescending(task=>task.Hora).FirstOrDefault();
    public void Crear(int habitacion,string tipo,string empleado,string prioridad,DateTime hora)
    {if(!NavigationService.Puede("Limpieza"))throw new UnauthorizedAccessException("Tu rol no puede crear tareas de limpieza.");var room=SampleData.Habitaciones.First(item=>item.Id==habitacion);SampleData.Tareas.Add(new(){Id=SampleData.Tareas.Max(item=>item.Id)+1,Habitacion=room.Id,Piso=room.Piso,Tipo=tipo,Empleado=empleado,Prioridad=prioridad,Hora=hora});room.Limpieza="Pendiente de limpieza";}
    public TareaModel SolicitarDesdeRecepcion(int habitacion,string motivo,string prioridad,string observaciones)
    {
        if(!NavigationService.Puede("Habitaciones"))throw new UnauthorizedAccessException("Tu rol no puede solicitar limpieza desde Habitaciones.");
        var room=SampleData.Habitaciones.FirstOrDefault(item=>item.Id==habitacion)??throw new InvalidOperationException("No se encontró la habitación.");
        if(string.IsNullOrWhiteSpace(motivo))throw new InvalidOperationException("Indica el motivo de la limpieza.");
        if(TareaActiva(habitacion) is { } active)throw new InvalidOperationException($"Ya existe la tarea #{active.Id} · {active.Estado}. Consulta su progreso en Limpieza.");
        var task=new TareaModel{Id=Persistencia.SiguienteId("tarea_limpieza","tarea_id",()=>SampleData.Tareas.Count==0?1:SampleData.Tareas.Max(item=>item.Id)+1),Habitacion=room.Id,Piso=room.Piso,Tipo=motivo,Estado="Pendiente",Empleado="",Prioridad=prioridad,Hora=DateTime.Now,MotivoSolicitud=motivo,Observaciones=observaciones.Trim(),SolicitadoPor=SesionActual.Usuario?.Nombre??"Recepción"};
        var markDirty=!(room.Estado=="Ocupada"&&room.Limpieza=="Limpia"&&motivo is "Limpieza adicional" or "Limpieza solicitada por huésped");
        OperacionesRepositorio.RegistrarSolicitudLimpieza(task,room,markDirty);
        SampleData.Tareas.Add(task);
        if(markDirty)room.Limpieza="Pendiente de limpieza";
        new NotificacionService().Crear($"Nueva solicitud de limpieza · habitación {room.Numero} · prioridad {prioridad}.","Encargado de servicios");
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Solicitud de limpieza","—",$"Habitación {room.Numero} · {motivo}"));
        return task;
    }
    internal void RegistrarCheckout(int habitacion,bool llegadaMismoDia)
    {
        if(TareaActiva(habitacion) is not null)return;
        var room=SampleData.Habitaciones.First(item=>item.Id==habitacion);
        var checkoutTask=new TareaModel{Id=Persistencia.SiguienteId("tarea_limpieza","tarea_id",()=>SampleData.Tareas.Count==0?1:SampleData.Tareas.Max(item=>item.Id)+1),Habitacion=room.Id,Piso=room.Piso,Tipo="Limpieza de salida",Estado="Pendiente",Empleado="",Prioridad=llegadaMismoDia?"Urgente":"Alta",Hora=DateTime.Now,MotivoSolicitud="Preparación después de checkout",Observaciones=llegadaMismoDia?"Hay una próxima llegada el mismo día.":"Tarea creada al completar el checkout.",SolicitadoPor="Sistema de estancia"};OperacionesRepositorio.InsertarTarea(checkoutTask);SampleData.Tareas.Add(checkoutTask);
    }
    public void Iniciar(TareaModel tarea){if(!NavigationService.Puede("Limpieza"))throw new UnauthorizedAccessException();if(tarea.Estado!="Pendiente")throw new InvalidOperationException("Solo se pueden iniciar tareas pendientes.");tarea.Estado="En progreso";SampleData.Habitaciones.First(r=>r.Id==tarea.Habitacion).Limpieza="En limpieza";}
    public void Completar(TareaModel tarea){if(!NavigationService.Puede("Limpieza"))throw new UnauthorizedAccessException();if(tarea.Estado!="En progreso")throw new InvalidOperationException("La tarea debe estar en progreso.");tarea.Estado="Completada";SampleData.Habitaciones.First(r=>r.Id==tarea.Habitacion).Limpieza="Pendiente de inspección";}
    public void Inspeccionar(TareaModel tarea,bool aprobada)
    {
        if(!NavigationService.Puede("Limpieza"))throw new UnauthorizedAccessException();if(tarea.Estado!="Completada")throw new InvalidOperationException("La tarea debe completarse antes de inspeccionarse.");
        SampleData.Inspecciones.Add(new(SampleData.Inspecciones.Count==0?1:SampleData.Inspecciones.Max(item=>item.Id)+1,tarea.Id,DateTime.Now,aprobada,aprobada?"Inspección aprobada.":"Inspección no aprobada; se requiere corrección."));
        if(aprobada){tarea.Estado="Inspeccionada";SampleData.Habitaciones.First(r=>r.Id==tarea.Habitacion).Limpieza="Limpia";}
        else{tarea.Estado="Pendiente";SampleData.Habitaciones.First(r=>r.Id==tarea.Habitacion).Limpieza="Inspección fallida";}
    }
}
public sealed class MantenimientoService
{
    public IReadOnlyList<IncidenciaModel> Listar(string? estado=null)=>SampleData.Incidencias.Where(item=>string.IsNullOrWhiteSpace(estado)||estado=="Todos los estados"||item.Estado==estado).OrderByDescending(item=>item.Prioridad=="Alta").ThenBy(item=>item.Id).ToList();
    public IReadOnlyList<IncidenciaModel> ListarHabitacion(int habitacion,bool incluirResueltas=true)=>SampleData.Incidencias.Where(item=>item.Habitacion==habitacion&&(incluirResueltas||item.Estado!="Resuelta")).OrderByDescending(item=>item.Estado!="Resuelta").ThenByDescending(item=>item.FechaReporte).ToList();
    public IReadOnlyList<OrdenMantenimientoModel> ListarOrdenes()=>SampleData.OrdenesMantenimiento;
    public IncidenciaModel Reportar(int habitacion,string descripcion,string prioridad,bool bloquea)
    {if(!NavigationService.Puede("Mantenimiento"))throw new UnauthorizedAccessException("Tu rol no puede reportar incidencias.");if(string.IsNullOrWhiteSpace(descripcion))throw new InvalidOperationException("Describe la incidencia.");var item=new IncidenciaModel{Id=SampleData.Incidencias.Count==0?1:SampleData.Incidencias.Max(i=>i.Id)+1,Habitacion=habitacion,Descripcion=descripcion.Trim(),Prioridad=prioridad,BloqueaHabitacion=bloquea};SampleData.Incidencias.Add(item);if(bloquea)ActualizarBloqueo(item,true);return item;}
    public IncidenciaModel ReportarDesdeRecepcion(int habitacion,string categoria,string descripcion,string prioridad,string impacto,string observaciones)
    {
        if(!NavigationService.Puede("Habitaciones"))throw new UnauthorizedAccessException("Tu rol no puede reportar incidencias desde Habitaciones.");
        var room=SampleData.Habitaciones.FirstOrDefault(item=>item.Id==habitacion)??throw new InvalidOperationException("No se encontró la habitación.");
        if(string.IsNullOrWhiteSpace(descripcion))throw new InvalidOperationException("Describe la incidencia.");
        var affected=SampleData.Huespedes.FirstOrDefault(guest=>guest.Habitacion==room.Id);
        var review=impacto!="Puede seguir utilizándose";
        var incident=new IncidenciaModel{Id=Persistencia.SiguienteId("incidencia","incidencia_id",()=>SampleData.Incidencias.Count==0?1:SampleData.Incidencias.Max(item=>item.Id)+1),Habitacion=room.Id,Descripcion=descripcion.Trim(),Categoria=categoria,Prioridad=prioridad,Estado=review?"Revisión operativa pendiente":"Abierta",BloqueaHabitacion=false,FechaReporte=DateTime.Now,ReportadaPor=SesionActual.Usuario?.Nombre??"Recepción",Observaciones=observaciones.Trim(),RequiereRevisionOperativa=review,GuestId=affected?.Id,ImpactoObservado=impacto};
        OperacionesRepositorio.InsertarIncidencia(incident);
        SampleData.Incidencias.Add(incident);
        if(review)new NotificacionService().Crear($"Revisión urgente de habitación {room.Numero} · {descripcion.Trim()}.","Encargado de servicios");
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Reporte de incidencia","—",$"Habitación {room.Numero} · {categoria} · {prioridad}"));
        return incident;
    }
    public void CrearOrden(IncidenciaModel incidencia,string asignado,DateTime? estimada)
    {if(!NavigationService.Puede("Mantenimiento"))throw new UnauthorizedAccessException();if(string.IsNullOrWhiteSpace(asignado))throw new InvalidOperationException("Indica quién atenderá la orden.");SampleData.OrdenesMantenimiento.Add(new(SampleData.OrdenesMantenimiento.Count+1,incidencia.Id,asignado,estimada,"Pendiente"));incidencia.Estado="En reparación";}
    public void ActualizarBloqueo(IncidenciaModel incidencia,bool bloquear)
    {if(!NavigationService.Puede("Mantenimiento"))throw new UnauthorizedAccessException("Solo Mantenimiento puede modificar bloqueos.");incidencia.BloqueaHabitacion=bloquear;new HabitacionService().Bloquear(incidencia.Habitacion,bloquear);}
    public void Resolver(IncidenciaModel incidencia){incidencia.Estado="Resuelta";if(incidencia.BloqueaHabitacion)ActualizarBloqueo(incidencia,false);}
}
public sealed class ObjetosOlvidadosService
{
    public IReadOnlyList<ObjetoOlvidadoModel> Listar(string busqueda="")=>SampleData.ObjetosOlvidados.Where(item=>string.IsNullOrWhiteSpace(busqueda)||$"{item.Descripcion} {item.Observaciones} {item.Estado}".Contains(busqueda,StringComparison.OrdinalIgnoreCase)).OrderByDescending(item=>item.Encontrado).ToList();
    public void Registrar(string descripcion,int? habitacion,string observaciones)
    {if(!NavigationService.Puede("Objetos olvidados"))throw new UnauthorizedAccessException();if(string.IsNullOrWhiteSpace(descripcion))throw new InvalidOperationException("Describe el objeto encontrado.");SampleData.ObjetosOlvidados.Add(new(SampleData.ObjetosOlvidados.Count==0?1:SampleData.ObjetosOlvidados.Max(item=>item.Id)+1,descripcion.Trim(),habitacion,DateTime.Today,"En custodia",observaciones.Trim()));}
    public void Devolver(int id){var index=SampleData.ObjetosOlvidados.FindIndex(item=>item.Id==id);if(index<0)throw new InvalidOperationException("El objeto ya no está disponible.");SampleData.ObjetosOlvidados[index]=SampleData.ObjetosOlvidados[index] with{Estado="Devuelto"};}
}

public sealed partial class EstanciaService
{
    public void ConfirmarReserva(int reservaId)
    {
        if(!NavigationService.Puede("Reservas"))throw new UnauthorizedAccessException("Tu rol no puede confirmar reservas.");
        var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==reservaId)??throw new InvalidOperationException("No se encontró la reserva.");
        if(reservation.Estado!="Pendiente")throw new InvalidOperationException("Solo se pueden confirmar reservas pendientes.");
        ReservaRepositorio.CambiarEstado(reservation.Id,"Confirmada");
        reservation.Estado="Confirmada";
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Confirmación de reserva",$"Reserva #{reservation.Id}","Confirmada"));
    }
    public EstanciaModel RealizarCheckIn(int reservaId)
    {
        if(!NavigationService.Puede("Reservas"))throw new UnauthorizedAccessException("Tu rol no puede realizar check-in.");
        var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==reservaId)??throw new InvalidOperationException("No se encontró la reserva.");
        if(reservation.Estado!="Confirmada")throw new InvalidOperationException("Confirma la reserva antes de iniciar la estancia.");
        var activeResources = SampleData.ReservaRecursos.Where(item => item.ReservaId == reservaId && item.Estado is not ("CANCELADO" or "DEVUELTO")).ToList();
        var activeServicePurchases = SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservaId && item.Estado == "ADQUIRIDO").ToList();
        if (activeResources.Any(item => item.PrecioUnitarioAcordado is null || item.TipoCobroAplicado is null)
            || activeServicePurchases.Any(item => item.PrecioUnitarioAcordado is null))
            throw new InvalidOperationException("Hay recursos o servicios adquiridos con condiciones comerciales sin verificar. Revísalos antes del check-in.");
        var nights = Math.Max(0, (reservation.Salida.Date - reservation.Llegada.Date).Days);
        var resourcePricing = new RecursoAdicionalService();
        foreach (var item in activeResources)
            resourcePricing.CalcularImporte(item.PrecioUnitarioAcordado!.Value, item.TipoCobroAplicado!, item.Cantidad, nights);
        if (activeServicePurchases.Any(item => item.Cantidad <= 0 || item.PrecioUnitarioAcordado < 0))
            throw new InvalidOperationException("Hay un servicio adquirido con cantidad o precio no válido. Revisa la reserva antes del check-in.");
        if(reservation.Llegada.Date>DateTime.Today)throw new InvalidOperationException("El check-in estará disponible desde la fecha de llegada.");
        if(reservation.Salida.Date<=DateTime.Today)throw new InvalidOperationException("La fecha de salida ya pasó; revisa la reserva.");
        if(SampleData.Estancias.Any(item=>item.ReservaId==reservaId&&item.Estado=="Activa"))throw new InvalidOperationException("La reserva ya tiene una estancia activa.");
        var roomIds=SampleData.ReservaHabitaciones.Where(item=>item.ReservaId==reservaId).Select(item=>item.HabitacionId).Distinct().ToList();
        if(roomIds.Count==0)roomIds.Add(reservation.Habitacion);
        var rooms=roomIds.Select(id=>SampleData.Habitaciones.First(item=>item.Id==id)).ToList();
        if(rooms.Any(room=>new HabitacionService().ObtenerEstadoOperativo(room.Id).Clasificacion!="Lista para ingreso"))throw new InvalidOperationException("Una de las habitaciones aún no está lista, requiere inspección o revisión, o está bloqueada. Revisa Habitaciones antes del check-in.");
        if(rooms.Any(room=>!new HabitacionDisponibilidadService().EsDisponible(room.Id,reservation.Llegada,reservation.Salida,reservation.Id)))throw new InvalidOperationException("Una habitación asignada tiene un conflicto de alojamiento.");
        var mainGuest=SampleData.ReservaHuespedes.FirstOrDefault(item=>item.ReservaId==reservaId&&item.Principal)?.HuespedId
            ??SampleData.Huespedes.FirstOrDefault(item=>item.Nombre==reservation.Huesped)?.Id
            ??throw new InvalidOperationException("Registra el huésped principal antes del check-in.");
        if(SampleData.Huespedes.First(item=>item.Id==mainGuest).Documento.Length==0)throw new InvalidOperationException("Completa el documento del huésped principal antes del check-in.");
        var primary=SampleData.Huespedes.First(item=>item.Id==mainGuest);
        var stayId=Persistencia.SiguienteId("estancia","estancia_id",()=>SampleData.Estancias.Count==0?1:SampleData.Estancias.Max(item=>item.Id)+1);
        var reservationFolio=SampleData.Folios.FirstOrDefault(item=>item.ReservaId==reservaId);
        var folioId=reservationFolio?.Id??Persistencia.SiguienteId("folio","folio_id",()=>SampleData.Folios.Count==0?1:SampleData.Folios.Max(item=>item.Id)+1);
        var stay=new EstanciaModel(stayId,reservaId,mainGuest,DateTime.Now,reservation.Salida,"Activa"){FolioId=folioId};
        // Con la base, el alojamiento se cobra al hacer el check-in: una línea por habitación con la tarifa vigente.
        var lodgingCharges=Persistencia.Activa
            ?rooms.Select(room=>(Room:room,Amount:new TarifaService().CalcularAlojamiento(room,reservation.Llegada,reservation.Salida))).Where(item=>item.Amount>0)
                .Select(item=>new CargoModel(Persistencia.SiguienteId("cargo","cargo_id",()=>SampleData.Cargos.Count==0?1:SampleData.Cargos.Max(charge=>charge.Id)+1),
                    $"Alojamiento · habitación {item.Room.Numero} · {nights} {(nights==1?"noche":"noches")}",item.Amount,DateTime.Now,mainGuest,stayId){FolioId=folioId}).ToList()
            :new List<CargoModel>();
        try{EstanciaRepositorio.RegistrarCheckIn(stay,reservationFolio is null,lodgingCharges,rooms.Select(room=>room.Id).ToList());}
        catch(Exception ex){throw new InvalidOperationException("No se pudo registrar el check-in en la base de datos. "+ex.Message,ex);}
        SampleData.Estancias.Add(stay);
        SampleData.Cargos.AddRange(lodgingCharges);
        if(reservationFolio is null)SampleData.Folios.Add(new FolioModel(folioId,stay.Id,lodgingCharges.Sum(charge=>charge.Monto),"Abierto"){HuespedPrincipalId=mainGuest,Tipo="Estancia"});
        else
        {
            var folioIndex=SampleData.Folios.FindIndex(item=>item.Id==reservationFolio.Id);
            SampleData.Folios[folioIndex]=reservationFolio with{EstanciaId=stay.Id,ReservaId=reservaId,HuespedPrincipalId=mainGuest,Tipo="Estancia"};
        }
        primary.Habitacion=rooms[0].Id;
        for(var index=0;index<rooms.Count;index++)
        {
            var room=rooms[index];room.Estado="Ocupada";
            var assignment=SampleData.ReservaHabitaciones.FirstOrDefault(item=>item.ReservaId==reservaId&&item.HabitacionId==room.Id);
            var guestId=assignment is null?null:SampleData.ReservaHabitacionHuespedes.FirstOrDefault(item=>item.ReservaHabitacionId==assignment.ReservaHabitacionId)?.HuespedId;
            var guest=guestId is int id?SampleData.Huespedes.FirstOrDefault(item=>item.Id==id):index==0?primary:null;
            if(guest is not null){guest.Habitacion=room.Id;room.Huesped=guest.Nombre;}
        }
        reservation.Estado="En estancia";
        RegistrarCargosAdquiridos(reservation, stay);
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Check-in",$"Reserva #{reservaId}","Estancia activa"));
        return stay;
    }

    private static void RegistrarCargosAdquiridos(ReservaModel reservation, EstanciaModel stay)
    {
        var folio = new FolioService();
        foreach (var item in SampleData.ReservaRecursos.Where(item => item.ReservaId == reservation.Id && item.Estado is not ("CANCELADO" or "DEVUELTO") && !item.CargoId.HasValue).ToList())
        {
            if (item.PrecioUnitarioAcordado is not decimal price || item.TipoCobroAplicado is null) throw new InvalidOperationException("Hay un recurso reservado con precio histórico sin verificar. Revísalo antes del check-in.");
            var nights = Math.Max(0, (reservation.Salida.Date - reservation.Llegada.Date).Days);
            var amount = new RecursoAdicionalService().CalcularImporte(price, item.TipoCobroAplicado, item.Cantidad, nights);
            if (amount <= 0) continue;
            folio.RegistrarCargoPorEstancia($"Recurso adicional · {SampleData.RecursosAdicionales.FirstOrDefault(resource => resource.RecursoId == item.RecursoId)?.Nombre ?? $"#{item.RecursoId}"} × {item.Cantidad}", amount, stay.Id);
            var index = SampleData.ReservaRecursos.FindIndex(value => value.ReservaId == item.ReservaId && value.RecursoId == item.RecursoId);
            if (index >= 0) SampleData.ReservaRecursos[index] = item with { CargoId = SampleData.Cargos.Last().Id };
        }
        foreach (var item in SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservation.Id && item.Estado == "ADQUIRIDO" && !item.CargoId.HasValue).ToList())
        {
            if (item.PrecioUnitarioAcordado is not decimal price) throw new InvalidOperationException("Hay un servicio adquirido con precio sin verificar. Revísalo antes del check-in.");
            var amount = price * item.Cantidad;
            if (amount <= 0) continue;
            var serviceName = SampleData.Servicios.FirstOrDefault(service => service.Id == item.ServicioId)?.Nombre ?? $"#{item.ServicioId}";
            folio.RegistrarCargoPorEstancia($"Servicio adquirido · {serviceName} × {item.Cantidad}", amount, stay.Id);
            var index = SampleData.ServiciosAdquiridos.FindIndex(value => value.ServicioAdquiridoId == item.ServicioAdquiridoId);
            if (index >= 0) SampleData.ServiciosAdquiridos[index] = item with { CargoId = SampleData.Cargos.Last().Id };
        }
    }
    public StayDirectoryMetrics ObtenerIndicadoresEstancias()
    {
        var active = SampleData.Estancias.Where(item => item.Estado == "Activa").ToList();
        var rows = active.Select(item => new { Stay = item, Row = CrearFilaDirectorio(item) }).ToList();
        return new(active.Count, active.Count(item => item.CheckOut.Date == DateTime.Today), rows.Count(item => item.Row.AttentionRequired), rows.Count(item => item.Row.Balance > 0));
    }

    public IReadOnlyList<StayDirectoryEntry> ListarActivas(string busqueda="",int? piso=null)=>Listar("Activa",busqueda,piso);
    public IReadOnlyList<StayDirectoryEntry> ListarTodas(string busqueda="",int? piso=null)=>Listar(null,busqueda,piso);
    public IReadOnlyList<StayDirectoryEntry> Listar(string? estado,string busqueda="",int? piso=null,string filtroRapido="Todas las activas",string? tipoHabitacion=null)
    {
        var rows = SampleData.Estancias.Where(stay => estado is null || stay.Estado == estado)
            .Select(CrearFilaDirectorio)
            .Where(row => piso is null || (row.Floors.Split(',', StringSplitOptions.TrimEntries).Any(value => value == piso.Value.ToString())))
            .Where(row => string.IsNullOrWhiteSpace(tipoHabitacion) || tipoHabitacion == "Todos los tipos" || row.RoomType.Split(',', StringSplitOptions.TrimEntries).Contains(tipoHabitacion, StringComparer.OrdinalIgnoreCase))
            .Where(row => string.IsNullOrWhiteSpace(busqueda) ||
                $"{row.Guest} {row.GuestDocument} {row.Room} {row.Id} {row.ReservationId} {row.Package} {row.Group} {string.Join(" ", SampleData.ReservaHuespedes.Where(link => link.ReservaId == row.ReservationId).Select(link => SampleData.Huespedes.FirstOrDefault(person => person.Id == link.HuespedId)?.Nombre))} {string.Join(" ", SampleData.ReservaHuespedes.Where(link => link.ReservaId == row.ReservationId).Select(link => SampleData.Huespedes.FirstOrDefault(person => person.Id == link.HuespedId)?.Documento))}"
                    .Contains(busqueda.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(row => filtroRapido switch
            {
                "Salidas de hoy" => row.Status == "Activa" && row.CheckOut.Date == DateTime.Today,
                "Salidas vencidas" => row.Status == "Activa" && row.CheckOut.Date < DateTime.Today,
                "Requieren atención" => row.Status == "Activa" && row.AttentionRequired,
                "Con saldo" => row.Status == "Activa" && row.Balance > 0,
                "Finalizadas" => row.Status == "Finalizada",
                _ => true
            })
            .OrderBy(row => row.CheckOut).ToList();
        return rows;
    }

    private static StayDirectoryEntry CrearFilaDirectorio(EstanciaModel stay)
    {
        var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == stay.HuespedId);
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == stay.ReservaId);
        var roomIds = SampleData.ReservaHabitaciones.Where(link => link.ReservaId == stay.ReservaId).Select(link => link.HabitacionId).Distinct().ToList();
        if (roomIds.Count == 0 && reservation is not null) roomIds.Add(reservation.Habitacion);
        if (roomIds.Count == 0 && guest is not null && guest.Habitacion > 0) roomIds.Add(guest.Habitacion);
        var rooms = roomIds.Select(id => SampleData.Habitaciones.FirstOrDefault(room => room.Id == id)).Where(room => room is not null).Select(room => room!).ToList();
        var roomNames = string.Join(", ", rooms.Select(room => room.Numero));
        var floors = string.Join(",", rooms.Select(room => room.Piso).Distinct().Order());
        var roomTypes = string.Join(",", rooms.Select(room => room.Tipo).Distinct());
        var balance = new FolioService().CalcularSaldoPorEstancia(stay.Id);
        var hasPendingResources = SampleData.ReservaRecursos.Any(item => item.ReservaId == stay.ReservaId && item.Estado is "SIN_VERIFICAR" or "PENDIENTE" or "ASIGNADO");
        var roomSet = roomIds.ToHashSet();
        var hasIncident = SampleData.Incidencias.Any(item => roomSet.Contains(item.Habitacion) && item.Estado is not ("Resuelta" or "Cerrada" or "Finalizada"));
        var attention = stay.Estado == "Activa" && (stay.CheckOut.Date < DateTime.Today || (stay.CheckOut.Date == DateTime.Today && balance > 0) || hasPendingResources || hasIncident);
        var groupName = reservation?.GrupoId is int groupId ? SampleData.GruposReservas.FirstOrDefault(group => group.Id == groupId)?.Nombre ?? "" : "";
        var packageName = SampleData.ReservaPaquetes.Where(item => item.ReservaId == stay.ReservaId)
            .Select(item => SampleData.Paquetes.FirstOrDefault(package => package.Id == item.PaqueteId)?.Nombre)
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().Aggregate("", (current, name) => current.Length == 0 ? name! : current + ", " + name);
        if (packageName.Length == 0) packageName = reservation?.Paquete ?? guest?.Paquete ?? "Sin paquete";
        return new(stay.Id, stay.ReservaId, guest?.Nombre ?? "Huésped", roomNames, rooms.FirstOrDefault()?.Piso ?? 0, packageName, stay.CheckIn, stay.CheckOut, stay.Estado, stay.HuespedId, stay.CheckOutReal)
        {
            RoomType = roomTypes, Floors = floors, GuestDocument = guest?.Documento ?? "", Group = groupName, Balance = balance, AttentionRequired = attention
        };
    }

    public StayExtensionQuote CotizarExtension(int estanciaId, DateTime nuevaSalida)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId && item.Estado == "Activa") ?? throw new InvalidOperationException("Solo se puede extender una estancia activa.");
        if (nuevaSalida.Date <= stay.CheckOut.Date) throw new InvalidOperationException("La nueva salida debe ser posterior a la salida prevista actual.");
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == stay.ReservaId) ?? throw new InvalidOperationException("No se encontró la reserva de la estancia.");
        var rooms = ObtenerHabitacionesEstancia(stay.Id);
        if (rooms.Count == 0) throw new InvalidOperationException("No hay habitaciones asignadas a esta estancia.");
        var availability = new HabitacionDisponibilidadService();
        foreach (var room in rooms)
            if (!availability.EsDisponible(room.Id, stay.CheckOut.Date, nuevaSalida.Date, stay.ReservaId))
                throw new InvalidOperationException($"La habitación {room.Numero} no está disponible durante todo el periodo solicitado.");
        foreach (var allocation in SampleData.ReservaRecursos.Where(item => item.ReservaId == stay.ReservaId && item.Estado is not ("CANCELADO" or "DEVUELTO")))
            new RecursoAdicionalService().ValidarCantidadDisponible(allocation.RecursoId, stay.CheckOut.Date, nuevaSalida.Date, allocation.Cantidad, stay.ReservaId);
        var tariff = new TarifaService();
        var lodging = rooms.Sum(room => tariff.CalcularAlojamiento(room, stay.CheckOut.Date, nuevaSalida.Date,reservation.Canal));
        var resources = SampleData.ReservaRecursos.Where(item => item.ReservaId == stay.ReservaId && item.Estado is not ("CANCELADO" or "DEVUELTO") && item.TipoCobroAplicado == "POR_NOCHE")
            .Sum(item => (item.PrecioUnitarioAcordado ?? 0m) * item.Cantidad * (nuevaSalida.Date - stay.CheckOut.Date).Days);
        return new(stay.Id, stay.ReservaId, stay.CheckOut.Date, nuevaSalida.Date, (nuevaSalida.Date - stay.CheckOut.Date).Days, rooms.Select(item => item.Numero).ToList(), lodging, resources, lodging + resources);
    }

    public StayExtensionQuote ExtenderEstancia(int estanciaId, DateTime nuevaSalida)
    {
        if (!NavigationService.Puede("Estancias")) throw new UnauthorizedAccessException("Tu rol no puede extender estancias.");
        var quote = CotizarExtension(estanciaId, nuevaSalida);
        var stayIndex = SampleData.Estancias.FindIndex(item => item.Id == estanciaId);
        var stay = SampleData.Estancias[stayIndex];
        var reservation = SampleData.Reservas.First(item => item.Id == stay.ReservaId);
        var charges = new List<(string Concepto, decimal Importe)>();
        if (quote.Lodging > 0) charges.Add(($"Alojamiento adicional · Estancia #{stay.Id} · {quote.AdditionalNights} noche(s)", quote.Lodging));
        if (quote.Resources > 0) charges.Add(($"Recursos por noche · extensión Estancia #{stay.Id} · {quote.AdditionalNights} noche(s)", quote.Resources));
        if (charges.Count > 0) new FolioService().RegistrarCargosPorEstancia(charges, stay.Id);
        SampleData.Estancias[stayIndex] = stay with { CheckOut = quote.NewDeparture };
        reservation.Salida = quote.NewDeparture;
        AuditoriaRepositorio.Registrar(new(DateTime.Now, SesionActual.Usuario?.Usuario ?? "demo", "Extensión de estancia", $"Estancia #{stay.Id}", $"Salida {quote.CurrentDeparture:dd/MM/yyyy} → {quote.NewDeparture:dd/MM/yyyy} · Bs {quote.Total:N2}"));
        return quote;
    }

    public IReadOnlyList<HabitacionModel> ObtenerHabitacionesEstancia(int estanciaId)
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == estanciaId) ?? throw new InvalidOperationException("No se encontró la estancia.");
        var reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == stay.ReservaId);
        var ids = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == stay.ReservaId).Select(item => item.HabitacionId).Distinct().ToList();
        if (ids.Count == 0 && reservation is not null) ids.Add(reservation.Habitacion);
        if (ids.Count == 0) ids.Add(SampleData.Huespedes.First(item => item.Id == stay.HuespedId).Habitacion);
        return ids.Select(id => SampleData.Habitaciones.FirstOrDefault(item => item.Id == id)).Where(item => item is not null).Select(item => item!).ToList();
    }

    public IReadOnlyList<StayActivityRow> ConsultarActividad(int estanciaId)
    {
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.Id==estanciaId)??throw new InvalidOperationException("No se encontró la estancia.");
        var items=new List<StayActivityRow>();
        var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==stay.ReservaId);
        var folioId=stay.FolioId??SampleData.Folios.FirstOrDefault(item=>item.EstanciaId==estanciaId)?.Id;
        if(reservation is not null)items.Add(new(stay.CheckIn,"Check-in",$"Reserva #{stay.ReservaId} · {reservation.Estado}","Recepción"));
        foreach(var item in SampleData.Cargos.Where(item=>item.FolioId==folioId))items.Add(new(item.Fecha,"Cargo",$"{item.Concepto} · Bs {item.Monto:N2}","Folio"));
        foreach(var item in SampleData.Pagos.Where(item=>item.FolioId==folioId))items.Add(new(item.Fecha,"Pago",$"{item.Concepto} · Bs {item.Monto:N2}",item.Metodo));
        foreach(var item in SampleData.UsosServicio.Where(item=>item.EstanciaId==estanciaId))items.Add(new(item.Fecha,"Servicio",SampleData.Servicios.First(service=>service.Id==item.ServicioId).Nombre,item.Estado));
        foreach(var item in SampleData.Consumos.Where(item=>item.EstanciaId==estanciaId))items.Add(new(item.Fecha,"Consumo",$"{SampleData.Productos.First(product=>product.Id==item.ProductoId).Nombre} × {item.Cantidad}","Inventario"));
        foreach(var item in SampleData.SolicitudesServicio.Where(item=>item.EstanciaId==estanciaId))items.Add(new(item.Solicitada,"Servicio solicitado",SampleData.Servicios.FirstOrDefault(service=>service.Id==item.ServicioId)?.Nombre??$"Servicio #{item.ServicioId}",item.Estado));
        foreach(var item in SampleData.Citas.Where(item=>item.EstanciaId==estanciaId))items.Add(new(item.Confirmada??item.Inicio,"Cita",$"{SampleData.Servicios.FirstOrDefault(service=>service.Id==item.ServicioId)?.Nombre??"Servicio"} · {item.Inicio:dd/MM/yyyy HH:mm}",item.Estado));
        foreach(var item in SampleData.CambiosHabitacion.Where(item=>item.EstanciaId==estanciaId))
        {
            var from=SampleData.Habitaciones.FirstOrDefault(room=>room.Id==item.HabitacionOrigenId)?.Numero??"—";
            var to=SampleData.Habitaciones.FirstOrDefault(room=>room.Id==item.HabitacionDestinoId)?.Numero??"—";
            items.Add(new(item.Fecha,"Cambio de habitación",$"{from} → {to} · {item.Motivo}","Recepción"));
        }
        foreach(var item in SampleData.Auditoria.Where(item=>item.Antes==$"Estancia #{estanciaId}"&&item.Cambio is "Extensión de estancia" or "Solicitud de recurso" or "Devolución de llave" or "Revocación de llave"))
            items.Add(new(item.Fecha,item.Cambio,item.Despues,item.Usuario));
        if(stay.CheckOutReal is DateTime checkout)items.Add(new(checkout,"Check-out","Estancia finalizada","Recepción"));
        return items.OrderByDescending(item=>item.Fecha).ToList();
    }

    public DateTime ObtenerSalidaPrevista(int estanciaId)=>SampleData.Estancias.FirstOrDefault(item=>item.Id==estanciaId)?.CheckOut??throw new InvalidOperationException("No se encontró la estancia.");
    public void Finalizar(int guestId)
    {
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.HuespedId==guestId&&item.Estado=="Activa")??throw new InvalidOperationException("No hay una estancia activa para este huésped.");
        FinalizarPorEstancia(stay.Id);
    }
    public void FinalizarPorEstancia(int estanciaId)
    {
        if(!NavigationService.Puede("Estancias"))throw new UnauthorizedAccessException("Tu rol no puede cerrar estancias.");
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.Id==estanciaId&&item.Estado=="Activa")??throw new InvalidOperationException("La estancia ya no está activa.");
        var reservation=SampleData.Reservas.FirstOrDefault(item=>item.Id==stay.ReservaId);
        var roomIds=reservation is null?SampleData.Huespedes.Where(item=>item.Id==stay.HuespedId&&item.Habitacion>0).Select(item=>item.Habitacion).Distinct().ToList():SampleData.ReservaHabitaciones.Where(item=>item.ReservaId==reservation.Id).Select(item=>item.HabitacionId).Distinct().ToList();
        if(roomIds.Count==0)throw new InvalidOperationException("No se encontraron habitaciones asignadas para preparar el check-out.");
        var rooms=roomIds.Select(id=>SampleData.Habitaciones.FirstOrDefault(room=>room.Id==id)??throw new InvalidOperationException("Una habitación asignada ya no existe.")).ToList();
        var folioId=stay.FolioId??SampleData.Folios.FirstOrDefault(item=>item.EstanciaId==stay.Id)?.Id??throw new InvalidOperationException("No se encontró el folio asociado. Resuelve la relación antes del check-out.");
        var folio=SampleData.Folios.FirstOrDefault(item=>item.Id==folioId)??throw new InvalidOperationException("El folio asociado ya no existe.");
        if(folio.Estado!="Abierto")throw new InvalidOperationException($"El folio está en estado {folio.Estado} y no puede liquidarse desde el check-out.");
        var balance=new FolioService().CalcularSaldoPorEstancia(estanciaId);
        if(balance>0)throw new InvalidOperationException($"El folio tiene un saldo pendiente de Bs {balance:N2}.");
        foreach(var roomId in roomIds)
        {
            var otherStay=SampleData.Estancias.FirstOrDefault(other=>other.Id!=stay.Id&&other.Estado=="Activa"&&
                (SampleData.ReservaHabitaciones.Any(link=>link.ReservaId==other.ReservaId&&link.HabitacionId==roomId)
                 ||!SampleData.ReservaHabitaciones.Any(link=>link.ReservaId==other.ReservaId)&&SampleData.Reservas.FirstOrDefault(res=>res.Id==other.ReservaId)?.Habitacion==roomId));
            if(otherStay is not null)throw new InvalidOperationException($"La habitación {SampleData.Habitaciones.First(item=>item.Id==roomId).Numero} sigue asociada a otra estancia activa #{otherStay.Id}. Revisa las asignaciones antes de liberar el espacio.");
        }
        var checkOutTime=DateTime.Now;
        try{EstanciaRepositorio.RegistrarCheckOut(stay.Id,folioId,stay.ReservaId,roomIds,checkOutTime);}
        catch(Exception ex){throw new InvalidOperationException("No se pudo registrar el check-out en la base de datos. "+ex.Message,ex);}
        var cleaning=new LimpiezaService();
        var roomPlan=rooms.Select(room=>
        {
            var arrivalToday=SampleData.Reservas.Any(item=>item.Habitacion==room.Id&&item.Id!=stay.ReservaId&&item.Llegada.Date==DateTime.Today&&item.Estado is "Confirmada" or "Pendiente");
            return (Room:room,ArrivalToday:arrivalToday,NeedsTask:cleaning.TareaActiva(room.Id) is null);
        }).ToList();
        var index=SampleData.Estancias.FindIndex(item=>item.Id==stay.Id);
        SampleData.Estancias[index]=stay with{Estado="Finalizada",CheckOutReal=checkOutTime};
        foreach(var (room,arrivalToday,needsTask) in roomPlan)
        {
            room.Estado="Disponible";room.Huesped=null;room.Limpieza="Pendiente de limpieza";
            if(needsTask)cleaning.RegistrarCheckout(room.Id,arrivalToday);
            foreach(var linkedGuest in SampleData.ReservaHabitacionHuespedes.Where(link=>link.ReservaId==stay.ReservaId&&SampleData.ReservaHabitaciones.Any(assignment=>assignment.ReservaHabitacionId==link.ReservaHabitacionId&&assignment.HabitacionId==room.Id)).Select(link=>SampleData.Huespedes.FirstOrDefault(person=>person.Id==link.HuespedId)).Where(person=>person is not null))if(linkedGuest!.Habitacion==room.Id)linkedGuest.Habitacion=0;
        }
        if(reservation is not null)reservation.Estado="Finalizada";
        var folioIndex=SampleData.Folios.FindIndex(item=>item.Id==folioId);
        SampleData.Folios[folioIndex]=folio with{Estado="Cerrado",Saldo=0};
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Check-out",$"Estancia {stay.Id}",$"Finalizada · salida prevista {stay.CheckOut:dd/MM/yyyy}"));
    }
}

public sealed class LlaveService
{
    public IReadOnlyList<LlaveModel> Listar(int guestId)=>SampleData.Estancias.Where(stay=>stay.HuespedId==guestId).SelectMany(stay=>SampleData.Llaves.Where(key=>key.EstanciaId==stay.Id)).ToList();
    public IReadOnlyList<LlaveModel> ListarPorEstancia(int estanciaId)=>SampleData.Llaves.Where(key=>key.EstanciaId==estanciaId).ToList();
    public LlaveModel EmitirPorEstancia(int estanciaId)
    {
        if(!NavigationService.Puede("Estancias"))throw new UnauthorizedAccessException();
        var stay=SampleData.Estancias.FirstOrDefault(item=>item.Id==estanciaId&&item.Estado=="Activa")??throw new InvalidOperationException("La estancia ya no está activa.");
        var guest=SampleData.Huespedes.First(item=>item.Id==stay.HuespedId);var room=SampleData.Habitaciones.First(item=>item.Id==guest.Habitacion);
        var suffix=Enumerable.Range(0,26).Select(index=>(char)('A'+index)).FirstOrDefault(candidate=>!SampleData.Llaves.Any(item=>item.EstanciaId==stay.Id&&item.Estado=="Activa"&&item.Codigo.EndsWith($"-{candidate}",StringComparison.OrdinalIgnoreCase)));
        if(suffix==default)throw new InvalidOperationException("Se alcanzó el límite de llaves activas para esta estancia.");
        var key=EstanciaRepositorio.NuevaLlave(stay.Id,$"CS-{room.Numero}-{suffix}");
        SampleData.Llaves.Add(key);
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Emisión de llave",$"Estancia #{stay.Id}",key.Codigo));
        return key;
    }
    public LlaveModel Emitir(int guestId)
    {
        if(!NavigationService.Puede("Estancias"))throw new UnauthorizedAccessException();
        var stay=SampleData.Estancias.First(item=>item.HuespedId==guestId&&item.Estado=="Activa");
        var room=SampleData.Habitaciones.First(item=>item.Id==SampleData.Huespedes.First(g=>g.Id==guestId).Habitacion);
        var key=EstanciaRepositorio.NuevaLlave(stay.Id,$"CS-{room.Numero}-{(char)('A'+SampleData.Llaves.Count%4)}");SampleData.Llaves.Add(key);return key;
    }
    public void RegistrarDevolucion(int keyId)
    {
        if(!NavigationService.Puede("Estancias"))throw new UnauthorizedAccessException("Tu rol no puede administrar llaves.");
        var index=SampleData.Llaves.FindIndex(item=>item.Id==keyId);if(index<0)throw new InvalidOperationException("No se encontró la llave.");
        var key=SampleData.Llaves[index];
        if(key.Estado!="Activa")throw new InvalidOperationException("Solo se puede registrar la devolución de una llave activa.");
        SampleData.Llaves[index]=key with{Estado="Devuelta"};
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Devolución de llave",$"Estancia #{key.EstanciaId}",key.Codigo));
    }
    public void Revocar(int keyId)
    {
        if(!NavigationService.Puede("Estancias"))throw new UnauthorizedAccessException("Tu rol no puede administrar llaves.");
        var index=SampleData.Llaves.FindIndex(item=>item.Id==keyId);if(index<0)throw new InvalidOperationException("No se encontró la llave.");
        var key=SampleData.Llaves[index];
        if(key.Estado!="Activa")throw new InvalidOperationException("Solo se puede revocar una llave activa.");
        EstanciaRepositorio.CambiarEstadoLlave(keyId,"Revocada");
        SampleData.Llaves[index]=key with{Estado="Revocada"};
        AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo","Revocación de llave",$"Estancia #{key.EstanciaId}",key.Codigo));
    }
}

public sealed record OpcionNavegacion(string Grupo,string Nombre,string Icono);
public static class NavigationService
{
    private static readonly Dictionary<string,string[]> Accesos=new()
    {
        ["Administrador"]=["Inicio","Empleados","Usuarios y roles","Configuración del hotel","Habitaciones y pisos","Módulos y parámetros","Auditoría"],
        ["Recepcionista"]=["Inicio","Reservas","Habitaciones","Huéspedes","Estancias","Caja y facturación","Servicios"],
        ["Encargado de servicios"]=["Inicio","Atención de servicios","Agenda de servicios","Catálogo de servicios","Consumos","Productos e inventario","Limpieza","Mantenimiento","Objetos olvidados"],
        ["Gerente"]=["Inicio","Tarifas y temporadas","Paquetes y promociones","Convenios","Catálogo de servicios","Finanzas","Operaciones","Reportes","Políticas del hotel"]
    };
    public static bool Puede(string modulo)=>SesionActual.Usuario is { } u&&Accesos.TryGetValue(u.Rol,out var items)&&items.Contains(modulo,StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyList<OpcionNavegacion> ParaRol(string rol)=>rol switch
    {
        "Administrador"=>[O("General","Inicio","⌂"),O("Administración","Empleados","♙"),O("Administración","Usuarios y roles","⚿"),O("Administración","Configuración del hotel","⚙"),O("Administración","Habitaciones y pisos","▦"),O("Sistema","Módulos y parámetros","◈"),O("Sistema","Auditoría","≡")],
        "Recepcionista"=>[O("General","Inicio","⌂"),O("Recepción","Reservas","▦"),O("Recepción","Habitaciones","▤"),O("Recepción","Huéspedes","♙"),O("Recepción","Estancias","▣"),O("Operaciones","Caja y facturación","Bs"),O("Operaciones","Servicios","✧")],
        "Encargado de servicios"=>[O("General","Inicio","⌂"),O("Servicios","Atención de servicios","✧"),O("Servicios","Agenda de servicios","◷"),O("Servicios","Catálogo de servicios","☷"),O("Servicios","Consumos","＋"),O("Servicios","Productos e inventario","▤"),O("Instalaciones","Limpieza","✓"),O("Instalaciones","Mantenimiento","⚒"),O("Instalaciones","Objetos olvidados","⌕")],
        _=>[O("General","Inicio","⌂"),O("Gestión comercial","Tarifas y temporadas","◷"),O("Gestión comercial","Paquetes y promociones","◇"),O("Gestión comercial","Convenios","⇄"),O("Gestión comercial","Catálogo de servicios","✧"),O("Supervisión","Finanzas","Bs"),O("Supervisión","Operaciones","▦"),O("Supervisión","Reportes","▥"),O("Supervisión","Políticas del hotel","⚙")]
    };
    private static OpcionNavegacion O(string g,string n,string i)=>new(g,n,i);
}
