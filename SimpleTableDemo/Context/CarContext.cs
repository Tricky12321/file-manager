using Microsoft.EntityFrameworkCore;
using SimpleTableDemo.Models;

namespace SimpleTableDemo.Context;

public class CarsContext : DbContext
{
    public DbSet<Car> Cars { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Same file as before: database.db
        optionsBuilder.UseSqlite("Data Source=database.db");
    }
}