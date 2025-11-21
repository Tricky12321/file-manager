using System.Threading.Tasks;
using FileManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QBitController(QBittorrentService qBittorrentService) : ControllerBase
{
    [HttpGet]
    [HttpGet("{clearCache}")]
    public async Task<IActionResult> GetTorrents(bool clearCache = false)
    {
        return Ok(await qBittorrentService.GetTorrentList(clearCache));
    }
    
    [HttpGet("torrentfiles")]
    [HttpGet("torrentfiles/{clearCache}")]
    public async Task<IActionResult> GetTorrentsFiles(bool clearCache = false)
    {
        return Ok(await qBittorrentService.GetTorrentFilesList(clearCache));
    }
}