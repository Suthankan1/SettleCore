using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;
using SettleCore.Modules.Payments.Domain;

namespace SettleCore.IntegrationTests;

public sealed class MarkPaymentSucceededEndpointTests
{
    [Fact]
    public async Task PostSucceedMarksPaymentSucceeded()
    {
        var payment = Payment.Create(200.00m, "SGD");

        using var factory =
            new PaymentsApiFactory(payment);

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var response = await client.PostAsync(
            $"/payments/{payment.Id.Value}/succeed",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<MarkPaymentSucceededResult>();

        var succeededPayment =
            Assert.IsType<MarkPaymentSucceededResult>(result);

        Assert.Equal(
            payment.Id.Value,
            succeededPayment.PaymentId);

        Assert.Equal(
            200.00m,
            succeededPayment.Amount);

        Assert.Equal(
            "SGD",
            succeededPayment.Currency);

        Assert.Equal(
            "Succeeded",
            succeededPayment.Status);

        var updatedPayment =
            Assert.IsType<Payment>(
                factory.Repository.UpdatedPayment);

        Assert.Equal(
            PaymentStatus.Succeeded,
            updatedPayment.Status);
    }

    [Fact]
    public async Task PostSucceedWhenPaymentAlreadySucceededReturnsConflict()
    {
        var payment = Payment.Create(200.00m, "SGD");
        payment.MarkSucceeded();

        using var factory =
            new PaymentsApiFactory(payment);

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var response = await client.PostAsync(
            $"/payments/{payment.Id.Value}/succeed",
            content: null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task PostSucceedWithEmptyPaymentIdReturnsBadRequest()
    {
        using var factory =
            new PaymentsApiFactory(null);

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var response = await client.PostAsync(
            $"/payments/{Guid.Empty}/succeed",
            content: null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private sealed class PaymentsApiFactory(
        Payment? payment)
        : WebApplicationFactory<Program>
    {
        public RecordingPaymentRepository Repository { get; } =
            new(payment);

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPaymentRepository>();

                services.AddSingleton<IPaymentRepository>(
                    Repository);
            });
        }
    }

    public sealed class RecordingPaymentRepository(
        Payment? payment)
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
            return Task.FromResult(payment);
        }

        public Task UpdateAsync(
            Payment payment,
            CancellationToken cancellationToken = default)
        {
            UpdatedPayment = payment;

            return Task.CompletedTask;
        }

        public Task<Payment?> GetByProviderReferenceAsync(
            ProviderPaymentReference providerReference,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
