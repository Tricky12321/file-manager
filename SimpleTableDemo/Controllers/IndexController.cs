using FileManager;
using Microsoft.AspNetCore.Mvc;
using SimpleTable;
using SimpleTable.Models;

namespace SimpleTableDemo.Controllers;
[ApiController]
public class IndexController (DatabaseService databaseService) : Controller 
{
    [HttpPost("/")]
    public IActionResult GetPostData([FromBody] TableRequest tableRequest)
    {
        return Ok(databaseService.Cars.ToTableResponseDeep(tableRequest));
    }
    
    [HttpGet("/")]
    public IActionResult GetData()
    {
        return Ok(databaseService.Cars.ToTableResponseDeep(Request.GetTableRequest()));
    }
}