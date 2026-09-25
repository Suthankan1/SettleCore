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
}