using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Ledger.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

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
}