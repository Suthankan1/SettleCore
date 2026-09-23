namespace SettleCore.Modules.Payments.Domain;

public sealed class Payment
{
    private Payment(
        PaymentId id,
        decimal amount,
        string currency)
    {
        Id = id;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    public PaymentId Id { get; }

    public decimal Amount { get; }

    public string Currency { get; }

    public PaymentStatus Status { get; }

    public static Payment Create(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Payment amount must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(currency);

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Payment currency must not be blank.",
                nameof(currency));
        }

        if (currency.Length != 3 || !currency.All(char.IsLetter))
        {
            throw new ArgumentException(
                "Payment currency must be a three-letter alphabetic code.",
                nameof(currency));
        }

        return new Payment(
            PaymentId.New(),
            amount,
            currency.ToUpperInvariant());
    }
}