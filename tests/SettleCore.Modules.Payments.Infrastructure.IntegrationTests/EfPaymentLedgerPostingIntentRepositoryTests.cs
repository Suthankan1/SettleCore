using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;

namespace SettleCore.Modules.Payments.Infrastructure.IntegrationTests;

public sealed class EfPaymentLedgerPostingIntentRepositoryTests
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18-alpine")
            .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CanPersistAndReloadPendingLedgerPostingIntent()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var dbContext = new PaymentsDbContext(options);

        await dbContext.Database.MigrateAsync();

        var paymentId = PaymentId.New();
        var transactionId = Guid.NewGuid();
        var ledgerId = Guid.NewGuid();
        var processorReceivableAccountId = Guid.NewGuid();
        var merchantPayableAccountId = Guid.NewGuid();
        var platformRevenueAccountId = Guid.NewGuid();

        var intent = PaymentLedgerPostingIntent.Create(
            paymentId,
            transactionId,
            ledgerId,
            processorReceivableAccountId,
            merchantPayableAccountId,
            platformRevenueAccountId,
            "SGD",
            grossAmountMinorUnits: 10_000,
            feeAmountMinorUnits: 300);

        var repository =
            new EfPaymentLedgerPostingIntentRepository(dbContext);
        await repository.AddAsync(intent);

        dbContext.ChangeTracker.Clear();

        await using var readContext = new PaymentsDbContext(options);
        var reader =
            new EfPaymentLedgerPostingIntentRepository(readContext);
        var reloaded = await reader.GetByPaymentIdAsync(paymentId);

        Assert.NotNull(reloaded);
        Assert.Null(await reader.GetByPaymentIdAsync(PaymentId.New()));

        Assert.Equal(paymentId, reloaded.PaymentId);
        Assert.Equal(transactionId, reloaded.TransactionId);
        Assert.Equal(ledgerId, reloaded.LedgerId);
        Assert.Equal(
            processorReceivableAccountId,
            reloaded.ProcessorReceivableAccountId);
        Assert.Equal(
            merchantPayableAccountId,
            reloaded.MerchantPayableAccountId);
        Assert.Equal(
            platformRevenueAccountId,
            reloaded.PlatformRevenueAccountId);
        Assert.Equal("SGD", reloaded.Currency);
        Assert.Equal(10_000, reloaded.GrossAmountMinorUnits);
        Assert.Equal(300, reloaded.FeeAmountMinorUnits);
        Assert.Equal(
            PaymentLedgerPostingIntentStatus.Pending,
            reloaded.Status);
    }
}
