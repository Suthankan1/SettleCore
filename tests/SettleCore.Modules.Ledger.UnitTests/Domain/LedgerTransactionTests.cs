using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Modules.Ledger.UnitTests.Domain;

public sealed class LedgerTransactionTests
{
    [Fact]
    public void PostAcceptsBalancedEntriesInEachCurrency()
    {
        var entries = new[]
        {
            LedgerEntry.Create(Guid.NewGuid(), "sgd", LedgerDirection.Debit, 1000),
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Credit, 1000),
            LedgerEntry.Create(Guid.NewGuid(), "usd", LedgerDirection.Debit, 250),
            LedgerEntry.Create(Guid.NewGuid(), "USD", LedgerDirection.Credit, 250)
        };

        var transaction = LedgerTransaction.Post(Guid.NewGuid(), entries);

        Assert.Equal(4, transaction.Entries.Count);
        Assert.All(transaction.Entries, entry =>
            Assert.Equal(entry.Currency.ToUpperInvariant(), entry.Currency));
    }

    [Fact]
    public void PostRejectsUnbalancedCurrency()
    {
        var entries = new[]
        {
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Debit, 1000),
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Credit, 900)
        };

        Assert.Throws<ArgumentException>(() =>
            LedgerTransaction.Post(Guid.NewGuid(), entries));
    }

    [Fact]
    public void PostRejectsCrossCurrencyOffsettingEntries()
    {
        var entries = new[]
        {
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Debit, 1000),
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Credit, 500),
            LedgerEntry.Create(Guid.NewGuid(), "USD", LedgerDirection.Debit, 500),
            LedgerEntry.Create(Guid.NewGuid(), "USD", LedgerDirection.Credit, 1000)
        };

        Assert.Throws<ArgumentException>(() =>
            LedgerTransaction.Post(Guid.NewGuid(), entries));
    }

    [Fact]
    public void PostRejectsSingleEntry()
    {
        var entries = new[]
        {
            LedgerEntry.Create(Guid.NewGuid(), "SGD", LedgerDirection.Debit, 1000)
        };

        Assert.Throws<ArgumentException>(() =>
            LedgerTransaction.Post(Guid.NewGuid(), entries));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EntryRejectsNonPositiveAmount(long amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LedgerEntry.Create(
                Guid.NewGuid(), "SGD", LedgerDirection.Debit, amount));
    }
}
