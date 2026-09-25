namespace SettleCore.Modules.Reconciliation.Domain;

public sealed record ReconciliationResult(
    ReconciliationStatus Status,
    decimal ExpectedAmount,
    decimal ActualAmount);
