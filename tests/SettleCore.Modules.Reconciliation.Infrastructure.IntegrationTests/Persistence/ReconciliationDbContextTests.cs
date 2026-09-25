using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Reconciliation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Reconciliation.Infrastructure.IntegrationTests.Persistence;

public sealed class ReconciliationDbContextTests
{
    [Fact]
    public async Task CanConnectToPostgreSql()
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

        var canConnect =
            await dbContext.Database.CanConnectAsync();

        Assert.True(canConnect);
    }
}