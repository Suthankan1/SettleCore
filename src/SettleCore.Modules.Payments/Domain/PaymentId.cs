namespace SettleCore.Modules.Payments.Domain;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId Empty => new(Guid.Empty);

    public static PaymentId New()
    {
        return new PaymentId(Guid.NewGuid());
    }
}