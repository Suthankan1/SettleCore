using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Ledger.Domain;
using SettleCore.Modules.Ledger.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Ledger.Infrastructure.IntegrationTests.Persistence;

public sealed class EfLedgerRepositoryTests
{
    [Fact]
    public async Task AddTransactionAsyncPersistsTransactionAndEntries()
    {
        await using var postgres =
            new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("settlecore_test")
                .WithUsername("settlecore")
                .WithPassword("settlecore")
                .Build();

        await postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<LedgerDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

        await using var dbContext =
            new LedgerDbContext(options);

        await dbContext.Database.MigrateAsync();

        var ledgerId = Guid.NewGuid();

        var debitAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        var creditAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        dbContext.LedgerAccounts.AddRange(
            debitAccount,
            creditAccount);

        await dbContext.SaveChangesAsync();

        var entries = new[]
        {
            LedgerEntry.Create(
                debitAccount.Id,
                "SGD",
                LedgerDirection.Debit,
                1000),

            LedgerEntry.Create(
                creditAccount.Id,
                "SGD",
                LedgerDirection.Credit,
                1000)
        };

        var transaction = LedgerTransaction.Post(
            Guid.NewGuid(),
            ledgerId,
            entries,
            new[]
            {
                debitAccount,
                creditAccount
            });

        var repository =
            new EfLedgerRepository(dbContext);

        await repository.AddTransactionAsync(transaction);

        dbContext.ChangeTracker.Clear();

        var persisted =
            await dbContext.LedgerTransactions
                .Include(record => record.Entries)
                .SingleAsync();

        Assert.Equal(transaction.Id, persisted.Id);
        Assert.Equal(ledgerId, persisted.LedgerId);
        Assert.Equal(2, persisted.Entries.Count);

        Assert.Contains(
            persisted.Entries,
            entry =>
                entry.AccountId == debitAccount.Id &&
                entry.Currency == "SGD" &&
                entry.Direction == LedgerDirection.Debit &&
                entry.AmountMinorUnits == 1000);

        Assert.Contains(
            persisted.Entries,
            entry =>
                entry.AccountId == creditAccount.Id &&
                entry.Currency == "SGD" &&
                entry.Direction == LedgerDirection.Credit &&
                entry.AmountMinorUnits == 1000);
    }

    [Fact]
    public async Task GetAccountsByIdsAsyncReturnsRequestedAccounts()
    {
        await using var postgres =
            new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("settlecore_test")
                .WithUsername("settlecore")
                .WithPassword("settlecore")
                .Build();

        await postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<LedgerDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

        await using var dbContext =
            new LedgerDbContext(options);

        await dbContext.Database.MigrateAsync();

        var ledgerId = Guid.NewGuid();

        var firstAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        var secondAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "USD");

        var unrelatedAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "EUR");

        dbContext.LedgerAccounts.AddRange(
            firstAccount,
            secondAccount,
            unrelatedAccount);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new EfLedgerRepository(dbContext);

        var accounts =
            await repository.GetAccountsByIdsAsync(
                new[]
                {
                    firstAccount.Id,
                    secondAccount.Id
                });

        Assert.Equal(2, accounts.Count);

        Assert.Contains(
            accounts,
            account =>
                account.Id == firstAccount.Id &&
                account.LedgerId == ledgerId &&
                account.Currency == "SGD");

        Assert.Contains(
            accounts,
            account =>
                account.Id == secondAccount.Id &&
                account.LedgerId == ledgerId &&
                account.Currency == "USD");

        Assert.DoesNotContain(
            accounts,
            account =>
                account.Id == unrelatedAccount.Id);
    }
}
