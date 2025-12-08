using Microsoft.EntityFrameworkCore;
using SimpleTableDemo.Context;
using SimpleTableDemo.Models;

namespace FileManager;

public class DatabaseService : ContextService
{
    public IQueryable<Car> Cars => Context.Cars.Include(x => x.CarMaker);
    

    public static void SeedDatabase()
    {
        using var context = new CarsContext();

        // Create database and tables if they don't exist
        var created = context.Database.EnsureCreated();

        // If there are already cars, don't seed again
        if (context.Cars.Any())
            return;

        Console.WriteLine("Seeding database...");
        var carMakers = new Dictionary<int, CarMaker>
        {
            { 1, new CarMaker { CarMakerId = 1, Brand = "Toyota", HomeCountry = "Japan" } },
            { 2, new CarMaker { CarMakerId = 2, Brand = "Honda", HomeCountry = "Japan" } },
            { 3, new CarMaker { CarMakerId = 3, Brand = "Nissan", HomeCountry = "Japan" } },
            { 4, new CarMaker { CarMakerId = 4, Brand = "Mazda", HomeCountry = "Japan" } },
            { 5, new CarMaker { CarMakerId = 5, Brand = "Subaru", HomeCountry = "Japan" } },
            { 6, new CarMaker { CarMakerId = 6, Brand = "Ford", HomeCountry = "USA" } },
            { 7, new CarMaker { CarMakerId = 7, Brand = "Chevrolet", HomeCountry = "USA" } },
            { 8, new CarMaker { CarMakerId = 8, Brand = "Tesla", HomeCountry = "USA" } },
            { 9, new CarMaker { CarMakerId = 9, Brand = "Dodge", HomeCountry = "USA" } },
            { 10, new CarMaker { CarMakerId = 10, Brand = "BMW", HomeCountry = "Germany" } },
            { 11, new CarMaker { CarMakerId = 11, Brand = "Audi", HomeCountry = "Germany" } },
            { 12, new CarMaker { CarMakerId = 12, Brand = "Mercedes-Benz", HomeCountry = "Germany" } },
            { 13, new CarMaker { CarMakerId = 13, Brand = "Volkswagen", HomeCountry = "Germany" } },
            { 14, new CarMaker { CarMakerId = 14, Brand = "Hyundai", HomeCountry = "South Korea" } },
            { 15, new CarMaker { CarMakerId = 15, Brand = "Kia", HomeCountry = "South Korea" } }
        };


        var cars = new[]
        {
            // --- Toyota ---
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "Camry", Year = 2020, Color = "Blue"},
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "Corolla", Year = 2021, Color = "White" },
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "RAV4", Year = 2019, Color = "Gray" },
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "Highlander", Year = 2022, Color = "Black" },
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "Prius", Year = 2018, Color = "Green" },
            new Car { CarMakerId = 1, CarMaker = carMakers[1], Model = "Yaris", Year = 2017, Color = "Silver" },

            // --- Honda ---
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "Civic", Year = 2020, Color = "Red" },
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "Accord", Year = 2019, Color = "Gray" },
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "CR-V", Year = 2021, Color = "Blue" },
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "Pilot", Year = 2022, Color = "Black" },
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "Fit", Year = 2018, Color = "White" },
            new Car { CarMakerId = 2, CarMaker = carMakers[2], Model = "HR-V", Year = 2020, Color = "Silver" },

            // --- Nissan ---
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Altima", Year = 2020, Color = "Gray" },
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Maxima", Year = 2018, Color = "Black" },
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Rogue", Year = 2021, Color = "White" },
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Sentra", Year = 2019, Color = "Red" },
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Frontier", Year = 2022, Color = "Blue" },
            new Car { CarMakerId = 3, CarMaker = carMakers[3], Model = "Leaf", Year = 2020, Color = "Green" },

            // --- Mazda ---
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "Mazda3", Year = 2020, Color = "White" },
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "Mazda6", Year = 2018, Color = "Black" },
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "CX-5", Year = 2021, Color = "Blue" },
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "CX-30", Year = 2022, Color = "Gray" },
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "MX-5", Year = 2019, Color = "Red" },
            new Car { CarMakerId = 4, CarMaker = carMakers[4], Model = "CX-9", Year = 2020, Color = "Silver" },

            // --- Subaru ---
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "Impreza", Year = 2019, Color = "Blue" },
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "Outback", Year = 2021, Color = "Green" },
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "Forester", Year = 2020, Color = "Gray" },
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "BRZ", Year = 2022, Color = "Red" },
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "Crosstrek", Year = 2021, Color = "Black" },
            new Car { CarMakerId = 5, CarMaker = carMakers[5], Model = "Legacy", Year = 2018, Color = "White" },

            // --- Ford ---
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "Mustang", Year = 2021, Color = "Yellow" },
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "F-150", Year = 2022, Color = "Silver" },
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "Explorer", Year = 2020, Color = "Black" },
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "Edge", Year = 2019, Color = "Blue" },
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "Escape", Year = 2020, Color = "White" },
            new Car { CarMakerId = 6, CarMaker = carMakers[6], Model = "Fusion", Year = 2018, Color = "Gray" },

            // --- Chevrolet ---
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Malibu", Year = 2019, Color = "White" },
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Impala", Year = 2018, Color = "Black" },
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Silverado", Year = 2021, Color = "Red" },
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Tahoe", Year = 2022, Color = "Blue" },
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Camaro", Year = 2020, Color = "Yellow" },
            new Car { CarMakerId = 7, CarMaker = carMakers[7], Model = "Blazer", Year = 2019, Color = "Gray" },

            // --- Tesla ---
            new Car { CarMakerId = 8, CarMaker = carMakers[8], Model = "Model 3", Year = 2021, Color = "White" },
            new Car { CarMakerId = 8, CarMaker = carMakers[8], Model = "Model S", Year = 2020, Color = "Black" },
            new Car { CarMakerId = 8, CarMaker = carMakers[8], Model = "Model X", Year = 2022, Color = "Blue" },
            new Car { CarMakerId = 8, CarMaker = carMakers[8], Model = "Model Y", Year = 2021, Color = "Gray" },

            // --- Dodge ---
            new Car { CarMakerId = 9, CarMaker = carMakers[9], Model = "Charger", Year = 2020, Color = "Black" },
            new Car { CarMakerId = 9, CarMaker = carMakers[9], Model = "Challenger", Year = 2021, Color = "Red" },
            new Car { CarMakerId = 9, CarMaker = carMakers[9], Model = "Durango", Year = 2019, Color = "White" },
            new Car { CarMakerId = 9, CarMaker = carMakers[9], Model = "Journey", Year = 2018, Color = "Silver" },

            // --- BMW ---
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "3 Series", Year = 2021, Color = "Black" },
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "5 Series", Year = 2020, Color = "White" },
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "X3", Year = 2019, Color = "Blue" },
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "X5", Year = 2021, Color = "Gray" },
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "M3", Year = 2022, Color = "Red" },
            new Car { CarMakerId = 10, CarMaker = carMakers[10], Model = "M4", Year = 2022, Color = "Yellow" },

            // --- Audi ---
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "A4", Year = 2020, Color = "White" },
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "A6", Year = 2019, Color = "Black" },
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "Q5", Year = 2021, Color = "Gray" },
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "Q7", Year = 2022, Color = "Blue" },
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "RS5", Year = 2020, Color = "Red" },
            new Car { CarMakerId = 11, CarMaker = carMakers[11], Model = "S4", Year = 2021, Color = "Silver" },

            // --- Mercedes-Benz ---
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "C-Class", Year = 2020, Color = "Black" },
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "E-Class", Year = 2021, Color = "White" },
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "S-Class", Year = 2022, Color = "Silver" },
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "GLA", Year = 2020, Color = "Red" },
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "GLC", Year = 2021, Color = "Blue" },
            new Car { CarMakerId = 12, CarMaker = carMakers[12], Model = "AMG GT", Year = 2022, Color = "Yellow" },

            // --- Volkswagen ---
            new Car { CarMakerId = 13, CarMaker = carMakers[13], Model = "Golf", Year = 2019, Color = "Blue" },
            new Car { CarMakerId = 13, CarMaker = carMakers[13], Model = "Passat", Year = 2018, Color = "White" },
            new Car { CarMakerId = 13, CarMaker = carMakers[13], Model = "Tiguan", Year = 2021, Color = "Gray" },
            new Car { CarMakerId = 13, CarMaker = carMakers[13], Model = "Jetta", Year = 2020, Color = "Silver" },
            new Car { CarMakerId = 13, CarMaker = carMakers[13], Model = "ID.4", Year = 2022, Color = "Black" },

            // --- Hyundai ---
            new Car { CarMakerId = 14, CarMaker = carMakers[14], Model = "Sonata", Year = 2021, Color = "Red" },
            new Car { CarMakerId = 14, CarMaker = carMakers[14], Model = "Elantra", Year = 2020, Color = "White" },
            new Car { CarMakerId = 14, CarMaker = carMakers[14], Model = "Kona", Year = 2022, Color = "Green" },
            new Car { CarMakerId = 14, CarMaker = carMakers[14], Model = "Tucson", Year = 2021, Color = "Gray" },
            new Car { CarMakerId = 14, CarMaker = carMakers[14], Model = "Santa Fe", Year = 2019, Color = "Blue" },

            // --- Kia ---
            new Car { CarMakerId = 15, CarMaker = carMakers[15], Model = "Optima", Year = 2018, Color = "White" },
            new Car { CarMakerId = 15, CarMaker = carMakers[15], Model = "Sorento", Year = 2021, Color = "Black" },
            new Car { CarMakerId = 15, CarMaker = carMakers[15], Model = "Sportage", Year = 2020, Color = "Blue" },
            new Car { CarMakerId = 15, CarMaker = carMakers[15], Model = "Soul", Year = 2019, Color = "Yellow" },
            new Car { CarMakerId = 15, CarMaker = carMakers[15], Model = "Stinger", Year = 2022, Color = "Red" }
        };
        context.CarMakers.AddRange(carMakers.Select(x => x.Value).ToList());
        context.SaveChanges();
        context.Cars.AddRange(cars);
        // Now add random features to the cars
        var featureList = new List<string>
        {
            "Sunroof", "Leather Seats", "Bluetooth", "Backup Camera", "Navigation System",
            "Heated Seats", "Alloy Wheels", "Remote Start", "Blind Spot Monitor", "Apple CarPlay",
            "Android Auto", "Keyless Entry", "Fog Lights", "Roof Rack", "Third Row Seating",
            "Premium Sound System", "Adaptive Cruise Control", "Lane Departure Warning", "Parking Sensors", "Turbocharged Engine",
            "Electric Windows", "Automatic Climate Control", "Power Seats", "LED Headlights", "Towing Package",
            "Wireless Charging", "Heads-Up Display", "Rain-Sensing Wipers", "Voice Recognition", "Collision Mitigation System"
        };
        context.SaveChanges();
        var newFeatures = new List<Feature>();
        foreach (Car car in context.Cars)
        {
            // Select 1-5 random features
            var random = new Random();
            int featureCount = random.Next(2, 8);
            var selectedFeatures = featureList.OrderBy(x => random.Next()).Take(featureCount).ToList();
            foreach (var featureName in selectedFeatures)
            {
                newFeatures.Add(new Feature()
                {
                    CarId = car.CarId,
                    Name = featureName,
                });
            }
        }
        context.AddRange(newFeatures);
        context.SaveChanges();
    }
}