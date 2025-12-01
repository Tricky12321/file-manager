using SimpleTable;

namespace SimpleTableDemo.Models;

public class Car
{
    [NoSearch]
    public int CarId { get; set; }
    public string Model { get; set; }
    [NoSearch]
    public int CarMakerId { get; set; }
    public CarMaker CarMaker { get; set; }
    public int Year { get; set; }
    public string Color { get; set; }
}

public class CarMaker
{
    [NoSearch]
    public int CarMakerId { get; set; }
    public string Brand { get; set; }
    public string HomeCountry { get; set; }
}
