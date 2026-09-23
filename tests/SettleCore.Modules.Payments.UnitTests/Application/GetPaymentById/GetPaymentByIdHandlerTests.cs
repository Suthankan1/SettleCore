using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Application.GetPaymentById;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.UnitTests.Application.GetPaymentById;

public sealed class GetPaymentByIdHandlerTests
{
    [Fact]
    public async Task HandleReturnsPaymentWhenItExists()
    {
        var payment = Payment.Create(99.90m, "sgd");
        var repository = new StubPaymentRepository(payment);
        var handler = new GetPaymentByIdHandler(repository);

        var result = await handler.HandleAsync(
            new GetPaymentByIdQuery(payment.Id.Value));

        var foundPayment = Assert.IsType<GetPaymentByIdResult>(result);

        Assert.Equal(payment.Id.Value, foundPayment.PaymentId);
        Assert.Equal(99.90m, foundPayment.Amount);
        Assert.Equal("SGD", foundPayment.Currency);
        Assert.Equal("Pending", foundPayment.Status);
    }

    [Fact]
    public async Task HandleReturnsNullWhenPaymentDoesNotExist()
    {
        var repository = new StubPaymentRepository(null);
        var handler = new GetPaymentByIdHandler(repository);

        var result = await handler.HandleAsync(
            new GetPaymentByIdQuery(Guid.NewGuid()));

        Assert.Null(result);
    }

    private sealed class StubPaymentRepository(Payment? payment)
        : IPaymentRepository
    {
        public Task AddAsync(
            Payment payment,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Payment?> GetByIdAsync(
            PaymentId id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(payment);
        }
    }
}