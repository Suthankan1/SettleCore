using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Dispatch;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Payments.Infrastructure.IntegrationTests;

public sealed class PaymentProviderEventBatchProcessorTests
{
    [Fact]
    public async Task DeferredAndInvalidReceiptsAreScheduledWithoutStarvingValidReceipt()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync();
        var now = new DateTimeOffset(2026, 10, 10, 5, 0, 0, TimeSpan.Zero);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new Clock(now));
        services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(postgres.GetConnectionString()));
        services.AddScoped<IPaymentProviderEventInbox, EfPaymentProviderEventInbox>();
        services.AddScoped<IPaymentProviderEventProcessor, EfPaymentProviderEventProcessor>();
        services.AddSingleton<PaymentProviderEventBatchProcessor>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var seed = provider.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            await db.Database.MigrateAsync();
            var payment = Payment.Create(12.34m, "SGD");
            payment.AttachProviderReference(ProviderPaymentReference.Create("stripe", "pi_test"));
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            await new EfPaymentLedgerPostingPreparationRepository(db).SaveAsync(PaymentLedgerPostingRequestFactory.Create(
                payment, new PaymentLedgerPostingInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100)));
            var inbox = seed.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>();
            var evidence = new ProviderPaymentSucceededEvent("evt_b_valid", payment.Id,
                ProviderPaymentReference.Create("stripe", "pi_test"), 1234, "SGD", now.AddMinutes(-1), false);
            await inbox.ReceiveAsync(evidence with { EventId = "evt_a_early", PaymentId = PaymentId.New() });
            await inbox.ReceiveAsync(evidence);
            await inbox.ReceiveAsync(evidence with { EventId = "evt_c_wrong_mode", IsLiveMode = true });
        }
        var batch = provider.GetRequiredService<PaymentProviderEventBatchProcessor>();
        var delay = TimeSpan.FromMinutes(1);
        Assert.Equal(0, await batch.ProcessAsync("stripe", false, 1, delay));
        Assert.Equal(1, await batch.ProcessAsync("stripe", false, 1, delay));
        Assert.Equal(0, await batch.ProcessAsync("stripe", false, 1, delay));
        Assert.Equal(0, await batch.ProcessAsync("stripe", false, 1, delay));
        await using var reader = provider.CreateAsyncScope();
        var context = reader.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var receipts = await context.PaymentProviderEventReceipts.OrderBy(x => x.EventId).ToListAsync();
        Assert.Null(receipts[0].ProcessedAt);
        Assert.Equal(now.Add(delay), receipts[0].NextAttemptAt);
        Assert.Equal(now, receipts[1].ProcessedAt);
        Assert.Null(receipts[1].NextAttemptAt);
        Assert.Null(receipts[2].ProcessedAt);
        Assert.Equal(now.Add(delay), receipts[2].NextAttemptAt);
        Assert.Equal(PaymentStatus.Succeeded, (await context.Payments.SingleAsync()).Status);
        Assert.Single(await context.PaymentLedgerPostingIntents.ToListAsync());
        Assert.Single(await context.PaymentLedgerPostingEvents.ToListAsync());
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
