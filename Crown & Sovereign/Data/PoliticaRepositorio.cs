using Crown___Sovereign.Models;
using static Crown___Sovereign.Data.DatabaseConnection;

namespace Crown___Sovereign.Data;

/// <summary>Parameterized persistence for the existing policy and configuration-audit tables.</summary>
public static class PoliticaRepositorio
{
    public static PoliticaModel GuardarPolitica(int hotelId, PoliticaModel? existing, string name, string description, string value, int userId, string changeKey)
    {
        if (!Persistencia.Activa) throw new InvalidOperationException("La persistencia PostgreSQL no está activa.");
        PoliticaModel? saved = null;
        EnTransaccion((connection, transaction) =>
        {
            int id;
            if (existing is null)
                id = Convert.ToInt32(Escalar(connection, transaction,
                    "INSERT INTO public.politica (hotel_id, nombre, descripcion, valor) VALUES (@hotel, @nombre, @descripcion, @valor) RETURNING politica_id",
                    Parametro("hotel", hotelId), Parametro("nombre", name), Parametro("descripcion", description), Parametro("valor", value)));
            else
            {
                id = existing.Id;
                var updated = Ejecutar(connection, transaction,
                    "UPDATE public.politica SET nombre = @nombre, descripcion = @descripcion, valor = @valor WHERE politica_id = @id AND hotel_id = @hotel",
                    Parametro("nombre", name), Parametro("descripcion", description), Parametro("valor", value), Parametro("id", id), Parametro("hotel", hotelId));
                if (updated != 1) throw new InvalidOperationException("La política ya no existe en el hotel configurado.");
            }
            RegistrarAuditoria(connection, transaction, hotelId, userId, changeKey, existing?.Valor ?? "(sin configurar)", value);
            saved = new PoliticaModel(id, name, description, value);
        });
        return saved!;
    }

