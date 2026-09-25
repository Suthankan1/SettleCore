using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Ledger.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Modules.Ledger.Infrastructure.IntegrationTests.Persistence;

public sealed class LedgerDbContextTests
{
    [Fact]
    public async Task CanConnectToPostgreSql()
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

        var canConnect =
            await dbContext.Database.CanConnectAsync();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task PersistsLedgerAccount()
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

        var account = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "sgd");

        dbContext.LedgerAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persisted =
            await dbContext.LedgerAccounts.SingleAsync();

        Assert.Equal(account.Id, persisted.Id);
        Assert.Equal(ledgerId, persisted.LedgerId);
        Assert.Equal("SGD", persisted.Currency);
    }
}
