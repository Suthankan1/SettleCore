namespace SettleCore.Modules.Payments.Application.AttachProviderReference;

public sealed class ProviderPaymentReferenceConflictException
    : Exception
{
    public ProviderPaymentReferenceConflictException(
        Exception innerException)
        : base(
            "Provider payment reference is already attached to another payment.",
            innerException)
    {
    }
}
