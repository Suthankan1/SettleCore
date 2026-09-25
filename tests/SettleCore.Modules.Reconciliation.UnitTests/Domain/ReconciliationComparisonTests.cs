using SettleCore.Modules.Reconciliation.Domain;

namespace SettleCore.Modules.Reconciliation.UnitTests.Domain;

public sealed class ReconciliationComparisonTests
{
    [Fact]
    public void CompareReturnsMatchedWhenAmountAndCurrencyMatch()
    {
        var result = ReconciliationComparison.Compare(
            expectedAmount: 100.00m,
            expectedCurrency: "SGD",
            actualAmount: 100.00m,
            actualCurrency: "SGD");

        Assert.Equal(
            ReconciliationStatus.Matched,
            result.Status);
    }

    [Fact]
    public void CompareReturnsAmountMismatchWhenAmountsDiffer()
    {
        var result = ReconciliationComparison.Compare(
            expectedAmount: 100.00m,
            expectedCurrency: "SGD",
            actualAmount: 95.00m,
            actualCurrency: "SGD");

        Assert.Equal(
            ReconciliationStatus.AmountMismatch,
            result.Status);
    }

    [Fact]
    public void CompareReturnsCurrencyMismatchWhenCurrenciesDiffer()
    {
        var result = ReconciliationComparison.Compare(
            expectedAmount: 100.00m,
            expectedCurrency: "SGD",
            actualAmount: 100.00m,
            actualCurrency: "USD");

        Assert.Equal(
            ReconciliationStatus.CurrencyMismatch,
            result.Status);
    }

    [Fact]
    public void CompareReturnsMismatchWhenAmountAndCurrencyDiffer()
    {
        var result = ReconciliationComparison.Compare(
            expectedAmount: 100.00m,
            expectedCurrency: "SGD",
            actualAmount: 95.00m,
            actualCurrency: "USD");

        Assert.Equal(
            ReconciliationStatus.Mismatch,
            result.Status);
    }
}
