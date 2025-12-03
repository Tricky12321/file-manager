using Microsoft.EntityFrameworkCore;
using SimpleTableDemo.Models;

namespace SimpleTableDemo.Context;
public class CarsContext : DbContext
{
    public DbSet<Car> Cars { get; set; }
    public DbSet<CarMaker> CarMakers { get; set; }
    public DbSet<Feature> Features { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=database.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- CarMaker ---
        modelBuilder.Entity<CarMaker>(entity =>
        {
            entity.HasKey(m => m.CarMakerId);

            entity.Property(m => m.Brand)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(m => m.HomeCountry)
                .IsRequired()
                .HasMaxLength(100);
        });

        // --- Car ---
        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(c => c.CarId);

            entity.Property(c => c.Model)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(c => c.Year)
                .IsRequired();

            // Car → CarMaker (many-to-one)
            entity.HasOne(c => c.CarMaker)
                .WithMany()
                .HasForeignKey(c => c.CarMakerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(x => x.FeatureId);
            entity.Property(x => x.FeatureId).IsRequired();
            entity.Property(x => x.CarId).IsRequired();
            entity.Property(x => x.Name).IsRequired();
            entity.HasOne<Car>()
                .WithMany(x => x.Features)
                .HasForeignKey(c => c.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }

}