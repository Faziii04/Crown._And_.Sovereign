using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda en la base los cambios de huéspedes, consentimientos y solicitudes especiales.</summary>
public static class HuespedRepositorio
{
    public static void Insertar(HuespedModel huesped)
    {
        if (!Persistencia.Activa) return;
        var (nombre, apellido) = Traducciones.SepararNombre(huesped.Nombre);
        Ejecutar(@"INSERT INTO public.huesped (huesped_id, nombre, apellido, documento, email, telefono)
                   VALUES (@id, @nombre, @apellido, @documento, @email, @telefono)",
            Parametro("id", huesped.Id), Parametro("nombre", nombre), Parametro("apellido", apellido),
            Parametro("documento", Persistencia.NuloSiVacio(huesped.Documento)),
            Parametro("email", Persistencia.NuloSiVacio(huesped.Email)), Parametro("telefono", Persistencia.NuloSiVacio(huesped.Telefono)));
    }

    public static void Actualizar(int huespedId, string nombreCompleto, string correo, string telefono)
    {
        if (!Persistencia.Activa) return;
        var (nombre, apellido) = Traducciones.SepararNombre(nombreCompleto);
        Ejecutar("UPDATE public.huesped SET nombre = @nombre, apellido = @apellido, email = @email, telefono = @telefono WHERE huesped_id = @id",
            Parametro("id", huespedId), Parametro("nombre", nombre), Parametro("apellido", apellido),
            Parametro("email", Persistencia.NuloSiVacio(correo)), Parametro("telefono", Persistencia.NuloSiVacio(telefono)));
    }

    /// <summary>Cada cambio de consentimiento queda como una fila nueva: la tabla es el historial.</summary>
    public static void GuardarConsentimiento(int huespedId, bool autorizado)
    {
        if (!Persistencia.Activa) return;
        Ejecutar("INSERT INTO public.consentimiento (huesped_id, tipo, autorizado, fecha, usuario_id) VALUES (@huesped, 'TRATAMIENTO_DATOS', @autorizado, LOCALTIMESTAMP, @usuario)",
            Parametro("huesped", huespedId), Parametro("autorizado", autorizado), Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id));
    }

    /// <summary>Deja la observación en el último cambio de consentimiento del huésped y devuelve su id.</summary>
    public static int? AnotarConsentimiento(int huespedId, string observaciones)
    {
        if (!Persistencia.Activa) return null;
        return Escalar(@"UPDATE public.consentimiento SET observaciones = @observaciones
                         WHERE consentimiento_id = (SELECT max(consentimiento_id) FROM public.consentimiento WHERE huesped_id = @huesped)
                         RETURNING consentimiento_id",
            Parametro("observaciones", Persistencia.NuloSiVacio(observaciones)), Parametro("huesped", huespedId)) as int?;
    }

    public static void InsertarSolicitud(SolicitudEspecialModel solicitud)
    {
        if (!Persistencia.Activa) return;
        Ejecutar("INSERT INTO public.solicitud_especial (solicitud_id, reserva_id, descripcion, estado) VALUES (@id, @reserva, @descripcion, @estado)",
            Parametro("id", solicitud.Id), Parametro("reserva", solicitud.ReservaId), Parametro("descripcion", solicitud.Descripcion),
            Parametro("estado", solicitud.Estado.ToUpperInvariant()));
    }
}
