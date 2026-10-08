using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class EfPaymentLedgerPostingIntentRepository(
    PaymentsDbContext dbContext, TimeProvider? timeProvider = null)
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
    public async Task<IReadOnlyList<PaymentLedgerPostingIntent>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        return await dbContext.PaymentLedgerPostingIntents.AsNoTracking()
            .Where(intent => intent.Status == PaymentLedgerPostingIntentStatus.Pending &&
                (intent.NextAttemptAt == null || intent.NextAttemptAt <= now) &&
                dbContext.Payments.Any(payment => payment.Id == intent.PaymentId &&
                    payment.Status == PaymentStatus.Succeeded))
            .OrderBy(intent => intent.NextAttemptAt.HasValue)
            .ThenBy(intent => intent.NextAttemptAt)
            .ThenBy(intent => intent.PaymentId)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    public async Task<bool> ScheduleRetryAsync(
        PaymentId paymentId,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        var utcNextAttempt = nextAttemptAt.ToUniversalTime();
        var affected = await dbContext.PaymentLedgerPostingIntents
            .Where(intent => intent.PaymentId == paymentId &&
                intent.Status == PaymentLedgerPostingIntentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                intent => intent.NextAttemptAt, utcNextAttempt), cancellationToken);
        return affected == 1;
    }
}
