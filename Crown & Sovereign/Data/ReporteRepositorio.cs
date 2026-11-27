using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda qué reportes se generaron, para que "Reportes recientes" sobreviva al cierre de la app. El contenido se vuelve a calcular al abrirlo.</summary>
public static class ReporteRepositorio
{
    public static void Guardar(ResultadoReporte reporte)
    {
        if (!Persistencia.Activa) return;
        try
        {
            Ejecutar(@"INSERT INTO public.reporte_generado (codigo, titulo, categoria, usuario_id, fecha, fecha_desde, fecha_hasta) VALUES (@codigo, @titulo, @categoria, @usuario, @fecha, @desde, @hasta)",
                Parametro("codigo", reporte.Id), Parametro("titulo", reporte.Titulo), Parametro("categoria", Persistencia.NuloSiVacio(reporte.Categoria)),
                Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id), Parametro("fecha", reporte.Generado), Parametro("desde", reporte.Desde.Date), Parametro("hasta", reporte.Hasta.Date));
        }
        catch { /* no guardar el historial no debe impedir ver el reporte */ }
    }

    public static List<(string Codigo, DateTime Generado, DateTime Desde, DateTime Hasta, string Usuario)> Recientes(int cantidad) =>
        Consultar(@"SELECT r.codigo, r.fecha, r.fecha_desde, r.fecha_hasta, COALESCE(u.username, '')
                    FROM public.reporte_generado r LEFT JOIN public.usuario u ON u.usuario_id = r.usuario_id
                    WHERE r.fecha_desde IS NOT NULL AND r.fecha_hasta IS NOT NULL ORDER BY r.fecha DESC, r.reporte_id DESC LIMIT @cantidad",
            l => (l.GetString(0), l.GetDateTime(1), l.GetDateTime(2), l.GetDateTime(3), l.GetString(4)), Parametro("cantidad", cantidad));
}
