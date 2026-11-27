using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda la lista de espera y las notificaciones. Los contactos de la espera no tienen tabla y quedan en memoria.</summary>
public static class EsperaRepositorio
{
    public static void InsertarEspera(ListaEsperaModel solicitud, int huespedId)
    {
        if (!Persistencia.Activa) return;
        var tipo = Escalar("SELECT tipo_habitacion_id FROM public.tipo_habitacion WHERE nombre = @nombre", Parametro("nombre", solicitud.TipoHabitacion))
            ?? throw new InvalidOperationException($"El tipo de habitación {solicitud.TipoHabitacion} no existe en la base de datos.");
        Ejecutar(@"INSERT INTO public.lista_espera (lista_espera_id, huesped_id, tipo_habitacion_id, fecha_entrada, fecha_salida, estado, cantidad_personas, cantidad_habitaciones, observaciones)
            VALUES (@id, @huesped, @tipo, @entrada, @salida, @estado, @personas, @habitaciones, @observaciones)",
            Parametro("id", solicitud.Id), Parametro("huesped", huespedId), Parametro("tipo", tipo), Parametro("entrada", solicitud.Llegada.Date),
            Parametro("salida", solicitud.Salida.Date), Parametro("estado", solicitud.Estado.ToUpperInvariant()), Parametro("personas", solicitud.Personas),
            Parametro("habitaciones", solicitud.Habitaciones), Parametro("observaciones", Persistencia.NuloSiVacio(solicitud.Observaciones)));
    }

    /// <summary>El id del contacto lo genera la base; devuelve 0 si no hay base.</summary>
    public static int InsertarContacto(ContactoListaEsperaModel contacto)
    {
        if (!Persistencia.Activa) return 0;
        return Convert.ToInt32(Escalar("INSERT INTO public.lista_espera_contacto (lista_espera_id, usuario_id, fecha, medio, resultado, observaciones) VALUES (@solicitud, @usuario, @fecha, @medio, @resultado, @observaciones) RETURNING contacto_id",
            Parametro("solicitud", contacto.SolicitudId),
            Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id ?? throw new InvalidOperationException("Inicia sesión para registrar el contacto.")),
            Parametro("fecha", contacto.Fecha), Parametro("medio", contacto.Medio), Parametro("resultado", contacto.Resultado), Parametro("observaciones", Persistencia.NuloSiVacio(contacto.Observaciones))));
    }

    public static void CambiarEstadoEspera(int id, string estado)
    {
        if (!Persistencia.Activa) return;
        Ejecutar("UPDATE public.lista_espera SET estado = @estado WHERE lista_espera_id = @id", Parametro("estado", estado.ToUpperInvariant()), Parametro("id", id));
    }

    /// <summary>El destino se guarda como título y, si coincide con un rol, también se enlaza a él.</summary>
    public static void InsertarNotificacion(NotificacionModel notificacion)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            Ejecutar(conexion, transaccion, "INSERT INTO public.notificacion (notificacion_id, titulo, mensaje, fecha) VALUES (@id, @titulo, @mensaje, @fecha)",
                Parametro("id", notificacion.Id), Parametro("titulo", notificacion.Destino), Parametro("mensaje", notificacion.Mensaje), Parametro("fecha", notificacion.Fecha));
            Ejecutar(conexion, transaccion,
                @"INSERT INTO public.notificacion_rol (notificacion_id, rol_id)
                  SELECT @id, rol_id FROM public.rol WHERE upper(nombre) = upper(@destino) OR upper(nombre) = upper(replace(@destino, ' ', '_'))",
                Parametro("id", notificacion.Id), Parametro("destino", notificacion.Destino));
        });
    }
}
