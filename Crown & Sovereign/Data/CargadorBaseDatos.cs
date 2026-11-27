using System.Text.RegularExpressions;
using Crown___Sovereign.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown___Sovereign.Data;

/// <summary>
/// Llena SampleData con lo que hay en PostgreSQL. Las vistas siguen leyendo las mismas colecciones;
/// aquí solo se traducen las filas de la base a los modelos que ya usa la interfaz.
/// </summary>
public static class CargadorBaseDatos
{
    public static void Cargar()
    {
        Persistencia.Cargando = true;
        try
        {
            LimpiarDemostracion();
            CargarCatalogos();
            CargarPoliticas();
            CargarHuespedesYHabitaciones();
            ComercialRepositorio.CargarTarifas();
            CargarReservas(null);
            CargarComplementos();
            CargarEstancias();
            CargarOperacion();
            CargarServicios();
            CargarFinanzas();
        }
        finally { Persistencia.Cargando = false; }
    }

    // Colecciones que SampleData trae con datos de demostración ya en su declaración.
    private static void LimpiarDemostracion()
    {
        SampleData.Usuarios.Clear();
        SampleData.Roles.Clear();
        SampleData.Temporadas.Clear();
        SampleData.Paquetes.Clear();
        SampleData.Promociones.Clear();
        SampleData.Convenios.Clear();
        SampleData.Politicas.Clear();
        SampleData.PoliticasServicio.Clear();
        SampleData.HistorialPoliticas.Clear();
        SampleData.MetodosPago.Clear();
        SampleData.Tarifas.Clear();
        SampleData.AperturaCaja = 0m;
        SampleData.CajaAbierta = false;
    }

    private static void CargarPoliticas()
    {
        var hotelId = Convert.ToInt32(DatabaseConnection.Escalar("SELECT hotel_id FROM public.hotel ORDER BY hotel_id LIMIT 1")
            ?? throw new InvalidOperationException("No se encontró un hotel para cargar sus políticas."));
        SampleData.Politicas.AddRange(DatabaseConnection.Consultar(
            "SELECT politica_id, nombre, COALESCE(descripcion, ''), COALESCE(valor, '') FROM public.politica WHERE hotel_id = @hotel ORDER BY politica_id",
            l => new PoliticaModel(l.GetInt32(0), l.GetString(1), l.GetString(2), l.GetString(3)), DatabaseConnection.Parametro("hotel", hotelId)));

        SampleData.PoliticasServicio.AddRange(DatabaseConnection.Consultar(@"SELECT hotel_id, horas_cancelacion_gratuita,
                tipo_penalizacion_inasistencia, valor_penalizacion_inasistencia, tipo_penalizacion_cancelacion,
                valor_penalizacion_cancelacion, consumir_beneficio_inasistencia, consumir_beneficio_cancelacion_tardia
            FROM public.politica_servicio WHERE hotel_id = @hotel",
            l => new PoliticaServicioModel(l.GetInt32(0), l.GetInt32(1), l.GetString(2), l.GetDecimal(3), l.GetString(4), l.GetDecimal(5), l.GetBoolean(6), l.GetBoolean(7)),
            DatabaseConnection.Parametro("hotel", hotelId)));

        SampleData.HistorialPoliticas.AddRange(DatabaseConnection.Consultar(@"SELECT audit_id, hotel_id, usuario_id, cambio,
                COALESCE(valor_anterior, ''), COALESCE(valor_nuevo, ''), fecha FROM public.configuracion_audit
            WHERE hotel_id = @hotel ORDER BY fecha DESC LIMIT 500",
            l => new ConfiguracionAuditModel(l.GetInt32(0), l.GetInt32(1), l.IsDBNull(2) ? 0 : l.GetInt32(2), l.GetString(3), l.GetString(4), l.GetString(5), l.GetDateTime(6)),
            DatabaseConnection.Parametro("hotel", hotelId)));
    }

    private static void CargarCatalogos()
    {
        var empleados = DatabaseConnection.Consultar("SELECT empleado_id, nombre, apellido, email, telefono FROM public.empleado ORDER BY empleado_id",
            l => new { Id = l.GetInt32(0), Nombre = $"{l.GetString(1)} {l.GetString(2)}".Trim(), Email = Texto(l, 3), Telefono = Texto(l, 4) });

        var usuarios = DatabaseConnection.Consultar(@"SELECT u.usuario_id, u.username, u.empleado_id, COALESCE(u.estado, 'ACTIVO'), r.nombre
                                                     FROM public.usuario u JOIN public.rol r ON r.rol_id = u.rol_id ORDER BY u.usuario_id",
            l => new { Id = l.GetInt32(0), Usuario = l.GetString(1), EmpleadoId = l.IsDBNull(2) ? (int?)null : l.GetInt32(2), Estado = l.GetString(3), Rol = RolDeInterfaz(l.GetString(4)) });

        foreach (var u in usuarios)
        {
            var empleado = empleados.FirstOrDefault(e => e.Id == u.EmpleadoId);
            SampleData.Usuarios.Add(new UsuarioModel(u.Id, u.Usuario, "", empleado?.Nombre ?? u.Usuario, u.Rol, u.Estado.Equals("ACTIVO", StringComparison.OrdinalIgnoreCase)));
        }
        foreach (var e in empleados)
        {
            var usuario = usuarios.FirstOrDefault(u => u.EmpleadoId == e.Id);
            SampleData.Empleados.Add(new EmpleadoModel(e.Id, e.Nombre, "", e.Email, e.Telefono, usuario?.Rol));
        }

        // La base tiene los roles repetidos en mayúsculas; se muestran una sola vez con el nombre de la interfaz.
        foreach (var rol in DatabaseConnection.Consultar("SELECT rol_id, nombre, descripcion FROM public.rol ORDER BY rol_id", l => new RolModel(l.GetInt32(0), RolDeInterfaz(l.GetString(1)), Texto(l, 2))))
            if (SampleData.Roles.All(r => r.Nombre != rol.Nombre)) SampleData.Roles.Add(rol);

        foreach (var m in DatabaseConnection.Consultar("SELECT metodo_pago_id, nombre, COALESCE(activo, true), es_efectivo FROM public.metodo_pago ORDER BY metodo_pago_id",
                     l => new MetodoPagoModel(l.GetInt32(0), l.GetString(1), l.GetBoolean(2), l.GetBoolean(3))))
            SampleData.MetodosPago.Add(m);

        var hotel = DatabaseConnection.Consultar("SELECT nombre, direccion, categoria, telefono, email FROM public.hotel ORDER BY hotel_id LIMIT 1",
            l => new { Nombre = l.GetString(0), Direccion = Texto(l, 1), Categoria = l.IsDBNull(2) ? 0 : l.GetInt32(2), Telefono = Texto(l, 3), Correo = Texto(l, 4) }).FirstOrDefault();
        if (hotel is not null)
        {
            SampleData.ConfiguracionHotel["Nombre"] = hotel.Nombre;
            SampleData.ConfiguracionHotel["Dirección"] = hotel.Direccion;
            SampleData.ConfiguracionHotel["Categoría"] = hotel.Categoria > 0 ? $"{hotel.Categoria} estrellas" : "";
            SampleData.ConfiguracionHotel["Teléfono"] = hotel.Telefono ?? "";
            SampleData.ConfiguracionHotel["Correo"] = hotel.Correo ?? "";
        }
    }

