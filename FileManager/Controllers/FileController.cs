using System.Collections.Generic;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.Services;
using Microsoft.AspNetCore.Mvc;
using SimpleTable;
using SimpleTable.Models;

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
    public async Task<IActionResult> GetFiles([FromQuery] string path, [FromQuery] bool? hardlink = null,
        [FromQuery] bool? inQbit = null, [FromQuery] bool? folderInQbit = null, [FromQuery] bool? hashDuplicate = null,
        [FromQuery] bool? clearCache = null)
    {
        return Ok(await _fileSystemService.GetFilesInDirectory(path, hardlink, inQbit, folderInQbit, hashDuplicate, clearCache == true));
    }

    [HttpPost("getFilesPost")]
    public async Task<IActionResult> GetFilesPost([FromBody] TableRequest tableRequest, [FromQuery] string path,
        [FromQuery] bool? hardlink = null, [FromQuery] bool? inQbit = null, [FromQuery] bool? folderInQbit = null,
        [FromQuery] bool? hashDuplicate = null, [FromQuery] bool? clearCache = null)
    {
        var files = await _fileSystemService.GetFilesInDirectory(path, hardlink, inQbit, folderInQbit, hashDuplicate, clearCache == true);
        return Ok(files.ToTableResponse(tableRequest));
    }

    [HttpGet("getFolders")]
    public async Task<IActionResult> GetFolder([FromQuery] string path, [FromQuery] bool? folderInQbit = null, [FromQuery] bool? clearCache = null)
    {
        return Ok(await _fileSystemService.GetDirectoriesInDirectory(path, folderInQbit, clearCache == true));
    }

    [HttpPost("getFoldersPost")]
    public async Task<IActionResult> GetFoldersPost([FromBody] TableRequest tableRequest, [FromQuery] string path, [FromQuery] bool? folderInQbit = null, [FromQuery] bool? clearCache = null)
    {
        var folders = await _fileSystemService.GetDirectoriesInDirectory(path, folderInQbit, clearCache == true);
        return Ok(folders.ToTableResponse(tableRequest));
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteFile([FromBody] DeleteFileDto deleteFileDto, [FromQuery] string? folderPath = null)
    {
        await _fileSystemService.DeleteFile(deleteFileDto.Path, folderPath);
        return Ok();
    }

    [HttpPost("deleteMultiple")]
    public async Task<IActionResult> DeleteFile([FromBody] List<string> deleteMultiple, [FromQuery] string? folderPath = null)
    {
        await _fileSystemService.DeleteMultipleFiles(deleteMultiple, folderPath);
        return Ok();
    }

    [HttpPost("getEmptyFolders")]
    public IActionResult GetEmptyFolders([FromBody] TableRequest tableRequest, [FromQuery] string path)
    {
        return Ok(_fileSystemService.GetEmptyFolders(path).ToTableResponse(tableRequest));
    }

    [HttpPost("getSmallFolders")]
    public IActionResult GetSmallFolders([FromBody] TableRequest tableRequest, [FromQuery] string path)
    {
        return Ok(_fileSystemService.GetSmallFolders(path).ToTableResponse(tableRequest));
    }

    [HttpPost("getSampleFiles")]
    public IActionResult GetSampleFiles([FromBody] TableRequest tableRequest, [FromQuery] string path)
    {
        return Ok(_fileSystemService.GetSampleFiles(path).ToTableResponse(tableRequest));
    }

    [HttpPost("deleteFolders")]
    public async Task<IActionResult> DeleteFolders([FromBody] List<string> foldersToDelete)
    {
        foreach (var folder in foldersToDelete)
        {
            await _fileSystemService.DeleteFolder(folder);
        }

        return Ok();
    }
}