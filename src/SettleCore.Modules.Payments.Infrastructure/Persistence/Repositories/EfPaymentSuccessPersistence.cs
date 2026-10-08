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

        dbContext.Payments.Update(payment);
        await dbContext.PaymentLedgerPostingIntents.AddAsync(intent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
