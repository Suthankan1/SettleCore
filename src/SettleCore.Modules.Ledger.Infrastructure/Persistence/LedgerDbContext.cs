using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Modules.Ledger.Infrastructure.Persistence;

public sealed class LedgerDbContext(
    DbContextOptions<LedgerDbContext> options)
    : DbContext(options)
{
    public DbSet<LedgerAccount> LedgerAccounts =>
        Set<LedgerAccount>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LedgerDbContext).Assembly);
    }
}
