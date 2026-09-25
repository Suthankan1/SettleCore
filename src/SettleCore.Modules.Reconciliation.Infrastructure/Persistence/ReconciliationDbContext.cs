using Microsoft.EntityFrameworkCore;

namespace SettleCore.Modules.Reconciliation.Infrastructure.Persistence;

public sealed class ReconciliationDbContext(
    DbContextOptions<ReconciliationDbContext> options)
    : DbContext(options)
{
    public DbSet<ReconciliationRecord> ReconciliationRecords =>
        Set<ReconciliationRecord>();
}
