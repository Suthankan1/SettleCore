using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.Modules.Payments.UnitTests.Application.MarkPaymentSucceeded;

public sealed class MarkPaymentSucceededHandlerTests
{
    [Fact]
    public async Task HandleMarksPaymentSucceededAndPersistsUpdate()
    {
        var payment = Payment.Create(150.00m, "SGD");
        var repository = new RecordingPaymentRepository(payment);
        var handler = new MarkPaymentSucceededHandler(repository);

        var result = await handler.HandleAsync(
            new MarkPaymentSucceededCommand(payment.Id.Value));

        var updatedPayment =
            Assert.IsType<Payment>(repository.UpdatedPayment);

        var succeededPayment =
            Assert.IsType<MarkPaymentSucceededResult>(result);

        Assert.Equal(
            PaymentStatus.Succeeded,
            updatedPayment.Status);

        Assert.Equal(
            payment.Id.Value,
            succeededPayment.PaymentId);

        Assert.Equal(
            "Succeeded",
            succeededPayment.Status);
    }

    private sealed class RecordingPaymentRepository(
        Payment payment)
        : IPaymentRepository
    {
        public Payment? UpdatedPayment { get; private set; }

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
            return Task.FromResult<Payment?>(payment);
        }

        public Task UpdateAsync(
            Payment payment,
            CancellationToken cancellationToken = default)
        {
            UpdatedPayment = payment;

            return Task.CompletedTask;
        }
    }
}