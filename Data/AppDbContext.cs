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
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Danfe> Danfes => Set<Danfe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(supplier => supplier.Id);
            entity.Property(supplier => supplier.Id).ValueGeneratedOnAdd();

            entity.Property(supplier => supplier.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(supplier => supplier.Cnpj)
                .HasMaxLength(14)
                .IsRequired(false);
            entity.Property(supplier => supplier.Observations)
                .HasMaxLength(255);

            entity.HasIndex(supplier => supplier.Cnpj)
                .IsUnique()
                .HasFilter("\"Cnpj\" IS NOT NULL");

            entity.HasMany(supplier => supplier.Danfes)
                .WithOne(danfe => danfe.Supplier)
                .HasForeignKey(danfe => danfe.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.ToTable("quotations");
            entity.HasKey(quotation => quotation.Id);
            entity.Property(quotation => quotation.Id).ValueGeneratedOnAdd();

            entity.Property(quotation => quotation.QuotationNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(quotation => quotation.QuotationNumber)
                .IsUnique();

            entity.HasMany(quotation => quotation.Danfes)
                .WithOne(danfe => danfe.Quotation)
                .HasForeignKey(danfe => danfe.QuotationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Danfe>(entity =>
        {
            entity.ToTable("danfes");
            entity.HasKey(danfe => danfe.Id);
            entity.Property(danfe => danfe.Id).ValueGeneratedOnAdd();

            entity.Property(danfe => danfe.DanfeNumber)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(danfe => danfe.Value)
                .HasPrecision(18, 2);
            entity.Property(danfe => danfe.UserReceiver)
                .HasMaxLength(255);
            entity.Property(danfe => danfe.ReceivedAt)
                .HasColumnType("date");
            entity.Property(danfe => danfe.PaymentDueAt)
                .HasColumnType("date");

            entity.HasIndex(danfe => danfe.SupplierId);
            entity.HasIndex(danfe => danfe.QuotationId);
        });

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
    }
}
