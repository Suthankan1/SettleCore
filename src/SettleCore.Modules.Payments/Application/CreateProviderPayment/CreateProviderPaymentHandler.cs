using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.Application.CreateProviderPayment;

public sealed class CreateProviderPaymentHandler(
    IPaymentRepository payments,
    IPaymentLedgerPostingPreparationRepository preparations,
    IPaymentProvider provider,
    IPaymentProviderReferencePersistence references)
{
    public async Task<CreateProviderPaymentResult?> HandleAsync(Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var identity = PaymentId.From(paymentId);
        var payment = await payments.GetByIdAsync(identity, cancellationToken);
        if (payment is null) return null;
        if (payment.Status != PaymentStatus.Pending || payment.ProviderReference is not null)
            throw new InvalidOperationException("Provider creation requires an unlinked pending payment.");
        var preparation = await preparations.GetByPaymentIdAsync(identity, cancellationToken);
        var amount = PaymentAmountConversion.ToMinorUnits(payment.Amount, payment.Currency);
        if (preparation is null || preparation.PaymentId != paymentId ||
            preparation.GrossAmountMinorUnits != amount || preparation.Currency != payment.Currency)
            throw new InvalidOperationException("Provider creation requires matching persisted posting preparation.");

        var result = await provider.CreatePaymentAsync(
            new CreateProviderPaymentRequest(identity, amount, payment.Currency), cancellationToken);
        if (!await references.AttachAsync(identity, result.ProviderReference, cancellationToken))
            throw new InvalidOperationException("Provider reference could not be persisted.");
        return result;
    }
}
