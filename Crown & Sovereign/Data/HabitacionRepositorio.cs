using Npgsql;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda el estado de las habitaciones y su historial.</summary>
public static class HabitacionRepositorio
{
    /// <summary>
    /// Cambia la ocupación y, si se indica, la limpieza de una habitación. El historial solo registra
    /// un cambio cuando el estado visible (ocupada, sucia, en limpieza o disponible) es distinto del anterior.
    /// </summary>
    public static void GuardarEstado(NpgsqlConnection conexion, NpgsqlTransaction transaccion, int habitacionId, string ocupacion, string? limpieza, string motivo)
    {
        Ejecutar(conexion, transaccion,
            "UPDATE public.habitacion SET estado = @ocupacion, estado_limpieza = COALESCE(@limpieza, estado_limpieza) WHERE habitacion_id = @id",
            Parametro("ocupacion", ocupacion), Parametro("limpieza", limpieza), Parametro("id", habitacionId));

        string visible = ocupacion == "OCUPADA" ? "OCUPADA"
            : limpieza is not null and not "LIMPIA" and not "SIN_VERIFICAR" ? limpieza : "DISPONIBLE";
        var abierto = Escalar(conexion, transaccion,
            "SELECT estado FROM public.habitacion_estado_historial WHERE habitacion_id = @id AND fecha_fin IS NULL ORDER BY fecha_inicio DESC LIMIT 1",
            Parametro("id", habitacionId)) as string;
        if (abierto == visible) return;

        Ejecutar(conexion, transaccion, "UPDATE public.habitacion_estado_historial SET fecha_fin = LOCALTIMESTAMP WHERE habitacion_id = @id AND fecha_fin IS NULL", Parametro("id", habitacionId));
        Ejecutar(conexion, transaccion,
            "INSERT INTO public.habitacion_estado_historial (habitacion_id, estado, fecha_inicio, motivo) VALUES (@id, @estado, LOCALTIMESTAMP, @motivo)",
            Parametro("id", habitacionId), Parametro("estado", visible), Parametro("motivo", motivo));
    }
}
