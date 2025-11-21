using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FileManager.Services;

public class QBittorrentService
{
    public static object Lock = new object();

    public QBittorrentService(IConfiguration configuration)
    {
        Configuration = configuration;
        Client = new QbittorrentClient(configuration["qbittorrent:url"], configuration["qbittorrent:username"],
            configuration["qbittorrent:password"]);
        BasePath = configuration["qbittorrent:basePath"];
    }

    public string BasePath { get; set; }

    public QbittorrentClient Client { get; set; }
    public IConfiguration Configuration { get; set; }

    public async Task<List<TorrentInfo>> GetTorrentList(bool clearCache = false)
    {
        lock (Lock)
        {
            var cachePath = Path.Combine(BasePath, "qbittorrent_cache.json");
            if (clearCache)
            {
                Console.WriteLine("Clearing qBittorrent cache");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }

            if (File.Exists(cachePath))
            {
                Console.WriteLine("Reading qBittorrent cache from disk");
                var result =  JsonConvert.DeserializeObject<List<TorrentInfo>>(File.ReadAllText(cachePath));
                Console.WriteLine($"Torrents fetched from qBittorrent files: {result.Count}");
                return result;
            }

            var torrents = Client.GetTorrentsAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Torrents fetched from qBittorrent: {torrents.Count}");
            File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(torrents)).GetAwaiter().GetResult();
            return torrents.ToList();
        }
    }

    public async Task<List<string>> GetTorrentFiles(List<TorrentInfo> torrents, bool clearCache = false)
    {
        lock (Lock)
        {
            var cachePath = Path.Combine(BasePath, "qbittorrent_files.json");
            if (clearCache)
            {
                Console.WriteLine("Clearing qBittorrent files cache");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }

            if (File.Exists(cachePath))
            {
                Console.WriteLine("Reading qBittorrent files cache from disk");
                var result =  JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(cachePath));
                Console.WriteLine($"Torrents fetched from qBittorrent files: {result.Count}");
                return result;
            }

            var output = new List<string>();
            foreach (var torrent in torrents)
            {
                var files = Client.GetFilesAsync(torrent.Hash).GetAwaiter().GetResult();
                foreach (var file in files)
                {
                    var fullPath = Path.Combine(torrent.SavePath, file.Name);
                    output.Add(fullPath);
                }
            }
            Console.WriteLine($"Torrent files fetched from qBittorrent: {output.Count}");
            UpdateAllFilesCache(output);
            return output;
        }
    }

    public void UpdateAllFilesCache(List<string> output)
    {
        var cachePath = Path.Combine(BasePath, "qbittorrent_files.json");
        File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(output)).GetAwaiter().GetResult();
    }

    public async Task<List<string>> GetTorrentFilesList(bool clearCache)
    {
        return await GetTorrentFiles(await GetTorrentList(clearCache).ConfigureAwait(false), clearCache);
    }
}   