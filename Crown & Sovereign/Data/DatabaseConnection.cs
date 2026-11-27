using System.IO;
using System.Text.Json;
using Npgsql;

namespace Crown___Sovereign.Data;

/// <summary>Acceso a PostgreSQL. Los datos de conexión se leen de appsettings.json, junto al ejecutable.</summary>
public static class DatabaseConnection
{
    private static readonly string Archivo = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static readonly Lazy<NpgsqlDataSource> Origen = new(CrearOrigen);

    public static bool Configurada => File.Exists(Archivo);

    public static NpgsqlConnection Abrir()
    {
        var conexion = Origen.Value.CreateConnection();
        conexion.Open();
        return conexion;
    }

    /// <summary>Abre y cierra una conexión; lanza una excepción con el motivo si no se puede.</summary>
    public static void Probar()
    {
        using var conexion = Abrir();
        using var comando = new NpgsqlCommand("SELECT 1", conexion);
        comando.ExecuteScalar();
    }

    public static List<T> Consultar<T>(string sql, Func<NpgsqlDataReader, T> mapa, params NpgsqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Consultar(conexion, null, sql, mapa, parametros);
    }

    public static List<T> Consultar<T>(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql,
                                       Func<NpgsqlDataReader, T> mapa, params NpgsqlParameter[] parametros)
    {
        using var comando = Preparar(conexion, transaccion, sql, parametros);
        using var lector = comando.ExecuteReader();
        var filas = new List<T>();
        while (lector.Read()) filas.Add(mapa(lector));
        return filas;
    }

    public static int Ejecutar(string sql, params NpgsqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Ejecutar(conexion, null, sql, parametros);
    }

    public static int Ejecutar(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql, params NpgsqlParameter[] parametros)
    {
        using var comando = Preparar(conexion, transaccion, sql, parametros);
        return comando.ExecuteNonQuery();
    }

    public static object? Escalar(string sql, params NpgsqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Escalar(conexion, null, sql, parametros);
    }

    public static object? Escalar(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql, params NpgsqlParameter[] parametros)
    {
        using var comando = Preparar(conexion, transaccion, sql, parametros);
        var valor = comando.ExecuteScalar();
        return valor is DBNull ? null : valor;
    }

    /// <summary>Ejecuta varias operaciones juntas: si alguna falla, no se guarda ninguna.</summary>
    public static void EnTransaccion(Action<NpgsqlConnection, NpgsqlTransaction> operacion)
    {
        using var conexion = Abrir();
        using var transaccion = conexion.BeginTransaction();
        operacion(conexion, transaccion);
        transaccion.Commit();
    }

    public static NpgsqlParameter Parametro(string nombre, object? valor) => new(nombre, valor ?? DBNull.Value);

    private static NpgsqlCommand Preparar(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql, NpgsqlParameter[] parametros)
    {
        var comando = new NpgsqlCommand(sql, conexion, transaccion);
        comando.Parameters.AddRange(parametros);
        return comando;
    }

    private static NpgsqlDataSource CrearOrigen()
    {
        if (!File.Exists(Archivo))
            throw new InvalidOperationException("Falta appsettings.json junto al ejecutable. Copia appsettings.example.json y completa los datos de la base.");

        using var documento = JsonDocument.Parse(File.ReadAllText(Archivo));
        var db = documento.RootElement.GetProperty("Db");
        var cadena = new NpgsqlConnectionStringBuilder
        {
            Host = db.GetProperty("Host").GetString(),
            Port = db.GetProperty("Port").GetInt32(),
            Database = db.GetProperty("Database").GetString(),
            Username = db.GetProperty("Username").GetString(),
            Password = db.GetProperty("Password").GetString(),
            SslMode = SslMode.Require,
            // Sin esto Npgsql limpia la sesión al devolver la conexión al pool y se pierde la zona horaria.
            NoResetOnClose = true
        };

        var constructor = new NpgsqlDataSourceBuilder(cadena.ConnectionString);
        // El servidor trabaja en UTC y la aplicación en hora local: sin esto "hoy" cambia de día a deshoras.
        constructor.UsePhysicalConnectionInitializer(
            conexion => FijarZonaHoraria(conexion),
            async conexion => await Task.Run(() => FijarZonaHoraria(conexion)));
        return constructor.Build();
    }

    private static void FijarZonaHoraria(NpgsqlConnection conexion)
    {
        var desfase = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        string signo = desfase < TimeSpan.Zero ? "-" : "+";
        using var comando = new NpgsqlCommand($"SET TIME ZONE INTERVAL '{signo}{desfase:hh\\:mm}' HOUR TO MINUTE", conexion);
        comando.ExecuteNonQuery();
    }
}
