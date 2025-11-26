namespace SimpleTableDemo.Models;

public class Car
{
    public int CarId { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    [NoSearch]
    public string Color { get; set; }
}

