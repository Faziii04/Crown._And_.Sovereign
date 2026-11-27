using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda las tareas de limpieza y las incidencias de las habitaciones.</summary>
public static class OperacionesRepositorio
{
    public static void InsertarTarea(TareaModel tarea)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) => InsertarTarea(conexion, transaccion, tarea));
    }

    /// <summary>Guarda la tarea pedida desde Habitaciones y, si corresponde, deja la habitación pendiente de limpieza.</summary>
    public static void RegistrarSolicitudLimpieza(TareaModel tarea, HabitacionModel habitacion, bool ensuciar)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            InsertarTarea(conexion, transaccion, tarea);
            if (ensuciar)
                HabitacionRepositorio.GuardarEstado(conexion, transaccion, habitacion.Id, habitacion.Estado == "Ocupada" ? "OCUPADA" : "DISPONIBLE", "SUCIA", $"Solicitud de limpieza: {tarea.MotivoSolicitud}");
        });
    }

    public static void InsertarIncidencia(IncidenciaModel incidencia)
    {
        if (!Persistencia.Activa) return;
        Ejecutar(@"INSERT INTO public.incidencia (incidencia_id, habitacion_id, huesped_id, tipo, descripcion, estado, fecha, prioridad, requiere_bloqueo, usuario_reportador_id, observaciones, impacto_observado)
                   VALUES (@id, @habitacion, @huesped, @tipo, @descripcion, @estado, @fecha, @prioridad, @bloqueo, @usuario, @observaciones, @impacto)",
            Parametro("id", incidencia.Id), Parametro("habitacion", incidencia.Habitacion), Parametro("huesped", incidencia.GuestId),
            Parametro("tipo", incidencia.Categoria), Parametro("descripcion", incidencia.Descripcion), Parametro("estado", Traducciones.EstadoIncidenciaBase(incidencia.Estado)),
            Parametro("fecha", incidencia.FechaReporte), Parametro("prioridad", incidencia.Prioridad.ToUpperInvariant()),
            Parametro("bloqueo", incidencia.BloqueaHabitacion), Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id),
            Parametro("observaciones", Persistencia.NuloSiVacio(incidencia.Observaciones)), Parametro("impacto", incidencia.ImpactoObservado));
    }

    private static void InsertarTarea(Npgsql.NpgsqlConnection conexion, Npgsql.NpgsqlTransaction transaccion, TareaModel tarea) =>
        Ejecutar(conexion, transaccion,
            @"INSERT INTO public.tarea_limpieza (tarea_id, habitacion_id, tipo, fecha_programada, estado, prioridad, requiere_inspeccion, observaciones)
              VALUES (@id, @habitacion, @tipo, @fecha, @estado, @prioridad, true, @observaciones)",
            Parametro("id", tarea.Id), Parametro("habitacion", tarea.Habitacion), Parametro("tipo", tarea.Tipo), Parametro("fecha", tarea.Hora),
            Parametro("estado", Traducciones.EstadoTareaBase(tarea.Estado)), Parametro("prioridad", tarea.Prioridad.ToUpperInvariant()),
            Parametro("observaciones", Persistencia.NuloSiVacio(tarea.Observaciones)));
}
