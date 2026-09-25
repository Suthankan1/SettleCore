using SettleCore.Modules.Reconciliation.Application;
using SettleCore.Modules.Reconciliation.Domain;

namespace SettleCore.Modules.Reconciliation.Infrastructure.Persistence;

public sealed class EfReconciliationRepository(
    ReconciliationDbContext dbContext)
    : IReconciliationRepository
{
    public async Task AddAsync(
        ReconciliationRecord record,
        CancellationToken cancellationToken = default)
    {
        dbContext.ReconciliationRecords.Add(record);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}