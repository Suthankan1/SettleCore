using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class EfPaymentLedgerPostingIntentRepository(
    PaymentsDbContext dbContext)
    : IPaymentLedgerPostingIntentRepository
{
    public async Task AddAsync(
        PaymentLedgerPostingIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        await dbContext.PaymentLedgerPostingIntents.AddAsync(
            intent,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<PaymentLedgerPostingIntent?> GetByPaymentIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PaymentLedgerPostingIntents.SingleOrDefaultAsync(
            intent => intent.PaymentId == paymentId,
            cancellationToken);
    }
}
