using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda reservas en la base. La reserva se escribe completa: datos, habitaciones, huéspedes, solicitudes y paquete.</summary>
public static class ReservaRepositorio
{
    /// <summary>Inserta o actualiza la reserva con lo que hay en SampleData, todo en una sola transacción.</summary>
    public static void Guardar(int reservaId)
    {
        if (!Persistencia.Activa) return;
        var reserva = SampleData.Reservas.First(item => item.Id == reservaId);
        var principal = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservaId && item.Principal)?.HuespedId
            ?? SampleData.Huespedes.First(item => item.Nombre == reserva.Huesped).Id;
        string canal = Traducciones.CanalBase(reserva.Canal);
        var canalId = Escalar("SELECT canal_id FROM public.canal_reserva WHERE nombre = @canal", Parametro("canal", canal))
            ?? throw new InvalidOperationException($"El canal {reserva.Canal} no existe en la base de datos.");

        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.reserva (reserva_id, huesped_principal_id, canal_id, grupo_id, fecha_reserva, fecha_entrada, fecha_salida, estado, cantidad_personas)
                  VALUES (@id, @principal, @canal, @grupo, LOCALTIMESTAMP, @entrada, @salida, @estado, @personas)
                  ON CONFLICT (reserva_id) DO UPDATE SET huesped_principal_id = EXCLUDED.huesped_principal_id, canal_id = EXCLUDED.canal_id,
                      grupo_id = EXCLUDED.grupo_id, fecha_entrada = EXCLUDED.fecha_entrada, fecha_salida = EXCLUDED.fecha_salida,
                      estado = EXCLUDED.estado, cantidad_personas = EXCLUDED.cantidad_personas",
                Parametro("id", reserva.Id), Parametro("principal", principal), Parametro("canal", canalId), Parametro("grupo", reserva.GrupoId),
                Parametro("entrada", reserva.Llegada.Date), Parametro("salida", reserva.Salida.Date),
                Parametro("estado", Traducciones.EstadoReservaBase(reserva.Estado)), Parametro("personas", reserva.Personas));

            // Se reescriben los vínculos: primero los que dependen de otros.
            foreach (var tabla in new[] { "reserva_servicio_adquirido", "reserva_recurso", "reserva_habitacion_huesped", "reserva_huesped", "reserva_habitacion", "solicitud_especial", "reserva_paquete" })
                Ejecutar(conexion, transaccion, $"DELETE FROM public.{tabla} WHERE reserva_id = @reserva", Parametro("reserva", reservaId));

            foreach (var huesped in SampleData.ReservaHuespedes.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion, "INSERT INTO public.reserva_huesped (reserva_id, huesped_id) VALUES (@reserva, @huesped)",
                    Parametro("reserva", reservaId), Parametro("huesped", huesped.HuespedId));

            foreach (var habitacion in SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.reserva_habitacion (reserva_habitacion_id, reserva_id, habitacion_id, fecha_entrada, fecha_salida) VALUES (@id, @reserva, @habitacion, @entrada, @salida)",
                    Parametro("id", habitacion.ReservaHabitacionId), Parametro("reserva", reservaId), Parametro("habitacion", habitacion.HabitacionId),
                    Parametro("entrada", reserva.Llegada.Date), Parametro("salida", reserva.Salida.Date));

            foreach (var noche in reserva.AlojamientoNochesAcordadas)
            {
                var asignacion = SampleData.ReservaHabitaciones.FirstOrDefault(item => item.ReservaId == reservaId && item.HabitacionId == noche.HabitacionId);
                if (asignacion is null) continue;
                Ejecutar(conexion, transaccion, "INSERT INTO public.reserva_habitacion_noche (reserva_habitacion_id, noche, precio) VALUES (@asignacion, @noche, @precio) ON CONFLICT DO NOTHING",
                    Parametro("asignacion", asignacion.ReservaHabitacionId), Parametro("noche", noche.Noche.Date), Parametro("precio", noche.Precio));
            }

            foreach (var vinculo in SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.reserva_habitacion_huesped (reserva_habitacion_id, reserva_id, huesped_id) VALUES (@habitacion, @reserva, @huesped)",
                    Parametro("habitacion", vinculo.ReservaHabitacionId), Parametro("reserva", reservaId), Parametro("huesped", vinculo.HuespedId));

            foreach (var recurso in SampleData.ReservaRecursos.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion,
                    @"INSERT INTO public.reserva_recurso (reserva_id, recurso_id, cantidad, precio_unitario_acordado, tipo_cobro_aplicado, estado, observaciones, cargo_id)
                      VALUES (@reserva, @recurso, @cantidad, @precio, @cobro, @estado, @observaciones, @cargo)",
                    Parametro("reserva", reservaId), Parametro("recurso", recurso.RecursoId), Parametro("cantidad", recurso.Cantidad), Parametro("precio", recurso.PrecioUnitarioAcordado),
                    Parametro("cobro", recurso.TipoCobroAplicado), Parametro("estado", recurso.Estado), Parametro("observaciones", Persistencia.NuloSiVacio(recurso.Observaciones)),
                    Parametro("cargo", recurso.CargoId));

            foreach (var servicio in SampleData.ServiciosAdquiridos.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion,
                    @"INSERT INTO public.reserva_servicio_adquirido (servicio_adquirido_id, reserva_id, servicio_id, huesped_id, reserva_habitacion_id, cantidad, precio_unitario_acordado, fecha_registro, estado, cargo_id, observaciones)
                      VALUES (@id, @reserva, @servicio, @huesped, @asignacion, @cantidad, @precio, @fecha, @estado, @cargo, @observaciones)",
                    Parametro("id", servicio.ServicioAdquiridoId), Parametro("reserva", reservaId), Parametro("servicio", servicio.ServicioId), Parametro("huesped", servicio.HuespedId),
                    Parametro("asignacion", servicio.ReservaHabitacionId), Parametro("cantidad", servicio.Cantidad), Parametro("precio", servicio.PrecioUnitarioAcordado),
                    Parametro("fecha", servicio.FechaRegistro), Parametro("estado", servicio.Estado), Parametro("cargo", servicio.CargoId),
                    Parametro("observaciones", Persistencia.NuloSiVacio(servicio.Observaciones)));

            foreach (var solicitud in SampleData.SolicitudesEspeciales.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion, "INSERT INTO public.solicitud_especial (solicitud_id, reserva_id, descripcion, estado) VALUES (@id, @reserva, @descripcion, @estado)",
                    Parametro("id", solicitud.Id), Parametro("reserva", reservaId), Parametro("descripcion", solicitud.Descripcion), Parametro("estado", solicitud.Estado.ToUpperInvariant()));

            foreach (var paquete in SampleData.ReservaPaquetes.Where(item => item.ReservaId == reservaId))
                Ejecutar(conexion, transaccion, "INSERT INTO public.reserva_paquete (reserva_id, paquete_id, cantidad, precio_unitario_acordado) VALUES (@reserva, @paquete, @cantidad, @precio)",
                    Parametro("reserva", reservaId), Parametro("paquete", paquete.PaqueteId), Parametro("cantidad", paquete.Cantidad), Parametro("precio", paquete.PrecioCompra));
        });
    }

    public static void CambiarEstado(int reservaId, string estadoInterfaz)
    {
        if (!Persistencia.Activa) return;
        Ejecutar("UPDATE public.reserva SET estado = @estado WHERE reserva_id = @id",
            Parametro("estado", Traducciones.EstadoReservaBase(estadoInterfaz)), Parametro("id", reservaId));
    }

    /// <summary>Quita de memoria todo lo que cuelga de la reserva; se usa cuando no se pudo guardar una reserva nueva.</summary>
    public static void QuitarDeMemoria(int reservaId)
    {
        SampleData.Reservas.RemoveAll(item => item.Id == reservaId);
        SampleData.ReservaHuespedes.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaHabitaciones.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaHabitacionHuespedes.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.SolicitudesEspeciales.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaPaquetes.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaRecursos.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ServiciosAdquiridos.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaPaqueteServicios.RemoveAll(item => item.ReservaId == reservaId);
        SampleData.ReservaPaqueteServicioProductos.RemoveAll(item => item.ReservaId == reservaId);
    }

    /// <summary>Devuelve la reserva al estado que tiene en la base; se usa cuando falla el guardado de una edición.</summary>
    public static void Recargar(int reservaId)
    {
        if (!Persistencia.Activa) return;
        var actual = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId);
        QuitarDeMemoria(reservaId);
        CargadorBaseDatos.CargarReservas(reservaId);
        var guardada = SampleData.Reservas.FirstOrDefault(item => item.Id == reservaId);
        if (actual is null || guardada is null) return;

        // La pantalla sigue apuntando al mismo objeto: se le devuelven los valores guardados.
        actual.Huesped = guardada.Huesped; actual.Habitacion = guardada.Habitacion; actual.Llegada = guardada.Llegada; actual.Salida = guardada.Salida;
        actual.Estado = guardada.Estado; actual.Canal = guardada.Canal; actual.Paquete = guardada.Paquete; actual.GrupoId = guardada.GrupoId;
        actual.PromocionId = guardada.PromocionId; actual.Personas = guardada.Personas;
        SampleData.Reservas[SampleData.Reservas.IndexOf(guardada)] = actual;
    }
}
