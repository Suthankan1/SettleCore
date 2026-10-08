using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
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

        var existing = await dbContext.PaymentLedgerPostingIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == intent.PaymentId, cancellationToken);
        if (existing is not null)
        {
            if (existing.TransactionId != intent.TransactionId ||
                existing.LedgerId != intent.LedgerId ||
                existing.ProcessorReceivableAccountId != intent.ProcessorReceivableAccountId ||
                existing.MerchantPayableAccountId != intent.MerchantPayableAccountId ||
                existing.PlatformRevenueAccountId != intent.PlatformRevenueAccountId ||
                existing.Currency != intent.Currency ||
                existing.GrossAmountMinorUnits != intent.GrossAmountMinorUnits ||
                existing.FeeAmountMinorUnits != intent.FeeAmountMinorUnits)
            {
                throw new PaymentLedgerPostingIntentConflictException(payment.Id);
            }
            return;
        }

        dbContext.Payments.Update(payment);
        await dbContext.PaymentLedgerPostingIntents.AddAsync(intent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
