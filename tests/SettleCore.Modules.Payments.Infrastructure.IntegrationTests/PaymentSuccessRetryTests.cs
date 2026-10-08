using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace SettleCore.Modules.Payments.Infrastructure.IntegrationTests;

public sealed class PaymentSuccessRetryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task RetryRequiresIdenticalPostingPayload(int changedField)
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options;
        var payment = Payment.Create(100m, "SGD");
        var original = PaymentLedgerPostingIntent.Create(payment.Id,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "SGD", 10_000, 300);
        await using (var seed = new PaymentsDbContext(options))
        {
            await seed.Database.MigrateAsync();
            seed.Payments.Add(payment);
            await seed.SaveChangesAsync();
            payment.MarkSucceeded();
            var persistence = new EfPaymentSuccessPersistence(seed);
            await persistence.SaveAsync(payment, original);
            if (changedField == 0)
            {
                await persistence.SaveAsync(payment, Copy(original, 0));
            }
        }
        await using var writer = new PaymentsDbContext(options);
        var loaded = await writer.Payments.SingleAsync(x => x.Id == payment.Id);
        var retry = Copy(original, changedField);
        var repository = new EfPaymentSuccessPersistence(writer);
        if (changedField == 0)
        {
            await repository.SaveAsync(loaded, retry);
        }
        else
        {
            await Assert.ThrowsAsync<PaymentLedgerPostingIntentConflictException>(
                () => repository.SaveAsync(loaded, retry));
        }
        await using var reader = new PaymentsDbContext(options);
        Assert.Equal(PaymentStatus.Succeeded, (await reader.Payments.SingleAsync()).Status);
        var stored = Assert.Single(await reader.PaymentLedgerPostingIntents.ToListAsync());
        Assert.Equal(original.TransactionId, stored.TransactionId);
        Assert.Equal(original.LedgerId, stored.LedgerId);
        Assert.Equal(original.ProcessorReceivableAccountId, stored.ProcessorReceivableAccountId);
        Assert.Equal(original.MerchantPayableAccountId, stored.MerchantPayableAccountId);
        Assert.Equal(original.PlatformRevenueAccountId, stored.PlatformRevenueAccountId);
        Assert.Equal(original.Currency, stored.Currency);
        Assert.Equal(original.GrossAmountMinorUnits, stored.GrossAmountMinorUnits);
        Assert.Equal(original.FeeAmountMinorUnits, stored.FeeAmountMinorUnits);
    }

    private static PaymentLedgerPostingIntent Copy(PaymentLedgerPostingIntent original, int changedField)
    {
        return PaymentLedgerPostingIntent.Create(original.PaymentId,
            changedField == 1 ? Guid.NewGuid() : original.TransactionId,
            changedField == 2 ? Guid.NewGuid() : original.LedgerId,
            changedField == 3 ? Guid.NewGuid() : original.ProcessorReceivableAccountId,
            changedField == 4 ? Guid.NewGuid() : original.MerchantPayableAccountId,
            changedField == 5 ? Guid.NewGuid() : original.PlatformRevenueAccountId,
            changedField == 6 ? "USD" : original.Currency,
            changedField == 7 ? 20_000 : original.GrossAmountMinorUnits,
            changedField == 8 ? 400 : original.FeeAmountMinorUnits);
    }
}
