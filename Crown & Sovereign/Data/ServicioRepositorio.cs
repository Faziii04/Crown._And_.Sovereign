using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>
/// Guarda las solicitudes de servicio. Cada solicitud es una fila de reserva_servicio; si el servicio
/// requiere cita o traslado, la cita va en cita_servicio y el traslado en traslado.
/// </summary>
public static class ServicioRepositorio
{
    public static void InsertarSolicitud(SolicitudServicioModel solicitud, CitaServicioModel? cita, TrasladoModel? traslado)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.reserva_servicio (reserva_servicio_id, estancia_id, servicio_id, fecha, estado, incluido_en_paquete, huesped_id, usuario_registro_id, observaciones)
                  VALUES (@id, @estancia, @servicio, @fecha, @estado, false, @huesped, @usuario, @observaciones)",
                Parametro("id", solicitud.Id), Parametro("estancia", solicitud.EstanciaId ?? throw new InvalidOperationException("La solicitud no tiene estancia.")),
                Parametro("servicio", solicitud.ServicioId), Parametro("fecha", solicitud.Solicitada), Parametro("estado", Traducciones.EstadoSolicitudBase(solicitud.Estado)),
                Parametro("huesped", solicitud.HuespedId), Parametro("usuario", solicitud.UsuarioSolicitaId == 0 ? null : solicitud.UsuarioSolicitaId),
                Parametro("observaciones", Persistencia.NuloSiVacio(solicitud.Descripcion)));

            if (cita is not null)
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.cita_servicio (cita_id, reserva_servicio_id, fecha_hora, duracion, estado) VALUES (@id, @solicitud, @inicio, @duracion, @estado)",
                    Parametro("id", cita.Id), Parametro("solicitud", solicitud.Id), Parametro("inicio", cita.Inicio),
                    Parametro("duracion", (int)(cita.Fin - cita.Inicio).TotalMinutes), Parametro("estado", Traducciones.EstadoSolicitudBase(cita.Estado)));

            if (traslado is not null)
                Ejecutar(conexion, transaccion,
                    "INSERT INTO public.traslado (traslado_id, reserva_servicio_id, origen, destino, fecha_hora, estado, pasajeros) VALUES (@id, @solicitud, @origen, @destino, @fecha, @estado, @pasajeros)",
                    Parametro("id", traslado.Id), Parametro("solicitud", solicitud.Id), Parametro("origen", traslado.Origen), Parametro("destino", traslado.Destino),
                    Parametro("fecha", traslado.Salida), Parametro("estado", traslado.Estado.ToUpperInvariant()), Parametro("pasajeros", traslado.Pasajeros));
        });
    }

    public static void CancelarSolicitud(int solicitudId, DateTime fecha, string motivo)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion,
                "UPDATE public.reserva_servicio SET estado = 'CANCELADA', observaciones = CASE WHEN COALESCE(observaciones, '') = '' THEN @motivo ELSE observaciones || ' · ' || @motivo END WHERE reserva_servicio_id = @id",
                Parametro("motivo", "Cancelada: " + motivo), Parametro("id", solicitudId));
            Ejecutar(conexion, transaccion,
                @"UPDATE public.cita_servicio SET estado = 'CANCELADA', fecha_cancelacion = @fecha, motivo_cancelacion = @motivo
                  WHERE reserva_servicio_id = @id AND COALESCE(estado, '') NOT IN ('COMPLETADA', 'EN_CURSO')",
                Parametro("fecha", fecha), Parametro("motivo", motivo), Parametro("id", solicitudId));
            Ejecutar(conexion, transaccion, "UPDATE public.traslado SET estado = 'CANCELADO' WHERE reserva_servicio_id = @id", Parametro("id", solicitudId));
        });
    }
}
