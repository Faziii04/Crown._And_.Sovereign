namespace Crown___Sovereign.Models;

public sealed record FinancialPeriod(string Name, DateTime Start, DateTime End, DateTime PreviousStart, DateTime PreviousEnd, bool ComparePrevious = true)
{
    public string Label => $"{Start:dd MMM yyyy} — {End.AddDays(-1):dd MMM yyyy}";
}
public sealed record FinancialSummary(decimal Charges, decimal GrossCollections, decimal Refunds, decimal Cancellations, decimal NetCollections,
    decimal CurrentOutstanding, int PendingAuthorizations, int ApprovedAwaitingExecution, int OpenShifts, int ClosuresAwaitingReview,
    int Invoices, decimal CreditNotes, decimal PreviousCharges, decimal PreviousNetCollections,
    IReadOnlyList<FinancialBreakdownRow> PaymentMethods, IReadOnlyList<FinancialTrendRow> Trend, IReadOnlyList<FinancialAlertRow> Alerts);
public sealed record FinancialBreakdownRow(string Name, decimal Amount, double Share, bool IsCash)
{ public string CashLabel => IsCash ? "Afecta efectivo físico" : "No afecta efectivo físico"; }
public sealed record FinancialTrendRow(string Label, decimal Charges, decimal Collections, decimal Refunds, decimal Cancellations);
public sealed record FinancialTrendDisplay(string Label, decimal Charges, decimal Collections, double ChargesPercent, double CollectionsPercent);
public sealed record FinancialAlertRow(string Label, string Detail, string Severity, string Destination);
public sealed record FinancialMovementRow(DateTime Date, string Kind, string Guest, int FolioId, string Concept, decimal Amount,
    string Method, string Status, int? SourceId, string Context);
public sealed record FinancialBalanceRow(int FolioId, string Guest, string Context, string Type, string Status,
    decimal Charges, decimal Collections, decimal Refunds, decimal Balance, int? ReservationId, int? StayId);
public sealed record FinancialInvoiceRow(FacturaModel Invoice, string Guest, string Folio, string Context, decimal CreditNotes, decimal DocumentedNet)
{
    public string Number => Invoice.Numero;
    public DateTime Date => Invoice.Fecha;
    public decimal Subtotal => Invoice.Subtotal;
    public decimal Tax => Invoice.Impuesto;
    public decimal Total => Invoice.Total;
    public string Status => Invoice.Estado;
}
public sealed record FinancialCreditNoteRow(int Id, string Number, int InvoiceId, string InvoiceNumber, DateTime Date, decimal Amount, string Reason);
public sealed record FinancialCashShiftRow(TurnoCajaModel Shift, string Cashier, decimal CashExpected, decimal NonCashCollections, decimal CashRefunds, decimal CashCancellations, CierreCajaModel? Closure)
{
    public string State => Shift.Estado;
    public DateTime Opened => Shift.Apertura;
    public decimal? Counted => Closure?.Contado;
    public decimal? Variance => Closure?.Diferencia;
    public string ClosureState => Closure is null ? (Shift.Estado == "Abierto" ? "En curso" : "Sin cierre") : Closure.FechaRevision is null ? "Por revisar" : "Revisado";
}
public sealed record FinancialDiagnosticRow(string Severity, string Reference, string Description, int? FolioId);
