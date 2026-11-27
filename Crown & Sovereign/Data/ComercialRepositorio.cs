using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>
/// Tarifas, temporadas, canales, paquetes, promociones, convenios y catálogo de servicios del Gerente.
/// Los servicios cambian la memoria y después llaman aquí; si la base falla se vuelve a leer lo guardado.
/// </summary>
public static class ComercialRepositorio
{
    // ---------- lectura ----------

    public static void CargarTarifas()
    {
        SampleData.TarifasHotel.Clear();
        SampleData.TarifasConfiguraciones.Clear();
        SampleData.CanalesReserva.Clear();
        SampleData.Temporadas.Clear();

        SampleData.TarifasHotel.AddRange(Consultar(@"SELECT t.tarifa_id, th.nombre, t.nombre, t.precio_base
                FROM public.tarifa t JOIN public.tipo_habitacion th ON th.tipo_habitacion_id = t.tipo_habitacion_id ORDER BY t.tarifa_id",
            l => new TarifaModel(l.GetInt32(0), l.GetString(1), l.GetString(2), l.GetDecimal(3), DateTime.MinValue, DateTime.MaxValue)));

        SampleData.Temporadas.AddRange(Consultar("SELECT temporada_id, nombre, fecha_inicio, fecha_fin FROM public.temporada ORDER BY fecha_inicio",
            l => new TemporadaModel(l.GetInt32(0), l.GetString(1), l.GetDateTime(2), l.GetDateTime(3))));

        SampleData.CanalesReserva.AddRange(Consultar("SELECT canal_id, nombre, COALESCE(comision, 0) FROM public.canal_reserva ORDER BY canal_id",
            l => new CanalReservaModel(l.GetInt32(0), Traducciones.CanalInterfaz(l.GetString(1)), l.GetDecimal(2))));

        SampleData.TarifasConfiguraciones.AddRange(Consultar("SELECT configuracion_id, tarifa_id, temporada_id, canal_id, precio FROM public.tarifa_configuracion ORDER BY configuracion_id",
            l => new TarifaConfiguracionModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.GetInt32(3), l.GetDecimal(4))));
    }

    public static void CargarCatalogo()
    {
        SampleData.Paquetes.Clear();
        SampleData.PaquetesServicios.Clear();
        SampleData.PaquetesImagenes.Clear();
        SampleData.Promociones.Clear();
        SampleData.Convenios.Clear();
        SampleData.ConveniosServicios.Clear();
        SampleData.ServiciosImagenes.Clear();
        SampleData.ServiciosProductos.Clear();
        SampleData.EmpleadosServicios.Clear();
        SampleData.Productos.Clear();
        SampleData.MovimientosInventario.Clear();
        SampleData.Consumos.Clear();

        SampleData.Paquetes.AddRange(Consultar("SELECT paquete_id, nombre, COALESCE(descripcion, ''), precio, COALESCE(activo, true) FROM public.paquete ORDER BY paquete_id",
            l => new PaqueteModel(l.GetInt32(0), l.GetString(1), l.GetString(2), l.GetDecimal(3), l.GetBoolean(4))));
        SampleData.PaquetesServicios.AddRange(Consultar("SELECT paquete_id, servicio_id, tipo_cobertura, limite_usos, COALESCE(compartido, false) FROM public.paquete_servicio ORDER BY paquete_id, servicio_id",
            l => new PaqueteServicioModel(l.GetInt32(0), l.GetInt32(1), CoberturaInterfaz(l.GetString(2)), l.IsDBNull(3) ? null : l.GetInt32(3), l.GetBoolean(4))));
        SampleData.PaquetesImagenes.AddRange(Consultar("SELECT imagen_id, paquete_id, url, COALESCE(descripcion, ''), COALESCE(es_principal, false), COALESCE(orden, 0) FROM public.paquete_imagen ORDER BY imagen_id",
            l => new PaqueteImagenModel(l.GetInt32(0), l.GetInt32(1), l.GetString(2), l.GetString(3), l.GetBoolean(4), l.GetInt32(5))));

        SampleData.Promociones.AddRange(Consultar("SELECT promocion_id, nombre, descuento, fecha_inicio, fecha_fin, COALESCE(estado, 'ACTIVA'), COALESCE(tipo_descuento, 'PORCENTAJE') FROM public.promocion ORDER BY promocion_id",
            l => new PromocionModel(l.GetInt32(0), l.GetString(1), l.GetString(6).Equals("NOCHES", StringComparison.OrdinalIgnoreCase) ? "Noches" : "Porcentaje", l.GetDecimal(2), l.GetDateTime(3), l.GetDateTime(4)) { Activa = l.GetString(5).Equals("ACTIVA", StringComparison.OrdinalIgnoreCase) }));

        SampleData.Convenios.AddRange(Consultar(@"SELECT convenio_id, nombre, empresa, COALESCE(tipo_proveedor, ''), fecha_inicio, fecha_vencimiento, COALESCE(estado, 'ACTIVO'),
                    COALESCE(descripcion, ''), COALESCE(contacto, ''), COALESCE(telefono, ''), COALESCE(email, ''), COALESCE(direccion, ''), COALESCE(condiciones, ''), COALESCE(logo_url, '')
                FROM public.convenio ORDER BY convenio_id",
            l => new ConvenioModel(l.GetInt32(0), l.GetString(2), l.GetString(3), l.IsDBNull(4) ? DateTime.Today : l.GetDateTime(4), l.IsDBNull(5) ? DateTime.MaxValue.Date : l.GetDateTime(5),
                l.GetString(6).Equals("ACTIVO", StringComparison.OrdinalIgnoreCase), l.GetString(1), l.GetString(7), l.GetString(8), l.GetString(9), l.GetString(10), l.GetString(11), l.GetString(12), l.GetString(13))));
        SampleData.ConveniosServicios.AddRange(Consultar("SELECT convenio_id, servicio_id, COALESCE(costo_proveedor, 0), COALESCE(observaciones, ''), COALESCE(activo, true) FROM public.convenio_servicio ORDER BY convenio_id, servicio_id",
            l => new ConvenioServicioModel(l.GetInt32(0), l.GetInt32(1), l.GetDecimal(2), l.GetString(3), l.GetBoolean(4))));

        SampleData.ServiciosImagenes.AddRange(Consultar("SELECT imagen_id, servicio_id, url, COALESCE(descripcion, ''), COALESCE(es_principal, false), COALESCE(orden, 0) FROM public.servicio_imagen ORDER BY imagen_id",
            l => new ServicioImagenModel(l.GetInt32(0), l.GetInt32(1), l.GetString(2), l.GetString(3), l.GetBoolean(4), l.GetInt32(5))));
        SampleData.ServiciosProductos.AddRange(Consultar("SELECT servicio_id, producto_id, COALESCE(cantidad_incluida, 1) FROM public.servicio_producto ORDER BY servicio_id, producto_id",
            l => new ServicioProductoModel(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2))));
        SampleData.EmpleadosServicios.AddRange(Consultar("SELECT empleado_id, servicio_id, COALESCE(activo, true) FROM public.empleado_servicio ORDER BY servicio_id, empleado_id",
            l => new EmpleadoServicioModel(l.GetInt32(0), l.GetInt32(1), l.GetBoolean(2))));

        // Inventario y consumos: aquí solo se leen; los registra el personal de servicios.
        SampleData.Productos.AddRange(Consultar("SELECT producto_id, nombre, COALESCE(categoria, ''), precio, COALESCE(stock_actual, 0), COALESCE(stock_minimo, 0) FROM public.producto ORDER BY producto_id",
            l => new ProductoModel { Id = l.GetInt32(0), Nombre = l.GetString(1), Categoria = l.GetString(2), Precio = l.GetDecimal(3), Stock = l.GetInt32(4), StockMinimo = l.GetInt32(5) }));
        SampleData.MovimientosInventario.AddRange(Consultar("SELECT movimiento_id, producto_id, COALESCE(fecha, LOCALTIMESTAMP), tipo, cantidad, COALESCE(motivo, '') FROM public.movimiento_inventario ORDER BY movimiento_id",
            l => new MovimientoInventarioModel(l.GetInt32(0), l.GetInt32(1), l.GetDateTime(2), l.GetString(3), l.GetInt32(4), l.GetString(5))));
        foreach (var c in Consultar(@"SELECT consumo_id, estancia_id, producto_id, cantidad, COALESCE(fecha, LOCALTIMESTAMP), COALESCE(origen, tipo, ''), cargo_id, huesped_id, COALESCE(incluido_en_paquete, false), precio_unitario_aplicado
                                      FROM public.consumo ORDER BY consumo_id",
                     l => new { Id = l.GetInt32(0), Estancia = l.GetInt32(1), Producto = l.GetInt32(2), Cantidad = l.GetInt32(3), Fecha = l.GetDateTime(4), Tipo = l.GetString(5),
                                Cargo = l.IsDBNull(6) ? (int?)null : l.GetInt32(6), Huesped = l.IsDBNull(7) ? (int?)null : l.GetInt32(7), Incluido = l.GetBoolean(8),
                                Precio = l.IsDBNull(9) ? (decimal?)null : l.GetDecimal(9) }))
        {
            var huesped = c.Huesped ?? SampleData.Estancias.FirstOrDefault(e => e.Id == c.Estancia)?.HuespedId ?? 0;
            SampleData.Consumos.Add(new ConsumoModel(c.Id, huesped, c.Producto, c.Cantidad, c.Fecha, c.Incluido) { EstanciaId = c.Estancia, PrecioUnitarioAcordado = c.Precio, CargoId = c.Cargo, Origen = c.Tipo });
        }
    }

    // ---------- tarifas, temporadas y canales ----------

    /// <summary>Deja en la base lo que hay en memoria: canales, temporadas, tarifas base y precios por temporada y canal.</summary>
    public static void SincronizarTarifas() => Proteger(CargarTarifas, () => EnTransaccion((c, t) =>
    {
        foreach (var canal in SampleData.CanalesReserva)
            Ejecutar(c, t, @"INSERT INTO public.canal_reserva (canal_id, nombre, comision) VALUES (@id, @nombre, @comision)
                             ON CONFLICT (canal_id) DO UPDATE SET nombre = EXCLUDED.nombre, comision = EXCLUDED.comision",
                Parametro("id", canal.Id), Parametro("nombre", Traducciones.CanalBase(canal.Nombre)), Parametro("comision", canal.Comision));

        foreach (var temporada in SampleData.Temporadas)
            Ejecutar(c, t, @"INSERT INTO public.temporada (temporada_id, nombre, fecha_inicio, fecha_fin) VALUES (@id, @nombre, @desde, @hasta)
                             ON CONFLICT (temporada_id) DO UPDATE SET nombre = EXCLUDED.nombre, fecha_inicio = EXCLUDED.fecha_inicio, fecha_fin = EXCLUDED.fecha_fin",
                Parametro("id", temporada.Id), Parametro("nombre", temporada.Nombre), Parametro("desde", temporada.Desde.Date), Parametro("hasta", temporada.Hasta.Date));

        foreach (var tarifa in SampleData.TarifasHotel)
            Ejecutar(c, t, "UPDATE public.tarifa SET precio_base = @precio WHERE tarifa_id = @id", Parametro("precio", tarifa.Precio), Parametro("id", tarifa.Id));

        var configuraciones = SampleData.TarifasConfiguraciones.Select(x => x.Id).ToArray();
        Ejecutar(c, t, "DELETE FROM public.tarifa_configuracion WHERE configuracion_id <> ALL(@ids)", Parametro("ids", configuraciones));
        foreach (var config in SampleData.TarifasConfiguraciones)
            Ejecutar(c, t, @"INSERT INTO public.tarifa_configuracion (configuracion_id, tarifa_id, temporada_id, canal_id, precio) VALUES (@id, @tarifa, @temporada, @canal, @precio)
                             ON CONFLICT (configuracion_id) DO UPDATE SET tarifa_id = EXCLUDED.tarifa_id, temporada_id = EXCLUDED.temporada_id, canal_id = EXCLUDED.canal_id, precio = EXCLUDED.precio",
                Parametro("id", config.Id), Parametro("tarifa", config.TarifaId), Parametro("temporada", config.TemporadaId), Parametro("canal", config.CanalId), Parametro("precio", config.Precio));

        var temporadas = SampleData.Temporadas.Select(x => x.Id).ToArray();
        Ejecutar(c, t, "DELETE FROM public.temporada WHERE temporada_id <> ALL(@ids)", Parametro("ids", temporadas));
    }));

    // ---------- paquetes y promociones ----------

    public static void GuardarPaquete(int id) => Proteger(CargarCatalogo, () => EnTransaccion((c, t) =>
    {
        var paquete = SampleData.Paquetes.First(x => x.Id == id);
        Ejecutar(c, t, @"INSERT INTO public.paquete (paquete_id, nombre, descripcion, precio, activo) VALUES (@id, @nombre, @descripcion, @precio, @activo)
                         ON CONFLICT (paquete_id) DO UPDATE SET nombre = EXCLUDED.nombre, descripcion = EXCLUDED.descripcion, precio = EXCLUDED.precio, activo = EXCLUDED.activo",
            Parametro("id", id), Parametro("nombre", paquete.Nombre), Parametro("descripcion", Persistencia.NuloSiVacio(paquete.Descripcion)), Parametro("precio", paquete.Precio), Parametro("activo", paquete.Activo));

        Ejecutar(c, t, "DELETE FROM public.paquete_servicio WHERE paquete_id = @id", Parametro("id", id));
        foreach (var b in SampleData.PaquetesServicios.Where(x => x.PaqueteId == id))
            Ejecutar(c, t, "INSERT INTO public.paquete_servicio (paquete_id, servicio_id, tipo_cobertura, limite_usos, compartido) VALUES (@paquete, @servicio, @cobertura, @limite, @compartido)",
                Parametro("paquete", id), Parametro("servicio", b.ServicioId), Parametro("cobertura", CoberturaBase(b.TipoCobertura)), Parametro("limite", b.LimiteUsos), Parametro("compartido", b.Compartido));

        Ejecutar(c, t, "DELETE FROM public.paquete_imagen WHERE paquete_id = @id", Parametro("id", id));
        foreach (var i in SampleData.PaquetesImagenes.Where(x => x.PaqueteId == id))
            Ejecutar(c, t, "INSERT INTO public.paquete_imagen (imagen_id, paquete_id, url, descripcion, es_principal, orden) VALUES (@imagen, @paquete, @url, @descripcion, @principal, @orden)",
                Parametro("imagen", i.Id), Parametro("paquete", id), Parametro("url", i.Url), Parametro("descripcion", Persistencia.NuloSiVacio(i.Descripcion)), Parametro("principal", i.EsPrincipal), Parametro("orden", i.Orden));
    }));

    public static void EliminarPaquete(int id) => Proteger(CargarCatalogo, () => EnTransaccion((c, t) =>
    {
        Ejecutar(c, t, "DELETE FROM public.paquete_servicio WHERE paquete_id = @id", Parametro("id", id));
        Ejecutar(c, t, "DELETE FROM public.paquete_imagen WHERE paquete_id = @id", Parametro("id", id));
        Ejecutar(c, t, "DELETE FROM public.paquete WHERE paquete_id = @id", Parametro("id", id));
    }));

    public static void GuardarPromocion(int id) => Proteger(CargarCatalogo, () =>
    {
        var p = SampleData.Promociones.First(x => x.Id == id);
        Ejecutar(@"INSERT INTO public.promocion (promocion_id, nombre, descuento, fecha_inicio, fecha_fin, estado, tipo_descuento) VALUES (@id, @nombre, @descuento, @desde, @hasta, @estado, @tipo)
                   ON CONFLICT (promocion_id) DO UPDATE SET nombre = EXCLUDED.nombre, descuento = EXCLUDED.descuento, fecha_inicio = EXCLUDED.fecha_inicio, fecha_fin = EXCLUDED.fecha_fin, estado = EXCLUDED.estado, tipo_descuento = EXCLUDED.tipo_descuento",
            Parametro("id", id), Parametro("nombre", p.Nombre), Parametro("descuento", p.Valor), Parametro("desde", p.Desde.Date), Parametro("hasta", p.Hasta.Date), Parametro("estado", p.Activa ? "ACTIVA" : "INACTIVA"), Parametro("tipo", p.TipoDescuento.Equals("Noches", StringComparison.OrdinalIgnoreCase) ? "NOCHES" : "PORCENTAJE"));
    });

    public static void EliminarPromocion(int id) => Proteger(CargarCatalogo, () => Ejecutar("DELETE FROM public.promocion WHERE promocion_id = @id", Parametro("id", id)));

    // ---------- convenios ----------

    public static void GuardarConvenio(int id) => Proteger(CargarCatalogo, () => EnTransaccion((c, t) =>
    {
        var v = SampleData.Convenios.First(x => x.Id == id);
        Ejecutar(c, t, @"INSERT INTO public.convenio (convenio_id, nombre, empresa, fecha_vencimiento, estado, tipo_proveedor, descripcion, contacto, telefono, email, direccion, fecha_inicio, condiciones, logo_url)
                         VALUES (@id, @nombre, @empresa, @hasta, @estado, @tipo, @descripcion, @contacto, @telefono, @email, @direccion, @desde, @condiciones, @logo)
                         ON CONFLICT (convenio_id) DO UPDATE SET nombre = EXCLUDED.nombre, empresa = EXCLUDED.empresa, fecha_vencimiento = EXCLUDED.fecha_vencimiento, estado = EXCLUDED.estado,
                             tipo_proveedor = EXCLUDED.tipo_proveedor, descripcion = EXCLUDED.descripcion, contacto = EXCLUDED.contacto, telefono = EXCLUDED.telefono, email = EXCLUDED.email,
                             direccion = EXCLUDED.direccion, fecha_inicio = EXCLUDED.fecha_inicio, condiciones = EXCLUDED.condiciones, logo_url = EXCLUDED.logo_url",
            Parametro("id", id), Parametro("nombre", string.IsNullOrWhiteSpace(v.Nombre) ? v.Empresa : v.Nombre), Parametro("empresa", v.Empresa), Parametro("hasta", v.VigenciaHasta.Date),
            Parametro("estado", v.Activo ? "ACTIVO" : "INACTIVO"), Parametro("tipo", Persistencia.NuloSiVacio(v.Categoria)), Parametro("descripcion", Persistencia.NuloSiVacio(v.Descripcion)),
            Parametro("contacto", Persistencia.NuloSiVacio(v.Contacto)), Parametro("telefono", Persistencia.NuloSiVacio(v.Telefono)), Parametro("email", Persistencia.NuloSiVacio(v.Email)),
            Parametro("direccion", Persistencia.NuloSiVacio(v.Direccion)), Parametro("desde", v.VigenciaDesde.Date), Parametro("condiciones", Persistencia.NuloSiVacio(v.Condiciones)),
            Parametro("logo", Persistencia.NuloSiVacio(v.LogoUrl)));

        Ejecutar(c, t, "DELETE FROM public.convenio_servicio WHERE convenio_id = @id", Parametro("id", id));
        foreach (var s in SampleData.ConveniosServicios.Where(x => x.ConvenioId == id))
            Ejecutar(c, t, "INSERT INTO public.convenio_servicio (convenio_id, servicio_id, costo_proveedor, observaciones, activo) VALUES (@convenio, @servicio, @costo, @observaciones, @activo)",
                Parametro("convenio", id), Parametro("servicio", s.ServicioId), Parametro("costo", s.CostoProveedor), Parametro("observaciones", Persistencia.NuloSiVacio(s.Observaciones)), Parametro("activo", s.Activo));
    }));

    public static void EliminarConvenio(int id) => Proteger(CargarCatalogo, () => Ejecutar("DELETE FROM public.convenio WHERE convenio_id = @id", Parametro("id", id)));

    // ---------- catálogo de servicios ----------

    public static void GuardarServicio(ServicioModel s) => Proteger(CargarServicioPorRecarga, () =>
        Ejecutar(@"INSERT INTO public.servicio (servicio_id, nombre, tipo, precio, activo, servicio_padre_id, descripcion, requiere_cita, duracion_minutos, reservable, requiere_traslado)
                   VALUES (@id, @nombre, @tipo, @precio, @activo, @padre, @descripcion, @cita, @duracion, @reservable, @traslado)
                   ON CONFLICT (servicio_id) DO UPDATE SET nombre = EXCLUDED.nombre, tipo = EXCLUDED.tipo, precio = EXCLUDED.precio, activo = EXCLUDED.activo, servicio_padre_id = EXCLUDED.servicio_padre_id,
                       descripcion = EXCLUDED.descripcion, requiere_cita = EXCLUDED.requiere_cita, duracion_minutos = EXCLUDED.duracion_minutos, reservable = EXCLUDED.reservable, requiere_traslado = EXCLUDED.requiere_traslado",
            Parametro("id", s.Id), Parametro("nombre", s.Nombre), Parametro("tipo", Persistencia.NuloSiVacio(s.Categoria)), Parametro("precio", s.Precio), Parametro("activo", s.Activo),
            Parametro("padre", s.PadreId), Parametro("descripcion", Persistencia.NuloSiVacio(s.Descripcion)), Parametro("cita", s.RequiereCita), Parametro("duracion", s.DuracionMinutos > 0 ? s.DuracionMinutos : null),
            Parametro("reservable", s.Reservable), Parametro("traslado", s.RequiereTraslado)));

    public static void GuardarEmpleadoServicio(int servicioId, int empleadoId) => Proteger(CargarCatalogo, () =>
    {
        var link = SampleData.EmpleadosServicios.First(x => x.ServicioId == servicioId && x.EmpleadoId == empleadoId);
        Ejecutar(@"INSERT INTO public.empleado_servicio (empleado_id, servicio_id, activo) VALUES (@empleado, @servicio, @activo)
                   ON CONFLICT (empleado_id, servicio_id) DO UPDATE SET activo = EXCLUDED.activo",
            Parametro("empleado", empleadoId), Parametro("servicio", servicioId), Parametro("activo", link.Activo));
    });

    public static void GuardarServicioProducto(int servicioId, int productoId) => Proteger(CargarCatalogo, () =>
    {
        var link = SampleData.ServiciosProductos.First(x => x.ServicioId == servicioId && x.ProductoId == productoId);
        Ejecutar(@"INSERT INTO public.servicio_producto (servicio_id, producto_id, cantidad_incluida) VALUES (@servicio, @producto, @cantidad)
                   ON CONFLICT (servicio_id, producto_id) DO UPDATE SET cantidad_incluida = EXCLUDED.cantidad_incluida",
            Parametro("servicio", servicioId), Parametro("producto", productoId), Parametro("cantidad", link.CantidadIncluida));
    });

    public static void QuitarServicioProducto(int servicioId, int productoId) => Proteger(CargarCatalogo, () =>
        Ejecutar("DELETE FROM public.servicio_producto WHERE servicio_id = @servicio AND producto_id = @producto", Parametro("servicio", servicioId), Parametro("producto", productoId)));

    /// <summary>Reescribe todas las imágenes de un servicio con lo que hay en memoria.</summary>
    public static void SincronizarImagenesServicio(int servicioId) => Proteger(CargarCatalogo, () => EnTransaccion((c, t) =>
    {
        Ejecutar(c, t, "DELETE FROM public.servicio_imagen WHERE servicio_id = @id", Parametro("id", servicioId));
        foreach (var i in SampleData.ServiciosImagenes.Where(x => x.ServicioId == servicioId))
            Ejecutar(c, t, "INSERT INTO public.servicio_imagen (imagen_id, servicio_id, url, descripcion, es_principal, orden) VALUES (@imagen, @servicio, @url, @descripcion, @principal, @orden)",
                Parametro("imagen", i.Id), Parametro("servicio", servicioId), Parametro("url", i.Url), Parametro("descripcion", Persistencia.NuloSiVacio(i.Descripcion)), Parametro("principal", i.EsPrincipal), Parametro("orden", i.Orden));
    }));

    // ---------- apoyo ----------

    // Un servicio es un objeto compartido con las pantallas; si la base falla se restituye leyendo su fila.
    private static void CargarServicioPorRecarga()
    {
        foreach (var fila in Consultar("SELECT servicio_id, nombre, COALESCE(tipo, ''), precio, COALESCE(activo, true), servicio_padre_id, COALESCE(descripcion, ''), requiere_cita, duracion_minutos, reservable FROM public.servicio",
                     l => new { Id = l.GetInt32(0), Nombre = l.GetString(1), Tipo = l.GetString(2), Precio = l.GetDecimal(3), Activo = l.GetBoolean(4), Padre = l.IsDBNull(5) ? (int?)null : l.GetInt32(5),
                                Descripcion = l.GetString(6), Cita = l.GetBoolean(7), Duracion = l.IsDBNull(8) ? 60 : l.GetInt32(8), Reservable = l.GetBoolean(9) }))
        {
            var actual = SampleData.Servicios.FirstOrDefault(x => x.Id == fila.Id);
            if (actual is null) continue;
            actual.Nombre = fila.Nombre; actual.Categoria = fila.Tipo; actual.Precio = fila.Precio; actual.Activo = fila.Activo; actual.PadreId = fila.Padre;
            actual.Descripcion = fila.Descripcion; actual.RequiereCita = fila.Cita; actual.DuracionMinutos = fila.Duracion; actual.Reservable = fila.Reservable;
        }
        SampleData.Servicios.RemoveAll(x => !Consultar("SELECT 1 FROM public.servicio WHERE servicio_id = @id", l => 1, Parametro("id", x.Id)).Any());
    }

    private static void Proteger(Action recargar, Action guardar)
    {
        if (!Persistencia.Activa) return;
        try { guardar(); }
        catch
        {
            try { recargar(); } catch { /* si tampoco se puede leer, se deja la excepción original */ }
            throw;
        }
    }

    private static string CoberturaInterfaz(string valor) => valor switch { "POR_DIA" => "Por día", "POR_ESTANCIA" => "Por estancia", _ => "Ilimitado" };
    private static string CoberturaBase(string valor) => valor switch { "Por día" => "POR_DIA", "Por estancia" => "POR_ESTANCIA", _ => "ILIMITADO" };
}
