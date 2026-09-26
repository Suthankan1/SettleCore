using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Ledger.Application;
using SettleCore.Modules.Ledger.Application.PostLedgerTransaction;
using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.IntegrationTests;

public sealed class PostLedgerTransactionEndpointTests
{
    [Fact]
    public async Task PostLedgerTransactionCreatesAndPersistsTransaction()
    {
        var ledgerId = Guid.NewGuid();

        var debitAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        var creditAccount = LedgerAccount.Open(
            Guid.NewGuid(),
            ledgerId,
            "SGD");

        using var factory = new LedgerApiFactory(
            debitAccount,
            creditAccount);

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
                        currency = "SGD",
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

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var created =
            Assert.IsType<PostLedgerTransactionResult>(
                await response.Content
                    .ReadFromJsonAsync<PostLedgerTransactionResult>());

        Assert.Equal(
            transactionId,
            created.TransactionId);

        Assert.Equal(
            ledgerId,
            created.LedgerId);

        Assert.Equal(
            2,
            created.EntryCount);

        Assert.Equal(
            $"/ledger/transactions/{transactionId}",
            response.Headers.Location?.OriginalString);

        Assert.NotNull(factory.Repository.AddedTransaction);

        Assert.Equal(
            transactionId,
            factory.Repository.AddedTransaction.Id);
    }

    [Fact]
    public async Task PostLedgerTransactionRejectsUnbalancedEntriesWithoutPersisting()
    {
        var ledgerId = Guid.NewGuid();
        var debitAccount = LedgerAccount.Open(Guid.NewGuid(), ledgerId, "SGD");
        var creditAccount = LedgerAccount.Open(Guid.NewGuid(), ledgerId, "SGD");

        using var factory = new LedgerApiFactory(debitAccount, creditAccount);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var response = await client.PostAsJsonAsync(
            "/ledger/transactions",
            new
            {
                transactionId = Guid.NewGuid(),
                ledgerId,
                entries = new[]
                {
                    new { accountId = debitAccount.Id, currency = "SGD",
                        direction = LedgerDirection.Debit, amountMinorUnits = 1000L },
                    new { accountId = creditAccount.Id, currency = "SGD",
                        direction = LedgerDirection.Credit, amountMinorUnits = 900L }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<
            Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("entries", problem.Errors.Keys);
        Assert.Null(factory.Repository.AddedTransaction);
    }

    private sealed class LedgerApiFactory(
        params LedgerAccount[] accounts)
        : WebApplicationFactory<Program>
    {
        public RecordingLedgerRepository Repository { get; } =
            new(accounts);

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILedgerRepository>();

                services.AddSingleton<ILedgerRepository>(
                    Repository);
            });
        }
    }

    public sealed class RecordingLedgerRepository(
        params LedgerAccount[] accounts)
        : ILedgerRepository
    {
        private readonly IReadOnlyList<LedgerAccount> accounts =
            accounts;

        public LedgerTransaction? AddedTransaction { get; private set; }

        public Task<IReadOnlyList<LedgerAccount>>
            GetAccountsByIdsAsync(
                IReadOnlyCollection<Guid> accountIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LedgerAccount> matches =
                accounts
                    .Where(account =>
                        accountIds.Contains(account.Id))
                    .ToArray();

            return Task.FromResult(matches);
        }

        public Task AddTransactionAsync(
            LedgerTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            AddedTransaction = transaction;

            return Task.CompletedTask;
        }
    }
}