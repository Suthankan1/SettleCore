namespace SettleCore.Modules.Reconciliation.Domain;

public static class ReconciliationComparison
{
    public static ReconciliationResult Compare(
        decimal expectedAmount,
        string expectedCurrency,
        decimal actualAmount,
        string actualCurrency)
    {
        if (expectedAmount != actualAmount &&
            expectedCurrency == actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.AmountMismatch);
        }

        if (expectedAmount == actualAmount &&
            expectedCurrency != actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.CurrencyMismatch);
        }

        if (expectedAmount == actualAmount &&
            expectedCurrency == actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.Matched);
        }

        throw new InvalidOperationException(
            "Combined reconciliation mismatch behavior is not implemented yet.");
    }
}
