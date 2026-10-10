using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SettleCore.Modules.Payments.Application.Abstractions;

namespace SettleCore.Modules.Payments.Infrastructure.Dispatch;

public sealed partial class PaymentProviderEventBatchProcessor(
    IServiceScopeFactory scopeFactory, TimeProvider clock, ILogger<PaymentProviderEventBatchProcessor> logger)
{
    public async Task<int> ProcessAsync(string provider, bool expectedLiveMode, int batchSize, TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retryDelay, TimeSpan.Zero);
        ProviderPaymentSucceededEvent[] receipts;
        await using (var selection = scopeFactory.CreateAsyncScope())
        {
            var inbox = selection.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>();
            receipts = (await inbox.GetPendingAsync(provider, batchSize, cancellationToken)).ToArray();
        }
        var completed = 0;
        foreach (var receipt in receipts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPaymentProviderEventProcessor>();
            try
            {
                if (await processor.ProcessAsync(provider, receipt.EventId, expectedLiveMode, cancellationToken))
                {
                    completed++;
                    continue;
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                ProcessingFailed(logger, exception, provider, receipt.EventId);
            }
            try
            {
                var inbox = scope.ServiceProvider.GetRequiredService<IPaymentProviderEventInbox>();
                await inbox.ScheduleRetryAsync(provider, receipt.EventId, clock.GetUtcNow().Add(retryDelay), cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                RetrySchedulingFailed(logger, exception, provider, receipt.EventId);
            }
        }
        return completed;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Provider {Provider} event {EventId} processing failed; scheduling retry.")]
    private static partial void ProcessingFailed(ILogger logger, Exception exception, string provider, string eventId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Provider {Provider} event {EventId} retry scheduling failed; receipt remains pending.")]
    private static partial void RetrySchedulingFailed(ILogger logger, Exception exception, string provider, string eventId);
}
