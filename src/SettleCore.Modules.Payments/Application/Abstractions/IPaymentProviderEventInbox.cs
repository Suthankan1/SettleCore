namespace SettleCore.Modules.Payments.Application.Abstractions;

public interface IPaymentProviderEventInbox
{
    Task<bool> ReceiveAsync(ProviderPaymentSucceededEvent evidence, CancellationToken cancellationToken = default);
}
