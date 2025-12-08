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
        return Ok(databaseService.Cars.ToList().ToTableResponse(tableRequest));

        return Ok(databaseService.Cars.ToTableResponse(databaseService.Context, tableRequest, searchOnlyEfMapped: true));
    }
    
    [HttpGet("/")]
    public IActionResult GetData()
    {
        return Ok(databaseService.Cars.ToList().ToTableResponse(Request.GetTableRequest()));
        return Ok(databaseService.Cars.ToTableResponse(databaseService.Context, Request.GetTableRequest()));
    }
}