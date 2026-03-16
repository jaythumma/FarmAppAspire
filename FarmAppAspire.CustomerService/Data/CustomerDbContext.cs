using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<InsulatedBoxConfig> InsulatedBoxConfigs => Set<InsulatedBoxConfig>();
    public DbSet<FedExTierConfig> FedExTierConfigs => Set<FedExTierConfig>();
    public DbSet<CustomerPricing> CustomerPricings => Set<CustomerPricing>();
    public DbSet<StandingOrder> StandingOrders => Set<StandingOrder>();
    public DbSet<StandingOrderLine> StandingOrderLines => Set<StandingOrderLine>();
    public DbSet<StandingOrderSkip> StandingOrderSkips => Set<StandingOrderSkip>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Type).HasConversion<string>();
            e.Property(c => c.ChannelType).HasConversion<string>();
            e.Property(c => c.PaymentTerms).HasConversion<string>();
            e.HasMany(c => c.Contacts)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Addresses)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerContact>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Role).HasConversion<string>();
        });

        modelBuilder.Entity<CustomerAddress>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Type).HasConversion<string>();
        });

        // ── Product catalog (static seed) ────────────────────────────────────

        modelBuilder.Entity<InsulatedBoxConfig>(e =>
        {
            e.HasKey(x => x.Size);
            e.Property(x => x.Size).HasConversion<string>();
            e.HasData(
                new InsulatedBoxConfig { Size = InsulatedBoxSize.FiveLb,   WeightLbs = 5m,  BasePricePerLb = 13m, IsDefault = false },
                new InsulatedBoxConfig { Size = InsulatedBoxSize.TenLb,   WeightLbs = 10m, BasePricePerLb = 13m, IsDefault = false },
                new InsulatedBoxConfig { Size = InsulatedBoxSize.TwelveLb, WeightLbs = 12m, BasePricePerLb = 13m, IsDefault = true  }
            );
        });

        modelBuilder.Entity<FedExTierConfig>(e =>
        {
            e.HasKey(x => x.TierSize);
            e.Property(x => x.TierSize).HasConversion<string>();
            e.HasData(
                new FedExTierConfig { TierSize = FedExTierSize.OneOz,   WeightOz = 1m,  FixedPrice = 5.99m  },
                new FedExTierConfig { TierSize = FedExTierSize.TwoOz,   WeightOz = 2m,  FixedPrice = 8.99m  },
                new FedExTierConfig { TierSize = FedExTierSize.FourOz,  WeightOz = 4m,  FixedPrice = 12.99m },
                new FedExTierConfig { TierSize = FedExTierSize.EightOz, WeightOz = 8m,  FixedPrice = 19.99m },
                new FedExTierConfig { TierSize = FedExTierSize.OneLb,   WeightOz = 16m, FixedPrice = 27.99m },
                new FedExTierConfig { TierSize = FedExTierSize.TwoLb,   WeightOz = 32m, FixedPrice = 42.99m },
                new FedExTierConfig { TierSize = FedExTierSize.ThreeLb, WeightOz = 48m, FixedPrice = 68.99m },
                new FedExTierConfig { TierSize = FedExTierSize.FiveLb,  WeightOz = 80m, FixedPrice = 99.99m }
            );
        });

        // ── Customer pricing ──────────────────────────────────────────────────

        modelBuilder.Entity<CustomerPricing>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BoxSize).HasConversion<string>();
            e.HasIndex(x => new { x.CustomerId, x.BoxSize }).IsUnique();
        });

        // ── Standing orders ───────────────────────────────────────────────────

        modelBuilder.Entity<StandingOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Frequency).HasConversion<string>();
            e.Property(x => x.MonthlyWeek).HasConversion<string>();
            e.HasMany(x => x.Lines).WithOne(l => l.StandingOrder).HasForeignKey(l => l.StandingOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Skips).WithOne(s => s.StandingOrder).HasForeignKey(s => s.StandingOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Orders).WithOne(i => i.StandingOrder).HasForeignKey(i => i.StandingOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CustomerId, x.Status });
            e.HasIndex(x => new { x.CustomerId, x.Channel }).IsUnique();
        });

        modelBuilder.Entity<StandingOrderLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BoxSize).HasConversion<string>();
        });

        modelBuilder.Entity<StandingOrderSkip>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StandingOrderId, x.WeekOf }).IsUnique();
        });

        // ── Order instances ───────────────────────────────────────────────────

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Invoice).WithOne(i => i.Order).HasForeignKey<Invoice>(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CustomerId, x.WeekOf, x.Status });
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BoxSize).HasConversion<string>();
            e.Property(x => x.FedExTierSize).HasConversion<string>();
            e.Property(x => x.PackagingType).HasConversion<string>();
        });

        // ── Invoices ──────────────────────────────────────────────────────────

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>();
            e.HasIndex(x => new { x.CustomerId, x.SeasonYear, x.Channel, x.SeekNum }).IsUnique();
            e.HasIndex(x => x.Label).IsUnique();
        });
    }
}

