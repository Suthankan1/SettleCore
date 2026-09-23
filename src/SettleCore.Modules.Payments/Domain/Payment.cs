namespace SettleCore.Modules.Payments.Domain;

public sealed class Payment
{
    private Payment(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public PaymentStatus Status { get; }

    public static Payment Create(decimal amount, string currency)
    {
        return new Payment(amount, currency);
    }
}