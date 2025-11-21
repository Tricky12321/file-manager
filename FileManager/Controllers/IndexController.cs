using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{

    public IndexController()
    {

    }
    
    [HttpGet()]
    public async Task<IActionResult> Index()
    {
        return Ok(new
        {
            Text = "Hello world from backend",
        });
    }
}