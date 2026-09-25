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
            expectedCurrency != actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.Mismatch);
        }

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

        return new ReconciliationResult(
            ReconciliationStatus.Matched);
    }
}
