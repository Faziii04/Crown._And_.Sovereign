using Crown___Sovereign.Models;

namespace Crown___Sovereign.Infrastructure;

public static class SesionActual
{
    public static UsuarioModel? Usuario { get; private set; }
    public static bool Iniciar(UsuarioModel usuario) { Usuario=usuario; return true; }
    public static void Cerrar() => Usuario=null;
}
