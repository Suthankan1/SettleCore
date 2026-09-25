using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Reconciliation.Application;
using SettleCore.Modules.Reconciliation.Application.CreateReconciliation;
using SettleCore.Modules.Reconciliation.Domain;

namespace SettleCore.IntegrationTests;

public sealed class CreateReconciliationEndpointTests
{
    [Fact]
    public async Task PostReconciliationsCreatesAndPersistsComparison()
    {
        using var factory = new ReconciliationApiFactory();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var response = await client.PostAsJsonAsync(
            "/reconciliations",
            new
            {
                expectedAmount = 100.00m,
                expectedCurrency = "sgd",
                actualAmount = 90.00m,
                actualCurrency = "sgd"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = Assert.IsType<CreateReconciliationResult>(
            await response.Content.ReadFromJsonAsync<CreateReconciliationResult>());

        Assert.NotEqual(Guid.Empty, created.ReconciliationId);
        Assert.Equal(ReconciliationStatus.AmountMismatch, created.Status);
        Assert.Equal(100.00m, created.ExpectedAmount);
        Assert.Equal(90.00m, created.ActualAmount);
        Assert.Equal("SGD", created.ExpectedCurrency);
        Assert.Equal("SGD", created.ActualCurrency);
        Assert.Equal(
            $"/reconciliations/{created.ReconciliationId}",
            response.Headers.Location?.OriginalString);

        var persisted = Assert.IsType<ReconciliationRecord>(
            factory.Repository.AddedRecord);
        Assert.Equal(created.ReconciliationId, persisted.Id);
    }

    private sealed class ReconciliationApiFactory : WebApplicationFactory<Program>
    {
        public RecordingReconciliationRepository Repository { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IReconciliationRepository>();
                services.AddSingleton<IReconciliationRepository>(Repository);
            });
        }
    }

    public sealed class RecordingReconciliationRepository : IReconciliationRepository
    {
        public ReconciliationRecord? AddedRecord { get; private set; }

        public Task AddAsync(
            ReconciliationRecord record,
            CancellationToken cancellationToken = default)
        {
            AddedRecord = record;
            return Task.CompletedTask;
        }
    }
}