    private static void CargarHuespedesYHabitaciones()
    {
        var tipos = DatabaseConnection.Consultar("SELECT tipo_habitacion_id, nombre, capacidad FROM public.tipo_habitacion ORDER BY tipo_habitacion_id",
            l => new { Id = l.GetInt32(0), Nombre = l.GetString(1), Capacidad = l.GetInt32(2) });
        SampleData.Tipos = tipos.Select(t => t.Nombre).ToArray();

        var precios = DatabaseConnection.Consultar("SELECT tipo_habitacion_id, MIN(precio_base) FROM public.tarifa GROUP BY tipo_habitacion_id",
            l => (Tipo: l.GetInt32(0), Precio: l.GetDecimal(1))).ToDictionary(x => x.Tipo, x => x.Precio);

        var comodidades = DatabaseConnection.Consultar(@"SELECT hc.habitacion_id, c.nombre FROM public.habitacion_comodidad hc
                                                         JOIN public.comodidad c ON c.comodidad_id = hc.comodidad_id WHERE hc.disponible ORDER BY c.nombre",
            l => (Habitacion: l.GetInt32(0), Nombre: l.GetString(1))).GroupBy(x => x.Habitacion).ToDictionary(g => g.Key, g => string.Join(" · ", g.Select(x => x.Nombre)));

        var bloqueadas = DatabaseConnection.Consultar("SELECT DISTINCT habitacion_id FROM public.bloqueo_habitacion WHERE fecha_fin IS NULL OR fecha_fin > LOCALTIMESTAMP",
            l => l.GetInt32(0)).ToHashSet();

        var filas = DatabaseConnection.Consultar(@"SELECT h.habitacion_id, h.numero, p.nombre, h.tipo_habitacion_id, h.estado, COALESCE(h.estado_limpieza, '')
                                                  FROM public.habitacion h JOIN public.piso p ON p.piso_id = h.piso_id ORDER BY h.habitacion_id",
            l => new { Id = l.GetInt32(0), Numero = l.GetString(1), Piso = NumeroDe(l.GetString(2)), TipoId = l.GetInt32(3), Estado = l.GetString(4), Limpieza = l.GetString(5) });

        foreach (var h in filas)
        {
            var tipo = tipos.First(t => t.Id == h.TipoId);
            bool bloqueada = h.Estado is "BLOQUEADA" or "MANTENIMIENTO" or "FUERA_DE_SERVICIO" || bloqueadas.Contains(h.Id);
            SampleData.Habitaciones.Add(new HabitacionModel
            {
                Id = h.Id, Numero = h.Numero, Piso = h.Piso, Tipo = tipo.Nombre, Capacidad = tipo.Capacidad,
                Estado = h.Estado == "OCUPADA" ? "Ocupada" : "Disponible",
                Limpieza = LimpiezaDeInterfaz(h.Estado, h.Limpieza), Bloqueada = bloqueada,
                Comodidades = comodidades.GetValueOrDefault(h.Id, "")
            });
            SampleData.Tarifas[h.Id] = precios.GetValueOrDefault(h.TipoId, 0m);
        }

        var consentimientos = DatabaseConnection.Consultar("SELECT huesped_id, autorizado FROM public.consentimiento ORDER BY fecha",
            l => (Huesped: l.GetInt32(0), Autorizado: l.GetBoolean(1))).GroupBy(x => x.Huesped).ToDictionary(g => g.Key, g => g.Last().Autorizado);

        foreach (var g in DatabaseConnection.Consultar("SELECT huesped_id, nombre, apellido, documento, email, telefono FROM public.huesped ORDER BY huesped_id",
                     l => new HuespedModel
                     {
                         Id = l.GetInt32(0), Nombre = $"{l.GetString(1)} {l.GetString(2)}".Trim(), Documento = Texto(l, 3),
                         Email = Texto(l, 4), Telefono = Texto(l, 5)
                     }))
        {
            g.ConsentimientoDatos = consentimientos.GetValueOrDefault(g.Id, true);
            SampleData.Huespedes.Add(g);
        }
    }

    /// <summary>Carga las reservas y lo que cuelga de ellas; con un id carga solo esa reserva.</summary>
    internal static void CargarReservas(int? soloReserva)
    {
        bool aislada = soloReserva is not null;
        if (aislada) Persistencia.Cargando = true;
        try
        {
            var filtro = new NpgsqlParameter("id", NpgsqlDbType.Integer) { Value = (object?)soloReserva ?? DBNull.Value };

            var paquetes = DatabaseConnection.Consultar(@"SELECT rp.reserva_id, p.nombre FROM public.reserva_paquete rp JOIN public.paquete p ON p.paquete_id = rp.paquete_id
                                                          WHERE (@id::int IS NULL OR rp.reserva_id = @id) ORDER BY rp.reserva_id",
                l => (Reserva: l.GetInt32(0), Nombre: l.GetString(1)), Clonar(filtro)).GroupBy(x => x.Reserva).ToDictionary(g => g.Key, g => g.First().Nombre);
            var promociones = DatabaseConnection.Consultar("SELECT reserva_id, promocion_id FROM public.reserva_promocion WHERE (@id::int IS NULL OR reserva_id = @id)",
                l => (Reserva: l.GetInt32(0), Promocion: l.GetInt32(1)), Clonar(filtro)).GroupBy(x => x.Reserva).ToDictionary(g => g.Key, g => g.First().Promocion);

            var habitaciones = DatabaseConnection.Consultar(@"SELECT reserva_habitacion_id, reserva_id, habitacion_id FROM public.reserva_habitacion
                                                              WHERE (@id::int IS NULL OR reserva_id = @id) ORDER BY reserva_habitacion_id",
                l => new ReservaHabitacionModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2)), Clonar(filtro));
            SampleData.ReservaHabitaciones.AddRange(habitaciones);

            foreach (var r in DatabaseConnection.Consultar(@"SELECT r.reserva_id, r.huesped_principal_id, c.nombre, r.fecha_entrada, r.fecha_salida, r.estado, r.grupo_id, r.cantidad_personas, r.fecha_reserva
                                                             FROM public.reserva r JOIN public.canal_reserva c ON c.canal_id = r.canal_id
                                                             WHERE (@id::int IS NULL OR r.reserva_id = @id) ORDER BY r.reserva_id",
                         l => new { Id = l.GetInt32(0), Principal = l.GetInt32(1), Canal = l.GetString(2), Llegada = l.GetDateTime(3), Salida = l.GetDateTime(4),
                                    Estado = l.GetString(5), Grupo = l.IsDBNull(6) ? (int?)null : l.GetInt32(6), Personas = l.GetInt32(7),
                                    Creada = l.IsDBNull(8) ? (DateTime?)null : l.GetDateTime(8) }, Clonar(filtro)))
            {
                var principal = SampleData.Huespedes.FirstOrDefault(h => h.Id == r.Principal);
                SampleData.Reservas.Add(new ReservaModel
                {
                    Id = r.Id, Huesped = principal?.Nombre ?? "", Habitacion = habitaciones.FirstOrDefault(x => x.ReservaId == r.Id)?.HabitacionId ?? 0,
                    Llegada = r.Llegada, Salida = r.Salida, Estado = Traducciones.EstadoReservaInterfaz(r.Estado), Canal = Traducciones.CanalInterfaz(r.Canal),
                    Paquete = paquetes.GetValueOrDefault(r.Id, "Sin paquete"), GrupoId = r.Grupo, PromocionId = promociones.TryGetValue(r.Id, out var promo) ? promo : null,
                    Personas = r.Personas, FechaCreacion = r.Creada
                });
                SampleData.ReservaHuespedes.Add(new ReservaHuespedModel(r.Id, r.Principal, true));
            }

