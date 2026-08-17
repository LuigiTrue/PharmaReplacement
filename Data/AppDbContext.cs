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
    public DbSet<ItemConsumptionAverage> ItemConsumptionAverages => Set<ItemConsumptionAverage>();
    public DbSet<ReplenishmentRule> ReplenishmentRules => Set<ReplenishmentRule>();
    public DbSet<ReportImport> ReportImports => Set<ReportImport>();
    public DbSet<ReportItem> ReportItems => Set<ReportItem>();

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
            entity.Property(item => item.ItemType)
                .HasConversion<int>();

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

            entity.HasMany(item => item.ConsumptionAverages)
                .WithOne(average => average.Item)
                .HasForeignKey(average => average.ItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(item => item.ReplenishmentRule)
                .WithOne(rule => rule.Item)
                .HasForeignKey<ReplenishmentRule>(rule => rule.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.ReportItems)
                .WithOne(reportItem => reportItem.Item)
                .HasForeignKey(reportItem => reportItem.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.HasMany(location => location.ReportImports)
                .WithOne(reportImport => reportImport.Location)
                .HasForeignKey(reportImport => reportImport.LocationId)
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

        modelBuilder.Entity<ItemConsumptionAverage>(entity =>
        {
            entity.ToTable("item_consumption_averages");
            entity.HasKey(average => average.Id);

            entity.Property(average => average.ItemCode)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(average => average.ItemName)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(average => average.AveragePeriodKind)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(average => average.SourceFileName)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(average => average.MonthlyAverageOutput)
                .HasPrecision(18, 3);
            entity.Property(average => average.WeeklyAverageOutput)
                .HasPrecision(18, 3);
            entity.Property(average => average.CurrentAverageOutput)
                .HasPrecision(18, 3);
            entity.Property(average => average.TotalOutput)
                .HasPrecision(18, 3);
            entity.Property(average => average.StockBalance)
                .HasPrecision(18, 3);
            entity.Property(average => average.ProjectedCoverageDays)
                .HasPrecision(18, 3);

            entity.HasIndex(average => new
            {
                average.ItemCode,
                average.ReportStartDate,
                average.ReportEndDate
            }).IsUnique();
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

        modelBuilder.Entity<ReportImport>(entity =>
        {
            entity.ToTable("report_imports", table =>
            {
                table.HasCheckConstraint(
                    "CK_report_imports_reference_month",
                    "\"ReferenceMonth\" BETWEEN 1 AND 12");
                table.HasCheckConstraint(
                    "CK_report_imports_reference_year",
                    "\"ReferenceYear\" >= 2000");
            });
            entity.HasKey(reportImport => reportImport.Id);

            entity.Property(reportImport => reportImport.Status)
                .HasConversion<int>();
            entity.Property(reportImport => reportImport.SourceFileName)
                .HasMaxLength(255);
            entity.Property(reportImport => reportImport.FileHash)
                .HasMaxLength(64);
            entity.Property(reportImport => reportImport.ErrorMessage)
                .HasColumnType("text");

            entity.HasIndex(reportImport => new
            {
                reportImport.LocationId,
                reportImport.ReferenceYear,
                reportImport.ReferenceMonth
            }).IsUnique();

            entity.HasMany(reportImport => reportImport.Items)
                .WithOne(reportItem => reportItem.ReportImport)
                .HasForeignKey(reportItem => reportItem.ReportImportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportItem>(entity =>
        {
            entity.ToTable("report_items", table =>
            {
                table.HasCheckConstraint(
                    "CK_report_items_total_output",
                    "\"TotalOutput\" >= 0");
                table.HasCheckConstraint(
                    "CK_report_items_movement_days",
                    "\"MovementDays\" IS NULL OR \"MovementDays\" BETWEEN 0 AND 31");
            });
            entity.HasKey(reportItem => reportItem.Id);

            entity.Property(reportItem => reportItem.TotalOutput)
                .HasPrecision(18, 3)
                .HasDefaultValue(0m);
            entity.Property(reportItem => reportItem.AverageDailyOutput)
                .HasPrecision(18, 6);
            entity.Property(reportItem => reportItem.TotalCost)
                .HasPrecision(18, 4);
            entity.Property(reportItem => reportItem.AverageUnitCost)
                .HasPrecision(18, 6);

            entity.HasIndex(reportItem => new { reportItem.ReportImportId, reportItem.ItemId })
                .IsUnique();
            entity.HasIndex(reportItem => reportItem.ItemId);
        });
    }
}
