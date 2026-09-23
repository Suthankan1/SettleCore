using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Payments.Domain;
using SettleCore.Modules.Payments.Infrastructure.Persistence.Configurations;

namespace SettleCore.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(
    DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}