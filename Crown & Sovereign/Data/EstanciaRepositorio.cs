using Crown___Sovereign.Models;
using Npgsql;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda el check-in, el check-out y las llaves de una estancia.</summary>
public static class EstanciaRepositorio
{
    /// <summary>Crea la estancia y su folio, registra el cargo del alojamiento y deja ocupadas las habitaciones, todo junto.</summary>
    public static void RegistrarCheckIn(EstanciaModel estancia, bool folioNuevo, IReadOnlyList<CargoModel> cargosAlojamiento, IEnumerable<int> habitaciones)
    {
        if (!Persistencia.Activa) return;
        int folioId = estancia.FolioId ?? throw new InvalidOperationException("La estancia no tiene folio.");
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                "INSERT INTO public.estancia (estancia_id, reserva_id, fecha_checkin, tipo_checkin, estado) VALUES (@id, @reserva, @entrada, 'NORMAL', 'ALOJADO')",
                Parametro("id", estancia.Id), Parametro("reserva", estancia.ReservaId), Parametro("entrada", estancia.CheckIn));

            if (folioNuevo)
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.folio (folio_id, estancia_id, nombre, tipo, saldo, estado, reserva_id) VALUES (@id, @estancia, 'Folio principal', 'INDIVIDUAL', 0, 'ABIERTO', @reserva)",
                    Parametro("id", folioId), Parametro("estancia", estancia.Id), Parametro("reserva", estancia.ReservaId));
            else
                Ejecutar(conexion, transaccion, "UPDATE public.folio SET estancia_id = @estancia, tipo = 'INDIVIDUAL' WHERE folio_id = @id",
                    Parametro("estancia", estancia.Id), Parametro("id", folioId));

            foreach (var cargo in cargosAlojamiento) FolioRepositorio.InsertarCargo(conexion, transaccion, cargo);
            FolioRepositorio.RecalcularSaldo(conexion, transaccion, folioId);

            Ejecutar(conexion, transaccion, "UPDATE public.reserva SET estado = 'CHECK_IN' WHERE reserva_id = @id", Parametro("id", estancia.ReservaId));
            foreach (var habitacion in habitaciones)
                HabitacionRepositorio.GuardarEstado(conexion, transaccion, habitacion, "OCUPADA", null, $"Check-in reserva {estancia.ReservaId}");
        });
    }

    /// <summary>Cierra la estancia y el folio, finaliza la reserva y deja las habitaciones pendientes de limpieza.</summary>
    public static void RegistrarCheckOut(int estanciaId, int? folioId, int reservaId, IEnumerable<int> habitaciones, DateTime salida)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion, "UPDATE public.estancia SET estado = 'FINALIZADA', fecha_checkout = @salida, tipo_checkout = 'NORMAL' WHERE estancia_id = @id",
                Parametro("salida", salida), Parametro("id", estanciaId));
            if (folioId is int folio)
                Ejecutar(conexion, transaccion, "UPDATE public.folio SET estado = 'CERRADO' WHERE folio_id = @id", Parametro("id", folio));
            Ejecutar(conexion, transaccion, "UPDATE public.reserva SET estado = 'CHECK_OUT' WHERE reserva_id = @id", Parametro("id", reservaId));
            foreach (var habitacion in habitaciones)
                HabitacionRepositorio.GuardarEstado(conexion, transaccion, habitacion, "DISPONIBLE", "SUCIA", $"Check-out reserva {reservaId}");
        });
    }

    /// <summary>Pasa la asignación de la reserva a otra habitación: huéspedes, estados de ambas habitaciones, historial y el registro del cambio. Devuelve el id del cambio.</summary>
    public static int RegistrarCambioHabitacion(int reservaId, int habitacionOrigen, int habitacionDestino, int asignacionAnterior, int asignacionNueva, IEnumerable<int> huespedes, string motivo, string observaciones)
    {
        if (!Persistencia.Activa) return 0;
        int cambioId = 0;
        var huespedesCambio = huespedes.ToList();
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion, "DELETE FROM public.reserva_habitacion_huesped WHERE reserva_id = @reserva AND reserva_habitacion_id = @anterior",
                Parametro("reserva", reservaId), Parametro("anterior", asignacionAnterior));
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.reserva_habitacion (reserva_habitacion_id, reserva_id, habitacion_id, fecha_entrada, fecha_salida)
                  SELECT @nueva, reserva_id, @destino, fecha_entrada, fecha_salida FROM public.reserva_habitacion WHERE reserva_habitacion_id = @anterior AND reserva_id = @reserva",
                Parametro("nueva", asignacionNueva), Parametro("destino", habitacionDestino), Parametro("anterior", asignacionAnterior), Parametro("reserva", reservaId));

            cambioId = Convert.ToInt32(Escalar(conexion, transaccion,
                @"INSERT INTO public.cambio_habitacion (reserva_id, estancia_id, reserva_habitacion_id, habitacion_origen_id, habitacion_destino_id, usuario_id, motivo, observaciones)
                  VALUES (@reserva, (SELECT estancia_id FROM public.estancia WHERE reserva_id = @reserva), @nueva, @origen, @destino, @usuario, @motivo, @observaciones)
                  RETURNING cambio_id",
                Parametro("reserva", reservaId), Parametro("nueva", asignacionNueva), Parametro("origen", habitacionOrigen), Parametro("destino", habitacionDestino),
                Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id ?? throw new InvalidOperationException("Inicia sesión para cambiar de habitación.")), Parametro("motivo", motivo), Parametro("observaciones", Persistencia.NuloSiVacio(observaciones))));
            foreach (var huesped in huespedesCambio)
                Ejecutar(conexion, transaccion, "INSERT INTO public.cambio_habitacion_huesped (cambio_id, huesped_id) VALUES (@cambio, @huesped)",
                    Parametro("cambio", cambioId), Parametro("huesped", huesped));

            Ejecutar(conexion, transaccion, "DELETE FROM public.reserva_habitacion WHERE reserva_habitacion_id = @anterior AND reserva_id = @reserva",
                Parametro("anterior", asignacionAnterior), Parametro("reserva", reservaId));
            foreach (var huesped in huespedesCambio)
                Ejecutar(conexion, transaccion, "INSERT INTO public.reserva_habitacion_huesped (reserva_habitacion_id, reserva_id, huesped_id) VALUES (@asignacion, @reserva, @huesped)",
                    Parametro("asignacion", asignacionNueva), Parametro("reserva", reservaId), Parametro("huesped", huesped));

            string detalle = $"Cambio de habitación reserva {reservaId}: {motivo}";
            HabitacionRepositorio.GuardarEstado(conexion, transaccion, habitacionOrigen, "DISPONIBLE", "SUCIA", detalle);
            HabitacionRepositorio.GuardarEstado(conexion, transaccion, habitacionDestino, "OCUPADA", "LIMPIA", detalle);
        });
        return cambioId;
    }

    /// <summary>Crea la llave con el siguiente id; sin base solo arma el modelo.</summary>
    public static LlaveModel NuevaLlave(int estanciaId, string codigo)
    {
        int id = Persistencia.SiguienteId("llave", "llave_id", () => SampleData.Llaves.Count == 0 ? 1 : SampleData.Llaves.Max(item => item.Id) + 1);
        if (Persistencia.Activa)
            Ejecutar("INSERT INTO public.llave (llave_id, estancia_id, codigo, fecha_emision, estado) VALUES (@id, @estancia, @codigo, LOCALTIMESTAMP, 'EMITIDA')",
                Parametro("id", id), Parametro("estancia", estanciaId), Parametro("codigo", codigo));
        return new LlaveModel(id, estanciaId, codigo, "Activa");
    }

    public static void CambiarEstadoLlave(int llaveId, string estadoInterfaz)
    {
        if (!Persistencia.Activa) return;
        string estado = estadoInterfaz switch { "Revocada" => "REVOCADA", "Devuelta" => "DEVUELTA", _ => estadoInterfaz.ToUpperInvariant() };
        Ejecutar("UPDATE public.llave SET estado = @estado, fecha_devolucion = CASE WHEN @estado = 'DEVUELTA' THEN LOCALTIMESTAMP ELSE fecha_devolucion END WHERE llave_id = @id",
            Parametro("estado", estado), Parametro("id", llaveId));
    }
}
