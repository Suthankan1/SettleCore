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
                ReconciliationStatus.Mismatch,
                expectedAmount,
                actualAmount,
                expectedCurrency,
                actualCurrency);
        }

        if (expectedAmount != actualAmount &&
            expectedCurrency == actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.AmountMismatch,
                expectedAmount,
                actualAmount,
                expectedCurrency,
                actualCurrency);
        }

        if (expectedAmount == actualAmount &&
            expectedCurrency != actualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.CurrencyMismatch,
                expectedAmount,
                actualAmount,
                expectedCurrency,
                actualCurrency);
        }

        return new ReconciliationResult(
            ReconciliationStatus.Matched,
            expectedAmount,
            actualAmount,
            expectedCurrency,
            actualCurrency);
    }
}
