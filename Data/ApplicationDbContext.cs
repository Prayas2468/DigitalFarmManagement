using DigitalFarmManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Livestock> Livestock => Set<Livestock>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineInventory> MedicineInventories => Set<MedicineInventory>();
    public DbSet<AntimicrobialTreatment> Treatments => Set<AntimicrobialTreatment>();
    public DbSet<MRLStandard> MrlStandards => Set<MRLStandard>();
    public DbSet<MRLTest> MrlTests => Set<MRLTest>();
    public DbSet<WithdrawalPeriod> WithdrawalPeriods => Set<WithdrawalPeriod>();
    public DbSet<ProductClearance> ProductClearances => Set<ProductClearance>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public override int SaveChanges()
    {
        Stamp();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Stamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void Stamp()
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Farm>(entity =>
        {
            entity.Property(f => f.FarmName).HasMaxLength(120).IsRequired();
            entity.Property(f => f.FarmerName).HasMaxLength(120).IsRequired();
            entity.Property(f => f.Location).HasMaxLength(120).IsRequired();
            entity.Property(f => f.ContactNumber).HasMaxLength(20).IsRequired();
            entity.Property(f => f.Email).HasMaxLength(120).IsRequired();
            entity.Property(f => f.FarmType).HasMaxLength(40).IsRequired();
            entity.HasOne(f => f.Owner)
                .WithMany(u => u.Farms)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Livestock>(entity =>
        {
            entity.Property(l => l.AnimalCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(l => l.AnimalCode).IsUnique();
            entity.Property(l => l.Weight).HasPrecision(10, 2);
            entity.HasOne(l => l.Farm)
                .WithMany(f => f.Livestock)
                .HasForeignKey(l => l.FarmId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Medicine>(entity =>
        {
            entity.Property(m => m.MedicineName).HasMaxLength(120).IsRequired();
            entity.Property(m => m.ActiveIngredient).HasMaxLength(120).IsRequired();
        });

        builder.Entity<MedicineInventory>(entity =>
        {
            entity.Property(i => i.AvailableQuantity).HasPrecision(12, 3);
            entity.Property(i => i.LowStockThreshold).HasPrecision(12, 3);
            entity.HasIndex(i => new { i.MedicineId, i.BatchNumber }).IsUnique();
            entity.HasOne(i => i.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(i => i.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AntimicrobialTreatment>(entity =>
        {
            entity.Property(t => t.Dosage).HasPrecision(12, 3);
            entity.HasOne(t => t.Livestock)
                .WithMany(l => l.Treatments)
                .HasForeignKey(t => t.LivestockId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Medicine)
                .WithMany(m => m.Treatments)
                .HasForeignKey(t => t.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.MedicineInventory)
                .WithMany(i => i.Treatments)
                .HasForeignKey(t => t.MedicineInventoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WithdrawalPeriod>(entity =>
        {
            entity.HasOne(w => w.Treatment)
                .WithOne(t => t.Withdrawal)
                .HasForeignKey<WithdrawalPeriod>(w => w.TreatmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Livestock)
                .WithMany(l => l.Withdrawals)
                .HasForeignKey(w => w.LivestockId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MRLStandard>(entity =>
        {
            entity.Property(s => s.LimitValue).HasPrecision(14, 4);
            entity.Property(s => s.Species).HasMaxLength(30).IsRequired();
            entity.HasIndex(s => new { s.Country, s.ProductType, s.ActiveIngredient, s.Species }).IsUnique();
        });

        builder.Entity<MRLTest>(entity =>
        {
            entity.Property(t => t.MeasuredResidueLevel).HasPrecision(14, 4);
            entity.Property(t => t.ApplicableMrlLimit).HasPrecision(14, 4);
            entity.HasOne(t => t.Livestock)
                .WithMany(l => l.MrlTests)
                .HasForeignKey(t => t.LivestockId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Standard)
                .WithMany(s => s.Tests)
                .HasForeignKey(t => t.MRLStandardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductClearance>(entity =>
        {
            entity.HasOne(c => c.Livestock)
                .WithMany(l => l.Clearances)
                .HasForeignKey(c => c.LivestockId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.SourceKey)
                .IsUnique()
                .HasFilter("[SourceKey] IS NOT NULL");
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
