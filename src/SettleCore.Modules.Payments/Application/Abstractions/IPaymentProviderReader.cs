namespace SettleCore.Modules.Payments.Application.Abstractions;

public interface IPaymentProviderReader
{
    Task<CreateProviderPaymentResult> GetPaymentAsync(GetProviderPaymentRequest request,
        CancellationToken cancellationToken = default);
}
