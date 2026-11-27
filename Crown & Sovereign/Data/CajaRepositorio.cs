using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda en la base los turnos de caja, pagos, devoluciones, anulaciones, cierres y facturas.</summary>
public static class CajaRepositorio
{
    public static void AbrirTurno(TurnoCajaModel turno, MovimientoCajaModel apertura)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                "INSERT INTO public.turno_caja (turno_id, usuario_id, fecha_apertura, estado, monto_apertura) VALUES (@id, @usuario, @fecha, 'ABIERTO', @monto)",
                Parametro("id", turno.Id), Parametro("usuario", turno.UsuarioId), Parametro("fecha", turno.Apertura), Parametro("monto", turno.MontoInicial));
            InsertarMovimiento(conexion, transaccion, apertura);
        });
    }

    public static void CerrarTurno(CierreCajaModel cierre)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.cierre_caja (cierre_id, turno_id, monto_esperado, monto_real, diferencia, fecha, observaciones)
                  VALUES (@id, @turno, @esperado, @real, @diferencia, @fecha, @observaciones)",
                Parametro("id", cierre.Id), Parametro("turno", cierre.TurnoCajaId), Parametro("esperado", cierre.Esperado), Parametro("real", cierre.Contado),
                Parametro("diferencia", cierre.Diferencia), Parametro("fecha", cierre.Fecha), Parametro("observaciones", Persistencia.NuloSiVacio(cierre.Observaciones)));
            Ejecutar(conexion, transaccion, "UPDATE public.turno_caja SET estado = 'CERRADO', fecha_cierre = @fecha WHERE turno_id = @id",
                Parametro("fecha", cierre.Fecha), Parametro("id", cierre.TurnoCajaId));
        });
    }

    /// <summary>Guarda los pagos de un cobro (uno por medio de pago) con sus movimientos de caja y actualiza el saldo del folio.</summary>
    public static void RegistrarPagos(IReadOnlyList<PagoModel> pagos, IReadOnlyList<MovimientoCajaModel> movimientos, int folioId)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            foreach (var pago in pagos)
            {
                var metodo = Escalar(conexion, transaccion, "SELECT metodo_pago_id FROM public.metodo_pago WHERE nombre = @nombre", Parametro("nombre", pago.Metodo))
                    ?? throw new InvalidOperationException($"El medio de pago {pago.Metodo} no existe en la base de datos.");
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.pago (pago_id, folio_id, metodo_pago_id, monto, fecha, estado, usuario_registro_id) VALUES (@id, @folio, @metodo, @monto, @fecha, 'COMPLETADO', @usuario)",
                    Parametro("id", pago.Id), Parametro("folio", pago.FolioId), Parametro("metodo", metodo), Parametro("monto", pago.Monto),
                    Parametro("fecha", pago.Fecha), Parametro("usuario", pago.UsuarioId));
            }
            foreach (var movimiento in movimientos) InsertarMovimiento(conexion, transaccion, movimiento);
            FolioRepositorio.RecalcularSaldo(conexion, transaccion, folioId);
        });
    }

    public static void InsertarDevolucion(DevolucionModel devolucion)
    {
        if (!Persistencia.Activa) return;
        Ejecutar(@"INSERT INTO public.devolucion (devolucion_id, pago_id, monto, motivo, fecha, estado, usuario_solicitante_id)
                   VALUES (@id, @pago, @monto, @motivo, @fecha, @estado, @usuario)",
            Parametro("id", devolucion.Id), Parametro("pago", devolucion.PagoId), Parametro("monto", devolucion.Monto), Parametro("motivo", devolucion.Motivo),
            Parametro("fecha", devolucion.Solicitada), Parametro("estado", Traducciones.EstadoCorreccionBase(devolucion.Estado)), Parametro("usuario", devolucion.UsuarioSolicitaId));
    }

    public static void InsertarAnulacion(AnulacionModel anulacion)
    {
        if (!Persistencia.Activa) return;
        Ejecutar(@"INSERT INTO public.anulacion (anulacion_id, pago_id, motivo, fecha, estado, usuario_solicitante_id)
                   VALUES (@id, @pago, @motivo, @fecha, @estado, @usuario)",
            Parametro("id", anulacion.Id), Parametro("pago", anulacion.PagoId), Parametro("motivo", anulacion.Motivo),
            Parametro("fecha", anulacion.Solicitada), Parametro("estado", Traducciones.EstadoCorreccionBase(anulacion.Estado)), Parametro("usuario", anulacion.UsuarioSolicitaId));
    }

    /// <summary>Guarda la revisión de Gerencia: autorizada o rechazada.</summary>
    public static void RevisarCorreccion(string tabla, int id, string estadoInterfaz, int usuarioId, DateTime fecha)
    {
        if (!Persistencia.Activa) return;
        string columna = tabla == "devolucion" ? "devolucion_id" : "anulacion_id";
        Ejecutar($"UPDATE public.{tabla} SET estado = @estado, usuario_autorizador_id = @usuario, fecha_autorizacion = @fecha WHERE {columna} = @id",
            Parametro("estado", Traducciones.EstadoCorreccionBase(estadoInterfaz)), Parametro("usuario", usuarioId), Parametro("fecha", fecha), Parametro("id", id));
    }

    public static void EjecutarDevolucion(DevolucionModel devolucion, MovimientoCajaModel movimiento, string estadoPagoInterfaz, int usuarioId)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion, "UPDATE public.devolucion SET estado = 'EJECUTADA', usuario_ejecutor_id = @usuario, fecha_ejecucion = @fecha WHERE devolucion_id = @id",
                Parametro("usuario", usuarioId), Parametro("fecha", devolucion.Ejecutada), Parametro("id", devolucion.Id));
            InsertarMovimiento(conexion, transaccion, movimiento);
            Ejecutar(conexion, transaccion, "UPDATE public.pago SET estado = @estado WHERE pago_id = @id",
                Parametro("estado", Traducciones.EstadoPagoBase(estadoPagoInterfaz)), Parametro("id", devolucion.PagoId));
            if (movimiento.FolioId is int folio) FolioRepositorio.RecalcularSaldo(conexion, transaccion, folio);
        });
    }

    public static void EjecutarAnulacion(AnulacionModel anulacion, MovimientoCajaModel movimiento, int usuarioId)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion, "UPDATE public.anulacion SET estado = 'EJECUTADA', usuario_ejecutor_id = @usuario, fecha_ejecucion = @fecha WHERE anulacion_id = @id",
                Parametro("usuario", usuarioId), Parametro("fecha", anulacion.Ejecutada), Parametro("id", anulacion.Id));
            InsertarMovimiento(conexion, transaccion, movimiento);
            Ejecutar(conexion, transaccion, "UPDATE public.pago SET estado = 'ANULADO' WHERE pago_id = @id", Parametro("id", anulacion.PagoId));
            if (movimiento.FolioId is int folio) FolioRepositorio.RecalcularSaldo(conexion, transaccion, folio);
        });
    }

    public static void InsertarFactura(FacturaModel factura)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.factura (factura_id, folio_id, numero, fecha, subtotal, impuesto, total, estado, nombre_facturacion, documento_facturacion)
                  VALUES (@id, @folio, @numero, @fecha, @subtotal, @impuesto, @total, 'EMITIDA', @cliente, @documento)",
                Parametro("id", factura.Id), Parametro("folio", factura.FolioId), Parametro("numero", factura.Numero), Parametro("fecha", factura.Fecha),
                Parametro("subtotal", factura.Subtotal), Parametro("impuesto", factura.Impuesto), Parametro("total", factura.Total),
                Parametro("cliente", Persistencia.NuloSiVacio(factura.Cliente)), Parametro("documento", Persistencia.NuloSiVacio(factura.DocumentoCliente)));
            foreach (var linea in factura.Lineas)
                Ejecutar(conexion, transaccion,
                    @"INSERT INTO public.factura_linea (factura_id, cargo_id, concepto, cantidad, precio_unitario, importe)
                      VALUES (@factura, @cargo, @concepto, @cantidad, @precio, @importe)",
                    Parametro("factura", factura.Id), Parametro("cargo", linea.CargoId), Parametro("concepto", linea.Concepto), Parametro("cantidad", linea.Cantidad),
                    Parametro("precio", linea.PrecioUnitario), Parametro("importe", linea.Importe));
        });
    }

    private static void InsertarMovimiento(Npgsql.NpgsqlConnection conexion, Npgsql.NpgsqlTransaction transaccion, MovimientoCajaModel movimiento)
    {
        // Una devolución o anulación apunta a su solicitud; el cobro apunta a su pago.
        int? devolucion = movimiento.Tipo == "Devolución" ? movimiento.CorreccionId : null;
        int? anulacion = movimiento.Tipo == "Anulación" ? movimiento.CorreccionId : null;
        Ejecutar(conexion, transaccion,
            @"INSERT INTO public.movimiento_caja (movimiento_id, turno_id, pago_id, tipo, monto, fecha, motivo, devolucion_id, anulacion_id)
              VALUES (@id, @turno, @pago, @tipo, @monto, @fecha, @motivo, @devolucion, @anulacion)",
            Parametro("id", movimiento.Id), Parametro("turno", movimiento.TurnoId), Parametro("pago", movimiento.PagoId),
            Parametro("tipo", Traducciones.TipoMovimientoBase(movimiento.Tipo)), Parametro("monto", movimiento.Monto), Parametro("fecha", movimiento.Fecha),
            Parametro("motivo", Persistencia.NuloSiVacio(movimiento.Concepto)), Parametro("devolucion", devolucion), Parametro("anulacion", anulacion));
    }
}
