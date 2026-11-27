using Crown___Sovereign.Models;
using Npgsql;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Guarda los cargos de los folios y mantiene su saldo.</summary>
public static class FolioRepositorio
{
    public static void InsertarCargo(CargoModel cargo)
    {
        if (!Persistencia.Activa) return;
        EnTransaccion((conexion, transaccion) =>
        {
            InsertarCargo(conexion, transaccion, cargo);
            if (cargo.FolioId is int folio) RecalcularSaldo(conexion, transaccion, folio);
        });
    }

    public static void InsertarCargo(NpgsqlConnection conexion, NpgsqlTransaction transaccion, CargoModel cargo)
    {
        var concepto = Escalar(conexion, transaccion, "SELECT concepto_id FROM public.concepto_cargo WHERE nombre = @nombre", Parametro("nombre", ConceptoDe(cargo.Concepto)))
            ?? throw new InvalidOperationException("No existe el concepto de cargo en la base de datos.");
        Ejecutar(conexion, transaccion,
            @"INSERT INTO public.cargo (cargo_id, folio_id, concepto_id, descripcion, cantidad, precio_unitario, fecha, usuario_registro_id)
              VALUES (@id, @folio, @concepto, @descripcion, 1, @monto, @fecha, @usuario)",
            Parametro("id", cargo.Id), Parametro("folio", cargo.FolioId ?? throw new InvalidOperationException("El cargo no tiene folio.")),
            Parametro("concepto", concepto), Parametro("descripcion", cargo.Concepto), Parametro("monto", cargo.Monto), Parametro("fecha", cargo.Fecha),
            Parametro("usuario", Infrastructure.SesionActual.Usuario?.Id));
    }

    /// <summary>Saldo del folio = cargos, menos pagos no anulados, más devoluciones ya ejecutadas.</summary>
    public static void RecalcularSaldo(NpgsqlConnection conexion, NpgsqlTransaction transaccion, int folioId) =>
        Ejecutar(conexion, transaccion,
            @"UPDATE public.folio SET saldo = GREATEST(0,
                  (SELECT COALESCE(SUM(cantidad * precio_unitario), 0) FROM public.cargo WHERE folio_id = @id)
                - (SELECT COALESCE(SUM(monto), 0) FROM public.pago WHERE folio_id = @id AND COALESCE(estado, '') <> 'ANULADO')
                + (SELECT COALESCE(SUM(d.monto), 0) FROM public.devolucion d JOIN public.pago p ON p.pago_id = d.pago_id WHERE p.folio_id = @id AND d.estado = 'EJECUTADA'))
              WHERE folio_id = @id", Parametro("id", folioId));

    // Los cargos de la interfaz llevan un texto libre; en la base cada uno pertenece a un concepto.
    private static string ConceptoDe(string texto)
    {
        var t = texto.ToLowerInvariant();
        if (t.Contains("alojamiento")) return "Alojamiento";
        if (t.Contains("minibar")) return "Minibar";
        if (t.Contains("restaur") || t.Contains("desayuno") || t.Contains("almuerzo") || t.Contains("cena")) return "Restaurante";
        if (t.Contains("spa") || t.Contains("masaje") || t.Contains("sauna") || t.Contains("yoga")) return "Spa";
        if (t.Contains("transporte") || t.Contains("traslado") || t.Contains("taxi")) return "Transporte";
        if (t.Contains("lavander")) return "Lavanderia";
        if (t.Contains("inasistencia")) return "Penalización por inasistencia";
        if (t.Contains("cancelación") || t.Contains("cancelacion")) return "Penalización por cancelación tardía";
        return "Otros";
    }
}
