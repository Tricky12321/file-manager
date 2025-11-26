using SimpleTableDemo.Context;
using SimpleTableDemo.Models;

namespace FileManager;

public class DatabaseService : ContextService
{
    public IQueryable<Car> Cars => Context.Cars;

    public static void SeedDatabase()
    {
        using var context = new CarsContext();

        // Create database and tables if they don't exist
        var created = context.Database.EnsureCreated();

        // If there are already cars, don't seed again
        if (context.Cars.Any())
            return;
        
        Console.WriteLine("Seeding database...");

        var cars = new[]
        {
            new Car { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue" },
            new Car { Make = "Honda", Model = "Civic", Year = 2019, Color = "Red" },
            new Car { Make = "Ford", Model = "Mustang", Year = 2021, Color = "Black" },
            new Car { Make = "Chevrolet", Model = "Malibu", Year = 2018, Color = "White" },
            new Car { Make = "Nissan", Model = "Altima", Year = 2020, Color = "Gray" },
            new Car { Make = "BMW", Model = "3 Series", Year = 2021, Color = "Silver" },
            new Car { Make = "Audi", Model = "A4", Year = 2019, Color = "Blue" },
            new Car { Make = "Mercedes-Benz", Model = "C-Class", Year = 2020, Color = "Black" },
            new Car { Make = "Volkswagen", Model = "Passat", Year = 2018, Color = "Red" },
            new Car { Make = "Hyundai", Model = "Sonata", Year = 2021, Color = "White" },
            new Car { Make = "Toyota", Model = "Corolla", Year = 2017, Color = "Black" },
            new Car { Make = "Honda", Model = "Accord", Year = 2020, Color = "White" },
            new Car { Make = "Ford", Model = "Fusion", Year = 2019, Color = "Gray" },
            new Car { Make = "Chevrolet", Model = "Impala", Year = 2021, Color = "Blue" },
            new Car { Make = "Nissan", Model = "Sentra", Year = 2018, Color = "Silver" },
            new Car { Make = "BMW", Model = "5 Series", Year = 2020, Color = "Black" },
            new Car { Make = "Audi", Model = "A6", Year = 2021, Color = "White" },
            new Car { Make = "Mercedes-Benz", Model = "E-Class", Year = 2019, Color = "Gray" },
            new Car { Make = "Volkswagen", Model = "Jetta", Year = 2020, Color = "Blue" },
            new Car { Make = "Hyundai", Model = "Elantra", Year = 2018, Color = "Red" },
            new Car { Make = "Toyota", Model = "RAV4", Year = 2021, Color = "Gray" },
            new Car { Make = "Honda", Model = "CR-V", Year = 2019, Color = "Blue" },
            new Car { Make = "Ford", Model = "Explorer", Year = 2020, Color = "Black" },
            new Car { Make = "Chevrolet", Model = "Traverse", Year = 2021, Color = "White" },
            new Car { Make = "Nissan", Model = "Rogue", Year = 2017, Color = "Red" },
            new Car { Make = "BMW", Model = "X3", Year = 2019, Color = "Gray" },
            new Car { Make = "Audi", Model = "Q5", Year = 2020, Color = "Black" },
            new Car { Make = "Mercedes-Benz", Model = "GLC", Year = 2021, Color = "Silver" },
            new Car { Make = "Volkswagen", Model = "Tiguan", Year = 2019, Color = "White" },
            new Car { Make = "Hyundai", Model = "Tucson", Year = 2020, Color = "Blue" },
            new Car { Make = "Toyota", Model = "Highlander", Year = 2018, Color = "Red" },
            new Car { Make = "Honda", Model = "Pilot", Year = 2021, Color = "Black" },
            new Car { Make = "Ford", Model = "Escape", Year = 2017, Color = "Gray" },
            new Car { Make = "Chevrolet", Model = "Equinox", Year = 2019, Color = "Silver" },
            new Car { Make = "Nissan", Model = "Murano", Year = 2020, Color = "White" },
            new Car { Make = "BMW", Model = "X5", Year = 2021, Color = "Blue" },
            new Car { Make = "Audi", Model = "Q7", Year = 2018, Color = "Black" },
            new Car { Make = "Mercedes-Benz", Model = "GLE", Year = 2020, Color = "Gray" },
            new Car { Make = "Volkswagen", Model = "Atlas", Year = 2021, Color = "Red" },
            new Car { Make = "Hyundai", Model = "Santa Fe", Year = 2019, Color = "White" },
            new Car { Make = "Toyota", Model = "Prius", Year = 2020, Color = "Green" },
            new Car { Make = "Honda", Model = "Insight", Year = 2021, Color = "Silver" },
            new Car { Make = "Ford", Model = "Focus", Year = 2018, Color = "Blue" },
            new Car { Make = "Chevrolet", Model = "Spark", Year = 2020, Color = "Yellow" },
            new Car { Make = "Nissan", Model = "Leaf", Year = 2019, Color = "White" },
            new Car { Make = "BMW", Model = "i3", Year = 2020, Color = "Black" },
            new Car { Make = "Audi", Model = "A3", Year = 2021, Color = "Red" },
            new Car { Make = "Mercedes-Benz", Model = "A-Class", Year = 2018, Color = "White" },
            new Car { Make = "Volkswagen", Model = "Golf", Year = 2019, Color = "Blue" },
            new Car { Make = "Hyundai", Model = "Ioniq", Year = 2021, Color = "Silver" },
            new Car { Make = "Toyota", Model = "Tacoma", Year = 2019, Color = "Gray" },
            new Car { Make = "Honda", Model = "Ridgeline", Year = 2020, Color = "Black" },
            new Car { Make = "Ford", Model = "F-150", Year = 2021, Color = "Blue" },
            new Car { Make = "Chevrolet", Model = "Silverado", Year = 2019, Color = "Red" },
            new Car { Make = "Nissan", Model = "Frontier", Year = 2021, Color = "White" },
            new Car { Make = "BMW", Model = "X6", Year = 2018, Color = "Black" },
            new Car { Make = "Audi", Model = "A5", Year = 2020, Color = "Silver" },
            new Car { Make = "Mercedes-Benz", Model = "CLA", Year = 2019, Color = "Blue" },
            new Car { Make = "Volkswagen", Model = "Arteon", Year = 2021, Color = "Gray" },
            new Car { Make = "Hyundai", Model = "Veloster", Year = 2020, Color = "Red" },
            new Car { Make = "Toyota", Model = "4Runner", Year = 2021, Color = "Black" },
            new Car { Make = "Honda", Model = "HR-V", Year = 2018, Color = "White" },
            new Car { Make = "Ford", Model = "Bronco", Year = 2021, Color = "Green" },
            new Car { Make = "Chevrolet", Model = "Blazer", Year = 2020, Color = "Black" },
            new Car { Make = "Nissan", Model = "Kicks", Year = 2019, Color = "Orange" },
            new Car { Make = "BMW", Model = "2 Series", Year = 2021, Color = "Blue" },
            new Car { Make = "Audi", Model = "Q3", Year = 2019, Color = "White" },
            new Car { Make = "Mercedes-Benz", Model = "GLA", Year = 2021, Color = "Gray" },
            new Car { Make = "Volkswagen", Model = "ID.4", Year = 2021, Color = "Silver" },
            new Car { Make = "Hyundai", Model = "Kona", Year = 2019, Color = "Blue" },
            new Car { Make = "Toyota", Model = "Supra", Year = 2020, Color = "Red" },
            new Car { Make = "Honda", Model = "S2000", Year = 2009, Color = "Yellow" },
            new Car { Make = "Ford", Model = "GT", Year = 2006, Color = "Blue" },
            new Car { Make = "Chevrolet", Model = "Corvette", Year = 2021, Color = "Red" },
            new Car { Make = "Nissan", Model = "370Z", Year = 2020, Color = "Black" },
            new Car { Make = "BMW", Model = "M4", Year = 2021, Color = "Yellow" },
            new Car { Make = "Audi", Model = "RS5", Year = 2020, Color = "Green" },
            new Car { Make = "Mercedes-Benz", Model = "AMG GT", Year = 2021, Color = "Silver" },
            new Car { Make = "Volkswagen", Model = "Beetle", Year = 2018, Color = "White" },
            new Car { Make = "Hyundai", Model = "Genesis Coupe", Year = 2016, Color = "Black" }
        };

        context.Cars.AddRange(cars);
        context.SaveChanges();
    }
}