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

    private sealed class Factory(string connection, HttpClient http) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Stripe:Enabled"] = "true", ["Payments:Stripe:ApiKey"] = "sk_test_placeholder",
                ["Payments:Stripe:WebhookSecret"] = "whsec_test_placeholder", ["Payments:Stripe:SignatureToleranceSeconds"] = "300",
                ["Payments:Stripe:IsLiveMode"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<PaymentsDbContext>>();
                services.RemoveAll<DbContextOptions<PaymentsDbContext>>();
                services.RemoveAll<PaymentsDbContext>();
                services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connection));
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
