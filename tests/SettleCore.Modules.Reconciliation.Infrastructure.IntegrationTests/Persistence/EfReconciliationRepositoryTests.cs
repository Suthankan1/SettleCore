using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Reconciliation.Domain;
using SettleCore.Modules.Reconciliation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Reconciliation.Infrastructure.IntegrationTests.Persistence;

public sealed class EfReconciliationRepositoryTests
{
    [Fact]
    public async Task AddAsyncPersistsReconciliationRecord()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("settlecore_test")
            .WithUsername("settlecore")
            .WithPassword("settlecore")
            .Build();

        await postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<ReconciliationDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

        await using var dbContext =
            new ReconciliationDbContext(options);

        await dbContext.Database.MigrateAsync();

        var repository =
            new EfReconciliationRepository(dbContext);

        var record = new ReconciliationRecord(
            Guid.NewGuid(),
            ReconciliationStatus.CurrencyMismatch,
            expectedAmount: 100.00m,
            actualAmount: 100.00m,
            expectedCurrency: "SGD",
            actualCurrency: "USD");

        await repository.AddAsync(record);

        dbContext.ChangeTracker.Clear();

        var persisted =
            await dbContext.ReconciliationRecords.SingleAsync();

        Assert.Equal(record.Id, persisted.Id);
        Assert.Equal(
            ReconciliationStatus.CurrencyMismatch,
            persisted.Status);
        Assert.Equal(100.00m, persisted.ExpectedAmount);
        Assert.Equal(100.00m, persisted.ActualAmount);
        Assert.Equal("SGD", persisted.ExpectedCurrency);
        Assert.Equal("USD", persisted.ActualCurrency);
    }
}