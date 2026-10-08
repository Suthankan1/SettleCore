using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Payments.Infrastructure.IntegrationTests;

public sealed class PaymentSuccessPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessAndIntentAreCommittedTogether(bool duplicateIntent)
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options;
        var payment = Payment.Create(100m, "SGD");
        var intent = CreateIntent(payment.Id);
        await using (var seed = new PaymentsDbContext(options))
        {
            await seed.Database.MigrateAsync();
            seed.Payments.Add(payment);
            if (duplicateIntent)
            {
                seed.PaymentLedgerPostingIntents.Add(CreateIntent(payment.Id));
            }
            await seed.SaveChangesAsync();
        }
        await using (var writer = new PaymentsDbContext(options))
        {
            var loaded = await writer.Payments.SingleAsync(x => x.Id == payment.Id);
            loaded.MarkSucceeded();
            var persistence = new EfPaymentSuccessPersistence(writer);
            if (duplicateIntent)
            {
                await Assert.ThrowsAsync<DbUpdateException>(
                    () => persistence.SaveAsync(loaded, intent));
            }
            else
            {
                await persistence.SaveAsync(loaded, intent);
            }
        }
        await using var reader = new PaymentsDbContext(options);
        var stored = await reader.Payments.SingleAsync(x => x.Id == payment.Id);
        Assert.Equal(duplicateIntent ? PaymentStatus.Pending : PaymentStatus.Succeeded,
            stored.Status);
        var storedIntent = await reader.PaymentLedgerPostingIntents
            .SingleAsync(x => x.PaymentId == payment.Id);
        if (!duplicateIntent)
        {
            Assert.Equal(intent.TransactionId, storedIntent.TransactionId);
        }
        Assert.Equal(PaymentLedgerPostingIntentStatus.Pending, storedIntent.Status);
    }

    private static PaymentLedgerPostingIntent CreateIntent(PaymentId paymentId)
    {
        return PaymentLedgerPostingIntent.Create(paymentId,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "SGD", 10_000, 300);
    }
}
