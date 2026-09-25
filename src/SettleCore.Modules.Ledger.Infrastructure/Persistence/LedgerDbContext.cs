using Microsoft.EntityFrameworkCore;

namespace SettleCore.Modules.Ledger.Infrastructure.Persistence;

public sealed class LedgerDbContext(
    DbContextOptions<LedgerDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LedgerDbContext).Assembly);
    }
}