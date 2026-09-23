using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.UnitTests.Domain;

public sealed class PaymentTests
{
    public static TheoryData<decimal> NonPositiveAmounts =>
        new()
        {
            0m,
            -1m,
            -100m
        };

    [Fact]
    public void CreateWithPositiveAmountCreatesPendingPayment()
    {
        var payment = Payment.Create(100.00m, "SGD");

        Assert.Equal(100.00m, payment.Amount);
        Assert.Equal("SGD", payment.Currency);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Theory]
    [MemberData(nameof(NonPositiveAmounts))]
    public void CreateWithNonPositiveAmountThrows(decimal amount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Payment.Create(amount, "SGD"));

        Assert.Equal("amount", exception.ParamName);
    }
}