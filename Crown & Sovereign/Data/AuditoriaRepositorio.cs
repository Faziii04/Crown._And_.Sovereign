using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda cada acción relevante en auditoria_accion y la deja también en la lista que lee la pantalla.</summary>
public static class AuditoriaRepositorio
{
    public static void Registrar(AuditoriaModel registro)
    {
        SampleData.Auditoria.Insert(0, registro);
        var usuario = SampleData.Usuarios.FirstOrDefault(item => item.Usuario == registro.Usuario);
        if (!Persistencia.Activa || usuario is null) return;
        try
        {
            Ejecutar(@"INSERT INTO public.auditoria_accion (usuario_id, fecha, accion, entidad, antes, despues)
                       VALUES (@usuario, @fecha, @accion, @entidad, to_jsonb(@antes::text), to_jsonb(@despues::text))",
                Parametro("usuario", usuario.Id), Parametro("fecha", registro.Fecha), Parametro("accion", registro.Cambio),
                Parametro("entidad", Entidad(registro.Cambio)), Parametro("antes", registro.Antes), Parametro("despues", registro.Despues));
        }
        catch
        {
            // Un fallo de auditoría no debe deshacer la operación que ya se guardó.
        }
    }

    private static string Entidad(string accion)
    {
        string texto = accion.ToLowerInvariant();
        if (texto.Contains("caja")) return "turno_caja";
        if (texto.Contains("check")) return "estancia";
        if (texto.Contains("reserva")) return "reserva";
        if (texto.Contains("limpieza")) return "tarea_limpieza";
        if (texto.Contains("incidencia")) return "incidencia";
        if (texto.Contains("habitaci")) return "habitacion";
        return "sistema";
    }
}
