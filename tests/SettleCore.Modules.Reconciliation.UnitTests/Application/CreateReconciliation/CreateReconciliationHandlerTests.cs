using SettleCore.Modules.Reconciliation.Application;
using SettleCore.Modules.Reconciliation.Application.CreateReconciliation;
using SettleCore.Modules.Reconciliation.Domain;

namespace SettleCore.Modules.Reconciliation.UnitTests.Application.CreateReconciliation;

public sealed class CreateReconciliationHandlerTests
{
    [Fact]
    public async Task HandlePersistsReconciliationResult()
    {
        var repository = new RecordingReconciliationRepository();
        var handler = new CreateReconciliationHandler(repository);

        var command = new CreateReconciliationCommand(
            ExpectedAmount: 100.00m,
            ExpectedCurrency: "SGD",
            ActualAmount: 95.00m,
            ActualCurrency: "SGD");

        var result = await handler.HandleAsync(command);

        Assert.Equal(
            ReconciliationStatus.AmountMismatch,
            result.Status);

        Assert.NotNull(repository.AddedRecord);

        Assert.Equal(
            ReconciliationStatus.AmountMismatch,
            repository.AddedRecord.Status);

        Assert.Equal(100.00m, repository.AddedRecord.ExpectedAmount);
        Assert.Equal(95.00m, repository.AddedRecord.ActualAmount);
        Assert.Equal("SGD", repository.AddedRecord.ExpectedCurrency);
        Assert.Equal("SGD", repository.AddedRecord.ActualCurrency);
    }

    private sealed class RecordingReconciliationRepository
        : IReconciliationRepository
    {
        public ReconciliationRecord? AddedRecord { get; private set; }

        public Task AddAsync(
            ReconciliationRecord record,
            CancellationToken cancellationToken = default)
        {
            AddedRecord = record;

            return Task.CompletedTask;
        }
    }
}