            foreach (var noche in DatabaseConnection.Consultar(@"SELECT rh.reserva_id, rh.habitacion_id, n.noche, n.precio FROM public.reserva_habitacion_noche n
                                                               JOIN public.reserva_habitacion rh ON rh.reserva_habitacion_id = n.reserva_habitacion_id
                                                               WHERE (@id::int IS NULL OR rh.reserva_id = @id) ORDER BY rh.reserva_id, n.noche",
                         l => (Reserva: l.GetInt32(0), Habitacion: l.GetInt32(1), Noche: l.GetDateTime(2), Precio: l.GetDecimal(3)), Clonar(filtro)))
                SampleData.Reservas.FirstOrDefault(x => x.Id == noche.Reserva)?.AlojamientoNochesAcordadas.Add(new PrecioAlojamientoAcordadoModel(noche.Habitacion, noche.Noche, noche.Precio));

            foreach (var rh in DatabaseConnection.Consultar("SELECT reserva_id, huesped_id FROM public.reserva_huesped WHERE (@id::int IS NULL OR reserva_id = @id)",
                         l => (Reserva: l.GetInt32(0), Huesped: l.GetInt32(1)), Clonar(filtro)))
                if (!SampleData.ReservaHuespedes.Any(x => x.ReservaId == rh.Reserva && x.HuespedId == rh.Huesped))
                    SampleData.ReservaHuespedes.Add(new ReservaHuespedModel(rh.Reserva, rh.Huesped, false));

