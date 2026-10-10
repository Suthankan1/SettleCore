using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SettleCore.Modules.Payments.Application.Abstractions;
using SettleCore.Modules.Payments.Infrastructure.Integrations.Stripe;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Repositories;

namespace SettleCore.IntegrationTests;

public sealed class StripeConfigurationTests
{
    [Fact]
    public void ProviderIsDisabledByDefault()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.False(factory.Services.GetRequiredService<IOptions<StripePaymentOptions>>().Value.Enabled);
        Assert.Throws<InvalidOperationException>(() => factory.Services.GetRequiredService<IPaymentProvider>());
        Assert.Throws<InvalidOperationException>(() => factory.Services.GetRequiredService<IPaymentProviderWebhookDecoder>());
    }

    [Fact]
    public void ExplicitConfigurationResolvesInfrastructureAdaptersAndScopedInbox()
    {
        using var factory = new StripeFactory();
        using var client = factory.CreateClient();
        Assert.IsType<StripePaymentProvider>(factory.Services.GetRequiredService<IPaymentProvider>());
        Assert.IsType<StripePaymentWebhookDecoder>(factory.Services.GetRequiredService<IPaymentProviderWebhookDecoder>());
        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var inbox = first.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>();
        Assert.IsType<EfPaymentProviderEventInbox>(inbox);
        Assert.Same(inbox, first.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>());
        Assert.NotSame(inbox, second.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>());
    }

    [Theory]
    [InlineData("ApiKey", "")]
    [InlineData("WebhookSecret", "")]
    [InlineData("SignatureToleranceSeconds", "0")]
    [InlineData("SignatureToleranceSeconds", "-1")]
    [InlineData("IsLiveMode", null)]
    public void IncompleteEnabledConfigurationPreventsStartup(string setting, string? value)
    {
        using var factory = new StripeFactory(setting, value);
        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task EnabledProviderDisablesManualHttpCompletionBeforeLocalReferenceExists()
    {
        using var factory = new StripeFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync($"/payments/{Guid.NewGuid()}/succeed", new
        {
            transactionId = Guid.NewGuid(), ledgerId = Guid.NewGuid(), processorReceivableAccountId = Guid.NewGuid(),
            merchantPayableAccountId = Guid.NewGuid(), platformRevenueAccountId = Guid.NewGuid(), feeAmountMinorUnits = 100
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private sealed class StripeFactory(string? setting = null, string? value = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Payments:Stripe:Enabled"] = "true",
                    ["Payments:Stripe:ApiKey"] = "sk_test_placeholder",
                    ["Payments:Stripe:WebhookSecret"] = "whsec_test_placeholder",
                    ["Payments:Stripe:SignatureToleranceSeconds"] = "300",
                    ["Payments:Stripe:IsLiveMode"] = "false"
                };
                if (setting is not null) values[$"Payments:Stripe:{setting}"] = value;
                configuration.AddInMemoryCollection(values);
            });
        }
    }
}