    public static PoliticaServicioModel GuardarPoliticaServicio(PoliticaServicioModel? current, PoliticaServicioModel next, int userId)
    {
        if (!Persistencia.Activa) throw new InvalidOperationException("La persistencia PostgreSQL no está activa.");
        EnTransaccion((connection, transaction) =>
        {
            var hotelId = current?.HotelId ?? next.HotelId;
            var exists = current is not null;
            if (exists)
                Ejecutar(connection, transaction, @"UPDATE public.politica_servicio SET horas_cancelacion_gratuita = @horas,
                    tipo_penalizacion_inasistencia = @tipoNoShow, valor_penalizacion_inasistencia = @montoNoShow,
                    tipo_penalizacion_cancelacion = @tipoTardia, valor_penalizacion_cancelacion = @montoTardia,
                    consumir_beneficio_inasistencia = @consumeNoShow, consumir_beneficio_cancelacion_tardia = @consumeTardia
                    WHERE hotel_id = @hotel", PolicyParameters(next));
            else
                Ejecutar(connection, transaction, @"INSERT INTO public.politica_servicio (hotel_id, horas_cancelacion_gratuita,
                    tipo_penalizacion_inasistencia, valor_penalizacion_inasistencia, tipo_penalizacion_cancelacion,
                    valor_penalizacion_cancelacion, consumir_beneficio_inasistencia, consumir_beneficio_cancelacion_tardia)
                    VALUES (@hotel, @horas, @tipoNoShow, @montoNoShow, @tipoTardia, @montoTardia, @consumeNoShow, @consumeTardia)", PolicyParameters(next));

            if (current is null || current.HorasCancelacionGratuita != next.HorasCancelacionGratuita)
                RegistrarAuditoria(connection, transaction, hotelId, userId, "POLITICA_SERVICIO.HORAS_CANCELACION_GRATUITA", current?.HorasCancelacionGratuita.ToString() ?? "(sin configurar)", next.HorasCancelacionGratuita.ToString());
            if (current is null || current.TipoPenalizacionCancelacion != next.TipoPenalizacionCancelacion || current.ValorPenalizacionCancelacion != next.ValorPenalizacionCancelacion)
                RegistrarAuditoria(connection, transaction, hotelId, userId, "POLITICA_SERVICIO.PENALIZACION_CANCELACION", current is null ? "(sin configurar)" : $"{current.TipoPenalizacionCancelacion}:{current.ValorPenalizacionCancelacion}", $"{next.TipoPenalizacionCancelacion}:{next.ValorPenalizacionCancelacion}");
            if (current is null || current.ConsumirBeneficioCancelacionTardia != next.ConsumirBeneficioCancelacionTardia)
                RegistrarAuditoria(connection, transaction, hotelId, userId, "POLITICA_SERVICIO.CONSUMIR_BENEFICIO_CANCELACION_TARDIA", current?.ConsumirBeneficioCancelacionTardia.ToString() ?? "(sin configurar)", next.ConsumirBeneficioCancelacionTardia.ToString());
            if (current is null || current.TipoPenalizacionInasistencia != next.TipoPenalizacionInasistencia || current.ValorPenalizacionInasistencia != next.ValorPenalizacionInasistencia)
                RegistrarAuditoria(connection, transaction, hotelId, userId, "POLITICA_SERVICIO.PENALIZACION_INASISTENCIA", current is null ? "(sin configurar)" : $"{current.TipoPenalizacionInasistencia}:{current.ValorPenalizacionInasistencia}", $"{next.TipoPenalizacionInasistencia}:{next.ValorPenalizacionInasistencia}");
            if (current is null || current.ConsumirBeneficioInasistencia != next.ConsumirBeneficioInasistencia)
                RegistrarAuditoria(connection, transaction, hotelId, userId, "POLITICA_SERVICIO.CONSUMIR_BENEFICIO_INASISTENCIA", current?.ConsumirBeneficioInasistencia.ToString() ?? "(sin configurar)", next.ConsumirBeneficioInasistencia.ToString());
        });
        return next;
    }

    public static ConfiguracionAuditModel GuardarYRestaurar(string changeKey, string oldValue, string newValue, int hotelId, int userId)
    {
        if (!Persistencia.Activa) throw new InvalidOperationException("La persistencia PostgreSQL no está activa.");
        using var connection = Abrir();
        using var transaction = connection.BeginTransaction();
        RegistrarAuditoria(connection, transaction, hotelId, userId, changeKey, oldValue, newValue);
        transaction.Commit();
        return new(SampleData.HistorialPoliticas.Count == 0 ? 1 : SampleData.HistorialPoliticas.Max(x => x.Id) + 1, hotelId, userId, changeKey, oldValue, newValue, DateTime.Now);
    }

    private static void RegistrarAuditoria(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, int hotelId, int userId, string key, string oldValue, string newValue)
    {
        Ejecutar(connection, transaction,
            "INSERT INTO public.configuracion_audit (hotel_id, usuario_id, cambio, valor_anterior, valor_nuevo, fecha) VALUES (@hotel, @user, @change, @old, @new, @fecha)",
            Parametro("hotel", hotelId), Parametro("user", userId), Parametro("change", key), Parametro("old", oldValue), Parametro("new", newValue), Parametro("fecha", DateTime.Now));
    }
    private static Npgsql.NpgsqlParameter[] PolicyParameters(PoliticaServicioModel p) =>
    [Parametro("hotel", p.HotelId), Parametro("horas", p.HorasCancelacionGratuita), Parametro("tipoNoShow", p.TipoPenalizacionInasistencia),
     Parametro("montoNoShow", p.ValorPenalizacionInasistencia), Parametro("tipoTardia", p.TipoPenalizacionCancelacion),
     Parametro("montoTardia", p.ValorPenalizacionCancelacion), Parametro("consumeNoShow", p.ConsumirBeneficioInasistencia),
     Parametro("consumeTardia", p.ConsumirBeneficioCancelacionTardia)];
}
