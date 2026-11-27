namespace Crown___Sovereign.Data;

/// <summary>Decide si los cambios se guardan en la base y reparte los ids que le corresponden a cada tabla.</summary>
public static class Persistencia
{
    /// <summary>Verdadero si la app trabaja con la base y la carga inicial salió bien.</summary>
    public static bool Activa => DatabaseConnection.Configurada && !Cargando && SampleData.ErrorCarga is null;

    /// <summary>Mientras se llenan las colecciones desde la base no se debe volver a escribir lo que se acaba de leer.</summary>
    internal static bool Cargando { get; set; }

    /// <summary>Pide el siguiente id a la secuencia de la tabla; sin base usa el cálculo de la demostración.</summary>
    public static int SiguienteId(string tabla, string columna, Func<int> demostracion)
    {
        if (!Activa) return demostracion();
        var id = DatabaseConnection.Escalar("SELECT nextval(pg_get_serial_sequence(@tabla, @columna))",
            DatabaseConnection.Parametro("tabla", "public." + tabla), DatabaseConnection.Parametro("columna", columna));
        return Convert.ToInt32(id);
    }

    public static object? NuloSiVacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
