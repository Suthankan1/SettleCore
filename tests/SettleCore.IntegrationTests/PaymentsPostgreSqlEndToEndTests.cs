using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettleCore.Modules.Payments.Application.CreatePayment;
using SettleCore.Modules.Payments.Application.GetPaymentById;
using SettleCore.Modules.Payments.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SettleCore.IntegrationTests;

public sealed class PaymentsPostgreSqlEndToEndTests
{
    [Fact]
    public async Task PostThenGetPaymentRoundTripsThroughPostgreSql()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("settlecore_test")
            .WithUsername("settlecore")
            .WithPassword("settlecore")
            .Build();

        await postgres.StartAsync();

        using var factory =
            new PaymentsApiFactory(postgres.GetConnectionString());

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var createResponse = await client.PostAsJsonAsync(
            "/payments",
            new
            {
                amount = 321.45m,
                currency = "sgd"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content.ReadFromJsonAsync<CreatePaymentResult>();

        var createdPayment =
            Assert.IsType<CreatePaymentResult>(created);

        Assert.NotEqual(
            Guid.Empty,
            createdPayment.PaymentId);

        Assert.Equal(
            321.45m,
            createdPayment.Amount);

        Assert.Equal(
            "SGD",
            createdPayment.Currency);

        Assert.Equal(
            "Pending",
            createdPayment.Status);

        var getResponse = await client.GetAsync(
            $"/payments/{createdPayment.PaymentId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var fetched =
            await getResponse.Content.ReadFromJsonAsync<GetPaymentByIdResult>();

        var fetchedPayment =
            Assert.IsType<GetPaymentByIdResult>(fetched);

        Assert.Equal(
            createdPayment.PaymentId,
            fetchedPayment.PaymentId);

        Assert.Equal(
            createdPayment.Amount,
            fetchedPayment.Amount);

        Assert.Equal(
            createdPayment.Currency,
            fetchedPayment.Currency);

        Assert.Equal(
            createdPayment.Status,
            fetchedPayment.Status);
    }

    private sealed class PaymentsApiFactory(
        string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<
                    IDbContextOptionsConfiguration<PaymentsDbContext>>();

                services.RemoveAll<
                    DbContextOptions<PaymentsDbContext>>();

                services.RemoveAll<
                    PaymentsDbContext>();

                services.AddDbContext<PaymentsDbContext>(
                    options =>
                        options.UseNpgsql(connectionString));
            });
        }
    }
}