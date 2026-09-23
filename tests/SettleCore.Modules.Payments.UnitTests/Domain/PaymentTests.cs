using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.UnitTests.Domain;

public sealed class PaymentTests
{
    [Fact]
    public void CreateWithPositiveAmountCreatesPendingPayment()
    {
        var payment = Payment.Create(100.00m, "SGD");

        Assert.Equal(100.00m, payment.Amount);
        Assert.Equal("SGD", payment.Currency);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }
}