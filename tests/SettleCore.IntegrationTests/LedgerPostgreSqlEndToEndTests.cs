using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Ledger.Application.PostLedgerTransaction;
using SettleCore.Modules.Ledger.Domain;
using SettleCore.Modules.Ledger.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using SettleCore.Modules.Ledger.Application.GetLedgerTransactionById;

namespace SettleCore.IntegrationTests;

public sealed class LedgerPostgreSqlEndToEndTests
{
    [Fact]
    public async Task PostLedgerTransactionPersistsEntriesInPostgreSql()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("settlecore_test")
            .WithUsername("settlecore")
            .WithPassword("settlecore")
            .Build();

        await postgres.StartAsync();

        using var factory = new LedgerApiFactory(postgres.GetConnectionString());
        var ledgerId = Guid.NewGuid();
        var debitAccount = LedgerAccount.Open(Guid.NewGuid(), ledgerId, "SGD");
        var creditAccount = LedgerAccount.Open(Guid.NewGuid(), ledgerId, "SGD");

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await context.Database.MigrateAsync();
            context.LedgerAccounts.AddRange(debitAccount, creditAccount);
            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var transactionId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            "/ledger/transactions",
            new
            {
                transactionId,
                ledgerId,
                entries = new[]
                {
                    new
                    {
                        accountId = debitAccount.Id,
                        currency = "sgd",
                        direction = LedgerDirection.Debit,
                        amountMinorUnits = 1000L
                    },
                    new
                    {
                        accountId = creditAccount.Id,
                        currency = "SGD",
                        direction = LedgerDirection.Credit,
                        amountMinorUnits = 1000L
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = Assert.IsType<PostLedgerTransactionResult>(
            await response.Content.ReadFromJsonAsync<PostLedgerTransactionResult>());
        Assert.Equal(transactionId, created.TransactionId);
        Assert.Equal(ledgerId, created.LedgerId);
        Assert.Equal(2, created.EntryCount);
        Assert.Equal($"/ledger/transactions/{transactionId}",
            response.Headers.Location?.OriginalString);

        using var verificationScope = factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider
            .GetRequiredService<LedgerDbContext>();
        var persisted = await verificationContext.LedgerTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Entries)
            .SingleAsync(transaction => transaction.Id == transactionId);

        Assert.Equal(ledgerId, persisted.LedgerId);
        Assert.Equal(2, persisted.Entries.Count);
        Assert.All(persisted.Entries, entry =>
        {
            Assert.Equal(transactionId, entry.TransactionId);
            Assert.Equal("SGD", entry.Currency);
            Assert.Equal(1000L, entry.AmountMinorUnits);
        });
        var debit = Assert.Single(persisted.Entries,
            entry => entry.Direction == LedgerDirection.Debit);
        var credit = Assert.Single(persisted.Entries,
            entry => entry.Direction == LedgerDirection.Credit);
        Assert.Equal(debitAccount.Id, debit.AccountId);
        Assert.Equal(creditAccount.Id, credit.AccountId);
    }

    private sealed class LedgerApiFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<LedgerDbContext>>();
                services.RemoveAll<DbContextOptions<LedgerDbContext>>();
                services.RemoveAll<LedgerDbContext>();
                services.AddDbContext<LedgerDbContext>(
                    options => options.UseNpgsql(connectionString));
            });
        }
    }

    [Fact]
    public async Task PostedLedgerTransactionCanBeRetrievedFromPostgreSql()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("settlecore_test")
            .WithUsername("settlecore")
            .WithPassword("settlecore")
            .Build();

        await postgres.StartAsync();

        using var factory =
            new LedgerApiFactory(postgres.GetConnectionString());

        var ledgerId = Guid.NewGuid();

        var debitAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        var creditAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        using (var scope = factory.Services.CreateScope())
        {
            var context =
                scope.ServiceProvider
                    .GetRequiredService<LedgerDbContext>();

            await context.Database.MigrateAsync();

            context.LedgerAccounts.AddRange(
                debitAccount,
                creditAccount);

            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var transactionId = Guid.NewGuid();

        var postResponse = await client.PostAsJsonAsync(
            "/ledger/transactions",
            new
            {
                transactionId,
                ledgerId,
                entries = new[]
                {
                    new
                    {
                        accountId = debitAccount.Id,
                        currency = "SGD",
                        direction = LedgerDirection.Debit,
                        amountMinorUnits = 2500L
                    },
                    new
                    {
                        accountId = creditAccount.Id,
                        currency = "SGD",
                        direction = LedgerDirection.Credit,
                        amountMinorUnits = 2500L
                    }
                }
            });

        Assert.Equal(
            HttpStatusCode.Created,
            postResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/ledger/transactions/{transactionId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var retrieved =
            Assert.IsType<GetLedgerTransactionByIdResult>(
                await getResponse.Content
                    .ReadFromJsonAsync<GetLedgerTransactionByIdResult>());

        Assert.Equal(
            transactionId,
            retrieved.TransactionId);

        Assert.Equal(
            ledgerId,
            retrieved.LedgerId);

        Assert.Equal(
            2,
            retrieved.Entries.Count);

        var debit = Assert.Single(
            retrieved.Entries,
            entry => entry.Direction == LedgerDirection.Debit);

        Assert.Equal(
            debitAccount.Id,
            debit.AccountId);

        Assert.Equal(
            "SGD",
            debit.Currency);

        Assert.Equal(
            2500L,
            debit.AmountMinorUnits);

        var credit = Assert.Single(
            retrieved.Entries,
            entry => entry.Direction == LedgerDirection.Credit);

        Assert.Equal(
            creditAccount.Id,
            credit.AccountId);

        Assert.Equal(
            "SGD",
            credit.Currency);

        Assert.Equal(
            2500L,
            credit.AmountMinorUnits);
    }
}
