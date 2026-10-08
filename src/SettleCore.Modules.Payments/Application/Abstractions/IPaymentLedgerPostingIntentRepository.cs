using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.Application.Abstractions;

public interface IPaymentLedgerPostingIntentRepository
{
    Task AddAsync(
        PaymentLedgerPostingIntent intent,
        CancellationToken cancellationToken = default);

    Task<PaymentLedgerPostingIntent?> GetByPaymentIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default);
}
