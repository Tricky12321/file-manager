using System.Collections.Generic;
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
    public IActionResult GetFiles([FromQuery] string path, [FromQuery] bool? hardlink = null,
        [FromQuery] bool? inQbit = null, [FromQuery] bool? folderInQbit = null, [FromQuery] bool? hashDuplicate = null,
        [FromQuery] bool? clearCache = null)
    {
        return Ok(_fileSystemService.GetFilesInDirectory(path, hardlink, inQbit, folderInQbit,hashDuplicate, clearCache == true));
    }

    [HttpPost("getFilesPost")]
    public IActionResult GetFilesPost([FromBody] TableRequestDto tableRequest, [FromQuery] string path,
        [FromQuery] bool? hardlink = null, [FromQuery] bool? inQbit = null, [FromQuery] bool? folderInQbit = null,
        [FromQuery] bool? hashDuplicate = null, [FromQuery] bool? clearCache = null)
    {
        var results = _fileSystemService.GetFilesInDirectory(path, hardlink, inQbit, folderInQbit,hashDuplicate, clearCache == true)
            .ToTableResponse(tableRequest);
        return Ok(results);
    }

    [HttpPost("delete")]
    public IActionResult DeleteFile([FromBody] DeleteFileDto deleteFileDto)
    {
        _fileSystemService.DeleteFile(deleteFileDto.Path);
        return Ok();
    }

    [HttpPost("deleteMultiple")]
    public IActionResult DeleteFile([FromBody] List<string> deleteMultiple)
    {
        _fileSystemService.DeleteMultipleFiles(deleteMultiple);
        return Ok();
    }
}