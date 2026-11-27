namespace Crown___Sovereign.Data;

/// <summary>Traduce entre los textos de la interfaz y los valores que guarda la base.</summary>
public static class Traducciones
{
    public static string EstadoReservaInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "CONFIRMADA" => "Confirmada",
        "CHECK_IN" => "En estancia",
        "CHECK_OUT" => "Finalizada",
        "CANCELADA" => "Cancelada",
        "NO_PRESENTADO" or "NO_SHOW" => "No presentado",
        _ => Capitalizar(estado)
    };

    public static string EstadoReservaBase(string estado) => estado switch
    {
        "Confirmada" => "CONFIRMADA",
        "En estancia" => "CHECK_IN",
        "Finalizada" => "CHECK_OUT",
        "Cancelada" => "CANCELADA",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    // La interfaz ofrece "Directa", "Teléfono" y "Sitio web"; en la base son Mostrador, Telefono y Web.
    public static string CanalInterfaz(string canal) => canal switch
    {
        "Mostrador" => "Directa",
        "Telefono" => "Teléfono",
        "Web" => "Sitio web",
        _ => canal
    };

    public static string CanalBase(string canal) => canal switch
    {
        "Directa" => "Mostrador",
        "Teléfono" => "Telefono",
        "Sitio web" => "Web",
        _ => canal
    };

    // Devoluciones y anulaciones: la interfaz dice "Autorizada" y la base "APROBADA".
    public static string EstadoCorreccionBase(string estado) => estado switch
    {
        "Pendiente" => "PENDIENTE",
        "Autorizada" => "APROBADA",
        "Rechazada" => "RECHAZADA",
        "Ejecutada" => "EJECUTADA",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    public static string EstadoCorreccionInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "APROBADA" => "Autorizada",
        _ => Capitalizar(estado)
    };

    public static string EstadoPagoBase(string estado) => estado switch
    {
        "Aplicado" => "COMPLETADO",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    public static string EstadoPagoInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "COMPLETADO" => "Aplicado",
        _ => Capitalizar(estado)
    };

    public static string TipoMovimientoBase(string tipo) => tipo switch
    {
        "Cobro" => "COBRO",
        "Devolución" => "DEVOLUCION",
        "Anulación" => "ANULACION",
        "Apertura" => "APERTURA",
        _ => tipo.ToUpperInvariant()
    };

    public static string TipoMovimientoInterfaz(string tipo) => tipo.ToUpperInvariant() switch
    {
        "COBRO" => "Cobro",
        "DEVOLUCION" => "Devolución",
        "ANULACION" => "Anulación",
        "APERTURA" => "Apertura",
        _ => Capitalizar(tipo)
    };

    public static string EstadoTareaBase(string estado) => estado switch
    {
        "Pendiente" => "PENDIENTE",
        "En progreso" => "EN_PROGRESO",
        "Completada" => "COMPLETADA",
        "Inspeccionada" => "INSPECCIONADA",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    public static string EstadoTareaInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "EN_CURSO" or "EN_PROGRESO" => "En progreso",
        _ => Capitalizar(estado)
    };

    public static string EstadoIncidenciaBase(string estado) => estado switch
    {
        "Abierta" => "ABIERTA",
        "Revisión operativa pendiente" => "REVISION_OPERATIVA",
        "En reparación" => "EN_REPARACION",
        "Resuelta" => "RESUELTA",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    public static string EstadoIncidenciaInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "REVISION_OPERATIVA" => "Revisión operativa pendiente",
        "EN_REPARACION" => "En reparación",
        _ => Capitalizar(estado)
    };

    /// <summary>La habitación guarda la limpieza en cinco valores fijos.</summary>
    public static string LimpiezaBase(string limpieza) => limpieza switch
    {
        "Limpia" or "Inspeccionada" => "LIMPIA",
        "Pendiente de limpieza" or "Inspección fallida" => "SUCIA",
        "En limpieza" => "EN_LIMPIEZA",
        "Pendiente de inspección" => "PENDIENTE_INSPECCION",
        _ => "SIN_VERIFICAR"
    };

    // Solicitudes de servicio y sus citas.
    public static string EstadoSolicitudBase(string estado) => estado switch
    {
        "En atención" => "EN_ATENCION",
        _ => estado.ToUpperInvariant().Replace(' ', '_')
    };

    public static string EstadoSolicitudInterfaz(string estado) => estado.ToUpperInvariant() switch
    {
        "EN_ATENCION" => "En atención",
        _ => Capitalizar(estado)
    };

    /// <summary>La interfaz maneja el nombre completo; la base guarda nombre y apellido por separado.</summary>
    public static (string Nombre, string Apellido) SepararNombre(string completo)
    {
        var partes = completo.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return partes.Length switch
        {
            0 => ("", ""),
            1 => (partes[0], ""),
            2 => (partes[0], partes[1]),
            3 => (partes[0], $"{partes[1]} {partes[2]}"),
            _ => ($"{partes[0]} {partes[1]}", string.Join(' ', partes.Skip(2)))
        };
    }

    public static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpperInvariant(texto[0]) + texto[1..].ToLowerInvariant().Replace('_', ' ');
}
