using Microsoft.EntityFrameworkCore;
using SimpleTableDemo.Models;

namespace SimpleTableDemo.Context;
public class CarsContext : DbContext
{
    public DbSet<Car> Cars { get; set; }
    public DbSet<CarMaker> CarMakers { get; set; }

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
                .WithMany()              // (optional: .WithMany(m => m.Cars))
                .HasForeignKey(c => c.CarMakerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OPTIONAL: If you want seeding here:
        // modelBuilder.Entity<CarMaker>().HasData(...);
        // modelBuilder.Entity<Car>().HasData(...);
    }
}