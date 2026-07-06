using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Domain.Entities;

namespace RepyPharma.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<DailyConsumption> DailyConsumptions => Set<DailyConsumption>();
    public DbSet<ReplenishmentRule> ReplenishmentRules => Set<ReplenishmentRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(user => user.AvatarDataUrl)
                .HasColumnType("text");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Code)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(item => item.Unit)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(item => item.Code)
                .IsUnique();

            entity.HasMany(item => item.Batches)
                .WithOne(batch => batch.Item)
                .HasForeignKey(batch => batch.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.StockBalances)
                .WithOne(balance => balance.Item)
                .HasForeignKey(balance => balance.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(item => item.DailyConsumptions)
                .WithOne(consumption => consumption.Item)
                .HasForeignKey(consumption => consumption.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.ReplenishmentRule)
                .WithOne(rule => rule.Item)
                .HasForeignKey<ReplenishmentRule>(rule => rule.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.ToTable("batches");
            entity.HasKey(batch => batch.Id);

            entity.Property(batch => batch.BatchNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(batch => new { batch.ItemId, batch.BatchNumber })
                .IsUnique();

            entity.HasMany(batch => batch.StockBalances)
                .WithOne(balance => balance.Batch)
                .HasForeignKey(balance => balance.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(location => location.Id);

            entity.Property(location => location.Code)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(location => location.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(location => location.Code)
                .IsUnique();

            entity.HasMany(location => location.StockBalances)
                .WithOne(balance => balance.Location)
                .HasForeignKey(balance => balance.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("stock_balances");
            entity.HasKey(balance => balance.Id);

            entity.Property(balance => balance.Quantity)
                .HasPrecision(18, 3);

            entity.HasIndex(balance => new { balance.ItemId, balance.BatchId, balance.LocationId })
                .IsUnique();
        });

        modelBuilder.Entity<DailyConsumption>(entity =>
        {
            entity.ToTable("daily_consumptions");
            entity.HasKey(consumption => consumption.Id);

            entity.Property(consumption => consumption.Quantity)
                .HasPrecision(18, 3);
            entity.Property(consumption => consumption.Source)
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<ReplenishmentRule>(entity =>
        {
            entity.ToTable("replenishment_rules");
            entity.HasKey(rule => rule.Id);

            entity.Property(rule => rule.MinimumStock)
                .HasPrecision(18, 3);
            entity.Property(rule => rule.SafetyStock)
                .HasPrecision(18, 3);
            entity.Property(rule => rule.CalculationMethod)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(rule => rule.ItemPriority)
                .HasConversion<int>();

            entity.HasIndex(rule => rule.ItemId)
                .IsUnique();
        });
    }
}
