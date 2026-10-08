using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class EfPaymentSuccessPersistence(PaymentsDbContext dbContext)
    : IPaymentSuccessPersistence
{
    public async Task SaveAsync(
        Payment payment,
        PaymentLedgerPostingIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(intent);

        if (payment.Id != intent.PaymentId)
        {
            throw new ArgumentException(
                "Posting intent must belong to the payment.", nameof(intent));
        }

        if (payment.Status != PaymentStatus.Succeeded)
        {
            throw new ArgumentException(
                "Payment must be succeeded.", nameof(payment));
        }

        dbContext.Payments.Update(payment);
        await dbContext.PaymentLedgerPostingIntents.AddAsync(intent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
