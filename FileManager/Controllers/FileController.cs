using FileManager.Models;
using FileManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly FileSystemService _fileSystemService;

    public FileController(FileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }
    
    [HttpGet("getFiles")]
    public IActionResult GetFiles([FromQuery] string path, [FromQuery] bool? hardlink = null, [FromQuery] bool? inQbit= null, [FromQuery] bool? folderInQbit= null, [FromQuery] bool? clearCache= null)
    {
        return Ok(_fileSystemService.GetFilesInDirectory(path, hardlink, inQbit, folderInQbit, clearCache == true));
    }
    
    [HttpPost("delete")]
    public IActionResult DeleteFile([FromBody] DeleteFileDto deleteFileDto)
    {
        _fileSystemService.DeleteFile(deleteFileDto.Path);
        return Ok();
    }
}