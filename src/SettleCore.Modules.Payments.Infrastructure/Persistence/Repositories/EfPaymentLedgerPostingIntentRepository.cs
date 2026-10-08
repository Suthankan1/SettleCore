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
        return dbContext.PaymentLedgerPostingIntents.AsNoTracking().SingleOrDefaultAsync(
            intent => intent.PaymentId == paymentId,
            cancellationToken);
    }
    public async Task<bool> MarkPostedAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.PaymentLedgerPostingIntents
            .Where(intent => intent.PaymentId == paymentId &&
                (intent.Status == PaymentLedgerPostingIntentStatus.Pending ||
                 intent.Status == PaymentLedgerPostingIntentStatus.Posted))
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                intent => intent.Status, PaymentLedgerPostingIntentStatus.Posted),
                cancellationToken);
        return affected == 1;
    }
}
