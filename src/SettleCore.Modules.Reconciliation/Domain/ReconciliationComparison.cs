namespace SettleCore.Modules.Reconciliation.Domain;

public static class ReconciliationComparison
{
    public static ReconciliationResult Compare(
        decimal expectedAmount,
        string expectedCurrency,
        decimal actualAmount,
        string actualCurrency)
    {
        var normalizedExpectedCurrency =
            expectedCurrency.ToUpperInvariant();

        var normalizedActualCurrency =
            actualCurrency.ToUpperInvariant();

        if (expectedAmount != actualAmount &&
            normalizedExpectedCurrency != normalizedActualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.Mismatch,
                expectedAmount,
                actualAmount,
                normalizedExpectedCurrency,
                normalizedActualCurrency);
        }

        if (expectedAmount != actualAmount &&
            normalizedExpectedCurrency == normalizedActualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.AmountMismatch,
                expectedAmount,
                actualAmount,
                normalizedExpectedCurrency,
                normalizedActualCurrency);
        }

        if (expectedAmount == actualAmount &&
            normalizedExpectedCurrency != normalizedActualCurrency)
        {
            return new ReconciliationResult(
                ReconciliationStatus.CurrencyMismatch,
                expectedAmount,
                actualAmount,
                normalizedExpectedCurrency,
                normalizedActualCurrency);
        }

        return new ReconciliationResult(
            ReconciliationStatus.Matched,
            expectedAmount,
            actualAmount,
            normalizedExpectedCurrency,
            normalizedActualCurrency);
    }
}
