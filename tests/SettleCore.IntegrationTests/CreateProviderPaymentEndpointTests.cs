using System.Diagnostics;
using System.Security.Cryptography;
using SettleCore.Modules.Ledger.Domain;
using SettleCore.Modules.Ledger.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Payments.Application.CreatePayment;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Stripe;

namespace SettleCore.IntegrationTests;

public sealed class CreateProviderPaymentEndpointTests
{
    [Fact]
    public async Task DisabledProviderCreationReturnsNotFoundWithoutResolvingStripe()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/payments/{Guid.NewGuid()}/provider-payment", null)).StatusCode);
    }

    [Fact]
    public async Task PreparedHttpPaymentCreatesIntentAndPersistsReferenceWithoutCompletingPayment()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync();
        using var transport = new Transport();
        using var http = new HttpClient(transport);
        using var factory = new Factory(postgres.GetConnectionString(), http);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/payments/{Guid.NewGuid()}/provider-payment", null)).StatusCode);
        var created = await client.PostAsJsonAsync("/payments", new { amount = 12.34m, currency = "SGD" });
        var payment = Assert.IsType<CreatePaymentResult>(await created.Content.ReadFromJsonAsync<CreatePaymentResult>());
        var path = $"/payments/{payment.PaymentId}/provider-payment";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path, null)).StatusCode);
        Assert.Equal(0, transport.Calls);
        var input = new PaymentLedgerPostingInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 34);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/payments/{payment.PaymentId}/ledger-posting/preparation", input)).StatusCode);
        var response = await client.PostAsync(path, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("test-client-secret", json.RootElement.GetProperty("clientSecret").GetString());
        Assert.Equal("pi_http_create", json.RootElement.GetProperty("providerReference").GetProperty("reference").GetString());
        Assert.Equal(1, transport.Calls);
        Assert.Equal($"settlecore:payment:{payment.PaymentId:N}:create", transport.IdempotencyKey);
        Assert.Equal("1234", transport.Fields["amount"]);
        Assert.Equal("sgd", transport.Fields["currency"]);
        Assert.Equal(payment.PaymentId.ToString("D"), transport.Fields["metadata[settlecore_payment_id]"]);
        Assert.False(transport.Fields.ContainsKey("application_fee_amount"));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path, null)).StatusCode);
        Assert.Equal(1, transport.Calls);
        using var final = factory.Services.CreateScope();
        var db = final.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var stored = await db.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, stored.Status);
        Assert.Equal("pi_http_create", stored.ProviderReference!.Value.Reference);
        Assert.Single(await db.PaymentLedgerPostingPreparations.ToListAsync());
        Assert.Empty(await db.PaymentLedgerPostingIntents.ToListAsync());
        Assert.Empty(await db.PaymentLedgerPostingEvents.ToListAsync());
    }

    [Fact]
    public async Task CreatedProviderPaymentCompletesOnlyFromSignedEvidenceAndPostsBalancedLedgerOnce()
    {
        await using var payments = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await using var ledger = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await payments.StartAsync();
        await ledger.StartAsync();
        using var transport = new Transport();
        using var http = new HttpClient(transport);
        using var factory = new Factory(payments.GetConnectionString(), http, ledger.GetConnectionString());
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var input = new PaymentLedgerPostingInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 34);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
            var ledgerDb = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await ledgerDb.Database.MigrateAsync();
            ledgerDb.LedgerAccounts.AddRange(
                LedgerAccount.Open(input.ProcessorReceivableAccountId, input.LedgerId, "SGD"),
                LedgerAccount.Open(input.MerchantPayableAccountId, input.LedgerId, "SGD"),
                LedgerAccount.Open(input.PlatformRevenueAccountId, input.LedgerId, "SGD"));
            await ledgerDb.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/payments", new { amount = 12.34m, currency = "SGD" });
        var payment = Assert.IsType<CreatePaymentResult>(await response.Content.ReadFromJsonAsync<CreatePaymentResult>());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/payments/{payment.PaymentId}/ledger-posting/preparation", input)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/payments/{payment.PaymentId}/provider-payment", null)).StatusCode);
        using (var beforeScope = factory.Services.CreateScope())
        {
            var before = beforeScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            Assert.Equal(PaymentStatus.Pending, (await before.Payments.AsNoTracking().SingleAsync()).Status);
            Assert.Empty(await before.PaymentLedgerPostingIntents.ToListAsync());
        }
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_created_payment", @object = "event", api_version = StripeConfiguration.ApiVersion,
            created = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), livemode = false,
            type = "payment_intent.succeeded", data = new { @object = new
            {
                id = "pi_http_create", @object = "payment_intent", amount = 1234, amount_received = 1234,
                currency = "sgd", status = "succeeded", metadata = new { settlecore_payment_id = payment.PaymentId.ToString("D") }
            }}
        });
        async Task<HttpStatusCode> SendWebhook(bool valid)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(valid ? "whsec_test_placeholder" : "wrong-secret"),
                Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
            using var request = new HttpRequestMessage(HttpMethod.Post, "/payments/webhooks/stripe")
            { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
            request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}");
            using var result = await client.SendAsync(request);
            return result.StatusCode;
        }
        Assert.Equal(HttpStatusCode.BadRequest, await SendWebhook(valid: false));
        using (var invalidScope = factory.Services.CreateScope())
        {
            var invalid = invalidScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            Assert.Empty(await invalid.PaymentProviderEventReceipts.ToListAsync());
            Assert.Equal(PaymentStatus.Pending, (await invalid.Payments.AsNoTracking().SingleAsync()).Status);
        }
        Assert.Equal(HttpStatusCode.OK, await SendWebhook(valid: true));
        using var finalScope = factory.Services.CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(15))
        {
            if (await db.PaymentLedgerPostingIntents.AsNoTracking().AnyAsync(x => x.Status == PaymentLedgerPostingIntentStatus.Posted)) break;
            await Task.Delay(50);
        }
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(PaymentLedgerPostingIntentStatus.Posted, (await db.PaymentLedgerPostingIntents.AsNoTracking().SingleAsync()).Status);
        Assert.NotNull((await db.PaymentProviderEventReceipts.AsNoTracking().SingleAsync()).ProcessedAt);
        Assert.Equal(HttpStatusCode.OK, await SendWebhook(valid: true));
        Assert.Single(await db.PaymentProviderEventReceipts.AsNoTracking().ToListAsync());
        var transaction = Assert.Single(await finalScope.ServiceProvider.GetRequiredService<LedgerDbContext>()
            .LedgerTransactions.AsNoTracking().Include(x => x.Entries).ToListAsync());
        Assert.Equal(input.TransactionId, transaction.Id);
        Assert.Equal(input.LedgerId, transaction.LedgerId);
        Assert.Equal(3, transaction.Entries.Count);
        Assert.Equal(1234, transaction.Entries.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.AmountMinorUnits));
        Assert.Equal(1234, transaction.Entries.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.AmountMinorUnits));
        Assert.Contains(transaction.Entries, x => x.AccountId == input.PlatformRevenueAccountId && x.AmountMinorUnits == 34);
    }

    private sealed class Factory(string connection, HttpClient http, string? ledgerConnection = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Stripe:Enabled"] = "true", ["Payments:Stripe:ApiKey"] = "sk_test_placeholder",
                ["Payments:Stripe:WebhookSecret"] = "whsec_test_placeholder", ["Payments:Stripe:SignatureToleranceSeconds"] = "300",
                ["Payments:Stripe:IsLiveMode"] = "false",
                ["PaymentProviderEventWorker:Enabled"] = (ledgerConnection is not null).ToString(),
                ["PaymentProviderEventWorker:PollIntervalMilliseconds"] = "50",
                ["PaymentProviderEventWorker:RetryDelaySeconds"] = "1",
                ["PaymentLedgerPostingWorker:Enabled"] = (ledgerConnection is not null).ToString(),
                ["PaymentLedgerPostingWorker:PollIntervalMilliseconds"] = "50",
                ["PaymentLedgerPostingWorker:RetryDelaySeconds"] = "1"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<PaymentsDbContext>>();
                services.RemoveAll<DbContextOptions<PaymentsDbContext>>();
                services.RemoveAll<PaymentsDbContext>();
                services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connection));
                if (ledgerConnection is not null)
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<LedgerDbContext>>();
                    services.RemoveAll<DbContextOptions<LedgerDbContext>>();
                    services.RemoveAll<LedgerDbContext>();
                    services.AddDbContext<LedgerDbContext>(options => options.UseNpgsql(ledgerConnection));
                }
                services.RemoveAll<StripeClient>();
                services.AddSingleton(new StripeClient("sk_test_placeholder", httpClient: new SystemNetHttpClient(http, maxNetworkRetries: 0)));
            });
        }
    }

    private sealed class Transport : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public Dictionary<string, string> Fields { get; private set; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            IdempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Fields = body.Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(
                pair => WebUtility.UrlDecode(pair[0]), pair => WebUtility.UrlDecode(pair[1]));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"pi_http_create","object":"payment_intent","amount":1234,"currency":"sgd","status":"succeeded","client_secret":"test-client-secret"}""", Encoding.UTF8, "application/json")
            };
        }
    }
}
