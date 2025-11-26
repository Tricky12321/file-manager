using SimpleTableDemo.Context;

namespace FileManager;

public class ContextService
{
    public CarsContext Context { get; set; }
    public ContextService()
    {
        Context = new CarsContext();
    }

}