            SampleData.ReservaHabitacionHuespedes.AddRange(DatabaseConnection.Consultar(
                "SELECT reserva_habitacion_id, reserva_id, huesped_id FROM public.reserva_habitacion_huesped WHERE (@id::int IS NULL OR reserva_id = @id)",
                l => new ReservaHabitacionHuespedModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2)), Clonar(filtro)));

            SampleData.SolicitudesEspeciales.AddRange(DatabaseConnection.Consultar(
                "SELECT solicitud_id, reserva_id, descripcion, COALESCE(estado, 'PENDIENTE') FROM public.solicitud_especial WHERE (@id::int IS NULL OR reserva_id = @id) ORDER BY solicitud_id DESC",
                l => new SolicitudEspecialModel(l.GetInt32(0), l.GetInt32(1), l.GetString(2), Traducciones.Capitalizar(l.GetString(3))), Clonar(filtro)));

            SampleData.ReservaPaquetes.AddRange(DatabaseConnection.Consultar(
                "SELECT reserva_id, paquete_id, cantidad, COALESCE(precio_unitario_acordado, 0) FROM public.reserva_paquete WHERE (@id::int IS NULL OR reserva_id = @id)",
                l => new ReservaPaqueteModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.GetDecimal(3)), Clonar(filtro)));

            SampleData.ReservaRecursos.AddRange(DatabaseConnection.Consultar(
                @"SELECT reserva_id, recurso_id, cantidad, precio_unitario_acordado, tipo_cobro_aplicado, COALESCE(estado, 'PENDIENTE'), COALESCE(observaciones, ''), cargo_id
                  FROM public.reserva_recurso WHERE (@id::int IS NULL OR reserva_id = @id)",
                l => new ReservaRecursoModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.IsDBNull(3) ? null : l.GetDecimal(3), Texto(l, 4), l.GetString(5), l.GetString(6),
                    l.IsDBNull(7) ? null : l.GetInt32(7)), Clonar(filtro)));

            SampleData.ServiciosAdquiridos.AddRange(DatabaseConnection.Consultar(
                @"SELECT servicio_adquirido_id, reserva_id, servicio_id, huesped_id, reserva_habitacion_id, cantidad, precio_unitario_acordado, COALESCE(fecha_registro, LOCALTIMESTAMP),
                         COALESCE(estado, 'ADQUIRIDO'), cargo_id, COALESCE(observaciones, '')
                  FROM public.reserva_servicio_adquirido WHERE (@id::int IS NULL OR reserva_id = @id) ORDER BY servicio_adquirido_id",
                l => new ReservaServicioAdquiridoModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.IsDBNull(3) ? null : l.GetInt32(3), l.IsDBNull(4) ? null : l.GetInt32(4),
                    l.GetInt32(5), l.IsDBNull(6) ? null : l.GetDecimal(6), l.GetDateTime(7), l.GetString(8), l.IsDBNull(9) ? null : l.GetInt32(9), l.GetString(10)), Clonar(filtro)));
        }
        finally { if (aislada) Persistencia.Cargando = false; }
    }

    // Grupos, recursos adicionales, lista de espera, historial de consentimientos y notificaciones.
    private static void CargarComplementos()
    {
        ComercialRepositorio.CargarCatalogo();
        SampleData.GruposReservas.AddRange(DatabaseConnection.Consultar(
            "SELECT grupo_id, nombre, COALESCE(fecha_creacion, LOCALTIMESTAMP) FROM public.grupo_reserva ORDER BY grupo_id",
            l => new GrupoReservaModel(l.GetInt32(0), l.GetString(1), l.GetDateTime(2))));

        SampleData.RecursosAdicionales.AddRange(DatabaseConnection.Consultar(
            @"SELECT recurso_id, nombre, COALESCE(tipo, ''), COALESCE(precio, 0), COALESCE(descripcion, ''), COALESCE(activo, true), COALESCE(tipo_cobro, 'POR_ESTANCIA'), stock_total, COALESCE(requiere_preparacion, false)
              FROM public.recurso_adicional ORDER BY recurso_id",
            l => new RecursoAdicionalModel
            {
                RecursoId = l.GetInt32(0), Nombre = l.GetString(1), Tipo = l.GetString(2), Precio = l.GetDecimal(3), Descripcion = l.GetString(4), Activo = l.GetBoolean(5),
                TipoCobro = l.GetString(6), StockTotal = l.IsDBNull(7) ? null : l.GetInt32(7), RequierePreparacion = l.GetBoolean(8)
            }));

        foreach (var e in DatabaseConnection.Consultar(
                     @"SELECT e.lista_espera_id, e.huesped_id, t.nombre, e.fecha_entrada, e.fecha_salida, COALESCE(e.estado, 'PENDIENTE'), COALESCE(e.cantidad_personas, 1), COALESCE(e.cantidad_habitaciones, 1), COALESCE(e.observaciones, '')
                       FROM public.lista_espera e JOIN public.tipo_habitacion t ON t.tipo_habitacion_id = e.tipo_habitacion_id ORDER BY e.lista_espera_id DESC",
                     l => new { Id = l.GetInt32(0), Huesped = l.GetInt32(1), Tipo = l.GetString(2), Llegada = l.GetDateTime(3), Salida = l.GetDateTime(4), Estado = l.GetString(5), Personas = l.GetInt32(6), Habitaciones = l.GetInt32(7), Nota = l.GetString(8) }))
        {
            var huesped = SampleData.Huespedes.FirstOrDefault(h => h.Id == e.Huesped);
            if (huesped is null) continue;
            SampleData.ListaEspera.Add(new ListaEsperaModel(e.Id, huesped.Nombre, e.Llegada, e.Salida, e.Tipo, Capitalizar(e.Estado), huesped.Id, e.Personas, e.Habitaciones, e.Nota));
        }

        SampleData.ContactosListaEspera.AddRange(DatabaseConnection.Consultar(
            @"SELECT c.contacto_id, c.lista_espera_id, c.fecha, c.medio, c.resultado, COALESCE(c.observaciones, ''), COALESCE(u.username, '')
              FROM public.lista_espera_contacto c LEFT JOIN public.usuario u ON u.usuario_id = c.usuario_id ORDER BY c.fecha DESC, c.contacto_id DESC",
            l => new ContactoListaEsperaModel(l.GetInt32(0), l.GetInt32(1), l.GetDateTime(2), l.GetString(3), l.GetString(4), l.GetString(5), l.GetString(6))));

        SampleData.Auditoria.AddRange(DatabaseConnection.Consultar(
            @"SELECT a.fecha, COALESCE(u.username, ''), a.accion, COALESCE(a.antes #>> '{}', ''), COALESCE(a.despues #>> '{}', '')
              FROM public.auditoria_accion a LEFT JOIN public.usuario u ON u.usuario_id = a.usuario_id ORDER BY a.fecha DESC, a.auditoria_id DESC",
            l => new AuditoriaModel(l.GetDateTime(0), l.GetString(1), l.GetString(2), l.GetString(3), l.GetString(4))));

        var huespedesCambio = DatabaseConnection.Consultar("SELECT cambio_id, huesped_id FROM public.cambio_habitacion_huesped",
            l => (Cambio: l.GetInt32(0), Huesped: l.GetInt32(1))).ToLookup(x => x.Cambio, x => x.Huesped);
        SampleData.CambiosHabitacion.AddRange(DatabaseConnection.Consultar(
            "SELECT cambio_id, reserva_id, COALESCE(estancia_id, 0), habitacion_origen_id, habitacion_destino_id, fecha, motivo, COALESCE(observaciones, '') FROM public.cambio_habitacion ORDER BY fecha, cambio_id",
            l => new CambioHabitacionModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.GetInt32(3), l.GetInt32(4), huespedesCambio[l.GetInt32(0)].ToList(), l.GetDateTime(5), l.GetString(6), l.GetString(7))));

        // La base guarda una fila por cada cambio; el estado anterior es el de la fila previa del mismo huésped.
        var ultimo = new Dictionary<int, bool>();
        foreach (var c in DatabaseConnection.Consultar(
                     "SELECT consentimiento_id, huesped_id, autorizado, COALESCE(fecha, LOCALTIMESTAMP), usuario_id, COALESCE(observaciones, '') FROM public.consentimiento ORDER BY huesped_id, fecha, consentimiento_id",
                     l => new { Id = l.GetInt32(0), Huesped = l.GetInt32(1), Autorizado = l.GetBoolean(2), Fecha = l.GetDateTime(3), Usuario = l.IsDBNull(4) ? (int?)null : l.GetInt32(4), Nota = l.GetString(5) }))
        {
            SampleData.ConsentimientosHistorial.Add(new ConsentimientoHistorialModel(c.Id, c.Huesped, ultimo.GetValueOrDefault(c.Huesped, true), c.Autorizado, c.Fecha,
                SampleData.Usuarios.FirstOrDefault(u => u.Id == c.Usuario)?.Nombre ?? "Recepción", c.Nota));
            ultimo[c.Huesped] = c.Autorizado;
        }

        SampleData.Notificaciones.AddRange(DatabaseConnection.Consultar(
            @"SELECT n.notificacion_id, n.mensaje, n.titulo, COALESCE(n.fecha, LOCALTIMESTAMP),
                     EXISTS (SELECT 1 FROM public.notificacion_recepcion r WHERE r.notificacion_id = n.notificacion_id AND r.fecha_recepcion IS NOT NULL)
              FROM public.notificacion n ORDER BY n.fecha DESC, n.notificacion_id DESC",
            l => new NotificacionModel(l.GetInt32(0), l.GetString(1), l.GetString(2), l.GetDateTime(3), l.GetBoolean(4))));
    }

    // Cada consulta necesita su propia copia del parámetro.
    private static NpgsqlParameter Clonar(NpgsqlParameter original) => new(original.ParameterName, NpgsqlDbType.Integer) { Value = original.Value };

    private static void CargarEstancias()
    {
        var folios = DatabaseConnection.Consultar("SELECT folio_id, estancia_id, COALESCE(saldo, 0), COALESCE(estado, 'ABIERTO'), reserva_id, tipo FROM public.folio ORDER BY folio_id",
            l => new { Id = l.GetInt32(0), Estancia = l.IsDBNull(1) ? (int?)null : l.GetInt32(1), Saldo = l.GetDecimal(2), Estado = l.GetString(3),
                       Reserva = l.IsDBNull(4) ? (int?)null : l.GetInt32(4) });

        foreach (var e in DatabaseConnection.Consultar("SELECT estancia_id, reserva_id, fecha_checkin, fecha_checkout, COALESCE(estado, 'ALOJADO') FROM public.estancia ORDER BY estancia_id",
                     l => new { Id = l.GetInt32(0), Reserva = l.GetInt32(1), CheckIn = l.IsDBNull(2) ? (DateTime?)null : l.GetDateTime(2),
                                CheckOut = l.IsDBNull(3) ? (DateTime?)null : l.GetDateTime(3), Estado = l.GetString(4) }))
        {
            var reserva = SampleData.Reservas.FirstOrDefault(r => r.Id == e.Reserva);
            if (reserva is null) continue;
            var huesped = SampleData.ReservaHuespedes.First(x => x.ReservaId == e.Reserva && x.Principal).HuespedId;
            bool activa = e.Estado.Equals("ALOJADO", StringComparison.OrdinalIgnoreCase);
            SampleData.Estancias.Add(new EstanciaModel(e.Id, e.Reserva, huesped, e.CheckIn ?? reserva.Llegada, reserva.Salida, activa ? "Activa" : "Finalizada")
            { FolioId = folios.FirstOrDefault(f => f.Estancia == e.Id)?.Id, CheckOutReal = e.CheckOut });

            if (activa)
            {
                // Quien está alojado: la habitación pasa a mostrar su huésped y a quienes comparten la reserva.
                var habitacion = SampleData.Habitaciones.FirstOrDefault(h => h.Id == reserva.Habitacion);
                if (habitacion is not null) habitacion.Huesped = reserva.Huesped;
                foreach (var h in SampleData.ReservaHuespedes.Where(x => x.ReservaId == e.Reserva))
                {
                    var persona = SampleData.Huespedes.FirstOrDefault(g => g.Id == h.HuespedId);
                    if (persona is null) continue;
                    persona.Habitacion = reserva.Habitacion;
                    persona.RolEnReserva = h.Principal ? "Principal" : "Acompañante";
                }
            }
        }

        foreach (var f in folios)
        {
            int? reservaId = f.Reserva ?? SampleData.Estancias.FirstOrDefault(e => e.Id == f.Estancia)?.ReservaId;
            int? principal = reservaId is int r ? SampleData.ReservaHuespedes.FirstOrDefault(x => x.ReservaId == r && x.Principal)?.HuespedId : null;
            SampleData.Folios.Add(new FolioModel(f.Id, f.Estancia, f.Saldo, f.Estado.Equals("CERRADO", StringComparison.OrdinalIgnoreCase) ? "Cerrado" : "Abierto")
            { ReservaId = reservaId, HuespedPrincipalId = principal, Tipo = f.Estancia is null ? "Reserva" : "Estancia" });
        }

        SampleData.Llaves.AddRange(DatabaseConnection.Consultar("SELECT llave_id, estancia_id, codigo, COALESCE(estado, 'EMITIDA') FROM public.llave ORDER BY llave_id",
            l => new LlaveModel(l.GetInt32(0), l.GetInt32(1), l.GetString(2), EstadoLlave(l.GetString(3)))));
    }

    // Limpieza y mantenimiento: lo que Habitaciones necesita para saber si una habitación está lista.
    private static void CargarOperacion()
    {
        string Empleado(int? id) => id is int e ? SampleData.Empleados.FirstOrDefault(x => x.Id == e)?.Nombre ?? "" : "";
        // Filas antiguas guardaron el tipo en mayúsculas ("LIMPIEZA").
        string TextoLibre(string texto) => texto == texto.ToUpperInvariant() ? Capitalizar(texto) : texto;

        foreach (var t in DatabaseConnection.Consultar(@"SELECT tarea_id, habitacion_id, empleado_id, tipo, fecha_programada, COALESCE(estado, 'PENDIENTE'), prioridad, COALESCE(observaciones, '')
                                                        FROM public.tarea_limpieza ORDER BY tarea_id",
                     l => new { Id = l.GetInt32(0), Habitacion = l.GetInt32(1), Empleado = l.IsDBNull(2) ? (int?)null : l.GetInt32(2), Tipo = l.GetString(3),
                                Fecha = l.GetDateTime(4), Estado = l.GetString(5), Prioridad = l.GetString(6), Observaciones = l.GetString(7) }))
        {
            var habitacion = SampleData.Habitaciones.FirstOrDefault(h => h.Id == t.Habitacion);
            if (habitacion is null) continue;
            SampleData.Tareas.Add(new TareaModel
            {
                Id = t.Id, Habitacion = habitacion.Id, Piso = habitacion.Piso, Tipo = TextoLibre(t.Tipo), Estado = Traducciones.EstadoTareaInterfaz(t.Estado),
                Empleado = Empleado(t.Empleado), Prioridad = Capitalizar(t.Prioridad), Hora = t.Fecha, MotivoSolicitud = TextoLibre(t.Tipo), Observaciones = t.Observaciones
            });
        }

        foreach (var i in DatabaseConnection.Consultar(@"SELECT incidencia_id, habitacion_id, huesped_id, tipo, descripcion, COALESCE(estado, 'ABIERTA'), COALESCE(fecha, LOCALTIMESTAMP),
                                                              prioridad, requiere_bloqueo, usuario_reportador_id, COALESCE(observaciones, ''), impacto_observado FROM public.incidencia ORDER BY incidencia_id",
                     l => new { Id = l.GetInt32(0), Habitacion = l.IsDBNull(1) ? 0 : l.GetInt32(1), Huesped = l.IsDBNull(2) ? (int?)null : l.GetInt32(2), Categoria = l.GetString(3),
                                Descripcion = l.GetString(4), Estado = l.GetString(5), Fecha = l.GetDateTime(6), Prioridad = l.GetString(7), Bloquea = l.GetBoolean(8),
                                Reporta = l.IsDBNull(9) ? (int?)null : l.GetInt32(9), Obs = l.GetString(10), Impacto = l.IsDBNull(11) ? null : l.GetString(11) }))
        {
            string estado = Traducciones.EstadoIncidenciaInterfaz(i.Estado);
            bool revision = estado == "Revisión operativa pendiente";
            SampleData.Incidencias.Add(new IncidenciaModel
            {
                Id = i.Id, Habitacion = i.Habitacion, Descripcion = i.Descripcion, Prioridad = Capitalizar(i.Prioridad), Estado = estado, BloqueaHabitacion = i.Bloquea,
                Categoria = TextoLibre(i.Categoria), FechaReporte = i.Fecha, ReportadaPor = SampleData.Usuarios.FirstOrDefault(u => u.Id == i.Reporta)?.Nombre ?? "",
                RequiereRevisionOperativa = revision, GuestId = i.Huesped, Observaciones = i.Obs,
                ImpactoObservado = i.Impacto ?? (revision ? "Requiere revisión urgente" : "Puede seguir utilizándose")
            });
        }

        SampleData.OrdenesMantenimiento.AddRange(DatabaseConnection.Consultar(
            "SELECT orden_id, incidencia_id, empleado_id, fecha_estimada_fin, COALESCE(estado, 'PENDIENTE') FROM public.orden_mantenimiento ORDER BY orden_id",
            l => new OrdenMantenimientoModel(l.GetInt32(0), l.GetInt32(1), Empleado(l.IsDBNull(2) ? null : l.GetInt32(2)), l.IsDBNull(3) ? null : l.GetDateTime(3), Capitalizar(l.GetString(4)))));

        SampleData.Inspecciones.AddRange(DatabaseConnection.Consultar(
            "SELECT inspeccion_id, tarea_id, COALESCE(fecha, LOCALTIMESTAMP), resultado, COALESCE(observaciones, '') FROM public.inspeccion ORDER BY inspeccion_id",
            l => new InspeccionModel(l.GetInt32(0), l.GetInt32(1), l.GetDateTime(2), !l.IsDBNull(3) && l.GetString(3).Equals("APROBADA", StringComparison.OrdinalIgnoreCase), l.GetString(4))));
    }

    // Catálogo de servicios y las solicitudes de los huéspedes con su cita o traslado.
    private static void CargarServicios()
    {
        foreach (var s in DatabaseConnection.Consultar(@"SELECT servicio_id, nombre, COALESCE(tipo, ''), precio, COALESCE(activo, true), servicio_padre_id, COALESCE(descripcion, ''), requiere_cita, duracion_minutos, reservable, COALESCE(requiere_traslado, false)
                                                        FROM public.servicio ORDER BY servicio_id",
                     l => new { Id = l.GetInt32(0), Nombre = l.GetString(1), Tipo = l.GetString(2), Precio = l.GetDecimal(3), Activo = l.GetBoolean(4),
                                Padre = l.IsDBNull(5) ? (int?)null : l.GetInt32(5), Descripcion = l.GetString(6), Cita = l.GetBoolean(7),
                                Duracion = l.IsDBNull(8) ? 60 : l.GetInt32(8), Reservable = l.GetBoolean(9), Traslado = l.GetBoolean(10) }))
        {
            // La base no guarda si un servicio es un traslado: se reconoce por su nombre.
            string texto = $"{s.Nombre} {s.Tipo}".ToLowerInvariant();
            SampleData.Servicios.Add(new ServicioModel
            {
                Id = s.Id, Nombre = s.Nombre, Categoria = s.Tipo, Precio = s.Precio, RequiereCita = s.Cita, PadreId = s.Padre, Activo = s.Activo, Reservable = s.Reservable,
                DuracionMinutos = s.Duracion, Descripcion = s.Descripcion,
                RequiereTraslado = s.Traslado || (s.Padre is not null && (texto.Contains("traslado") || texto.Contains("taxi")))
            });
        }

        var citas = DatabaseConnection.Consultar(@"SELECT c.cita_id, c.reserva_servicio_id, c.fecha_hora, COALESCE(c.duracion, 60), COALESCE(c.estado, 'SOLICITADA'), c.empleado_id, c.fecha_cancelacion, c.motivo_cancelacion, c.politica_aplicada::text
                                                  FROM public.cita_servicio c ORDER BY c.cita_id",
            l => new { Id = l.GetInt32(0), Solicitud = l.GetInt32(1), Inicio = l.GetDateTime(2), Duracion = l.GetInt32(3), Estado = l.GetString(4),
                       Empleado = l.IsDBNull(5) ? (int?)null : l.GetInt32(5), Cancelada = l.IsDBNull(6) ? (DateTime?)null : l.GetDateTime(6), Motivo = Texto(l, 7), PolicySnapshot = Texto(l, 8) });
        var traslados = DatabaseConnection.Consultar("SELECT traslado_id, reserva_servicio_id, origen, destino, fecha_hora, COALESCE(estado, 'SOLICITADO'), COALESCE(pasajeros, 1) FROM public.traslado ORDER BY traslado_id",
            l => new { Id = l.GetInt32(0), Solicitud = l.GetInt32(1), Origen = l.GetString(2), Destino = l.GetString(3), Fecha = l.GetDateTime(4), Estado = l.GetString(5), Pasajeros = l.GetInt32(6) });

        foreach (var r in DatabaseConnection.Consultar(@"SELECT rs.reserva_servicio_id, rs.estancia_id, rs.servicio_id, COALESCE(rs.fecha, LOCALTIMESTAMP), COALESCE(rs.estado, 'PENDIENTE'),
                                                              rs.huesped_id, rs.usuario_registro_id, COALESCE(rs.observaciones, ''), rs.fecha_utilizacion
                                                         FROM public.reserva_servicio rs ORDER BY rs.reserva_servicio_id",
                     l => new { Id = l.GetInt32(0), Estancia = l.GetInt32(1), Servicio = l.GetInt32(2), Fecha = l.GetDateTime(3), Estado = l.GetString(4),
                                Huesped = l.IsDBNull(5) ? (int?)null : l.GetInt32(5), Usuario = l.IsDBNull(6) ? 0 : l.GetInt32(6), Observaciones = l.GetString(7),
                                Utilizada = l.IsDBNull(8) ? (DateTime?)null : l.GetDateTime(8) }))
        {
            var estancia = SampleData.Estancias.FirstOrDefault(e => e.Id == r.Estancia);
            var servicio = SampleData.Servicios.FirstOrDefault(x => x.Id == r.Servicio);
            if (estancia is null || servicio is null) continue;
            int huesped = r.Huesped ?? estancia.HuespedId;
            var cita = citas.FirstOrDefault(c => c.Solicitud == r.Id);
            var traslado = traslados.FirstOrDefault(t => t.Solicitud == r.Id);

            if (cita is not null)
                SampleData.Citas.Add(new CitaServicioModel(cita.Id, huesped, servicio.Id, cita.Inicio, cita.Inicio.AddMinutes(cita.Duracion),
                    SampleData.Empleados.FirstOrDefault(e => e.Id == cita.Empleado)?.Nombre ?? "Por asignar", Traducciones.EstadoSolicitudInterfaz(cita.Estado))
                { EstanciaId = estancia.Id, SolicitudId = r.Id, PoliticaAplicadaJson = cita.PolicySnapshot ?? "" });
            if (traslado is not null)
                SampleData.Traslados.Add(new TrasladoModel(traslado.Id, huesped, traslado.Origen, traslado.Destino, traslado.Fecha, Traducciones.Capitalizar(traslado.Estado))
                { EstanciaId = estancia.Id, ServicioId = servicio.Id, SolicitudId = r.Id, Observaciones = r.Observaciones, Pasajeros = traslado.Pasajeros });

            SampleData.SolicitudesServicio.Add(new SolicitudServicioModel(r.Id, huesped, servicio.Id, r.Fecha, cita?.Inicio ?? traslado?.Fecha, r.Observaciones,
                Traducciones.EstadoSolicitudInterfaz(r.Estado), servicio.RequiereCita)
            {
                EstanciaId = estancia.Id, ReservaId = estancia.ReservaId, UsuarioSolicitaId = r.Usuario, CitaServicioId = cita?.Id,
                Cancelada = cita?.Cancelada, MotivoCancelacion = cita?.Motivo ?? "", OrigenTraslado = traslado?.Origen ?? "", DestinoTraslado = traslado?.Destino ?? "",
                Completada = r.Utilizada
            });
        }
    }

    private static void CargarFinanzas()
    {
        foreach (var c in DatabaseConnection.Consultar(@"SELECT c.cargo_id, COALESCE(c.descripcion, k.nombre), c.cantidad * c.precio_unitario, COALESCE(c.fecha, LOCALTIMESTAMP), c.folio_id
                                                         FROM public.cargo c JOIN public.concepto_cargo k ON k.concepto_id = c.concepto_id ORDER BY c.cargo_id",
                     l => new { Id = l.GetInt32(0), Concepto = l.GetString(1), Monto = l.GetDecimal(2), Fecha = l.GetDateTime(3), Folio = l.GetInt32(4) }))
        {
            var folio = SampleData.Folios.FirstOrDefault(f => f.Id == c.Folio);
            SampleData.Cargos.Add(new CargoModel(c.Id, c.Concepto, c.Monto, c.Fecha, folio?.HuespedPrincipalId ?? 0, folio?.EstanciaId) { FolioId = c.Folio });
        }

        foreach (var t in DatabaseConnection.Consultar("SELECT turno_id, usuario_id, fecha_apertura, fecha_cierre, COALESCE(estado, 'ABIERTO'), monto_apertura FROM public.turno_caja ORDER BY turno_id",
                     l => new { Id = l.GetInt32(0), Usuario = l.GetInt32(1), Apertura = l.GetDateTime(2), Cierre = l.IsDBNull(3) ? (DateTime?)null : l.GetDateTime(3),
                                Estado = l.GetString(4), Monto = l.GetDecimal(5) }))
        {
            bool abierto = t.Estado.Equals("ABIERTO", StringComparison.OrdinalIgnoreCase);
            SampleData.TurnosCaja.Add(new TurnoCajaModel(t.Id, t.Usuario, t.Apertura, t.Monto, abierto ? "Abierto" : "Cerrado") { Cierre = t.Cierre });
            if (abierto) { SampleData.CajaAbierta = true; SampleData.AperturaCaja = t.Monto; }
        }

        var turnoDePago = DatabaseConnection.Consultar("SELECT pago_id, turno_id FROM public.movimiento_caja WHERE pago_id IS NOT NULL AND tipo = 'COBRO'",
            l => (Pago: l.GetInt32(0), Turno: l.GetInt32(1))).GroupBy(x => x.Pago).ToDictionary(g => g.Key, g => g.First().Turno);

        foreach (var p in DatabaseConnection.Consultar(@"SELECT p.pago_id, p.folio_id, p.monto, m.nombre, COALESCE(p.fecha, LOCALTIMESTAMP), COALESCE(p.estado, 'COMPLETADO'), COALESCE(p.usuario_registro_id, 0)
                                                         FROM public.pago p JOIN public.metodo_pago m ON m.metodo_pago_id = p.metodo_pago_id ORDER BY p.pago_id",
                     l => new { Id = l.GetInt32(0), Folio = l.GetInt32(1), Monto = l.GetDecimal(2), Metodo = l.GetString(3), Fecha = l.GetDateTime(4),
                                Estado = l.GetString(5), Usuario = l.GetInt32(6) }))
        {
            var estado = Traducciones.EstadoPagoInterfaz(p.Estado);
            SampleData.Pagos.Add(new PagoModel(p.Id, p.Folio, p.Monto, p.Metodo, p.Fecha, estado) { TurnoCajaId = turnoDePago.GetValueOrDefault(p.Id), UsuarioId = p.Usuario });
            var folio = SampleData.Folios.FirstOrDefault(f => f.Id == p.Folio);
            SampleData.Movimientos.Add(new MovimientoModel(p.Fecha, "Pago", $"Folio #{p.Folio}", p.Monto, p.Metodo, folio?.HuespedPrincipalId ?? 0)
            { EstanciaId = folio?.EstanciaId, TurnoCajaId = turnoDePago.TryGetValue(p.Id, out var turno) ? turno : null, PagoId = p.Id, FolioId = p.Folio });
        }

        foreach (var m in DatabaseConnection.Consultar("SELECT movimiento_id, turno_id, fecha, tipo, monto, COALESCE(motivo, ''), pago_id, COALESCE(devolucion_id, anulacion_id) FROM public.movimiento_caja ORDER BY movimiento_id",
                     l => new { Id = l.GetInt32(0), Turno = l.GetInt32(1), Fecha = l.GetDateTime(2), Tipo = l.GetString(3), Monto = l.GetDecimal(4), Motivo = l.GetString(5),
                                Pago = l.IsDBNull(6) ? (int?)null : l.GetInt32(6), Correccion = l.IsDBNull(7) ? (int?)null : l.GetInt32(7) }))
        {
            var pago = SampleData.Pagos.FirstOrDefault(p => p.Id == m.Pago);
            var turno = SampleData.TurnosCaja.FirstOrDefault(t => t.Id == m.Turno);
            bool efectivo = m.Tipo.Equals("APERTURA", StringComparison.OrdinalIgnoreCase);
            SampleData.MovimientosCaja.Add(new MovimientoCajaModel(m.Id, m.Turno, m.Fecha, Traducciones.TipoMovimientoInterfaz(m.Tipo), m.Monto, m.Motivo)
            { PagoId = m.Pago, FolioId = pago?.FolioId, Metodo = efectivo ? "Efectivo" : pago?.Metodo ?? "", UsuarioId = turno?.UsuarioId ?? 0, CorreccionId = m.Correccion });
        }

        foreach (var d in DatabaseConnection.Consultar(@"SELECT devolucion_id, pago_id, monto, COALESCE(motivo, ''), COALESCE(fecha, LOCALTIMESTAMP), COALESCE(estado, 'PENDIENTE'),
                                                              COALESCE(usuario_solicitante_id, 0), usuario_autorizador_id, fecha_autorizacion, fecha_ejecucion, COALESCE(descripcion, '') FROM public.devolucion ORDER BY devolucion_id",
                     l => new { Id = l.GetInt32(0), Pago = l.GetInt32(1), Monto = l.GetDecimal(2), Motivo = l.GetString(3), Solicitada = l.GetDateTime(4), Estado = l.GetString(5),
                                Solicita = l.GetInt32(6), Autoriza = l.IsDBNull(7) ? (int?)null : l.GetInt32(7), FechaAut = l.IsDBNull(8) ? (DateTime?)null : l.GetDateTime(8),
                                Ejecutada = l.IsDBNull(9) ? (DateTime?)null : l.GetDateTime(9), Descripcion = l.GetString(10) }))
        {
            var movimiento = SampleData.MovimientosCaja.FirstOrDefault(x => x.Tipo == "Devolución" && x.CorreccionId == d.Id);
            SampleData.Devoluciones.Add(new DevolucionModel(d.Id, d.Pago, d.Monto, d.Motivo, Traducciones.EstadoCorreccionInterfaz(d.Estado))
            {
                Solicitada = d.Solicitada, UsuarioSolicitaId = d.Solicita, UsuarioAutorizaId = d.Autoriza, FechaAutorizacion = d.FechaAut, Ejecutada = d.Ejecutada,
                TurnoEjecucionId = movimiento?.TurnoId, MetodoEjecucion = movimiento?.Metodo ?? "", Descripcion = d.Descripcion
            });
        }

        foreach (var a in DatabaseConnection.Consultar(@"SELECT anulacion_id, pago_id, COALESCE(motivo, ''), COALESCE(fecha, LOCALTIMESTAMP), estado, COALESCE(usuario_solicitante_id, 0),
                                                              usuario_autorizador_id, fecha_autorizacion, fecha_ejecucion, COALESCE(descripcion, '') FROM public.anulacion ORDER BY anulacion_id",
                     l => new { Id = l.GetInt32(0), Pago = l.GetInt32(1), Motivo = l.GetString(2), Solicitada = l.GetDateTime(3), Estado = l.GetString(4), Solicita = l.GetInt32(5),
                                Autoriza = l.IsDBNull(6) ? (int?)null : l.GetInt32(6), FechaAut = l.IsDBNull(7) ? (DateTime?)null : l.GetDateTime(7),
                                Ejecutada = l.IsDBNull(8) ? (DateTime?)null : l.GetDateTime(8), Descripcion = l.GetString(9) }))
        {
            var movimiento = SampleData.MovimientosCaja.FirstOrDefault(x => x.Tipo == "Anulación" && x.CorreccionId == a.Id);
            SampleData.Anulaciones.Add(new AnulacionModel(a.Id, a.Pago, a.Motivo, Traducciones.EstadoCorreccionInterfaz(a.Estado))
            { Solicitada = a.Solicitada, UsuarioSolicitaId = a.Solicita, UsuarioAutorizaId = a.Autoriza, FechaAutorizacion = a.FechaAut, Ejecutada = a.Ejecutada, TurnoEjecucionId = movimiento?.TurnoId, Descripcion = a.Descripcion });
        }

        // Los cierres guardan lo esperado y lo contado; los cobros y devoluciones en efectivo se vuelven a sumar de los movimientos del turno.
        foreach (var c in DatabaseConnection.Consultar("SELECT cierre_id, turno_id, monto_esperado, monto_real, diferencia, COALESCE(fecha, LOCALTIMESTAMP), COALESCE(observaciones, ''), usuario_revisor_id, fecha_revision FROM public.cierre_caja ORDER BY cierre_id",
                     l => new { Id = l.GetInt32(0), Turno = l.GetInt32(1), Esperado = l.GetDecimal(2), Real = l.GetDecimal(3), Diferencia = l.GetDecimal(4), Fecha = l.GetDateTime(5), Observaciones = l.GetString(6),
                                Revisor = l.IsDBNull(7) ? (int?)null : l.GetInt32(7), Revisado = l.IsDBNull(8) ? (DateTime?)null : l.GetDateTime(8) }))
        {
            var turno = SampleData.TurnosCaja.FirstOrDefault(t => t.Id == c.Turno);
            if (turno is null) continue;
            var movimientos = SampleData.MovimientosCaja.Where(x => x.TurnoId == c.Turno).ToList();
            bool EsEfectivo(string metodo) => SampleData.MetodosPago.FirstOrDefault(x => x.Nombre == metodo)?.EsEfectivo ?? false;
            SampleData.CierresCaja.Add(new CierreCajaModel(c.Id, c.Fecha, c.Esperado, c.Real, c.Diferencia, c.Observaciones)
            {
                TurnoCajaId = c.Turno, UsuarioId = turno.UsuarioId, Apertura = turno.Apertura, AperturaMonto = turno.MontoInicial, UsuarioRevisorId = c.Revisor, FechaRevision = c.Revisado,
                EfectivoCobros = movimientos.Where(x => x.Tipo == "Cobro" && EsEfectivo(x.Metodo)).Sum(x => x.Monto),
                EfectivoDevoluciones = movimientos.Where(x => x.Tipo == "Devolución" && EsEfectivo(x.Metodo)).Sum(x => x.Monto),
                NoEfectivo = movimientos.Where(x => x.Tipo == "Cobro" && !EsEfectivo(x.Metodo)).Sum(x => x.Monto)
            });
            int indice = SampleData.TurnosCaja.IndexOf(turno);
            SampleData.TurnosCaja[indice] = turno with { EfectivoContado = c.Real, CierreCajaId = c.Id, ObservacionesCierre = c.Observaciones };
        }

        SampleData.Impuestos.AddRange(DatabaseConnection.Consultar("SELECT impuesto_id, nombre, porcentaje, COALESCE(activo, true) FROM public.impuesto ORDER BY impuesto_id",
            l => new ImpuestoModel(l.GetInt32(0), l.GetString(1), l.GetDecimal(2), l.GetBoolean(3))));

        // La base no guarda las líneas de una factura. Una factura incluye todos los cargos del folio pendientes de facturar
        // hasta su fecha, así que se reconstruyen de esa manera.
        var facturados = new HashSet<int>();
        var lineasGuardadas = DatabaseConnection.Consultar(
            "SELECT factura_id, cargo_id, concepto, cantidad, precio_unitario, importe FROM public.factura_linea ORDER BY factura_linea_id",
            l => (Factura: l.GetInt32(0), Linea: new FacturaLineaModel(l.IsDBNull(1) ? 0 : l.GetInt32(1), l.GetString(2), (int)l.GetDecimal(3), l.GetDecimal(4), l.GetDecimal(5))))
            .GroupBy(x => x.Factura).ToDictionary(g => g.Key, g => g.Select(x => x.Linea).ToList());
        foreach (var f in DatabaseConnection.Consultar("SELECT factura_id, folio_id, numero, fecha, subtotal, impuesto, total, COALESCE(estado, 'EMITIDA') FROM public.factura ORDER BY fecha, factura_id",
                     l => new { Id = l.GetInt32(0), Folio = l.GetInt32(1), Numero = l.GetString(2), Fecha = l.GetDateTime(3), Subtotal = l.GetDecimal(4),
                                Impuesto = l.GetDecimal(5), Total = l.GetDecimal(6), Estado = l.GetString(7) }))
        {
            var folio = SampleData.Folios.FirstOrDefault(x => x.Id == f.Folio);
            var cliente = SampleData.Huespedes.FirstOrDefault(h => h.Id == folio?.HuespedPrincipalId);
            var lineas = lineasGuardadas.TryGetValue(f.Id, out var propias) ? propias : SampleData.Cargos.Where(c => c.FolioId == f.Folio && !c.Anulado && c.Fecha <= f.Fecha.AddMinutes(1) && !facturados.Contains(c.Id))
                .Select(c => new FacturaLineaModel(c.Id, c.Concepto, 1, c.Monto, c.Monto)).ToList();
            foreach (var linea in lineas) facturados.Add(linea.CargoId);
            var reserva = folio?.ReservaId;
            SampleData.Facturas.Add(new FacturaModel(f.Id, f.Folio, f.Numero, f.Fecha, f.Total, Capitalizar(f.Estado))
            {
                Subtotal = f.Subtotal, Impuesto = f.Impuesto, PorcentajeImpuesto = f.Subtotal > 0 ? Math.Round(f.Impuesto / f.Subtotal * 100, 2) : 0,
                Cliente = cliente?.Nombre ?? "", DocumentoCliente = cliente?.Documento ?? "", Lineas = lineas, ReservaId = reserva, EstanciaId = folio?.EstanciaId,
                HabitacionId = reserva is int r ? SampleData.Reservas.FirstOrDefault(x => x.Id == r)?.Habitacion : null
            });
        }
    }

    private static string Texto(Npgsql.NpgsqlDataReader lector, int columna) => lector.IsDBNull(columna) ? "" : lector.GetString(columna);

    private static int NumeroDe(string texto)
    {
        var coincidencia = Regex.Match(texto ?? "", @"\d+");
        return coincidencia.Success ? int.Parse(coincidencia.Value) : 0;
    }

    private static string Capitalizar(string texto) => Traducciones.Capitalizar(texto);

    // En la base conviven "Servicios" y "ENCARGADO_SERVICIOS"; la interfaz usa cuatro nombres fijos.
    public static string RolDeInterfaz(string rol) => rol.Trim().ToUpperInvariant() switch
    {
        "ADMINISTRADOR" => "Administrador",
        "RECEPCIONISTA" => "Recepcionista",
        "GERENTE" => "Gerente",
        "SERVICIOS" or "ENCARGADO_SERVICIOS" or "ENCARGADO DE SERVICIOS" => "Encargado de servicios",
        _ => rol
    };

    private static string EstadoLlave(string estado) => estado.ToUpperInvariant() switch
    {
        "EMITIDA" => "Activa",
        "DEVUELTA" => "Devuelta",
        "REVOCADA" => "Revocada",
        _ => Capitalizar(estado)
    };

    private static string LimpiezaDeInterfaz(string estado, string limpieza)
    {
        // Filas antiguas guardaban la limpieza en la columna de ocupación.
        if (estado.Equals("SUCIA", StringComparison.OrdinalIgnoreCase)) return "Pendiente de limpieza";
        return limpieza.ToUpperInvariant() switch
        {
            "LIMPIA" => "Limpia",
            "SUCIA" => "Pendiente de limpieza",
            "EN_LIMPIEZA" => "En limpieza",
            "PENDIENTE_INSPECCION" => "Pendiente de inspección",
            _ => "Sin verificar"
        };
    }
}
